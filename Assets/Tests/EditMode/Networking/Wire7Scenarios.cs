using System;
using System.IO;
using System.Numerics;
using DefenseDuell.Core;

namespace DefenseDuell.Networking.Tests
{
    // Shared by NUnit and the isolated .NET acceptance harness.
    public static class Wire7Scenarios
    {
        static void Check(bool value,string message) { if(!value)throw new Exception(message); }
        public static void Run()
        {
            var window=new StateTransferWindow(70000);
            for(int batch=0;!window.Complete;batch++)
            {
                int start=window.Sent;
                while(window.TryTake(out _)) { }
                Check(window.Sent-start<=16,"unbounded transport queue");
                Check(!window.Acknowledge(window.Sent),"unsent acknowledgement accepted");
                for(int part=start;part<window.Sent;part++)Check(window.Acknowledge(part),"acknowledgement order");
            }
            Check(window.Sent==70000,"large revision starved/truncated");
            // An incomplete UDP revision must not suppress page0 ACK of a later large reliable revision.
            var mixed=new SnapshotAssembler(double.PositiveInfinity);mixed.Reset(12);
            mixed.TryAdd(WireProtocol.EncodeChunk(12,1,new byte[2000],0),0,out _);
            var replacement=new byte[40000];var replacementWindow=new StateTransferWindow((replacement.Length+879)/880);
            int acknowledgements=0;byte[] replaced=null;
            while(!replacementWindow.Complete)
            {
                Check(replacementWindow.TryTake(out var page),"replacement stalled before final page");
                mixed.TryAdd(WireProtocol.EncodeChunk(12,2,replacement,page),.01+page*.01+(page>0?2:0),out var complete,out var accepted);
                Check(accepted,"new revision page not reported as progress");
                Check(replacementWindow.Acknowledge(page),"replacement page ACK lost");acknowledgements++;
                if(complete!=null)replaced=complete;
            }
            Check(acknowledgements==replacementWindow.Count&&replaced.Length==replacement.Length,"incomplete UDP to reliable replacement deadlocked");
            mixed.TryAdd(WireProtocol.EncodeChunk(12,2,replacement,0),2,out _,out var duplicateAccepted);
            Check(!duplicateAccepted,"duplicate page extends watchdog");
            var content=GameContent.CreateDefault();var sim=WireFixtures.Playing(content,false);var state=sim.State;
            var huge=BigInteger.One<<2048;state.Players[0].Gold=huge*huge;
            state.Players[0].ResearchLevels[0]=huge;
            WireFixtures.Execute(sim,PlayerSlot.Zero,CommandType.BuildTower,0);
            var tower=state.Towers[0];tower.Slot=256;tower.Level=tower.DamageLevel=huge;tower.Invested=TowerUpgradeRules.TotalInvestment(content,0,huge);
            // Distinct accepted packets, beyond both UInt16 record counts and byte page indices.
            for(int i=0;i<66000;i++)WireFixtures.Queue(content,state,PlayerSlot.Zero,0,1);
            var bytes=WireProtocol.EncodeSnapshot(state);
            Check(bytes.Length>65535 && bytes.Length/WireProtocol.ChunkPayloadBytes>255,"large framing threshold");
            Check(WireProtocol.TryDecodeSnapshot(bytes,content,out var copy),"large exact state rejected");
            Check(copy.SpawnOrders.Count==66000 && copy.Towers[0].Slot==256 && copy.Towers[0].Level==huge,"large counts/levels lost");
            Check(copy.Players[0].Gold==state.Players[0].Gold && WireProtocol.Digest(WireProtocol.EncodeSnapshot(copy))==WireProtocol.Digest(bytes),"exact canonical state differs");
            var assembly=new SnapshotAssembler();assembly.Reset(state.SessionId);
            var pages=(bytes.Length+WireProtocol.ChunkPayloadBytes-1)/WireProtocol.ChunkPayloadBytes;
            for(int part=pages-1;part>=0;part--)
            {
                var done=assembly.TryAdd(WireProtocol.EncodeChunk(state.SessionId,8,bytes,part),(pages-part)*.01,out var joined);
                Check(done==(part==0),"partial state published");
                if(done)Check(WireProtocol.Digest(joined)==WireProtocol.Digest(bytes),"page bytes changed");
            }
            Check(!assembly.TryAdd(WireProtocol.EncodeChunk(state.SessionId,7,bytes,0),100,out _),"stale revision accepted");
            var request=new CommandEnvelope { SessionId=state.SessionId,Sequence=99,Type=CommandType.ResearchUnit,Kind=0,Target=-1,ExpectedResearch=huge };
            var command=WireProtocol.EncodeCommand(request);
            Check(WireProtocol.TryDecodeCommand(command,out var decoded)&&decoded.ExpectedResearch==huge,"large quote changed");
            // Noncanonical positive zero and impossible allocation length must fail before reading/allocating.
            foreach(var integer in new[]{new byte[]{2,0,0,0,0,0},new byte[]{255,255,255,127}})
            {
                try { using var reader=new BinaryReader(new MemoryStream(integer));WireProtocol.ReadBig(reader);throw new Exception("malformed integer accepted"); }
                catch(IOException) { }
            }
            // Claimed huge incomplete state consumes only received bytes, never claimed total size.
            var fake=WireProtocol.EncodeChunk(state.SessionId,9,bytes,0);
            Buffer.BlockCopy(BitConverter.GetBytes(WireProtocol.MaxSnapshotBytes),0,fake,12,4);
            Buffer.BlockCopy(BitConverter.GetBytes(WireProtocol.MaxChunkCount),0,fake,20,4);
            assembly.TryAdd(fake,101,out _);Check(assembly.BufferedBytes==WireProtocol.ChunkPayloadBytes,"unreceived bytes allocated");
            assembly.Reset(state.SessionId+1);Check(assembly.BufferedBytes==0,"epoch leaked incomplete state");
            Console.WriteLine("Wire9 stress (legacy regression): "+bytes.Length+" bytes, "+pages+" pages, 66000 exact FIFO orders, 2049-bit progression, sparse slot256.");
        }
    }
}
