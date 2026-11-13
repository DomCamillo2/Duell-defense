using System;
using System.Collections;
using System.Reflection;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace DefenseDuell.Presentation.Tests
{
 public sealed class Loadout16Tests
 {
  const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  GameUI ui;SessionCoordinator session;
  T Field<T>(string name)=>(T)typeof(GameUI).GetField(name,Flags).GetValue(ui);
  void Call(string name,params object[] args)=>typeof(GameUI).GetMethod(name,Flags).Invoke(ui,args);
  [UnityTest] public IEnumerator SixthChoiceIsRejectedAndRemovingThenAddingWorks()
  {
   yield return SceneManager.LoadSceneAsync("Boot");yield return null;yield return null;
   ui=UnityEngine.Object.FindFirstObjectByType<GameUI>();session=UnityEngine.Object.FindFirstObjectByType<SessionCoordinator>();
   Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));int old=session.LocalTowerMask;
   try
   {
    Assert.That(session.SetTowerAvailability(31),Is.True);bool continued=false;
    Call("OpenHomeLoadout",new Action(()=>continued=true));yield return null;
    var choices=Field<Button[]>("homeLoadoutChoices");Assert.That(choices.Length,Is.EqualTo(8));
    choices[7].onClick.Invoke();Assert.That(Field<int>("homeDraftTowerMask"),Is.EqualTo(31));
    StringAssert.Contains("5 / 5",Field<Text>("homeLoadoutCounter").text);
    choices[0].onClick.Invoke();choices[7].onClick.Invoke();Assert.That(Field<int>("homeDraftTowerMask"),Is.EqualTo(158));
    Field<Button>("homeLoadoutContinue").onClick.Invoke();Assert.That(continued,Is.True);Assert.That(session.LocalTowerMask,Is.EqualTo(158));
    Call("OpenHomeLoadout",new Action(()=>{}));choices=Field<Button[]>("homeLoadoutChoices");
    foreach(int kind in new[]{1,2,3,4})choices[kind].onClick.Invoke();choices[7].onClick.Invoke();
    Assert.That(Field<int>("homeDraftTowerMask"),Is.EqualTo(128),"last selection remains");
   }
   finally{Call("CloseModal");session.SetTowerAvailability(old);}
  }
 }
}
