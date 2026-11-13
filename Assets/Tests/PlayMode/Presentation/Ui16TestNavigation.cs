using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DefenseDuell.Presentation.Tests
{
 // Legacy acceptance flows explicitly traverse the new real dialog/pages. New Loadout16
 // cases independently assert modal ownership and min/max choices before confirmation.
 public static class Ui16TestNavigation
 {
  static GameUI Ui=>Object.FindFirstObjectByType<GameUI>();
  static void Refresh(){typeof(GameUI).GetMethod("RefreshLive",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Ui,null);Canvas.ForceUpdateCanvases();}
  public static void ConfirmHomeIfOpen()
  {
   var button=Ui?.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="Confirm home loadout");
   if(button==null)return;Canvas.ForceUpdateCanvases();AssertHit(button);button.onClick.Invoke();Canvas.ForceUpdateCanvases();
  }
  public static void Reveal(string name)
  {
   if(Ui==null)return;
   bool build=name.StartsWith("Build class ");bool attack=name.StartsWith("Send class ")||name.StartsWith("Attack info ");
   if(!build&&!attack)return;
   string previous=build?"Previous tower classes":"Previous attack classes",next=build?"Next tower classes":"Next attack classes";
   for(int i=0;i<8;i++)
   {
    var target=Ui.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name);if(target!=null)return;
    var buttons=Ui.GetComponentsInChildren<Button>();var p=buttons.FirstOrDefault(b=>b.name==previous&&b.interactable);
    var n=buttons.FirstOrDefault(b=>b.name==next&&b.interactable);var pager=i==0&&p!=null?p:n;
    if(pager==null)return;AssertHit(pager);pager.onClick.Invoke();Refresh();
   }
  }
  public static void ScrollIntoView(Component control)
  {
   var scroll=control.GetComponentInParent<ScrollRect>();if(scroll==null||scroll.content==null||scroll.viewport==null)return;
   Canvas.ForceUpdateCanvases();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,control.transform);
   float shift=bounds.min.y<scroll.viewport.rect.yMin?scroll.viewport.rect.yMin-bounds.min.y:bounds.max.y>scroll.viewport.rect.yMax?scroll.viewport.rect.yMax-bounds.max.y:0;
   scroll.StopMovement();scroll.content.anchoredPosition+=Vector2.up*shift;Canvas.ForceUpdateCanvases();
  }
  public static void AssertHit(Button button)
  {
   ScrollIntoView(button);var c=button.GetComponentInParent<Canvas>();var cam=c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera;
   var rect=(RectTransform)button.transform;var point=RectTransformUtility.WorldToScreenPoint(cam,rect.TransformPoint(rect.rect.center));
   var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
   Assert.That(hits,Is.Not.Empty,button.name);Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),"Navigation covered: "+button.name);
  }
 }
}
