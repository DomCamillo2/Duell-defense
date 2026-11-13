using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class ConnectPresentationTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameUI ui;SessionCoordinator session;
        static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Private).GetValue(o);
        static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Private).Invoke(o,args);
        Button Button(string name)=>ui.GetComponentsInChildren<Button>().Single(b=>b.name==name);
        InputField Input(string name)=>ui.GetComponentsInChildren<InputField>().Single(b=>b.name==name);
        static GameObject Hit(Component target)
        {
            var rect=(RectTransform)target.transform;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Assert.That(hits,Is.Not.Empty,target.name);return hits[0].gameObject;
        }
        static void Tap(Button button)
        {
            var hit=Hit(button);Assert.That(hit.GetComponentInParent<Button>(),Is.SameAs(button),"Covered: "+button.name);
            var rect=(RectTransform)button.transform;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,pressPosition=point,pointerId=2401,
                button=PointerEventData.InputButton.Left,eligibleForClick=true};
            ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.pointerClickHandler);
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Boot");yield return null;yield return null;
            ui=Object.FindFirstObjectByType<GameUI>();session=Object.FindFirstObjectByType<SessionCoordinator>();
            Assert.That(ui,Is.Not.Null);Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));
            Tap(Button("Home lan"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(ui!=null)Call(ui,"CloseModal");if(session!=null)session.Shutdown();
            if(ui!=null)Object.Destroy(ui.gameObject);yield return null;
        }
        IEnumerator Layout(float fullWidth,float left=0,float right=0)
        {
            var safe=Field<RectTransform>(ui,"safe");var root=(RectTransform)safe.parent;
            safe.anchorMin=safe.anchorMax=safe.pivot=new Vector2(.5f,.5f);
            safe.sizeDelta=new Vector2(fullWidth-left-right,720);
            float scale=Mathf.Min(root.rect.width/fullWidth,root.rect.height/720)*.94f;
            safe.localScale=Vector3.one*scale;safe.anchoredPosition=new Vector2((left-right)*.5f*scale,0);
            Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");yield return null;yield return null;Canvas.ForceUpdateCanvases();
        }
        static float Luminance(Color c)
        {var x=c.linear;return .2126f*x.r+.7152f*x.g+.0722f*x.b;}
        static float Contrast(Color a,Color b)
        {float x=Luminance(a),y=Luminance(b);return (Mathf.Max(x,y)+.05f)/(Mathf.Min(x,y)+.05f);}
        void AssertLayout()
        {
            var safe=Field<RectTransform>(ui,"safe");var corners=new Vector3[4];
            foreach(var selectable in ui.GetComponentsInChildren<Selectable>())
            {
                var rect=(RectTransform)selectable.transform;Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44),selectable.name);
                rect.GetWorldCorners(corners);
                foreach(var corner in corners)
                {var p=safe.InverseTransformPoint(corner);Assert.That(p.x,Is.InRange(safe.rect.xMin-.5f,safe.rect.xMax+.5f),selectable.name);Assert.That(p.y,Is.InRange(safe.rect.yMin-.5f,safe.rect.yMax+.5f),selectable.name);}
                if(selectable is Button button)Assert.That(Hit(button).GetComponentInParent<Button>(),Is.SameAs(button));
                else if(selectable is InputField input)Assert.That(Hit(input).GetComponentInParent<InputField>(),Is.SameAs(input));
            }
            foreach(var text in ui.GetComponentsInChildren<Text>())
            {
                if(string.IsNullOrEmpty(text.text)||text.GetComponentInParent<InputField>()!=null)continue;
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),"Clipped text: "+text.text);
            }
            var address=Input("Connect address");Assert.That(Contrast(address.textComponent.color,address.GetComponent<Image>().color),Is.GreaterThan(4.5f));
            Assert.That(Contrast(((Text)address.placeholder).color,address.GetComponent<Image>().color),Is.GreaterThan(4.5f));
            Assert.That(InputFieldsOnScreen(),Is.EqualTo(1),"Advanced port stays out of primary form");
            Assert.That(ui.GetComponentsInChildren<BoardView>().Length,Is.Zero,"Connect must not keep a menu 3D render target alive");
        }
        int InputFieldsOnScreen()=>ui.GetComponentsInChildren<InputField>().Length;
        [UnityTest] public IEnumerator LanAndOnlineHaveReadableSafeTouchTargetsAtNarrowWideAndAsymmetricPhoneSizes()
        {
            foreach(string mode in new[]{"lan","online"})
            {
                Call(ui,"ShowConnect",mode);yield return null;yield return null;
                foreach(float w in new[]{900f,1280f,2048f/955*720})
                {
                    bool phone=w>1500;yield return Layout(w,phone?86:0,phone?24:0);
                    AssertLayout();Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));Assert.That(session.State,Is.Null);
                    Capture(mode+(phone?"-asymmetric-phone":"-"+(int)w),w,phone?86:0,phone?24:0,phone?2048:Mathf.RoundToInt(w*1.5f),phone?955:1080);
                }
            }
        }
        [UnityTest] public IEnumerator HelpPreservesEditedFormsBlocksBackgroundAndReturnsFocus()
        {
            yield return Layout(900);Input("Connect address").text="192.168.7.42";
            var help=Button("Connect help");EventSystem.current.SetSelectedGameObject(help.gameObject);Tap(help);yield return null;
            Assert.That(Field<RectTransform>(ui,"modal"),Is.Not.Null);Input("Connect port").text="23456";
            var create=Button("Connect create");Assert.That(Hit(create).GetComponentInParent<Button>(),Is.Not.SameAs(create));
            ExecuteEvents.Execute(create.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));Assert.That(session.State,Is.Null);
            Capture("lan-help",900,0,0,1350,1080);
            Tap(Button("×"));yield return null;Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(help.gameObject));
            Call(ui,"Rebuild");yield return null;yield return null;Assert.That(Input("Connect address").text,Is.EqualTo("192.168.7.42"));
            Tap(Button("Connect help"));yield return null;Assert.That(Input("Connect port").text,Is.EqualTo("23456"));Tap(Button("×"));yield return null;
            Tap(Button("Zurück"));yield return null;yield return null;Tap(Button("Home online"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;yield return null;
            Input("Connect address").text="AbC7xy";Call(ui,"Rebuild");yield return null;yield return null;
            Assert.That(Input("Connect address").text,Is.EqualTo("AbC7xy"));
            Tap(Button("Connect settings"));yield return null;Assert.That(Field<RectTransform>(ui,"modal"),Is.Not.Null);Tap(Button("×"));yield return null;
            Assert.That(Input("Connect address").text,Is.EqualTo("AbC7xy"));
        }
        [UnityTest] public IEnumerator InvalidPortAddressAndCodeStayOnFormWithoutStartingTransportOrServices()
        {
            yield return Layout(900);Tap(Button("Connect help"));yield return null;Input("Connect port").text="0";Tap(Button("×"));yield return null;
            Tap(Button("Connect create"));yield return null;Assert.That(Field<string>(ui,"notice"),Does.Contain("65535"));
            Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));Assert.That(session.State,Is.Null);
            Tap(Button("Connect help"));yield return null;Input("Connect port").text="7777";Tap(Button("×"));yield return null;
            Input("Connect address").text="not-an-ip";Tap(Button("Connect join"));yield return null;
            Assert.That(session.Message,Does.Contain("Host-IP"));Assert.That(Field<Text>(ui,"noticeText").text,Does.Contain("Host-IP"));Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));
            Tap(Button("Zurück"));yield return null;yield return null;Tap(Button("Home online"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;yield return null;
            Input("Connect address").text="a";Tap(Button("Connect join"));yield return null;yield return null;
            Assert.That(session.Message,Does.Contain("Beitrittscode"));Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));
            Assert.That(session.RegisteredMessageHandlers,Is.Zero);Assert.That(session.State,Is.Null);Assert.That(ui.CurrentScreen,Is.EqualTo("connect"));
        }
        void Capture(string name,float fullWidth,float left,float right,int pw,int ph)
        {
            var canvas=ui.GetComponentInChildren<Canvas>().rootCanvas;var safe=Field<RectTransform>(ui,"safe");
            var scale=safe.localScale;var position=safe.anchoredPosition;float factor=canvas.scaleFactor,distance=canvas.planeDistance;
            var scaler=canvas.GetComponent<CanvasScaler>();bool enabled=scaler.enabled;var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;var active=RenderTexture.active;
            var go=new GameObject("Connect v13 proof camera");var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=new Vector3(0,0,-100);
            var target=new RenderTexture(pw,ph,24);target.Create();camera.targetTexture=target;Texture2D image=null;
            try
            {
                scaler.enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;canvas.scaleFactor=ph/720f;
                safe.localScale=Vector3.one;safe.anchoredPosition=new Vector2((left-right)*.5f,0);Canvas.ForceUpdateCanvases();
                Assert.That(((RectTransform)canvas.transform).rect.width,Is.EqualTo(fullWidth).Within(1));camera.Render();RenderTexture.active=target;
                image=new Texture2D(pw,ph,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,pw,ph),0,0);image.Apply();
                var folder=Path.GetFullPath("TestResults/Connect-v15");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
                var edge=image.GetPixel(2,ph/2);Assert.That(edge.b,Is.GreaterThan(edge.r),"Safe-inset gutter must stay navy");
            }
            finally
            {
                RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;canvas.scaleFactor=factor;
                safe.localScale=scale;safe.anchoredPosition=position;scaler.enabled=enabled;camera.targetTexture=null;
                if(image!=null)Object.Destroy(image);target.Release();Object.Destroy(target);Object.Destroy(go);Canvas.ForceUpdateCanvases();
            }
        }
    }
}
