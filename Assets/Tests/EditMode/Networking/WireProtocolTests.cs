using System;
using System.Numerics;
using System.IO;
using System.Collections.Generic;
using DefenseDuell.Core;
using NUnit.Framework;

namespace DefenseDuell.Networking.Tests
{
    public sealed class WireProtocolTests
    {
        [Test] public void ArbitraryProgressionAndLargeAtomicFifoStates() => Wire7Scenarios.Run();

        [Test] public void SpeedConsentRoundTripsBothVotesAndRejectsMalformedBoolean()
        {
            var content = GameContent.CreateDefault(); var state = Playing(content).State;
            for (int mask = 0; mask < 4; mask++)
            {
                state.Players[0].SpeedReady = (mask & 1) != 0; state.Players[1].SpeedReady = (mask & 2) != 0;
                var bytes = WireProtocol.EncodeSnapshot(state);
                Assert.That(WireProtocol.TryDecodeSnapshot(bytes, content, out var copy), Is.True);
                Assert.That(copy.SimulationSpeed, Is.EqualTo(mask == 3 ? 2 : 1));
                Assert.That(WireProtocol.EncodeSnapshot(copy), Is.EqualTo(bytes));
                var readyOffset = FirstPlayerReadyOffset(bytes);
                for (int flag=0;flag<4;flag++) {
                    var invalid=(byte[])bytes.Clone();invalid[readyOffset+flag]=2;
                    Assert.That(WireProtocol.TryDecodeSnapshot(invalid,content,out _),Is.False);
                }
            }
            var command = new CommandEnvelope { SessionId = state.SessionId, Sequence = 5, Type = CommandType.SetSpeedVote, Kind = 1, Target = -1 };
            Assert.That(WireProtocol.TryDecodeCommand(WireProtocol.EncodeCommand(command), out var decoded), Is.True);
            Assert.That(decoded.Type, Is.EqualTo(CommandType.SetSpeedVote));
            var old = WireProtocol.Handshake(WireProtocol.CatalogHash(new[] { content }));
            old[4] = 4;
            Assert.That(WireProtocol.ValidateHandshake(old, WireProtocol.CatalogHash(new[] { content }), out _), Is.False);
        }

        [Test] public void CatalogHashIncludesAllMapsAndTheirOrder()
        {
            var catalog = MapCatalog.CreateContents(GameContent.CreateDefault());
            var hash = WireProtocol.CatalogHash(catalog);
            Assert.That(hash.Length, Is.EqualTo(64));
            Assert.That(WireProtocol.CatalogHash(MapCatalog.CreateContents(GameContent.CreateDefault())), Is.EqualTo(hash));
            var changed = (GameContent[])catalog.Clone(); changed[2] = catalog[1];
            Assert.That(WireProtocol.CatalogHash(changed), Is.Not.EqualTo(hash));
            changed = (GameContent[])catalog.Clone(); var first = changed[0]; changed[0] = changed[1]; changed[1] = first;
            Assert.That(WireProtocol.CatalogHash(changed), Is.Not.EqualTo(hash));
        }

        [Test] public void WelcomeSelectsEveryHostMapRegardlessOfClientInitialMap()
        {
            var catalog = MapCatalog.CreateContents(GameContent.CreateDefault()); var hash = WireProtocol.CatalogHash(catalog);
            for (var index = 0; index < MapCatalog.Count; index++)
            {
                var sim = Playing(catalog[index]);
                Execute(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, FirstBuildSlot(catalog[index], true));
                var welcome = WireProtocol.EncodeWelcome(hash, (byte)index, 7, sim.State,sim.Content,7);
                Assert.That(WireProtocol.TryDecodeWelcome(welcome, hash, catalog, out var selected, out var revision, out var state, out var reason), Is.True, reason);
                Assert.That(selected, Is.EqualTo(index)); Assert.That(revision, Is.EqualTo(7));
                Assert.That(state.Towers[0].Kind, Is.EqualTo(4));
                Assert.That(WireProtocol.EncodeSnapshot(state), Is.EqualTo(WireProtocol.EncodeSnapshot(sim.State)));
            }
        }

        [Test] public void WelcomeRejectsMissingMapWrongCatalogAndMalformedSnapshot()
        {
            var catalog = MapCatalog.CreateContents(GameContent.CreateDefault()); var hash = WireProtocol.CatalogHash(catalog);
            var welcome = WireProtocol.EncodeWelcome(hash, 1, 1, Playing(catalog[1]).State,catalog[1],1);
            Assert.That(WireProtocol.TryDecodeWelcome(welcome, new string('f', 64), catalog, out _, out _, out _, out _), Is.False);
            welcome[WireProtocol.HandshakeBytes] = 255;
            Assert.That(WireProtocol.TryDecodeWelcome(welcome, hash, catalog, out _, out _, out _, out _), Is.False);
            welcome = WireProtocol.EncodeWelcome(hash, 1, 0, Playing(catalog[1]).State,catalog[1],1);
            Assert.That(WireProtocol.TryDecodeWelcome(welcome, hash, catalog, out _, out _, out _, out _), Is.False);
            welcome = WireProtocol.EncodeWelcome(hash, 1, 1, Playing(catalog[1]).State,catalog[1],1);
            Array.Resize(ref welcome, welcome.Length - 1);
            Assert.That(WireProtocol.TryDecodeWelcome(welcome, hash, catalog, out _, out _, out _, out _), Is.False);
        }

        [Test] public void GoldmineClassLimitAcknowledgementRoundTrips()
        {
            var input = new CommandResult { Sequence = 3, Code = ErrorCode.ClassLimit, Tick = 100 };
            Assert.That(WireProtocol.TryDecodeResult(WireProtocol.EncodeResult(5, input), out _, out var output), Is.True);
            Assert.That(output.Code, Is.EqualTo(ErrorCode.ClassLimit));
        }

        [Test] public void PlacementAcknowledgementsRoundTripAndRejectUnknownCodes()
        {
            foreach (var code in new[] { ErrorCode.OutsideMap, ErrorCode.OnPath, ErrorCode.BlockedArea, ErrorCode.TooClose })
            {
                var input = new CommandResult { Sequence = 4, Tick = 60, Code = code };
                Assert.That(WireProtocol.TryDecodeResult(WireProtocol.EncodeResult(1234, input), out _, out var output), Is.True);
                Assert.That(output.Code, Is.EqualTo(code));
            }
            Assert.That(WireProtocol.TryDecodeResult(WireProtocol.EncodeResult(1234, new CommandResult { Sequence = 4, Code = (ErrorCode)255 }), out _, out _), Is.False);
        }

