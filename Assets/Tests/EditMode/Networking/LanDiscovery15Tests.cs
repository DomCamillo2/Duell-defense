using System;
using System.Net;
using NUnit.Framework;
namespace DefenseDuell.Networking.Tests
{
    public sealed class LanDiscovery15Tests
    {
        const string Hash="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        static LanLobbyAdvertisement Beacon(ulong sequence=1)=>new LanLobbyAdvertisement{ProcessId=Guid.NewGuid(),Epoch=99,Sequence=sequence,CatalogHash=Hash,
            Port=7777,PlayerCount=2,Capacity=6,Joinable=true,MapName="Aurora-Bucht",FormatName="2v2v2"};
        static LanLobbyDirectory Directory()=>new LanLobbyDirectory(Guid.NewGuid(),Hash);
        [Test] public void CompatibleLobbyUsesDatagramSourceAddressAndExactPort()
        {
            var directory=Directory();var beacon=Beacon();Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Parse("192.168.4.7"),10),Is.True);
            Assert.That(directory.TryGet(beacon.LobbyId,11,out var lobby),Is.True);Assert.That(lobby.HostAddress,Is.EqualTo("192.168.4.7"));
            Assert.That(lobby.Port,Is.EqualTo(7777));Assert.That(lobby.Capacity,Is.EqualTo(6));Assert.That(lobby.FormatName,Is.EqualTo("2v2v2"));
        }
        [Test] public void IncompatibleCatalogProtocolAndOwnProcessAreNeverListed()
        {
            var beacon=Beacon();var packet=LanDiscoveryProtocol.Encode(beacon);
            Assert.That(LanDiscoveryProtocol.TryDecode(packet,new string('b',64),out _),Is.False);
            packet[4]=(byte)(WireProtocol.Version-1);Assert.That(LanDiscoveryProtocol.TryDecode(packet,Hash,out _),Is.False);
            var directory=new LanLobbyDirectory(beacon.ProcessId,Hash);
            Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,0),Is.False);Assert.That(directory.Snapshot().Count,Is.Zero);
        }
        [Test] public void DeduplicationDoesNotLetOldPacketsRenewExpiry()
        {
            var directory=Directory();var beacon=Beacon();var packet=LanDiscoveryProtocol.Encode(beacon);
            Assert.That(directory.Receive(packet,IPAddress.Loopback,10),Is.True);Assert.That(directory.Receive(packet,IPAddress.Loopback,14),Is.False);
            Assert.That(directory.Snapshot().Count,Is.EqualTo(1));Assert.That(directory.Expire(14.6),Is.True);Assert.That(directory.Snapshot().Count,Is.Zero);
        }
        [Test] public void FullOrWithdrawnLobbyCannotReappearFromReorderedBeacon()
        {
            var directory=Directory();var beacon=Beacon();var old=LanDiscoveryProtocol.Encode(beacon);
            directory.Receive(old,IPAddress.Loopback,0);beacon.Sequence=2;beacon.Joinable=false;beacon.PlayerCount=6;
            Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,1),Is.True);
            Assert.That(directory.Receive(old,IPAddress.Loopback,2),Is.False);Assert.That(directory.Snapshot().Count,Is.Zero);
            beacon.Sequence=3;beacon.Joinable=true;beacon.PlayerCount=5;
            Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,3),Is.True);
        }
        [Test] public void VisibleChangesNotifyButHeartbeatsOnlyRefreshTtl()
        {
            var directory=Directory();var beacon=Beacon();directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,0);
            beacon.Sequence++;Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,3),Is.False);
            Assert.That(directory.TryGet(beacon.LobbyId,6,out _),Is.True);
            beacon.Sequence++;beacon.MapName="Smaragdbecken";
            Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,6),Is.True);
            Assert.That(directory.TryGet(beacon.LobbyId,6,out var lobby),Is.True);Assert.That(lobby.MapName,Is.EqualTo("Smaragdbecken"));
        }
        [Test] public void LifecycleClearInvalidatesJoinAndAllReplayState()
        {
            var directory=Directory();var beacon=Beacon();directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,0);
            Assert.That(directory.Clear(),Is.True);Assert.That(directory.TryGet(beacon.LobbyId,0,out _),Is.False);
            Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,1),Is.True);
            Assert.That(directory.Expire(.5),Is.True);Assert.That(directory.Snapshot().Count,Is.Zero);
        }
        [Test] public void DirectoryHasFiniteIdentityBudgetIncludingTombstones()
        {
            var directory=Directory();for(int i=0;i<LanLobbyDirectory.Capacity;i++)directory.Receive(LanDiscoveryProtocol.Encode(Beacon()),IPAddress.Loopback,0);
            Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(Beacon()),IPAddress.Loopback,0),Is.False);
            Assert.That(directory.Snapshot().Count,Is.EqualTo(LanLobbyDirectory.Capacity));directory.Expire(5);Assert.That(directory.Snapshot().Count,Is.Zero);
        }
        [Test] public void MalformedAndUnjoinablePacketsDoNotCreateCandidates()
        {
            var beacon=Beacon();var bytes=LanDiscoveryProtocol.Encode(beacon);
            for(int i=0;i<bytes.Length;i++){var truncated=new byte[i];Array.Copy(bytes,truncated,i);Assert.That(LanDiscoveryProtocol.TryDecode(truncated,Hash,out _),Is.False);}
            Array.Resize(ref bytes,bytes.Length+1);Assert.That(LanDiscoveryProtocol.TryDecode(bytes,Hash,out _),Is.False);
            beacon.Joinable=false;var directory=Directory();Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,0),Is.False);Assert.That(directory.Snapshot().Count,Is.Zero);
            beacon.Joinable=true;beacon.PlayerCount=6;Assert.Throws<ArgumentException>(()=>LanDiscoveryProtocol.Encode(beacon));
        }
        [Test] public void UntrustedLabelsAndImpossibleSourceAddressesAreRejected()
        {
            var beacon=Beacon();beacon.MapName="<size=999>map</size>";Assert.Throws<ArgumentException>(()=>LanDiscoveryProtocol.Encode(beacon));
            beacon.MapName="map\njoin";Assert.Throws<ArgumentException>(()=>LanDiscoveryProtocol.Encode(beacon));beacon.MapName="Map";
            var directory=Directory();var bytes=LanDiscoveryProtocol.Encode(beacon);
            foreach(var source in new[]{IPAddress.Any,IPAddress.Broadcast,IPAddress.IPv6Loopback,IPAddress.Parse("224.0.0.1")})Assert.That(directory.Receive(bytes,source,0),Is.False);
        }
        [Test] public void JoinLookupExpiresEvenBeforeNextUiTick()
        {
            var directory=Directory();var beacon=Beacon();directory.Receive(LanDiscoveryProtocol.Encode(beacon),IPAddress.Loopback,0);
            Assert.That(directory.TryGet(beacon.LobbyId,4,out _),Is.True);Assert.That(directory.TryGet(beacon.LobbyId,5,out _),Is.False);
        }
    }
}
