using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>Only the visible child moves. Its parent remains the stable raycast rectangle.</summary>
    public sealed class ButtonPressFeedback : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,ICancelHandler
    {
        static readonly HashSet<ButtonPressFeedback> instances=new HashSet<ButtonPressFeedback>();
        static bool reduced;
        RectTransform visual;
        Button button;
        float from=1,target=1,began,duration;
        public RectTransform Visual=>visual;
        public void Initialize(RectTransform child,Button owner){visual=child;button=owner;instances.Add(this);ResetVisual();}
        public static void SetReducedEffects(bool value){reduced=value;if(value)foreach(var instance in instances)instance.ResetVisual();}
        public static void ResetAll(){foreach(var instance in instances)instance.ResetVisual();}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&button!=null&&button.IsInteractable()&&UiPointerGate.Allows(e.pointerId))Animate(.97f,.08f);}
        public void OnPointerUp(PointerEventData e)=>Animate(1,.12f);
        public void OnPointerExit(PointerEventData e)=>Animate(1,.12f);
        public void OnCancel(BaseEventData e)=>ResetVisual();
        void Animate(float value,float seconds){if(visual==null)return;if(reduced){ResetVisual();return;}from=visual.localScale.x;target=value;began=Time.unscaledTime;duration=seconds;}
        void Update(){if(visual==null||duration<=0)return;float t=Mathf.Clamp01((Time.unscaledTime-began)/duration);float u=t*t*(3-2*t);visual.localScale=Vector3.one*Mathf.Lerp(from,target,u);if(t>=1){visual.localScale=Vector3.one*target;duration=0;}}
        public void ResetVisual(){from=target=1;duration=0;if(visual!=null)visual.localScale=Vector3.one;}
        void OnDisable()=>ResetVisual();
        void OnDestroy(){ResetVisual();instances.Remove(this);}
    }
}
