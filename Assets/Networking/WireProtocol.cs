using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Numerics;
using DefenseDuell.Core;

namespace DefenseDuell.Networking
{
    /// <summary>Bounded binary messages. Network identity is deliberately absent from commands.</summary>
    public static class WireProtocol
    {
        public const ushort Version = 10;
        // Technical byte-array address space, not a gameplay/entity cap. Pages allocate only on receipt.
        public const int MaxSnapshotBytes = int.MaxValue - 1024;
        public const int ChunkPayloadBytes = 880;
        public const int MaxChunkCount = (int)(((long)MaxSnapshotBytes + ChunkPayloadBytes - 1) / ChunkPayloadBytes);
        public const int ChunkHeaderBytes = 24;
        public const int CommandBytes = 95; // minimum, including three canonical zero batch scalars
        public const int MaxCommandBytes = 131072; // hostile-command allocation guard, not a progression level
        public const int ResultBytes = 29;
        public const int HandshakeBytes = 74;
        public const int WelcomeHeaderBytes = 168;
        private const uint Magic = 0x41444444;

        public static string CatalogHash(GameContent[] catalog)
        {
            if (catalog == null || catalog.Length == 0 || catalog.Length > byte.MaxValue) throw new ArgumentException("A bounded map catalog is required.");
            var canonical = new StringBuilder("defense-duell-map-catalog-v10\n");
            canonical.Append(catalog.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            foreach (var content in catalog)
            {
                if (content == null) throw new ArgumentException("Catalog entries cannot be null.");
                content.Validate(); canonical.Append(content.ContentHash).Append('\n');
            }
            return Digest(Encoding.UTF8.GetBytes(canonical.ToString()));
        }

        public static byte[] Handshake(string hash, int towerMask = 31)
        {
            if (hash == null || hash.Length != 64) throw new ArgumentException("A SHA-256 content hash is required.");
            using var stream = new MemoryStream(HandshakeBytes);
            using var writer = new BinaryWriter(stream);
            writer.Write(Magic); writer.Write(Version); writer.Write(Encoding.ASCII.GetBytes(hash)); writer.Write(towerMask);
            return stream.ToArray();
        }

        public static bool ValidateHandshake(byte[] bytes, string hash, out string reason)
            => ValidateHandshake(bytes, hash, 8, out _, out reason);

        public static bool ValidateHandshake(byte[] bytes, string hash, int towerCount, out int towerMask, out string reason)
        {
            towerMask = 31;
            reason = "Ungültige Verbindungsanfrage.";
            if (bytes == null || bytes.Length != HandshakeBytes) return false;
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != Magic || reader.ReadUInt16() != Version)
            { reason = "Andere Spielversion. Bitte beide Apps aktualisieren."; return false; }
            if (!string.Equals(Encoding.ASCII.GetString(reader.ReadBytes(64)), hash, StringComparison.Ordinal))
            { reason = "Spieldaten stimmen nicht überein. Bitte dieselbe Version verwenden."; return false; }
            var mask = reader.ReadInt32();
            if (!WireSnapshotValidator.ValidTowerMask(mask, towerCount))
            { reason = "Ungültige Turmfreigaben für diesen Spielkatalog."; return false; }
            towerMask = mask;
            reason = string.Empty; return true;
        }

        public static byte[] EncodeWelcome(string catalogHash, byte mapIndex, uint revision, MatchState state,
            GameContent content, ulong publicationSequence, ulong previousEpoch = 0, PlayerSlot recipient = PlayerSlot.One, ulong recipientCommandSequence = 0)
        {
            if (content == null || !content.MatchRules.Equals(state.MatchRules)) throw new ArgumentException("Rules/content mismatch.");
            var handshake = Handshake(catalogHash, state.Players[0].AvailableTowerMask); var snapshot = EncodeSnapshot(state);
            using var stream = new MemoryStream(WelcomeHeaderBytes + snapshot.Length); using var writer = new BinaryWriter(stream);
            writer.Write(handshake); writer.Write(mapIndex); writer.Write(revision); writer.Write(publicationSequence); writer.Write(previousEpoch); writer.Write((byte)recipient);writer.Write(recipientCommandSequence);
            writer.Write(Encoding.ASCII.GetBytes(content.ContentHash)); writer.Write(snapshot); return stream.ToArray();
        }

