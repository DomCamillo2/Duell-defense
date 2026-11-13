using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DefenseDuell.Presentation
{
    /// <summary>A stationary hit target: dragging only reveals research, never purchases anything.</summary>
    public sealed class TroopResearchSwipe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, ICancelHandler
    {
        PointerSafeButton button;
        RectTransform hit, face;
        Func<bool> allowed, reduced;
        Action openResearch;
        int pointer;
        bool held, moved, pendingTap;
        Vector2 origin, faceOrigin;
        float returnStarted;
        Vector2 returnFrom;
        public bool ContactActive=>held;
        public bool ContactMoved=>moved;
        public void Initialize(PointerSafeButton owner, Action tap, Action research, Func<bool> mayInteract, Func<bool> reducedEffects)
        {
            button=owner;hit=(RectTransform)transform;face=(RectTransform)UiKit.ButtonContent(owner);
            faceOrigin=face.anchoredPosition;allowed=mayInteract;reduced=reducedEffects;openResearch=research;
            // Pointer and keyboard each have one purchase path; no duplicate onClick listener.
            button.PointerClickOverride=e=>{if(pendingTap&&e.pointerId==pointer&&Allowed){pendingTap=false;tap?.Invoke();}};
        }
        bool Allowed=>isActiveAndEnabled&&(allowed==null||allowed())&&UiPointerGate.AllowsTarget(this);
        public void OnPointerDown(PointerEventData e)
        {
            if(held||e.button!=PointerEventData.InputButton.Left||!Allowed||!UiPointerGate.Claim(this,e.pointerId))return;
            pointer=e.pointerId;held=true;moved=pendingTap=false;returnStarted=0;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hit,e.position,e.pressEventCamera,out origin);
        }
        public void OnInitializePotentialDrag(PointerEventData e){e.useDragThreshold=false;}
        public void OnBeginDrag(PointerEventData e){OnDrag(e);}
        Vector2 Delta(PointerEventData e)
        {RectTransformUtility.ScreenPointToLocalPointInRectangle(hit,e.position,e.pressEventCamera,out var p);return p-origin;}
        void Observe(PointerEventData e)
        {
            var delta=Delta(e);
            if(delta.sqrMagnitude>=144){moved=true;button.InvalidatePointer(e.pointerId);e.eligibleForClick=false;UiPointerGate.SuppressClick=true;}
            if(moved&&face!=null&&!(reduced?.Invoke()??false))
                face.anchoredPosition=faceOrigin+new Vector2(Mathf.Clamp(delta.x,-Mathf.Min(96,hit.rect.width*.5f),0),0);
        }
        public void OnDrag(PointerEventData e)
        {if(!held||e.pointerId!=pointer)return;if(!Allowed){CancelGesture();return;}Observe(e);}
        public void OnPointerUp(PointerEventData e)
        {
            if(!held||e.pointerId!=pointer)return;
            Observe(e);var d=Delta(e);
            bool open=Allowed&&moved&&d.x<=-Mathf.Max(48,hit.rect.width*.3f)&&-d.x>Mathf.Abs(d.y)*1.35f;
            pendingTap=Allowed&&!moved&&RectTransformUtility.RectangleContainsScreenPoint(hit,e.position,e.pressEventCamera);
            held=false;UiPointerGate.Release(this);ReturnFace();
            if(open)openResearch?.Invoke();
        }
        public void OnEndDrag(PointerEventData e){if(held&&e.pointerId==pointer)OnPointerUp(e);}
        void ReturnFace(){if(face==null)return;if(reduced?.Invoke()??false){face.anchoredPosition=faceOrigin;returnStarted=0;}else{returnFrom=face.anchoredPosition;returnStarted=Time.unscaledTime+0.00001f;}}
        void Update()
        {
            if(held&&!Allowed)CancelGesture();
            if(returnStarted<=0||face==null)return;
            float t=Mathf.Clamp01((Time.unscaledTime-returnStarted)/.18f);face.anchoredPosition=Vector2.Lerp(returnFrom,faceOrigin,t*t*(3-2*t));
            if(t>=1)returnStarted=0;
        }
        public void CancelGesture()
        {if(held&&button!=null)button.InvalidatePointer(pointer);held=pendingTap=false;UiPointerGate.Release(this);returnStarted=0;if(face!=null)face.anchoredPosition=faceOrigin;}
        public void OnCancel(BaseEventData e)=>CancelGesture();
        void OnApplicationFocus(bool focused){if(!focused)CancelGesture();}
        void OnApplicationPause(bool paused){if(paused)CancelGesture();}
        void OnDisable()=>CancelGesture();
    }
}
