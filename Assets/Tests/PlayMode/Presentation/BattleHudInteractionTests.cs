using BigInteger=System.Numerics.BigInteger;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class BattleHudInteractionTests
    {
        SessionCoordinator session;
        GameUI ui;
        int reduced;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        [UnitySetUp] public IEnumerator Start()
        {
            reduced=PlayerPrefs.GetInt("duell.reduced",0);PlayerPrefs.SetInt("duell.reduced",0);
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            session.StartOffline(4);session.State.Phase=MatchPhase.Playing;session.State.WavePhase=WavePhase.Active;session.State.Players[0].Gold=10000;session.enabled=false;
            yield return null;yield return null;Refresh();
        }
        [UnityTearDown] public IEnumerator Stop()
        {session.enabled=true;session.Shutdown();PlayerPrefs.SetInt("duell.reduced",reduced);yield return null;}
        void Refresh(){Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();}
        static Button Find(string name)
        {
            Ui16TestNavigation.Reveal(name);
            foreach(var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name==name&&b.gameObject.activeInHierarchy)return b;
            Assert.Fail("Missing "+name);return null;
        }
        static Vector2 Center(Component component)
        {var r=(RectTransform)component.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        static void Tap(Vector2 position,int id=900)
        {
            var e=new PointerEventData(EventSystem.current){pointerId=id,position=position,pressPosition=position,button=PointerEventData.InputButton.Left,eligibleForClick=true};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);Assert.That(hits,Is.Not.Empty);
            e.pointerPress=hits[0].gameObject;
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerClickHandler);
        }
        BattleInfoPanel Info=>Field<BattleInfoPanel>(ui,"attackInfo");
        static void CaptureHud(string name)
        {
            var canvas = Object.FindFirstObjectByType<GameUI>().GetComponentInChildren<Canvas>();
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
            canvas = canvas.rootCanvas;
            var originalMode = canvas.renderMode; var originalCamera = canvas.worldCamera; float distance = canvas.planeDistance;
            var go = new GameObject("HUD proof camera"); var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f,.15f,.13f);
            camera.orthographic = true; camera.transform.position = new Vector3(0,0,-100);
            var rt = new RenderTexture(Screen.width, Screen.height, 24); rt.Create(); camera.targetTexture = rt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            var folder = System.IO.Path.GetFullPath("TestResults/Hud-v15"); System.IO.Directory.CreateDirectory(folder);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),image.EncodeToPNG());
            RenderTexture.active = previous; canvas.renderMode = originalMode;canvas.worldCamera=originalCamera;canvas.planeDistance=distance;
            camera.targetTexture=null;Object.Destroy(image);rt.Release();Object.Destroy(rt);Object.Destroy(go);Canvas.ForceUpdateCanvases();
        }

        [UnityTest] public IEnumerator TempoButtonUsesConfirmedStateAndInfoInvalidatesHeldPress()
        {
            var tempo = Find("Match tempo");
            Assert.That(tempo.GetComponent<ActionButtonVisual>().Artwork.GetComponent<CanvasRenderer>(),Is.Not.Null);
            Tap(Center(tempo), 950); Refresh();
            Assert.That(session.State.SimulationSpeed, Is.EqualTo(2)); CaptureHud("tempo-2x");
            Assert.That(tempo.GetComponent<ActionButtonVisual>().ValueText, Is.EqualTo("Zurück zu 1×"));
            Tap(Center(tempo), 951); Refresh(); Assert.That(session.State.SimulationSpeed, Is.EqualTo(1));
            var held = new PointerEventData(EventSystem.current) { pointerId = 952, position = Center(tempo), button = PointerEventData.InputButton.Left };
            var guarded = tempo.GetComponent<PointerSafeButton>(); guarded.OnPointerDown(held);
            Call(ui, "ToggleAttackInfo", 0); Call(ui, "CloseAttackInfo");
            guarded.OnPointerUp(held); guarded.OnPointerClick(held);
            Assert.That(session.State.SimulationSpeed, Is.EqualTo(1));
            Tap(Center(tempo), 952); Refresh(); Assert.That(session.State.SimulationSpeed, Is.EqualTo(2));
            yield return null;
        }

        [UnityTest] public IEnumerator MoneyThresholdsGateCardsAndIndependentDragHandler()
        {
            for(int group=0;group<2;group++)for(int kind=0;kind<(group==0?5:4);kind++)
            {
                var price=group==0?session.Content.Towers[kind].Price:session.Content.Units[kind].Price;
                var button=Find((group==0?"Build class ":"Send class ")+kind);
                for(int delta=-1;delta<=1;delta++)
                {
                    session.State.Players[0].Gold=price+delta;Refresh();
                    Assert.That(button.interactable,Is.EqualTo(delta>=0));
                    Assert.That(button.GetComponent<PurchaseAvailability>().State,Is.EqualTo(delta<0?PurchaseState.Unaffordable:PurchaseState.Available));
                    if(group==0&&delta<0)
                    {
                        ulong before=session.LastResult.Sequence;var source=button.GetComponent<BuildDragSource>();
                        var e=new PointerEventData(EventSystem.current){pointerId=100+kind,position=Center(button),button=PointerEventData.InputButton.Left};
                        source.OnPointerDown(e);e.position+=Vector2.up*100;e.dragging=true;source.OnBeginDrag(e);source.OnDrag(e);source.OnEndDrag(e);source.OnPointerUp(e);
                        Assert.That(Field<BuildDragSource>(ui,"dragSource"),Is.Null);Assert.That(session.LastResult.Sequence,Is.EqualTo(before));
                    }
                }
            }
            session.State.Players[0].Gold=0;Refresh();Tap(Center(Find("Attack info 0")));Assert.That(Info.Open,Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator InfoConsumesOutsideTapAndNeverPurchasesEvenWithHeldSecondPointer()
        {
            var send=Find("Send class 0");var held=new PointerEventData(EventSystem.current){pointerId=804,position=Center(send),button=PointerEventData.InputButton.Left,eligibleForClick=true};
            ExecuteEvents.Execute(send.gameObject,held,ExecuteEvents.pointerDownHandler);
            var gold=session.State.Players[0].Gold;ulong seq=session.LastResult.Sequence;
            Assert.That(send.GetComponent<TroopResearchSwipe>().ContactActive,Is.True,"Exercise an actually held troop gesture");
            Tap(Center(Find("Attack info 0")),805);Assert.That(Info.Blocking,Is.True);
            Assert.That(send.GetComponent<TroopResearchSwipe>().ContactActive,Is.False,"Opening details cancels the held purchase gesture");
            ExecuteEvents.Execute(send.gameObject,held,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(send.gameObject,held,ExecuteEvents.pointerClickHandler);
            Assert.That(session.LastResult.Sequence,Is.EqualTo(seq));
            Tap(Center(Find("Attack info 2")),806);Assert.That(Field<int>(ui,"infoKind"),Is.EqualTo(2));
            Tap(Center(Field<RectTransform>(ui,"attackInfoContent")),807);Assert.That(Info.Open,Is.True,"Inside tap must not dismiss");
            Tap(Center(send),808);Assert.That(Info.Open,Is.False);Assert.That(Info.Blocking,Is.True,"Shield survives closing animation");
            Tap(Center(send),809);Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders,Is.Empty);
            yield return new WaitForSecondsRealtime(.2f);Assert.That(Info.Blocking,Is.False);
            Tap(Center(send),810);Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-session.Content.Units[0].Price));Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RapidInfoSwitchReducedEffectsBackFocusAndRebuildLeaveNoShield()
        {
            var gold=session.State.Players[0].Gold;
            for(int i=0;i<12;i++){if(i%4==3||i>0&&i%4==0){Call(ui,"CloseAttackInfo");yield return null;}var infoButton=Find("Attack info "+(i%4));yield return null;Tap(Center(infoButton),900+i);yield return null;}
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders,Is.Empty);
            var settings=Field<LocalSettings>(ui,"settings");settings.SetReduced(true);Call(ui,"ApplyVisualSettings");
            Assert.That(Info.Progress,Is.EqualTo(1));Call(ui,"Back");Assert.That(Info.Blocking,Is.False);Assert.That(Field<RectTransform>(ui,"modal"),Is.Null);
            Tap(Center(Find("Attack info 1")));Call(ui,"OnApplicationFocus",false);Assert.That(Info.Blocking,Is.False);
            Tap(Center(Find("Attack info 2")));Call(ui,"Rebuild");Assert.That(Info.Blocking,Is.False);
            foreach(var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name.StartsWith("Attack info "))
            {var rect=(RectTransform)b.transform;Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(44));Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44));}
            yield return null;
        }
        [UnityTest] public IEnumerator HeldPurchaseCannotSurviveInfoCloseReducedEffectsOrFocusLoss()
        {
            var settings=Field<LocalSettings>(ui,"settings");var send=Find("Send class 0");
            for(int mode=0;mode<3;mode++)
            {
                settings.SetReduced(mode==1);Call(ui,"ApplyVisualSettings");
                var gold=session.State.Players[0].Gold;int orders=session.State.SpawnOrders.Count;ulong seq=session.LastResult.Sequence;
                var held=new PointerEventData(EventSystem.current){pointerId=1200+mode,position=Center(send),button=PointerEventData.InputButton.Left,eligibleForClick=true};
                ExecuteEvents.Execute(send.gameObject,held,ExecuteEvents.pointerDownHandler);
                Assert.That(send.GetComponent<TroopResearchSwipe>().ContactActive,Is.True);
                Tap(Center(Find("Attack info 0")),1300+mode);Assert.That(Info.Open,Is.True);
                Assert.That(send.GetComponent<TroopResearchSwipe>().ContactActive,Is.False);
                if(mode==2)Call(ui,"OnApplicationFocus",false);else Tap(Center(Find("Attack info 0")),1400+mode);
                yield return new WaitForSecondsRealtime(.2f);Assert.That(Info.Blocking,Is.False);
                ExecuteEvents.Execute(send.gameObject,held,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(send.gameObject,held,ExecuteEvents.pointerClickHandler);
                Assert.That(session.LastResult.Sequence,Is.EqualTo(seq));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(orders));
                Tap(Center(send),held.pointerId);Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-session.Content.Units[0].Price));Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(orders+1));
            }
        }
        [UnityTest] public IEnumerator UpgradeUsesCurrentVariantAndTurnsGreyBelowExactPrice()
        {
            MapPoint point=default;bool found=false;
            for(int y=80;y<800&&!found;y+=67)for(int x=80;x<1500;x+=73)
                if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,0,-1,new MapPoint(x,y))==ErrorCode.None){point=new MapPoint(x,y);found=true;break;}
            Assert.That(found,Is.True);session.Submit(CommandType.BuildTower,0,-1,point.X,point.Y);
            var tower=session.State.Towers[0];Call(ui,"SelectEntity",tower.Id);Refresh();
            yield return new WaitForSecondsRealtime(.1f); // Newly activated context must acquire native Canvas depth.
            var price=TowerUpgradeRules.Quote(session.Content,tower,TowerUpgradePath.Range).Price;
            session.State.Players[0].Gold=price-1;Refresh();Assert.That(Find("Upgrade range").interactable,Is.False);CaptureHud("upgrade-unaffordable");
            session.State.Players[0].Gold=price;Refresh();var upgrade=Find("Upgrade range");Assert.That(upgrade.interactable,Is.True);CaptureHud("upgrade-affordable");
            Tap(Center(upgrade));Refresh();Assert.That(tower.RangeLevel,Is.EqualTo((BigInteger)1));Assert.That(session.State.Players[0].Gold,Is.EqualTo(BigInteger.Zero));
            Assert.That(Field<Image>(ui,"selectionPortrait").sprite,Is.SameAs(GeneratedClassIcons.GetTowerEvolution(tower.Kind,tower.Kind==4?tower.Level:tower.DamageLevel,tower.RangeLevel)));
            session.State.Players[0].Gold=1000;Refresh();Tap(Center(Find("Upgrade damage")),980);Refresh();
            Assert.That(tower.Level,Is.EqualTo((BigInteger)2));Assert.That(Find("Upgrade damage").interactable,Is.True);
            var next=TowerUpgradeRules.Quote(session.Content,tower,TowerUpgradeRules.PrimaryPath(session.Content,tower.Kind));
            var before=session.State.Players[0].Gold;Tap(Center(Find("Upgrade damage")),981);Refresh();
            Assert.That(tower.Level,Is.EqualTo((BigInteger)3));Assert.That(session.State.Players[0].Gold,Is.EqualTo(before-next.Price));CaptureHud("upgrade-beyond-two");
            yield return null;
        }
    }
}