        public static bool TryDecodeWelcome(byte[] body, string catalogHash, GameContent[] catalog,
            out int mapIndex, out uint revision, out MatchState state, out string reason)
            => TryDecodeWelcome(body,catalogHash,catalog,out mapIndex,out revision,out _,out _,out _,out state,out reason);

        public static bool TryDecodeWelcome(byte[] body, string catalogHash, GameContent[] catalog,
            out int mapIndex, out uint revision, out ulong publicationSequence, out ulong previousEpoch,
            out GameContent effectiveContent, out MatchState state, out string reason)
            => TryDecodeWelcome(body,catalogHash,catalog,out mapIndex,out revision,out publicationSequence,out previousEpoch,out _,out effectiveContent,out state,out reason);

        public static bool TryDecodeWelcome(byte[] body, string catalogHash, GameContent[] catalog,
            out int mapIndex,out uint revision,out ulong publicationSequence,out ulong previousEpoch,out PlayerSlot recipient,
            out GameContent effectiveContent,out MatchState state,out string reason)
            => TryDecodeWelcome(body,catalogHash,catalog,out mapIndex,out revision,out publicationSequence,out previousEpoch,out recipient,out _,out effectiveContent,out state,out reason);

        public static bool TryDecodeWelcome(byte[] body,string catalogHash,GameContent[] catalog,
            out int mapIndex,out uint revision,out ulong publicationSequence,out ulong previousEpoch,out PlayerSlot recipient,out ulong recipientCommandSequence,
            out GameContent effectiveContent,out MatchState state,out string reason)
        {
            recipientCommandSequence=0;recipient=PlayerSlot.One;mapIndex=-1;revision=0;publicationSequence=previousEpoch=0;effectiveContent=null;state=null;reason="Ungültiger Spielzustand empfangen.";
            if(body==null || body.Length<=WelcomeHeaderBytes || (long)body.Length>WelcomeHeaderBytes+(long)MaxSnapshotBytes || catalog==null)return false;
            var handshake=new byte[HandshakeBytes];Buffer.BlockCopy(body,0,handshake,0,HandshakeBytes);
            int index=body[HandshakeBytes];
            if(index>=catalog.Length || catalog[index]==null){reason="Die Host-Karte ist nicht verfügbar.";return false;}
            if(!ValidateHandshake(handshake,catalogHash,catalog[index].Towers.Length,out var hostMask,out reason))return false;
            using var stream=new MemoryStream(body,false);using var reader=new BinaryReader(stream);
            stream.Position=HandshakeBytes+1;var nextRevision=reader.ReadUInt32();var publication=reader.ReadUInt64();var previous=reader.ReadUInt64();var slot=(PlayerSlot)reader.ReadByte();var nextCommandSequence=reader.ReadUInt64();
            var effectiveHash=Encoding.ASCII.GetString(reader.ReadBytes(64));
            if(nextRevision==0 || publication==0){reason="Ungültige Zustandsrevision.";return false;}
            var snapshot=new byte[body.Length-WelcomeHeaderBytes];Buffer.BlockCopy(body,WelcomeHeaderBytes,snapshot,0,snapshot.Length);
            if(!TryDecodeSnapshot(snapshot,catalog[index],out var decoded,out var content)
                || content.ContentHash!=effectiveHash || content.Map.Id!=catalog[index].Map.Id || (int)slot<1 || (int)slot>=decoded.Players.Length || decoded.Players[0].AvailableTowerMask!=hostMask)
            {reason="Regeln, Contenthash oder Spielzustand stimmen nicht überein.";return false;}
            mapIndex=index;revision=nextRevision;publicationSequence=publication;previousEpoch=previous;recipient=slot;recipientCommandSequence=nextCommandSequence;state=decoded;effectiveContent=content;reason=string.Empty;return true;
        }

