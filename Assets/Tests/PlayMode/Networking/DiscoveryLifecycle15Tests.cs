using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace DefenseDuell.Networking.Tests
{
    public sealed class DiscoveryLifecycle15Tests
    {
        static object Field(object instance,string name)=>instance.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(instance);
        [UnityTest] public IEnumerator BrowsingFlagChangesNeverSuspendAnActiveHostLobbyAdvertisement()
        {
            // Own isolated LAN host only; this fixture does not claim a connected multiplayer game.
            var go=new GameObject("Discovery15HostLifecycle");var session=go.AddComponent<SessionCoordinator>();
            try
            {
                session.Initialize(GameContent.CreateDefault());ushort port;
                using(var reservation=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp)){reservation.Bind(new IPEndPoint(IPAddress.Loopback,0));port=(ushort)((IPEndPoint)reservation.LocalEndPoint).Port;}
                session.StartLanHost(port);
                for(int frame=0;frame<180&&session.Status!=ConnectionState.Lobby;frame++)yield return null;
                Assert.That(session.Status,Is.EqualTo(ConnectionState.Lobby));yield return null;
                var discovery=(LanLobbyDiscovery)Field(session,"discovery");var socket=(Socket)Field(discovery,"socket");
                Assert.That(socket,Is.Not.Null);Assert.That(((IPEndPoint)socket.LocalEndPoint).Port,Is.Not.EqualTo(LanDiscoveryProtocol.Port));
                for(int i=0;i<10;i++)session.LanDiscoveryEnabled=false;
                Assert.That(Field(discovery,"socket"),Is.SameAs(socket),"repeated UI false is a no-op");
                session.LanDiscoveryEnabled=true;session.LanDiscoveryEnabled=false;
                Assert.That(Field(discovery,"socket"),Is.SameAs(socket),"turning off browsing must preserve host advertising");
                yield return null;Assert.That(Field(discovery,"socket"),Is.SameAs(socket));
            }
            finally{session.Shutdown();Object.DestroyImmediate(go);}
        }
    }
}
