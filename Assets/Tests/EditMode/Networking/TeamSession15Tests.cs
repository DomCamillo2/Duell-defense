#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
namespace DefenseDuell.Networking.Tests
{
    /// <summary>Native Session/codec fixtures; approved peer records are arranged without claiming real NGO connections.</summary>
    public sealed class TeamSession15Tests
    {
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static object Field(object instance,string name)=>instance.GetType().GetField(name,Private|BindingFlags.Public).GetValue(instance);
        static void Set(object instance,string name,object value)
        {var field=instance.GetType().GetField(name,Private|BindingFlags.Public);if(field!=null)field.SetValue(instance,value);else instance.GetType().GetProperty(name,Private|BindingFlags.Public).SetValue(instance,value);}
        static object Call(object instance,string method,params object[] args)=>instance.GetType().GetMethod(method,Private).Invoke(instance,args);
        static SessionCoordinator Add(GameObject go)=>go.AddComponent<SessionCoordinator>();
        static byte[] ApproveAndEncode(SessionCoordinator host,ulong clientId)
        {
            var approve=typeof(SessionCoordinator).GetMethod("Approve",Private);var parameters=approve.GetParameters();
            var request=Activator.CreateInstance(parameters[0].ParameterType);var response=Activator.CreateInstance(parameters[1].ParameterType);
            Set(request,"ClientNetworkId",clientId);Set(request,"Payload",WireProtocol.Handshake(host.CatalogHash,31));approve.Invoke(host,new[]{request,response});
            Assert.That(Field(response,"Approved"),Is.True);
            var peer=((IDictionary)Field(host,"peers"))[clientId];return (byte[])Call(host,"EncodePeerEnvelope",peer,(uint)1);
        }
        [Test] public void ReplacementLobbyPeerContinuesExactSequenceAndLaterSnapshotsCannotResetIt()
        {
            var hostGo=new GameObject("Team15AuthorityFixture");var guestGo=new GameObject("Team15ReplacementFixture");
            try
            {
                var host=Add(hostGo);var guest=Add(guestGo);var content=GameContent.CreateDefault();host.Initialize(content);guest.Initialize(content);
                var sim=new MatchSimulation(content,7,123);Set(host,"simulation",sim);Set(host,"publicationSequence",(ulong)10);
                for(int i=0;i<3;i++)Assert.That(WireFixtures.Execute(sim,PlayerSlot.One,CommandType.SetReady,i%2).Accepted,Is.True);
                Assert.That(sim.ConfigureConnection(PlayerSlot.One,false),Is.True);Assert.That(sim.ConfigureConnection(PlayerSlot.One,true),Is.True);
                var bytes=ApproveAndEncode(host,12345);Assert.That((bool)Call(guest,"ApplyStateEnvelope",bytes,(uint)1),Is.True);
                Assert.That((ulong)Field(guest,"localSequence"),Is.EqualTo(sim.LastSequence(PlayerSlot.One)));
                ulong baseline=(ulong)Field(guest,"localSequence");var next=new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=baseline+1,Type=CommandType.SetReady,Target=-1,ExpectedConfigRevision=sim.State.ConfigRevision,ExpectedRosterRevision=sim.State.RosterRevision};
                Assert.That(sim.Execute(PlayerSlot.One,next).Accepted,Is.True);
                Set(guest,"localSequence",baseline+5);Set(host,"publicationSequence",(ulong)11);
                var peer=((IDictionary)Field(host,"peers"))[(ulong)12345];bytes=(byte[])Call(host,"EncodePeerEnvelope",peer,(uint)2);
                Assert.That((bool)Call(guest,"ApplyStateEnvelope",bytes,(uint)2),Is.True);Assert.That((ulong)Field(guest,"localSequence"),Is.EqualTo(baseline+5));
            }
            finally{UnityEngine.Object.DestroyImmediate(guestGo);UnityEngine.Object.DestroyImmediate(hostGo);}
        }
        [Test] public void NewPeerCanJoinExistingRematchEpochWithoutClaimingAnObservedTransition()
        {
            var hostGo=new GameObject("Team15RematchAuthorityFixture");var guestGo=new GameObject("Team15FreshJoinFixture");
            try
            {
                var host=Add(hostGo);var guest=Add(guestGo);var content=GameContent.CreateDefault();host.Initialize(content);guest.Initialize(content);
                var sim=new MatchSimulation(content,7,124);Set(host,"simulation",sim);Set(host,"publicationSequence",(ulong)100);Set(host,"previousEpoch",(ulong)123);
                var bytes=ApproveAndEncode(host,54321);var catalog=MapCatalog.CreateContents(content);
                Assert.That(WireProtocol.TryDecodeWelcome(bytes,host.CatalogHash,catalog,out _,out _,out _,out var prior,out _,out _,out _),Is.True);Assert.That(prior,Is.Zero);
                Assert.That((bool)Call(guest,"ApplyStateEnvelope",bytes,(uint)1),Is.True);Assert.That(guest.State.SessionId,Is.EqualTo(124));
                var peer=((IDictionary)Field(host,"peers"))[(ulong)54321];Set(peer,"JoinEpoch",(ulong)123);
                bytes=(byte[])Call(host,"EncodePeerEnvelope",peer,(uint)2);
                Assert.That(WireProtocol.TryDecodeWelcome(bytes,host.CatalogHash,catalog,out _,out _,out _,out prior,out _,out _,out _),Is.True);Assert.That(prior,Is.EqualTo(123));
            }
            finally{UnityEngine.Object.DestroyImmediate(guestGo);UnityEngine.Object.DestroyImmediate(hostGo);}
        }
    }
}
#endif