        public static byte[] EncodeCommand(CommandEnvelope command)
        {
            using var stream = new MemoryStream(CommandBytes);
            using var writer = new BinaryWriter(stream);
            writer.Write(command.SessionId); writer.Write(command.Sequence); writer.Write((byte)command.Type);
            writer.Write(command.Kind); writer.Write(command.Target); writer.Write(command.PositionX); writer.Write(command.PositionY);
            WriteBig(writer,command.ExpectedWave); WriteBig(writer,command.ExpectedResearch);
            writer.Write(command.ExpectedConfigRevision); writer.Write(command.ExpectedRosterRevision); writer.Write(command.RuleWaveCount); WriteBig(writer,command.RuleStartGold);
            writer.Write(command.RuleEndless);writer.Write(command.RuleIncomeMultiplier);writer.Write((byte)command.RuleFormat);WriteText(writer,command.RuleMapId);writer.Write(command.EnemyTarget);
            WriteBig(writer,command.UpgradeCount);WriteBig(writer,command.ExpectedTowerLevel);WriteBig(writer,command.UpgradePriceLimit);
            if(stream.Length>MaxCommandBytes)throw new ArgumentException("Command exceeds transport budget.");
            return stream.ToArray();
        }

        public static bool TryDecodeCommand(byte[] bytes, out CommandEnvelope command)
        {
            command = default;
            if (bytes == null || bytes.Length < CommandBytes || bytes.Length > MaxCommandBytes) return false;
            try
            {
                using var stream = new MemoryStream(bytes, false); using var reader = new BinaryReader(stream);
                var next=new CommandEnvelope { SessionId=reader.ReadUInt64(),Sequence=reader.ReadUInt64(),
                    Type=(CommandType)reader.ReadByte(),Kind=reader.ReadInt32(),Target=reader.ReadInt32(),
                    PositionX=reader.ReadInt32(),PositionY=reader.ReadInt32(),ExpectedWave=ReadBig(reader),ExpectedResearch=ReadBig(reader),
                    ExpectedConfigRevision=reader.ReadUInt64(),ExpectedRosterRevision=reader.ReadUInt64(),RuleWaveCount=reader.ReadInt32(),RuleStartGold=ReadBig(reader),
                    RuleEndless=ReadBoolean(reader),RuleIncomeMultiplier=reader.ReadInt32(),RuleFormat=(MatchFormat)reader.ReadByte(),RuleMapId=ReadText(reader),EnemyTarget=reader.ReadInt32(),
                    UpgradeCount=ReadBig(reader),ExpectedTowerLevel=ReadBig(reader),UpgradePriceLimit=ReadBig(reader) };
                if(stream.Position!=stream.Length || next.ExpectedResearch<0 || next.Sequence==0 || (byte)next.Type>(byte)CommandType.UpgradeTowerBatch)return false;
                if(next.UpgradeCount<0||next.ExpectedTowerLevel<0||next.UpgradePriceLimit<0)return false;
                if(next.Type!=CommandType.UpgradeTowerBatch&&(next.UpgradeCount!=0||next.ExpectedTowerLevel!=0||next.UpgradePriceLimit!=0))return false;
                if(next.Type==CommandType.UpgradeTowerBatch&&(next.UpgradeCount==0||next.UpgradePriceLimit==0))return false;
                command=next;return true;
            }
            catch(IOException){return false;}catch(ArgumentException){return false;}catch(OverflowException){return false;}
        }

        public static byte[] EncodeResult(ulong sessionId, CommandResult result, ulong publicationSequence = 1)
        {
            using var stream = new MemoryStream(ResultBytes);
            using var writer = new BinaryWriter(stream);
            writer.Write(sessionId); writer.Write(result.Sequence); writer.Write((byte)result.Code); writer.Write(result.Tick); writer.Write(publicationSequence);
            return stream.ToArray();
        }

        public static bool TryDecodeResult(byte[] bytes, out ulong sessionId, out CommandResult result)
            => TryDecodeResult(bytes,out sessionId,out result,out _);

