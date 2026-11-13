using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
namespace DefenseDuell.Networking.Tests
{
    /// <summary>Real local UDP fixtures with an intentionally incompatible test catalog. Not app/phone evidence.</summary>
    public sealed class LanDiscoverySocket15Tests
    {
        const string Hash="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        static Socket ActiveSocket(LanLobbyDiscovery service)=>(Socket)typeof(LanLobbyDiscovery).GetField("socket",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(service);
        static LanLobbyAdvertisement Beacon()=>new LanLobbyAdvertisement{ProcessId=Guid.NewGuid(),Epoch=999,Sequence=1,CatalogHash=Hash,Port=7777,
            PlayerCount=1,Capacity=4,Joinable=true,MapName="TOOLS-UDP-FIXTURE",FormatName="2v2"};
        static LanLobbyAdvertisement Receive(Socket socket)
        {
            var bytes=new byte[513];EndPoint source=new IPEndPoint(IPAddress.Any,0);int count=socket.ReceiveFrom(bytes,ref source);Array.Resize(ref bytes,count);
            Assert.That(LanDiscoveryProtocol.TryDecode(bytes,Hash,out var value),Is.True);return value;
        }
        [Test] public void AdvertiserUsesEphemeralPortEvenWhenDiscoveryReceivePortIsExclusivelyOccupied()
        {
            using var receiver=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
            receiver.Bind(new IPEndPoint(IPAddress.Any,LanDiscoveryProtocol.Port));receiver.ReceiveTimeout=2000;
            using var advertiser=new LanLobbyDiscovery(Hash);
            advertiser.Tick(0,true,null);Assert.That(ActiveSocket(advertiser),Is.Null,"exclusive receiver blocks browsing, not advertising");
            advertiser.Tick(.1,false,Beacon());
            Assert.That(advertiser.Status,Is.Empty);var socket=ActiveSocket(advertiser);Assert.That(socket,Is.Not.Null);
            int port=((IPEndPoint)socket.LocalEndPoint).Port;Assert.That(port,Is.GreaterThan(0));Assert.That(port,Is.Not.EqualTo(LanDiscoveryProtocol.Port));
            var actual=Receive(receiver);Assert.That(actual.Joinable,Is.True);Assert.That(actual.Port,Is.EqualTo(7777));Assert.That(actual.ProcessId,Is.EqualTo(advertiser.ProcessId));
            advertiser.Suspend();var withdrawn=Receive(receiver);Assert.That(withdrawn.Joinable,Is.False);Assert.That(withdrawn.Sequence,Is.GreaterThan(actual.Sequence));
            Assert.That(ActiveSocket(advertiser),Is.Null);
        }
        [Test] public void BrowseAdvertiseAndIdleTransitionsRebindAndClearWithoutReopeningAnUnchangedRole()
        {
            using var service=new LanLobbyDiscovery(Hash);service.Tick(0,true,null);
            var browser=ActiveSocket(service);Assert.That(browser,Is.Not.Null);Assert.That(((IPEndPoint)browser.LocalEndPoint).Port,Is.EqualTo(LanDiscoveryProtocol.Port));
            using var sender=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
            sender.SendTo(LanDiscoveryProtocol.Encode(Beacon()),new IPEndPoint(IPAddress.Loopback,LanDiscoveryProtocol.Port));
            for(int i=0;i<30&&service.Lobbies.Count==0;i++){Thread.Sleep(10);service.Tick(.1+i*.01,true,null);}
            Assert.That(service.Lobbies.Count,Is.EqualTo(1));
            service.Tick(.5,false,Beacon());var advertiser=ActiveSocket(service);
            Assert.That(advertiser,Is.Not.SameAs(browser));Assert.That(((IPEndPoint)advertiser.LocalEndPoint).Port,Is.Not.EqualTo(LanDiscoveryProtocol.Port));Assert.That(service.Lobbies.Count,Is.Zero);
            service.Tick(.6,false,Beacon());Assert.That(ActiveSocket(service),Is.SameAs(advertiser));
            service.Tick(.7,true,null);Assert.That(ActiveSocket(service),Is.Not.SameAs(advertiser));Assert.That(((IPEndPoint)ActiveSocket(service).LocalEndPoint).Port,Is.EqualTo(LanDiscoveryProtocol.Port));
            service.Tick(.8,false,null);Assert.That(ActiveSocket(service),Is.Null);Assert.That(service.Lobbies.Count,Is.Zero);
            service.Suspend();service.Dispose();Assert.That(ActiveSocket(service),Is.Null);
        }
    }
}
