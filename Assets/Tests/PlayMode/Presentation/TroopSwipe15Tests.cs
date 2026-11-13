using System.Collections;
using DefenseDuell.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class TroopSwipe15Tests
    {
        GameObject root,eventRoot;PointerSafeButton button;TroopResearchSwipe swipe;int sent,opened;bool allowed=true,reduced;
        [SetUp] public void Setup()
        {
            if(EventSystem.current==null)eventRoot=new GameObject("Swipe events",typeof(EventSystem));
            root=new GameObject("Swipe canvas",typeof(RectTransform),typeof(Canvas));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var card=new GameObject("Card",typeof(RectTransform),typeof(Image),typeof(PointerSafeButton));card.transform.SetParent(root.transform,false);
            var rt=(RectTransform)card.transform;rt.sizeDelta=new Vector2(184,120);
            var face=new GameObject("Face",typeof(RectTransform),typeof(Image));face.transform.SetParent(card.transform,false);
            button=card.GetComponent<PointerSafeButton>();button.targetGraphic=face.GetComponent<Image>();button.onClick.AddListener(()=>sent++);
            swipe=card.AddComponent<TroopResearchSwipe>();swipe.Initialize(button,()=>sent++,()=>opened++,()=>allowed,()=>reduced);
            Canvas.ForceUpdateCanvases();sent=opened=0;
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);Object.DestroyImmediate(eventRoot);}
        PointerEventData Event(int id=3)=>new PointerEventData(EventSystem.current){pointerId=id,position=RectTransformUtility.WorldToScreenPoint(null,button.transform.position),button=PointerEventData.InputButton.Left,eligibleForClick=true};
        void Down(PointerEventData e){button.OnPointerDown(e);swipe.OnPointerDown(e);}
        void Up(PointerEventData e){button.OnPointerUp(e);swipe.OnPointerUp(e);button.OnPointerClick(e);}
        [Test] public void TapAndJitterSendOnceEachWhileKeyboardKeepsOnePath()
        {var e=Event();Down(e);Up(e);Assert.That(sent,Is.EqualTo(1));e=Event();Down(e);e.position+=new Vector2(3,2);swipe.OnDrag(e);Up(e);Assert.That(sent,Is.EqualTo(2));button.OnSubmit(new BaseEventData(EventSystem.current));Assert.That(sent,Is.EqualTo(3));Assert.That(opened,Is.Zero);}
        [Test] public void LeftSwipeOnlyOpensResearchIncludingReleaseWithoutMove()
        {var e=Event();Down(e);e.position+=Vector2.left*70;Up(e);Assert.That(opened,Is.EqualTo(1));Assert.That(sent,Is.Zero);}
        [Test] public void ReturningDragVerticalAndRightDragNeverPurchase()
        {foreach(var d in new[]{new Vector2(-70,0),new Vector2(0,70),new Vector2(70,0)}){var e=Event();Down(e);var start=e.position;e.position+=d;swipe.OnDrag(e);e.position=start;Up(e);}Assert.That(sent,Is.Zero);Assert.That(opened,Is.Zero);}
        [Test] public void ForeignPointerCannotFinishAndCancelledReleaseCannotBuy()
        {var e=Event();Down(e);var other=Event(4);swipe.OnPointerUp(other);Assert.That(swipe.ContactActive,Is.True);swipe.CancelGesture();Up(e);Assert.That(sent,Is.Zero);Assert.That(opened,Is.Zero);e=Event();Down(e);Up(e);Assert.That(sent,Is.EqualTo(1),"A new contact with a reused pointer ID is valid");}
        [Test] public void ModalOrFocusCancellationKeepsReleaseBlocked()
        {var e=Event();Down(e);allowed=false;swipe.OnDrag(e);allowed=true;Up(e);Assert.That(sent,Is.Zero);e=Event();Down(e);swipe.SendMessage("OnApplicationFocus",false);Up(e);Assert.That(sent,Is.Zero);}
        [Test] public void UnaffordableSendStillAllowsResearchAndReducedEffectsUsesSameRules()
        {button.interactable=false;reduced=true;var e=Event();Down(e);e.position+=Vector2.left*70;swipe.OnDrag(e);Assert.That(((RectTransform)button.targetGraphic.transform).anchoredPosition,Is.EqualTo(Vector2.zero));Up(e);Assert.That(opened,Is.EqualTo(1));Assert.That(sent,Is.Zero);}
    }
}