        public static bool TryDecodeResult(byte[] bytes,out ulong sessionId,out CommandResult result,out ulong publicationSequence)
        {
            sessionId = 0; result = default; publicationSequence=0;
            if (bytes == null || bytes.Length != ResultBytes) return false;
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            sessionId = reader.ReadUInt64(); result.Sequence = reader.ReadUInt64(); result.Code = (ErrorCode)reader.ReadByte(); result.Tick = reader.ReadInt32(); publicationSequence=reader.ReadUInt64();
            return sessionId>0 && publicationSequence>0 && result.Sequence > 0 && (byte)result.Code <= (byte)ErrorCode.RevisionExhausted && result.Tick >= 0;
        }

        // Wire9 explicitly binds variable roster/team shape and bounded absolute-wave ledgers.
        public static byte[] EncodeSnapshot(MatchState state)
        {
            using var stream=new MemoryStream(4096);using var writer=new BinaryWriter(stream);
            writer.Write(0x41534444u);writer.Write(Version);writer.Write(state.SessionId);writer.Write(state.Seed);WriteRules(writer,state.MatchRules);
            writer.Write(state.ConfigRevision);writer.Write(state.RosterRevision);writer.Write(state.ConnectedPlayerMask);writer.Write(state.StartGoldGranted);
            writer.Write(state.Tick);writer.Write(state.PlayTicks);writer.Write(state.CountdownRemaining);writer.Write((byte)state.Phase);writer.Write((byte)state.Outcome);writer.Write(state.WinningTeamId);
            writer.Write((byte)state.WavePhase);WriteBig(writer,state.WaveId);writer.Write(state.PreparationRemaining);writer.Write(state.WaveStartPlayTick);
            if(state.Players==null||state.Teams==null||state.Players.Length!=state.MatchRules.PlayerCount||state.Teams.Length!=state.MatchRules.TeamCount)throw new ArgumentException("Invalid roster shape.");
            writer.Write((byte)state.Players.Length);writer.Write((byte)state.Teams.Length);
            foreach(var team in state.Teams)writer.Write(team.Hp);
            for(int i=0;i<state.Players.Length;i++)
            {writer.Write(state.NeutralCursor[i]);writer.Write(state.NextSpawnTicks[i]);writer.Write((byte)state.NextSpawnOrigins[i]);writer.Write(state.NextPaidSender[i]);writer.Write(state.NextPaidSpawnTicks[i]);}
            foreach(var player in state.Players)
            {
                writer.Write((byte)player.TeamId);writer.Write((byte)player.AttackTarget);WriteBig(writer,player.Gold);
                writer.Write(player.Ready);writer.Write(player.Rematch);writer.Write(player.SpeedReady);writer.Write(player.WaveReady);writer.Write(player.AvailableTowerMask);
                for(int k=0;k<GameContent.UnitClassCount;k++)writer.Write(player.NextSendTicks[k]);for(int k=0;k<GameContent.UnitClassCount;k++)WriteBig(writer,player.ResearchLevels[k]);
                writer.Write(player.AttackIncomeUnits);WriteBig(writer,player.GoldRemainder);WriteBig(writer,player.ArchivedSpawnedQuarterGold);
                WriteLedger(writer,player.ReservedGoldByWave);WriteLedger(writer,player.SpawnedQuarterGoldByWave);WriteLedger(writer,player.CancelledQuarterGoldByWave);
            }
            writer.Write(state.Towers.Count);
            foreach(var tower in state.Towers)
            {
                writer.Write(tower.Id);writer.Write((byte)tower.Owner);writer.Write(tower.Slot);writer.Write((byte)tower.Kind);
                WriteBig(writer,tower.Level);WriteBig(writer,tower.DamageLevel);WriteBig(writer,tower.RangeLevel);writer.Write(tower.CooldownTicks);WriteBig(writer,tower.Invested);
                writer.Write(tower.Position.X);writer.Write(tower.Position.Y);writer.Write(tower.SiteId);writer.Write((byte)tower.Priority);
            }
            writer.Write(state.Units.Count);
            foreach(var unit in state.Units)
            {
                writer.Write(unit.Id);writer.Write((byte)unit.Defender);writer.Write((byte)unit.Sender);writer.Write((byte)unit.Kind);WriteBig(writer,unit.Hp);
                writer.Write(unit.Distance);writer.Write(unit.SlowPermille);WriteBig(writer,unit.SlowUntilTick);writer.Write(unit.MovementRemainder);
                writer.Write((byte)unit.Origin);WriteBig(writer,unit.WaveId);WriteBig(writer,unit.ResearchLevel);writer.Write(unit.NeutralRecipeIndex);
            }
            writer.Write(state.SpawnOrders.Count);
            foreach(var order in state.SpawnOrders)
            {
                writer.Write((byte)order.Sender);writer.Write((byte)order.EnemyTarget);writer.Write((byte)order.Kind);writer.Write(order.Remaining);writer.Write(order.NextTick);
                writer.Write((byte)order.Origin);WriteBig(writer,order.ResearchLevel);WriteBig(writer,order.PaidPrice);writer.Write(order.InitialCount);WriteBig(writer,order.WaveId);
            }
            if(stream.Length>MaxSnapshotBytes)throw new InvalidOperationException("Snapshot exceeds protocol address space.");return stream.ToArray();
        }
        public static bool TryDecodeSnapshot(byte[] bytes,GameContent content,out MatchState state)=>TryDecodeSnapshot(bytes,content,out state,out _);
        public static bool TryDecodeSnapshot(byte[] bytes,GameContent baseContent,out MatchState state,out GameContent content)
        {
            state=null;content=null;if(bytes==null||baseContent==null||bytes.Length<100||bytes.Length>MaxSnapshotBytes)return false;
            try
            {
                using var stream=new MemoryStream(bytes,false);using var reader=new BinaryReader(stream);
                if(reader.ReadUInt32()!=0x41534444u||reader.ReadUInt16()!=Version)return false;
                var next=new MatchState{SessionId=reader.ReadUInt64(),Seed=reader.ReadUInt32(),MatchRules=ReadRules(reader)};
                if(MatchRulesPolicy.Validate(baseContent,next.MatchRules)!=ErrorCode.None)return false;
                content=baseContent.BaseContent.WithMatchRules(next.MatchRules);if(!content.MatchRules.Equals(next.MatchRules))return false;
                next.ConfigRevision=reader.ReadUInt64();next.RosterRevision=reader.ReadUInt64();next.ConnectedPlayerMask=reader.ReadInt32();next.StartGoldGranted=ReadBoolean(reader);
                next.Tick=reader.ReadInt32();next.PlayTicks=reader.ReadInt32();next.CountdownRemaining=reader.ReadInt32();next.Phase=(MatchPhase)reader.ReadByte();next.Outcome=(MatchOutcome)reader.ReadByte();next.WinningTeamId=reader.ReadInt32();
                next.WavePhase=(WavePhase)reader.ReadByte();next.WaveId=ReadBig(reader);next.PreparationRemaining=reader.ReadInt32();next.WaveStartPlayTick=reader.ReadInt32();
                int players=reader.ReadByte(),teams=reader.ReadByte();if(players!=next.MatchRules.PlayerCount||teams!=next.MatchRules.TeamCount)return false;
                next.Players=new PlayerState[players];next.Teams=new TeamState[teams];
                for(int t=0;t<teams;t++)next.Teams[t]=new TeamState{Hp=reader.ReadInt32()};
                next.NeutralCursor=new int[players];next.NextSpawnTicks=new int[players];next.NextSpawnOrigins=new UnitOrigin[players];next.NextPaidSender=new int[players];next.NextPaidSpawnTicks=new int[players];
                for(int p=0;p<players;p++){next.NeutralCursor[p]=reader.ReadInt32();next.NextSpawnTicks[p]=reader.ReadInt32();next.NextSpawnOrigins[p]=(UnitOrigin)reader.ReadByte();next.NextPaidSender[p]=reader.ReadInt32();next.NextPaidSpawnTicks[p]=reader.ReadInt32();}
                for(int p=0;p<players;p++)
                {
                    var player=new PlayerState{TeamId=reader.ReadByte(),AttackTarget=(PlayerSlot)reader.ReadByte(),Gold=ReadBig(reader),Ready=ReadBoolean(reader),Rematch=ReadBoolean(reader),SpeedReady=ReadBoolean(reader),WaveReady=ReadBoolean(reader),AvailableTowerMask=reader.ReadInt32()};
                    for(int k=0;k<GameContent.UnitClassCount;k++)player.NextSendTicks[k]=reader.ReadInt32();for(int k=0;k<GameContent.UnitClassCount;k++)player.ResearchLevels[k]=ReadBig(reader);
                    player.AttackIncomeUnits=reader.ReadInt32();player.GoldRemainder=ReadBig(reader);player.ArchivedSpawnedQuarterGold=ReadBig(reader);
                    player.ReservedGoldByWave=ReadLedger(reader);player.SpawnedQuarterGoldByWave=ReadLedger(reader);player.CancelledQuarterGoldByWave=ReadLedger(reader);next.Players[p]=player;
                }
                next.BindTeamHealth();
                int towers=ReadCount(reader,30);
                for(int i=0;i<towers;i++)next.Towers.Add(new TowerState{Id=reader.ReadInt32(),Owner=(PlayerSlot)reader.ReadByte(),Slot=reader.ReadInt32(),Kind=reader.ReadByte(),Level=ReadBig(reader),DamageLevel=ReadBig(reader),RangeLevel=ReadBig(reader),CooldownTicks=reader.ReadInt32(),Invested=ReadBig(reader),Position=new MapPoint(reader.ReadInt32(),reader.ReadInt32()),SiteId=reader.ReadInt32(),Priority=(TargetPriority)reader.ReadByte()});
                int units=ReadCount(reader,30);
                for(int i=0;i<units;i++)next.Units.Add(new UnitState{Id=reader.ReadInt32(),Defender=(PlayerSlot)reader.ReadByte(),Sender=(PlayerSlot)reader.ReadByte(),Kind=reader.ReadByte(),Hp=ReadBig(reader),Distance=reader.ReadInt32(),SlowPermille=reader.ReadInt32(),SlowUntilTick=ReadBig(reader),MovementRemainder=reader.ReadInt32(),Origin=(UnitOrigin)reader.ReadByte(),WaveId=ReadBig(reader),ResearchLevel=ReadBig(reader),NeutralRecipeIndex=reader.ReadInt32()});
                int orders=ReadCount(reader,20);
                for(int i=0;i<orders;i++)next.SpawnOrders.Add(new SpawnOrder{Sender=(PlayerSlot)reader.ReadByte(),EnemyTarget=(PlayerSlot)reader.ReadByte(),Kind=reader.ReadByte(),Remaining=reader.ReadInt32(),NextTick=reader.ReadInt32(),Origin=(UnitOrigin)reader.ReadByte(),ResearchLevel=ReadBig(reader),PaidPrice=ReadBig(reader),InitialCount=reader.ReadInt32(),WaveId=ReadBig(reader)});
                if(stream.Position!=stream.Length||!WireSnapshotValidator.IsValid(content,next))return false;state=next;return true;
            }
            catch(IOException){return false;}catch(ArgumentException){return false;}catch(OverflowException){return false;}catch(IndexOutOfRangeException){return false;}
        }
        static readonly UTF8Encoding TextEncoding=new UTF8Encoding(false,true);
        static void WriteText(BinaryWriter writer,string value)
        {var bytes=TextEncoding.GetBytes(value??"");if(bytes.Length>256)throw new ArgumentException("Map identifier too long.");writer.Write((ushort)bytes.Length);writer.Write(bytes);}
        static string ReadText(BinaryReader reader)
        {int count=reader.ReadUInt16();if(count>256||count>reader.BaseStream.Length-reader.BaseStream.Position)throw new IOException("Invalid map identifier.");return TextEncoding.GetString(reader.ReadBytes(count));}
        static void WriteRules(BinaryWriter writer,MatchRules rules)
        {if(rules==null)throw new ArgumentException("Missing rules.");writer.Write(rules.WaveCount);WriteBig(writer,rules.StartGold);writer.Write(rules.Endless);writer.Write(rules.IncomeMultiplier);writer.Write((byte)rules.Format);WriteText(writer,rules.MapId);}
        static MatchRules ReadRules(BinaryReader reader)=>new MatchRules(reader.ReadInt32(),ReadBig(reader),ReadBoolean(reader),reader.ReadInt32(),(MatchFormat)reader.ReadByte(),ReadText(reader));
        static void WriteLedger(BinaryWriter writer,WaveLedger ledger)
        {if(ledger==null||ledger.Length<1||ledger.Length>200)throw new ArgumentException("Invalid ledger.");WriteBig(writer,ledger.FirstWave);writer.Write(ledger.IsEndless);writer.Write((ushort)ledger.Length);foreach(var value in ledger)WriteBig(writer,value);}
        static WaveLedger ReadLedger(BinaryReader reader)
        {var first=ReadBig(reader);bool endless=ReadBoolean(reader);int count=reader.ReadUInt16();if(count<1||count>200||(long)count*5>reader.BaseStream.Length-reader.BaseStream.Position)throw new IOException("Invalid ledger shape.");var values=new BigInteger[count];for(int i=0;i<count;i++)values[i]=ReadBig(reader);return new WaveLedger(first,values,endless);}

