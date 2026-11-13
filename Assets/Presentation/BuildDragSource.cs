using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace DefenseDuell.Presentation
{
    public sealed class BuildDragSource : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,ICancelHandler
    {
        GameUI ui;
        int kind,pointer;
        bool pressed,dragging;
        Vector2 origin;
        float scale=1;
        public void Initialize(GameUI owner,int towerKind){ui=owner;kind=towerKind;var canvas=GetComponentInParent<Canvas>();scale=canvas!=null?canvas.scaleFactor:1;}
        public void OnPointerDown(PointerEventData e)
        {
            if(pressed||e.button!=PointerEventData.InputButton.Left||ui==null||!ui.BeginBuildPress(this,kind,e))return;
            pressed=true;dragging=false;pointer=e.pointerId;origin=e.position;
        }
        public void OnInitializePotentialDrag(PointerEventData e){if(pressed&&e.pointerId==pointer)e.useDragThreshold=false;}
        public void OnBeginDrag(PointerEventData e)=>OnDrag(e);
        public void OnDrag(PointerEventData e)
        {
            if(!pressed||e.pointerId!=pointer)return;
            if(!dragging&&(e.position-origin).sqrMagnitude<64*scale*scale)return;
            dragging=true;e.eligibleForClick=false;UiPointerGate.SuppressClick=true;
            ui.MoveBuildDrag(this,e);
        }
        // PointerUp is the reliable terminal event on touch. Some input/module transitions
        // omit EndDrag; finishing here also makes a later EndDrag an idempotent no-op.
        public void OnPointerUp(PointerEventData e)=>FinishGesture(e);
        public void OnEndDrag(PointerEventData e)=>FinishGesture(e);
        void FinishGesture(PointerEventData e)
        {
            if(!pressed||e.pointerId!=pointer)return;
            bool commit=dragging&&!WasCancelled(e);pressed=false;dragging=false;
            if(commit)ui.EndBuildDrag(this,e);else ui.CancelBuildDrag(this);
        }
        static bool WasCancelled(PointerEventData e)
        {
            if(!(e is ExtendedPointerEventData data))return false;
            if(data.device==null||!data.device.added)return true;
            if(data.device is Touchscreen touch)
                for(int i=0;i<touch.touches.Count;i++)if(touch.touches[i].touchId.ReadValue()==data.touchId)return touch.touches[i].phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled;
            return false;
        }
        public void CancelGesture(){pressed=false;dragging=false;if(ui!=null)ui.CancelBuildDrag(this);}
        public void OnCancel(BaseEventData e)=>CancelGesture();
        void OnDisable()=>CancelGesture();
    }
}