        [Test] public void SnapshotRoundTripsEveryUpgradePathWithoutMergingTheLevels()
        {
            var content = GameContent.CreateDefault(); var state = Playing(content).State;
            var tower = new TowerState { Id = 1, Owner = PlayerSlot.Zero, Slot = 0, SiteId = -1,
                Position = WireFixtures.FreeCombatPosition(content, state, PlayerSlot.Zero) };
            state.Towers.Add(tower);
            for (var kind = 0; kind < 5; kind++)
            for (var level = 0; level <= 2; level++)
            for (var range = 0; range <= (kind == 4 ? 0 : level); range++)
            {
                tower.Kind = kind; tower.Level = level; tower.RangeLevel = range; tower.DamageLevel = kind == 4 ? 0 : level - range;
                tower.Invested = WireFixtures.Investment(content, kind, level);
                Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out var replica), Is.True);
                var copy = replica.Towers[0];
                Assert.That(copy.DamageLevel, Is.EqualTo(tower.DamageLevel)); Assert.That(copy.RangeLevel, Is.EqualTo(tower.RangeLevel));
                Assert.That(TowerUpgradeRules.ValidateState(content, copy), Is.EqualTo(ErrorCode.None));
                Assert.That(WireProtocol.EncodeSnapshot(replica), Is.EqualTo(WireProtocol.EncodeSnapshot(state)));
            }
        }

        [Test] public void SnapshotRejectsImpossiblePathSumsAndMineCombatLevelsAtomically()
        {
            var content = GameContent.CreateDefault(); var state = Playing(content).State;
            var tower = new TowerState { Id = 1, Owner = PlayerSlot.Zero, Slot = 0, SiteId = -1,
                Position = WireFixtures.FreeCombatPosition(content, state, PlayerSlot.Zero), Invested = 65 };
            state.Towers.Add(tower);
            foreach (var invalid in new[] { (0, 0, 1, 0), (0, 1, 0, 0), (0, 2, 2, 1), (0, 2, 3, 0), (0, 2, 0, 3), (4, 1, 1, 0), (4, 2, 0, 1) })
            {
                tower.Kind = invalid.Item1; tower.Level = invalid.Item2; tower.DamageLevel = invalid.Item3; tower.RangeLevel = invalid.Item4;
                tower.Invested = WireFixtures.Investment(content, tower.Kind, tower.Level);
                Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out var partial), Is.False);
                Assert.That(partial, Is.Null);
            }
            tower.Kind = 0; tower.Level = tower.DamageLevel = tower.RangeLevel = 0; tower.Invested = -1;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
        }

        [Test] public void PaidBuildAndUpgradeSnapshotsRejectNegativeExtraOrMissingInvestment()
        {
            var content = GameContent.CreateDefault(new GameRules(startGold: 1000));
            for (var kind = 0; kind < content.Towers.Length; kind++)
            {
                var sim = WireFixtures.Playing(content,zeroMask:1<<kind); Execute(sim, PlayerSlot.Zero, CommandType.BuildTower, kind);
                var tower = sim.State.Towers[0];
                for (var level = 0; level <= 2; level++)
                {
                    if (level > 0) Execute(sim, PlayerSlot.Zero, CommandType.UpgradeTower,
                        (int)(kind == 4 ? TowerUpgradePath.Income : level == 1 ? TowerUpgradeRules.SecondaryPath(content,kind) : TowerUpgradeRules.PrimaryPath(content,kind)), tower.Id);
                    var paidInvestment = tower.Invested;
                    Assert.That(paidInvestment, Is.EqualTo(WireFixtures.Investment(content, kind, level)));
                    Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.True);
                    var missingUpgrade = level == 0 ? 0 : paidInvestment - content.Towers[kind].Upgrades[level - 1].Price;
                    foreach (var corrupt in new[] { new BigInteger(-1), paidInvestment + 1, paidInvestment - 1, missingUpgrade })
                    {
                        tower.Invested = corrupt;
                        Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out var partial), Is.False,
                            "kind=" + kind + " level=" + level + " investment=" + corrupt);
                        Assert.That(partial, Is.Null);
                    }
                    tower.Invested = paidInvestment;
                    Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.True);
                }
            }
        }

        [Test] public void SerializedUpgradePathCommandsSpendOnceAndChangedPathCannotReplay()
        {
            var content = GameContent.CreateDefault(new GameRules(startGold: 1000)); var sim = Playing(content);
            Execute(sim, PlayerSlot.Zero, CommandType.BuildTower, 0); var tower = sim.State.Towers[0];
            foreach (var path in new[] { TowerUpgradePath.Range, TowerUpgradePath.Damage })
            {
                Assert.That(TowerUpgradeRules.ValidateState(content, tower), Is.EqualTo(ErrorCode.None));
                var quote = TowerUpgradeRules.Quote(content, tower, path); Assert.That(quote.IsValid, Is.True);
                var gold = sim.State.Players[0].Gold;
                var request = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                    Type = CommandType.UpgradeTower, Kind = (int)path, Target = tower.Id };
                Assert.That(WireProtocol.TryDecodeCommand(WireProtocol.EncodeCommand(request), out var decoded), Is.True);
                var ack = sim.Execute(PlayerSlot.Zero, decoded); Assert.That(ack.Accepted, Is.True);
                Assert.That(sim.State.Players[0].Gold, Is.EqualTo(gold - quote.Price));
                Assert.That(tower.DamageLevel, Is.EqualTo(quote.NextDamageLevel)); Assert.That(tower.RangeLevel, Is.EqualTo(quote.NextRangeLevel));
                var beforeReplay = WireProtocol.EncodeSnapshot(sim.State);
                var replay = sim.Execute(PlayerSlot.Zero, decoded);
                Assert.That(WireProtocol.EncodeResult(request.SessionId, replay), Is.EqualTo(WireProtocol.EncodeResult(request.SessionId, ack)));
                decoded.Kind = (int)(path == TowerUpgradePath.Range ? TowerUpgradePath.Damage : TowerUpgradePath.Range);
                Assert.That(sim.Execute(PlayerSlot.Zero, decoded).Code, Is.EqualTo(ErrorCode.Duplicate));
                Assert.That(WireProtocol.EncodeSnapshot(sim.State), Is.EqualTo(beforeReplay));
            }
            Assert.That(tower.Level, Is.EqualTo(new BigInteger(2))); Assert.That(tower.DamageLevel, Is.EqualTo(new BigInteger(1))); Assert.That(tower.RangeLevel, Is.EqualTo(new BigInteger(1)));
            Execute(sim, PlayerSlot.Zero, CommandType.BuildTower, 4); var mine = sim.State.Towers[1];
            var before = WireProtocol.EncodeSnapshot(sim.State);
            var wrong = sim.Execute(PlayerSlot.Zero, new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.UpgradeTower, Kind = (int)TowerUpgradePath.Damage, Target = mine.Id });
            Assert.That(wrong.Code, Is.EqualTo(ErrorCode.InvalidKind)); Assert.That(WireProtocol.EncodeSnapshot(sim.State), Is.EqualTo(before));
            Execute(sim, PlayerSlot.Zero, CommandType.UpgradeTower, (int)TowerUpgradePath.Income, mine.Id);
            Assert.That(mine.Level, Is.EqualTo(new BigInteger(1))); Assert.That(mine.DamageLevel, Is.EqualTo(BigInteger.Zero)); Assert.That(mine.RangeLevel, Is.EqualTo(BigInteger.Zero));
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.True);
        }

        [Test] public void FreePositionCommandsPreserveCoordinatesAndReplayIdentityOnMirroredFields()
        {
            var content = GameContent.CreateDefault(); Assert.That(content.Map.FreePlacement, Is.True);
            var sim = Playing(content); var position = WireFixtures.FreeCombatPosition(content, sim.State, PlayerSlot.Zero);
            foreach (var player in new[] { PlayerSlot.Zero, PlayerSlot.One })
            {
                var request = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(player) + 1,
                    Type = CommandType.BuildTower, Kind = 0, Target = -1, PositionX = position.X, PositionY = position.Y };
                Assert.That(WireProtocol.TryDecodeCommand(WireProtocol.EncodeCommand(request), out var decoded), Is.True);
                var accepted = sim.Execute(player, decoded); Assert.That(accepted.Accepted, Is.True);
                var before = WireProtocol.EncodeSnapshot(sim.State);
                var replay = sim.Execute(player, decoded);
                Assert.That(WireProtocol.EncodeResult(request.SessionId, replay), Is.EqualTo(WireProtocol.EncodeResult(request.SessionId, accepted)));
                decoded.PositionX++;
                Assert.That(sim.Execute(player, decoded).Code, Is.EqualTo(ErrorCode.Duplicate));
                Assert.That(WireProtocol.EncodeSnapshot(sim.State), Is.EqualTo(before));
            }
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out var snapshot), Is.True);
            Assert.That(snapshot.Towers[0].Position.X, Is.EqualTo(position.X));
            Assert.That(snapshot.Towers[1].Position.Y, Is.EqualTo(position.Y));
            Assert.That(snapshot.Towers[0].SiteId, Is.EqualTo(-1));
        }

        [Test] public void FreeSnapshotRejectsUnsafeGeometryAndSameOwnerOverlapAtomically()
        {
            var content = GameContent.CreateDefault(); var state = Playing(content).State;
            var first = WireFixtures.FreeCombatPosition(content, state, PlayerSlot.Zero);
            state.Towers.Add(new TowerState { Id = 1, Owner = PlayerSlot.Zero, Kind = 0, Slot = 0, Position = first, SiteId = -1, Invested = content.Towers[0].Price });
            var second = WireFixtures.FreeCombatPosition(content, state, PlayerSlot.Zero);
            state.Towers.Add(new TowerState { Id = 2, Owner = PlayerSlot.Zero, Kind = 0, Slot = 1, Position = second, SiteId = -1, Invested = content.Towers[0].Price });
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.True);
            foreach (var invalid in new[] { new MapPoint(0, 0), new MapPoint(int.MaxValue, int.MinValue), content.Map.Waypoints[0], first })
            {
                state.Towers[1].Position = invalid;
                Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out var partial), Is.False);
                Assert.That(partial, Is.Null);
            }
            foreach (var blocker in content.Map.BlockedAreas)
            {
                state.Towers[1].Position = new MapPoint((blocker.MinX + blocker.MaxX) / 2, (blocker.MinY + blocker.MaxY) / 2);
                Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
            }
            state.Towers[1].Position = first; state.Towers[1].Owner = PlayerSlot.One;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.True, "Opposing fields may share coordinates.");
            state.Towers[1].Owner = PlayerSlot.Zero; state.Towers[1].Position = second; state.Towers[1].Slot = 0;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False, "Renderer slots must be unique per owner.");
        }

        [Test] public void FreeSnapshotAcceptsBeyondFormerCombatCapacityAndSparseSlots()
        {
            var content = GameContent.CreateDefault(); var state = Playing(content).State; var id = 1;
            WireFixtures.FillTowers(content, state, ref id);
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.True);
            var extra = state.Towers[0];
            extra.Position = WireFixtures.FreeCombatPosition(content, state, PlayerSlot.Zero);
            extra.Kind = 0; extra.SiteId = -1; extra.Slot=256; extra.DamageLevel = 2; extra.Invested = WireFixtures.Investment(content, 0, 2);
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.True);
        }

        [Test] public void LegacySnapshotRequiresRenderSlotToEqualFixedSite()
        {
            var standard = GameContent.CreateDefault(); var map = standard.Map;
            var content = new GameContent(standard.Rules, standard.Towers, standard.Units,
                new MapDefinition(map.Waypoints, map.BuildSlots, map.Id, map.Name, map.Theme, map.Width, map.Height));
            var state = Playing(content).State;
            state.Towers.Add(new TowerState { Id = 1, Owner = PlayerSlot.Zero, Kind = 0, Slot = 1, SiteId = 0, Position = map.BuildSlots[0], Invested = content.Towers[0].Price });
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
            state.Towers[0].Slot = 0;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.True);
        }

        [Test] public void SnapshotAcceptsMoreThanTwoGoldminesForOnePlayer()
        {
            var standard = GameContent.CreateDefault(); var map = standard.Map;
            // A map without reserved slots isolates the class limit from the independent slot-category guard.
            var content = new GameContent(standard.Rules, standard.Towers, standard.Units,
                new MapDefinition(map.Waypoints, map.BuildSlots, map.Id, map.Name, map.Theme, map.Width, map.Height));
            var sim = Playing(content);
            for (var slot = 0; slot < 3; slot++) sim.State.Towers.Add(new TowerState { Id = slot + 1, Slot = slot, SiteId = slot,
                Position = content.Map.BuildSlots[slot], Owner = PlayerSlot.Zero, Kind = 4, Invested = content.Towers[4].Price });
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.True);
            sim.State.Towers.RemoveAt(2);
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.True);
        }

        [Test] public void FreeMinesUseActualCoordinatesAndRejectFormerSiteIdsOnEveryMap()
        {
            foreach (var content in MapCatalog.CreateContents(GameContent.CreateDefault()))
            {
                var state = Playing(content).State;
                Assert.That(content.Map.MineSlots, Is.Empty);
                var position = WireFixtures.FreePosition(content, state, PlayerSlot.Zero, 4);
                state.Towers.Add(new TowerState { Id = 1, Owner = PlayerSlot.Zero, Kind = 4, Slot = 0,
                    SiteId = -1, Position = position, Invested = content.Towers[4].Price });
                state.Towers.Add(new TowerState { Id = 2, Owner = PlayerSlot.One, Kind = 0, Slot = 0,
                    SiteId = -1, Position = position, Invested = content.Towers[0].Price });
                Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.True);
                state.Towers[0].SiteId = 0;
                Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
                state.Towers[0].SiteId = -1; state.Towers[1].SiteId = 0;
                Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
            }
        }

        [Test] public void HandshakeRejectsDifferentContentProtocolAndLengths()
        {
            var content = GameContent.CreateDefault();
            var bytes = WireProtocol.Handshake(content.ContentHash);
            Assert.That(WireProtocol.ValidateHandshake(bytes, content.ContentHash, out _), Is.True);
            Assert.That(WireProtocol.ValidateHandshake(bytes, new string('0', 64), out var reason), Is.False);
            Assert.That(reason, Does.Contain("Spieldaten"));
            bytes[4] = 3; // Protocol 3 cannot carry authoritative upgrade path levels or free-mine semantics.
            Assert.That(WireProtocol.ValidateHandshake(bytes, content.ContentHash, out reason), Is.False);
            Assert.That(reason, Does.Contain("Spielversion"));
            Assert.That(WireProtocol.ValidateHandshake(new byte[70000], content.ContentHash, out _), Is.False);
        }

        [Test] public void CommandsCarryNoTrustedPlayerOrMoneyAndRoundTripExactly()
        {
            var input = new CommandEnvelope { SessionId = ulong.MaxValue - 2, Sequence = 12, Type = CommandType.BuildTower, Kind = 3, Target = -1, PositionX = 307, PositionY = 811, ExpectedWave = 8, ExpectedResearch = 5 };
            var bytes = WireProtocol.EncodeCommand(input);
            Assert.That(bytes.Length, Is.EqualTo(WireProtocol.CommandBytes));
            Assert.That(WireProtocol.TryDecodeCommand(bytes, out var output), Is.True);
            Assert.That(output.SessionId, Is.EqualTo(input.SessionId)); Assert.That(output.Sequence, Is.EqualTo(input.Sequence));
            Assert.That(output.Type, Is.EqualTo(input.Type)); Assert.That(output.Kind, Is.EqualTo(input.Kind)); Assert.That(output.Target, Is.EqualTo(input.Target));
            Assert.That(output.PositionX, Is.EqualTo(input.PositionX)); Assert.That(output.PositionY, Is.EqualTo(input.PositionY));
            Assert.That(output.ExpectedWave, Is.EqualTo(input.ExpectedWave)); Assert.That(output.ExpectedResearch, Is.EqualTo(input.ExpectedResearch));
            bytes[16] = 255;
            Assert.That(WireProtocol.TryDecodeCommand(bytes, out _), Is.False);
            Assert.That(WireProtocol.TryDecodeCommand(new byte[25], out _), Is.False);
            Assert.That(WireProtocol.TryDecodeCommand(new byte[32], out _), Is.False);
            Assert.That(WireProtocol.TryDecodeCommand(new byte[34], out _), Is.False);
        }

        [Test] public void AcknowledgementsIncludeEpochAndReason()
        {
            var input = new CommandResult { Sequence = 99, Tick = 600, Code = ErrorCode.QueueFull };
            Assert.That(WireProtocol.TryDecodeResult(WireProtocol.EncodeResult(1234, input), out var epoch, out var output), Is.True);
            Assert.That(epoch, Is.EqualTo(1234)); Assert.That(output.Sequence, Is.EqualTo(99)); Assert.That(output.Code, Is.EqualTo(ErrorCode.QueueFull));
            Assert.That(output.Tick, Is.EqualTo(600)); Assert.That(output.Accepted, Is.False);
        }

        [Test] public void SnapshotRoundTripPreservesEveryReplicatedField()
        {
            var content = GameContent.CreateDefault(); var sim = Playing(content);
            Execute(sim, PlayerSlot.Zero, CommandType.BuildTower, 2, 5);
            Execute(sim, PlayerSlot.One, CommandType.SendUnit, 3);
            for (var i = 0; i < 75; i++) sim.Step();
            var bytes = WireProtocol.EncodeSnapshot(sim.State);
            Assert.That(WireProtocol.TryDecodeSnapshot(bytes, content, out var copy), Is.True);
            Assert.That(WireProtocol.EncodeSnapshot(copy), Is.EqualTo(bytes));
            Assert.That(ReferenceEquals(copy, sim.State), Is.False);
            copy.Players[0].Gold = 0;
            Assert.That(sim.State.Players[0].Gold, Is.GreaterThan(BigInteger.Zero));
        }

        [Test] public void FullLoadSnapshotHasBoundedSizeAndMatchesAllEntities()
        {
            var state = WireFixtures.Maximum(out var content);
            var bytes = WireProtocol.EncodeSnapshot(state);
            Assert.That(bytes.Length, Is.GreaterThan(12241));
            Assert.That(bytes.Length, Is.LessThanOrEqualTo(WireProtocol.MaxSnapshotBytes));
            Assert.That(WireProtocol.TryDecodeSnapshot(bytes, content, out var copy), Is.True);
            Assert.That(copy.Units.Count, Is.EqualTo(content.MaxUnitsPerField * 2)); Assert.That(copy.Towers.Count, Is.EqualTo(64));
            Assert.That(WireProtocol.EncodeSnapshot(copy), Is.EqualTo(bytes));
        }

        [Test] public void PaidFifoSnapshotPreservesSeparateOrdersBeyondFormerCapacity()
        {
            var state = WireFixtures.Maximum(out var content);
            Assert.That(content.MaxSpawnOrdersPerPlayer, Is.EqualTo(80));
            state.Players[0].NextSendTicks[0] = state.PlayTicks + 20;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out var copy), Is.True);
            Assert.That(copy.SpawnOrders.Count, Is.EqualTo(160));
            Assert.That(WireProtocol.EncodeSnapshot(copy), Is.EqualTo(WireProtocol.EncodeSnapshot(state)));
            for(int i=0;i<300;i++)WireFixtures.Queue(content,state,PlayerSlot.One,0,1);
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state),content,out copy),Is.True);
            Assert.That(copy.SpawnOrders.Count,Is.EqualTo(460));
            Assert.That(WireProtocol.EncodeSnapshot(copy),Is.EqualTo(WireProtocol.EncodeSnapshot(state)));
        }

        [Test] public void SerializedRapidPurchasesReceiveDistinctAcceptedAcksDespiteDepartureCooldown()
        {
            var content = GameContent.CreateDefault(); var sim = Playing(content);
            Execute(sim, PlayerSlot.Zero, CommandType.SendUnit, 0); sim.Step(); sim.Step(); sim.Step(); sim.Step(); sim.Step();
            Assert.That(sim.State.Players[0].NextSendTicks[0], Is.GreaterThan(sim.State.PlayTicks));
            var originalGold = sim.State.Players[0].Gold; var accepted = 0;
            for (var i = 0; i <= originalGold / content.Units[0].Price; i++)
            {
                var quote = SendRules.Quote(content, sim.State, PlayerSlot.Zero, 0);
                var request = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1, Type = CommandType.SendUnit, Kind = 0, Target = -1, ExpectedWave = quote.WaveId, ExpectedResearch = quote.ResearchLevel, EnemyTarget = quote.EnemyTarget };
                Assert.That(WireProtocol.TryDecodeCommand(WireProtocol.EncodeCommand(request), out var decoded), Is.True);
                var result = sim.Execute(PlayerSlot.Zero, decoded);
                Assert.That(WireProtocol.TryDecodeResult(WireProtocol.EncodeResult(sim.State.SessionId, result), out var epoch, out var ack), Is.True);
                Assert.That(epoch, Is.EqualTo(request.SessionId)); Assert.That(ack.Sequence, Is.EqualTo(request.Sequence));
                Assert.That(ack.Code, Is.EqualTo(i < originalGold / content.Units[0].Price ? ErrorCode.None : ErrorCode.InsufficientGold));
                var beforeReplay = WireProtocol.EncodeSnapshot(sim.State);
                var replay = sim.Execute(PlayerSlot.Zero, decoded);
                Assert.That(WireProtocol.EncodeResult(sim.State.SessionId, replay), Is.EqualTo(WireProtocol.EncodeResult(sim.State.SessionId, result)));
                Assert.That(WireProtocol.EncodeSnapshot(sim.State), Is.EqualTo(beforeReplay));
                if (ack.Accepted) accepted++;
            }
            Assert.That(accepted, Is.GreaterThan(1));
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out var client), Is.True);
            Assert.That(client.Players[0].Gold, Is.EqualTo(originalGold - accepted * content.Units[0].Price));
            Assert.That(client.SpawnOrders.Count, Is.EqualTo(accepted));
            Assert.That(client.Players[0].NextSendTicks[0], Is.EqualTo(sim.State.Players[0].NextSendTicks[0]));
        }

        [Test] public void MalformedSnapshotsCannotAllocateUnboundedCountsOrApplyPartialState()
        {
            var content = GameContent.CreateDefault(); var bytes = WireProtocol.EncodeSnapshot(Playing(content).State);
            for (var len = 0; len < bytes.Length; len++)
            {
                var truncated = new byte[len]; Buffer.BlockCopy(bytes, 0, truncated, 0, len);
                Assert.That(WireProtocol.TryDecodeSnapshot(truncated, content, out _), Is.False, "length " + len);
            }
            var extra = new byte[bytes.Length + 1]; Buffer.BlockCopy(bytes, 0, extra, 0, bytes.Length);
            Assert.That(WireProtocol.TryDecodeSnapshot(extra, content, out _), Is.False);
            // A zero-entity state ends with three Int32 counts. Impossible counts fail before allocation.
            Buffer.BlockCopy(BitConverter.GetBytes(int.MaxValue),0,bytes,bytes.Length-12,4);
            Assert.That(WireProtocol.TryDecodeSnapshot(bytes,content,out var partial),Is.False);
            Assert.That(partial,Is.Null);
        }

        [Test] public void DuplicateEntityIdsAndInvalidOwnershipAreRejected()
        {
            var content = GameContent.CreateDefault(); var sim = Playing(content);
            sim.State.Units.Add(new UnitState { Id = 1, Defender = PlayerSlot.Zero, Kind = 0, Hp = 1 });
            sim.State.Units.Add(new UnitState { Id = 1, Defender = PlayerSlot.One, Kind = 0, Hp = 1 });
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.False);
            sim.State.Units[1].Id = 2; sim.State.Units[1].Defender = (PlayerSlot)2;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.False);
        }

        [Test] public void SnapshotRejectsImpossibleEconomySlowAndOutcomeValues()
        {
            var content = GameContent.CreateDefault(); var state = Playing(content).State;
            state.Players[0].Gold = -1;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
            state.Players[0].Gold = content.StartGold;
            state.Units.Add(new UnitState { Id = 1, Defender = PlayerSlot.Zero, Kind = 0, Hp = 1, SlowPermille = content.Rules.MinimumSlowPermille - 1 });
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
            state.Units.Clear(); state.Outcome = MatchOutcome.Draw;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
            state.Phase = MatchPhase.Finished; state.Outcome = MatchOutcome.None;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
            state.Outcome = MatchOutcome.Draw;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.True);
        }

        [Test] public void ChunksSupportReorderingButRejectDuplicatesOldEpochsAndOldFrames()
        {
            var bytes = new byte[2000]; for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)(i % 251);
            var assembler = new SnapshotAssembler(); assembler.Reset(123);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(999, 1, bytes, 0), 0, out _), Is.False);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(123, 1, bytes, 2), 0, out _), Is.False);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(123, 1, bytes, 2), .1, out _), Is.False);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(123, 1, bytes, 0), .2, out _), Is.False);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(123, 1, bytes, 1), .3, out var copy), Is.True);
            Assert.That(copy, Is.EqualTo(bytes)); Assert.That(assembler.CompletedSequence, Is.EqualTo(1));
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(123, 1, bytes, 0), .4, out _), Is.False);
            assembler.Reset(124);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(123, 2, bytes, 0), .5, out _), Is.False);
        }

        [Test] public void MissingChunksExpireAndNewerCompleteFrameRecovers()
        {
            var bytes = new byte[2000]; var assembler = new SnapshotAssembler(); assembler.Reset(9);
            assembler.TryAdd(WireProtocol.EncodeChunk(9, 1, bytes, 0), 0, out _);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(9, 1, bytes, 1), 2, out _), Is.False);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(9, 1, bytes, 2), 2.1, out _), Is.False);
            assembler.TryAdd(WireProtocol.EncodeChunk(9, 2, bytes, 2), 2.2, out _);
            assembler.TryAdd(WireProtocol.EncodeChunk(9, 2, bytes, 1), 2.3, out _);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(9, 1, bytes, 0), 2.4, out _), Is.False);
            Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(9, 2, bytes, 0), 2.5, out var copy), Is.True);
            Assert.That(copy, Is.EqualTo(bytes));
        }

        [Test] public void FloodLimiterHasFiniteBurstAndRecoversWithTime()
        {
            var limiter = new MessageRateLimiter(10, 16);
            for (var i = 0; i < 16; i++) Assert.That(limiter.Allow(0), Is.True);
            Assert.That(limiter.Allow(0), Is.False); Assert.That(limiter.Allow(.05), Is.False);
            Assert.That(limiter.Allow(.1), Is.True); Assert.That(limiter.Allow(.1), Is.False);
            Assert.That(limiter.Allow(10), Is.True);
        }

        [Test] public void WaveHandshakeBindsUnlockMaskAndRejectsEveryOlderProtocol()
        {
            var content = GameContent.CreateDefault(); var hash = content.ContentHash;
            foreach (var mask in new[] { 1, 31, 47, 79, 128, 143 })
            {
                var bytes = WireProtocol.Handshake(hash, mask);
                Assert.That(bytes.Length, Is.EqualTo(74));
                Assert.That(WireProtocol.ValidateHandshake(bytes, hash, 8, out var decoded, out _), Is.True);
                Assert.That(decoded, Is.EqualTo(mask));
                if ((mask & ~31) != 0) Assert.That(WireProtocol.ValidateHandshake(bytes, hash, 5, out _, out _), Is.False);
            }
            foreach (var invalid in new[] { 0, -1, 63, 95, 127, 255, 256, 511 })
                Assert.That(WireProtocol.ValidateHandshake(WireProtocol.Handshake(hash, invalid), hash, 8, out _, out _), Is.False);
            for (var old = 0; old < WireProtocol.Version; old++)
            {
                var bytes = WireProtocol.Handshake(hash); bytes[4] = (byte)old;
                Assert.That(WireProtocol.ValidateHandshake(bytes, hash, out _), Is.False);
            }
            var catalog = MapCatalog.CreateContents(content); var sim = WireFixtures.Playing(content, zeroMask: 47, oneMask: 79);
            var welcome = WireProtocol.EncodeWelcome(WireProtocol.CatalogHash(catalog), 0, 1, sim.State,sim.Content,1);
            welcome[70] = 79; // valid mask but disagrees with authoritative host slot.
            Assert.That(WireProtocol.TryDecodeWelcome(welcome, WireProtocol.CatalogHash(catalog), catalog, out _, out _, out _, out _), Is.False);
        }

        [Test] public void ResearchRaceAndSendQuoteExpectationsSurviveSerializationAndCannotReplayDifferently()
        {
            var content = GameContent.CreateDefault(new GameRules(startGold: 1000)); var sim = WireFixtures.Playing(content, active: false);
            var oldQuote = SendRules.Quote(content, sim.State, PlayerSlot.Zero, 3);
            Execute(sim, PlayerSlot.Zero, CommandType.SendUnit, 3);
            Execute(sim, PlayerSlot.Zero, CommandType.ResearchUnit, 3);
            var before = WireProtocol.EncodeSnapshot(sim.State);
            var command = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.SendUnit, Kind = 3, Target = -1, ExpectedWave = oldQuote.WaveId, ExpectedResearch = oldQuote.ResearchLevel, EnemyTarget = oldQuote.EnemyTarget };
            Assert.That(WireProtocol.TryDecodeCommand(WireProtocol.EncodeCommand(command), out var decoded), Is.True);
            var ack = sim.Execute(PlayerSlot.Zero, decoded);
            Assert.That(ack.Code, Is.EqualTo(ErrorCode.QuoteChanged));
            Assert.That(WireProtocol.EncodeSnapshot(sim.State), Is.EqualTo(before));
            Assert.That(WireProtocol.EncodeResult(sim.State.SessionId, sim.Execute(PlayerSlot.Zero, decoded)), Is.EqualTo(WireProtocol.EncodeResult(sim.State.SessionId, ack)));
            decoded.ExpectedResearch++;
            Assert.That(sim.Execute(PlayerSlot.Zero, decoded).Code, Is.EqualTo(ErrorCode.Duplicate));
            decoded.ExpectedResearch--; decoded.ExpectedWave++;
            Assert.That(sim.Execute(PlayerSlot.Zero, decoded).Code, Is.EqualTo(ErrorCode.Duplicate));
            Execute(sim, PlayerSlot.Zero, CommandType.SendUnit, 3);
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out var copy), Is.True);
            Assert.That(copy.SpawnOrders[0].ResearchLevel, Is.EqualTo(BigInteger.Zero));
            Assert.That(copy.SpawnOrders[1].ResearchLevel, Is.EqualTo(new BigInteger(1)));
            Assert.That(copy.Players[0].ResearchLevels[3], Is.EqualTo(new BigInteger(1)));
            Assert.That(copy.SpawnOrders[0].InitialCount, Is.EqualTo(4));
            Assert.That(copy.Players[0].AttackIncomeUnits, Is.Zero);
        }

        [Test] public void WaveLedgerRejectsBudgetCreditRemainderAndRecipeCorruptionAtomically()
        {
            var content = GameContent.CreateDefault(); var sim = WireFixtures.Playing(content, active: false);
            Execute(sim, PlayerSlot.Zero, CommandType.SendUnit, 3);
            var valid = WireProtocol.EncodeSnapshot(sim.State);
            Action<MatchState>[] corruptions = {
                s => s.Players[0].ReservedGoldByWave[0]++, s => s.Players[0].SpawnedQuarterGoldByWave[0]++,
                s => s.Players[0].SpawnedQuarterGoldByWave[1] = 1, s => s.Players[0].AttackIncomeUnits = 1,
                s => s.Players[0].GoldRemainder = 960, s => s.Players[0].ResearchLevels[0] = -1,
                s => s.SpawnOrders[0].PaidPrice++, s => s.SpawnOrders[0].InitialCount++,
                s => s.SpawnOrders[0].Remaining = 5, s => s.SpawnOrders[0].Origin = UnitOrigin.NeutralWave,
                s => s.SpawnOrders[0].ResearchLevel = 1, s => s.SpawnOrders[0].WaveId = 11,
                s => s.NextSpawnOrigins[0] = (UnitOrigin)2, s => s.NeutralCursor[0] = 1,
                s => s.WaveStartPlayTick = s.PlayTicks + 1, s => s.WaveId = 0,
                s => s.PreparationRemaining = 201, s => s.Players[0].AvailableTowerMask = 256
            };
            foreach (var corrupt in corruptions)
            {
                Assert.That(WireProtocol.TryDecodeSnapshot(valid, content, out var copy), Is.True);
                corrupt(copy);
                Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(copy), content, out var partial), Is.False);
                Assert.That(partial, Is.Null);
            }
        }

        [Test] public void ActualPartialSwarmKeepsFrozenRecipeAndExactSpawnCreditAfterResearch()
        {
            var content = GameContent.CreateDefault(new GameRules(startGold: 1000)); var sim = Playing(content);
            Execute(sim, PlayerSlot.Zero, CommandType.SendUnit, 3);
            for (var i = 0; i < 20&&sim.State.SpawnOrders[0].Remaining==4; i++) sim.Step();
            Assert.That(sim.State.SpawnOrders[0].Remaining, Is.EqualTo(3));
            Execute(sim, PlayerSlot.Zero, CommandType.ResearchUnit, 3);
            Execute(sim, PlayerSlot.Zero, CommandType.SendUnit, 3);
            for (var i = 0; i < 100; i++)
            {
                var bytes = WireProtocol.EncodeSnapshot(sim.State);
                Assert.That(WireProtocol.TryDecodeSnapshot(bytes, content, out var replica), Is.True, "active tick " + sim.State.PlayTicks);
                Assert.That(EconomyRules.ValidateLedger(content, replica, PlayerSlot.Zero), Is.EqualTo(ErrorCode.None));
                foreach (var unit in replica.Units)
                    if (unit.Origin == UnitOrigin.NeutralWave) Assert.That(unit.ResearchLevel, Is.EqualTo(BigInteger.Zero));
                sim.Step();
            }
            Assert.That(sim.State.Players[0].SpawnedQuarterGoldByWave[0], Is.EqualTo(new BigInteger(2 * 4 * content.Units[3].Price)));
        }

        [Test] public void PaidFifoMayExceedFormerBudgetWithoutMovingPurchasedPacketsToFutureWaves()
        {
            var content = GameContent.CreateDefault(new GameRules(startGold: 10000)); var sim = WireFixtures.Playing(content, active: false);
            BigInteger previous = 0;
            for (var i = 0; i < 90; i++)
            {
                var quote = SendRules.Quote(content, sim.State, PlayerSlot.Zero, 2);
                Assert.That(quote.IsValid, Is.True); Assert.That(quote.WaveId, Is.GreaterThanOrEqualTo(previous)); previous = quote.WaveId;
                Execute(sim, PlayerSlot.Zero, CommandType.SendUnit, 2);
                if (i % 20 == 19) for(int tick=0;tick<20;tick++)sim.Step(); // elapsed control second replenishes request budget.
            }
            Assert.That(previous, Is.EqualTo(BigInteger.One));
            Assert.That(SendRules.Quote(content, sim.State, PlayerSlot.Zero, 2).IsValid, Is.True);
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out var state), Is.True);
            Assert.That(state.Players[0].AttackIncomeUnits, Is.Zero);
            Assert.That(state.SpawnOrders.Count,Is.EqualTo(90));
            Assert.That(state.Players[0].ReservedGoldByWave[0],Is.EqualTo(new BigInteger(90*content.Units[2].Price)));
            state.SpawnOrders[state.SpawnOrders.Count - 1].WaveId = 0;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state), content, out _), Is.False);
        }

        [Test] public void OriginCapsNeutralResearchAndPaidCreditsAreStrict()
        {
            var state = WireFixtures.Maximum(out var content); var valid = WireProtocol.EncodeSnapshot(state);
            Assert.That(WireProtocol.TryDecodeSnapshot(valid, content, out _), Is.True);
            Action<MatchState>[] corruptions = {
                s => s.Units[0].ResearchLevel = 1, s => s.Units[0].WaveId = 2,
                s => s.Units[64].Origin = UnitOrigin.NeutralWave, s => s.Units[0].Hp = 100000,
                s => s.NeutralCursor[0] = 63, s => s.Units[0].Origin = (UnitOrigin)2,
                s => s.Players[1].SpawnedQuarterGoldByWave[0] = 0
            };
            foreach (var corrupt in corruptions)
            {
                WireProtocol.TryDecodeSnapshot(valid, content, out var copy); corrupt(copy);
                Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(copy), content, out _), Is.False);
            }
        }

        [Test] public void PriorityAndUnlockStateRoundTripAndInvalidMinePriorityIsRejected()
        {
            var content = GameContent.CreateDefault(new GameRules(startGold: 1000)); var sim = WireFixtures.Playing(content,zeroMask:48);
            Execute(sim, PlayerSlot.Zero, CommandType.BuildTower, 5);
            var tower = sim.State.Towers[0];
            Execute(sim, PlayerSlot.Zero, CommandType.SetTowerPriority, (int)TargetPriority.Strongest, tower.Id);
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out var copy), Is.True);
            Assert.That(copy.Towers[0].Priority, Is.EqualTo(TargetPriority.Strongest));
            sim.State.Players[0].AvailableTowerMask = 31;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.False);
            sim.State.Players[0].AvailableTowerMask = 48; tower.Priority = (TargetPriority)2;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.False);
            tower.Priority = TargetPriority.Front; Execute(sim, PlayerSlot.Zero, CommandType.BuildTower, 4);
            sim.State.Towers[1].Priority = TargetPriority.Strongest;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.False);
        }

        [Test] public void RematchRetainsAsymmetricUnlocksAndResetsWaveResearchLedgerAndVotes()
        {
            var content = GameContent.CreateDefault(); var sim = WireFixtures.Playing(content, active: false, zeroMask: 47, oneMask: 79);
            Execute(sim, PlayerSlot.Zero, CommandType.ResearchUnit, 0); Execute(sim, PlayerSlot.One, CommandType.SendUnit, 0);
            Execute(sim, PlayerSlot.Zero, CommandType.SetSpeedVote, 1); Execute(sim, PlayerSlot.One, CommandType.SetWaveReady, 1, 1);
            var oldEpoch = sim.State.SessionId;
            sim.State.Phase = MatchPhase.Finished; sim.State.Outcome = MatchOutcome.Draw;
            Execute(sim, PlayerSlot.Zero, CommandType.RequestRematch); Execute(sim, PlayerSlot.One, CommandType.RequestRematch);
            Assert.That(sim.State.SessionId, Is.Not.EqualTo(oldEpoch));
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out var copy), Is.True);
            Assert.That(copy.Players[0].AvailableTowerMask, Is.EqualTo(47)); Assert.That(copy.Players[1].AvailableTowerMask, Is.EqualTo(79));
            foreach (var player in copy.Players)
            {
                Assert.That(player.Gold, Is.EqualTo(BigInteger.Zero)); Assert.That(copy.StartGoldGranted,Is.False); Assert.That(player.ResearchLevels, Is.EqualTo(new BigInteger[GameContent.UnitClassCount]));
                Assert.That(player.ReservedGoldByWave, Is.EqualTo(new BigInteger[10])); Assert.That(player.SpawnedQuarterGoldByWave, Is.EqualTo(new BigInteger[10]));
                Assert.That(player.AttackIncomeUnits, Is.Zero); Assert.That(player.GoldRemainder, Is.EqualTo(BigInteger.Zero));
                Assert.That(player.SpeedReady || player.WaveReady, Is.False);
            }
        }

        [Test] public void ControlAndGameplayClocksRoundTripPastTheOldEightMinuteLimit()
        {
            var content = GameContent.CreateDefault(); var sim = Playing(content);
            sim.State.Tick = 12000; sim.State.PlayTicks = 20000; sim.State.WavePhase = WavePhase.Clearing;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out var copy), Is.True);
            Assert.That(copy.PlayTicks, Is.EqualTo(20000));
            sim.State.PlayTicks = 24001;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State), content, out _), Is.False);
        }

        // Walk the canonical Wire9 prefix so corruptions hit the intended boolean,
        // independent of map-id length, arbitrary gold magnitude, and roster size.
        private static int FirstPlayerReadyOffset(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes); using var reader = new BinaryReader(stream);
            Assert.That(reader.ReadUInt32(), Is.EqualTo(0x41534444u));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(WireProtocol.Version));
            reader.ReadUInt64(); reader.ReadUInt32(); reader.ReadInt32(); WireProtocol.ReadBig(reader);
            reader.ReadBoolean(); reader.ReadInt32(); reader.ReadByte();
            reader.ReadBytes(reader.ReadUInt16()); // map id
            reader.ReadUInt64(); reader.ReadUInt64(); reader.ReadInt32(); reader.ReadBoolean();
            reader.ReadInt32(); reader.ReadInt32(); reader.ReadInt32(); reader.ReadByte(); reader.ReadByte(); reader.ReadInt32();
            reader.ReadByte(); WireProtocol.ReadBig(reader); reader.ReadInt32(); reader.ReadInt32();
            int players = reader.ReadByte(), teams = reader.ReadByte();
            for (int i = 0; i < teams; i++) reader.ReadInt32();
            for (int i = 0; i < players; i++)
            { reader.ReadInt32(); reader.ReadInt32(); reader.ReadByte(); reader.ReadInt32(); reader.ReadInt32(); }
            reader.ReadByte(); reader.ReadByte(); WireProtocol.ReadBig(reader);
            return checked((int)stream.Position);
        }

        private static MatchSimulation Playing(GameContent content) => WireFixtures.Playing(content, zeroMask: 31, oneMask: 31);
        private static int FirstBuildSlot(GameContent content, bool mine)
        {
            for (var slot = 0; slot < content.Map.BuildSlots.Length; slot++)
                if (!content.Map.HasDedicatedMineSlots || content.Map.IsMineSlot(slot) == mine) return slot;
            throw new InvalidOperationException("Map has no matching build slot.");
        }
        private static void Execute(MatchSimulation sim, PlayerSlot slot, CommandType type, int kind = 0, int target = -1)
        {
            var result = WireFixtures.Execute(sim, slot, type, kind, target);
            Assert.That(result.Accepted, Is.True, type + " " + result.Code);
        }
    }
}