        static int ReadCount(BinaryReader reader,int minimumBytes)
        {
            int count=reader.ReadInt32();
            if(count<0 || (long)count*minimumBytes>reader.BaseStream.Length-reader.BaseStream.Position)throw new IOException("Invalid record count.");
            return count;
        }
        public static void WriteBig(BinaryWriter writer,BigInteger value)
        { var bytes=value.ToByteArray();writer.Write(bytes.Length);writer.Write(bytes); }
        public static BigInteger ReadBig(BinaryReader reader)
        {
            int size=reader.ReadInt32();
            if(size<1 || size>reader.BaseStream.Length-reader.BaseStream.Position)throw new IOException("Invalid integer length.");
            byte[] bytes=reader.ReadBytes(size);
            // Reject redundant sign extension to keep one canonical digest per state.
            if(size>1 && ((bytes[size-1]==0 && (bytes[size-2]&128)==0) || (bytes[size-1]==255 && (bytes[size-2]&128)!=0)))throw new IOException("Non-canonical integer.");
            return new BigInteger(bytes);
        }

        private static bool ReadBoolean(BinaryReader reader)
        {
            var value = reader.ReadByte();
            if (value > 1) throw new IOException("Invalid boolean.");
            return value == 1;
        }

        public static string Digest(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var digest = sha.ComputeHash(bytes);
            var text = new StringBuilder(64);
            foreach (var value in digest) text.Append(value.ToString("x2"));
            return text.ToString();
        }

        public static byte[] EncodeChunk(ulong epoch, uint sequence, byte[] snapshot, int part)
        {
            if(snapshot==null || snapshot.Length==0 || snapshot.Length>MaxSnapshotBytes)throw new ArgumentException("Empty/unsupported state.");
            int count=(int)(((long)snapshot.Length+ChunkPayloadBytes-1)/ChunkPayloadBytes);
            if(part<0 || part>=count)throw new ArgumentOutOfRangeException(nameof(part));
            int offset=part*ChunkPayloadBytes,length=Math.Min(ChunkPayloadBytes,snapshot.Length-offset);
            using var stream=new MemoryStream(length+ChunkHeaderBytes);using var writer=new BinaryWriter(stream);
            writer.Write(epoch);writer.Write(sequence);writer.Write(snapshot.Length);writer.Write(part);writer.Write(count);
            writer.Write(snapshot,offset,length);return stream.ToArray();
        }
    }

    /// <summary>One revision, allocated from received pages only. Nothing is visible before completion.</summary>
    public sealed class SnapshotAssembler
    {
        public uint CompletedSequence=>appliedSequence;
        public ulong CompletedEpoch { get; private set; }
        public int BufferedBytes=>receivedBytes;
        ulong epoch,assemblingEpoch;
        uint sequence,appliedSequence;
        bool hasApplied;
        int totalLength,totalParts,receivedBytes;
        double lastProgress;
        readonly double expiry;
        public SnapshotAssembler(double expirySeconds=1.5){expiry=expirySeconds;}
        readonly Dictionary<int,byte[]> parts=new Dictionary<int,byte[]>();
        public void Reset(ulong sessionId)
        { epoch=sessionId;sequence=appliedSequence=0;hasApplied=false;Clear();CompletedEpoch=0; }
        void Clear(){parts.Clear();receivedBytes=totalLength=totalParts=0;assemblingEpoch=0;}
        public bool TryAdd(byte[] bytes,double now,out byte[] complete)=>TryAdd(bytes,now,out complete,out _);
        public bool TryAdd(byte[] bytes,double now,out byte[] complete,out bool accepted)
        {
            complete=null;accepted=false;
            if(double.IsNaN(now)||double.IsInfinity(now)||bytes==null || bytes.Length<=WireProtocol.ChunkHeaderBytes || bytes.Length>WireProtocol.ChunkHeaderBytes+WireProtocol.ChunkPayloadBytes)return false;
            using var stream=new MemoryStream(bytes,false);using var reader=new BinaryReader(stream);
            ulong incomingEpoch=reader.ReadUInt64();uint incomingSequence=reader.ReadUInt32();
            int total=reader.ReadInt32(),part=reader.ReadInt32(),count=reader.ReadInt32();
            if(incomingEpoch==0 || (epoch!=0 && incomingEpoch!=epoch) || incomingSequence==0 || total<=0 || total>WireProtocol.MaxSnapshotBytes || part<0 || count<=0 || part>=count
                || count!=(int)(((long)total+WireProtocol.ChunkPayloadBytes-1)/WireProtocol.ChunkPayloadBytes)
                || bytes.Length-WireProtocol.ChunkHeaderBytes!=Math.Min(WireProtocol.ChunkPayloadBytes,(long)total-(long)part*WireProtocol.ChunkPayloadBytes))return false;
            if(hasApplied && incomingEpoch==CompletedEpoch && incomingSequence<=appliedSequence)return false;
            if(parts.Count>0 && incomingEpoch==assemblingEpoch && incomingSequence<sequence)return false;
            if(parts.Count==0 || incomingEpoch!=assemblingEpoch || incomingSequence>sequence || now-lastProgress>expiry)
            { Clear();sequence=incomingSequence;assemblingEpoch=incomingEpoch;totalLength=total;totalParts=count; }
            if(total!=totalLength || count!=totalParts || parts.ContainsKey(part))return false;
            int length=bytes.Length-WireProtocol.ChunkHeaderBytes;var payload=new byte[length];
            Buffer.BlockCopy(bytes,WireProtocol.ChunkHeaderBytes,payload,0,length);parts.Add(part,payload);receivedBytes+=length;lastProgress=now;accepted=true;
            if(parts.Count!=totalParts)return false;
            if(receivedBytes!=totalLength){Clear();return false;}
            complete=new byte[totalLength];foreach(var entry in parts)Buffer.BlockCopy(entry.Value,0,complete,entry.Key*WireProtocol.ChunkPayloadBytes,entry.Value.Length);
            appliedSequence=sequence;CompletedEpoch=assemblingEpoch;hasApplied=true;Clear();return true;
        }
    }

    public sealed class MessageRateLimiter
    {
        private double tokens;
        private double lastTime;
        private readonly double perSecond;
        private readonly double capacity;
        public MessageRateLimiter(double perSecond = 10, double burst = 16)
        { this.perSecond = perSecond; capacity = burst; tokens = burst; }
        public bool Allow(double now)
        {
            tokens = Math.Min(capacity, tokens + Math.Max(0, now - lastTime) * perSecond); lastTime = now;
            if (tokens < 1) return false;
            tokens--; return true;
        }
    }
}
