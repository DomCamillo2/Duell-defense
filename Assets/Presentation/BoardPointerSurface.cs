using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

namespace DefenseDuell.Presentation
{
    /// <summary>One main-board owner separates taps, single-pointer pans and two-contact pinch gestures.</summary>
    public sealed class BoardPointerSurface : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerClickHandler,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IScrollHandler,ICancelHandler
    {
        sealed class Contact { public Vector2 Start,Position; public Camera Camera; }
        BoardView board;
        readonly Dictionary<int,Contact> contacts=new Dictionary<int,Contact>(2);
        readonly PointerSuppressionHistory suppressed=new PointerSuppressionHistory();
        public int RetainedPointerCount=>suppressed.RetainedCount;
        int ownerPointer;
        bool navigating;
        float canvasScale=1;
        public void Initialize(BoardView owner){board=owner;var canvas=GetComponentInParent<Canvas>();canvasScale=canvas!=null?canvas.scaleFactor:1;}
        bool OwnsGate=>UiPointerGate.OwnedBy(this);
        public void OnPointerDown(PointerEventData e)
        {
            suppressed.Begin(e.pointerId);
            if(board==null||!board.IsNavigable||e.button!=PointerEventData.InputButton.Left||(UiPointerGate.Active&&!OwnsGate)||contacts.Count>=2)
            {suppressed.Block(e.pointerId);e.eligibleForClick=false;return;}
            contacts[e.pointerId]=new Contact{Start=e.position,Position=e.position,Camera=e.pressEventCamera};
            if(contacts.Count==2)
            {
                StartNavigation();foreach(var pair in contacts)suppressed.Block(pair.Key);e.eligibleForClick=false;
            }
        }
        public void OnInitializePotentialDrag(PointerEventData e){e.useDragThreshold=false;}
        void StartNavigation()
        {
            if(navigating)return;
            foreach(var pair in contacts){ownerPointer=pair.Key;break;}
            if(!UiPointerGate.Claim(this,ownerPointer))return;
            UiPointerGate.SuppressClick=true;navigating=true;
        }
        public void OnBeginDrag(PointerEventData e)=>OnDrag(e);
        public void OnDrag(PointerEventData e)
        {
            if(!contacts.TryGetValue(e.pointerId,out var contact))return;
            Vector2 previous=contact.Position;
            if(contacts.Count==2)
            {
                Contact other=null;foreach(var pair in contacts)if(pair.Key!=e.pointerId){other=pair.Value;break;}
                float before=Vector2.Distance(previous,other.Position),after=Vector2.Distance(e.position,other.Position);
                Vector2 oldCenter=(previous+other.Position)*.5f,newCenter=(e.position+other.Position)*.5f;
                contact.Position=e.position;StartNavigation();if(!OwnsGate)return;
                e.eligibleForClick=false;suppressed.Block(e.pointerId);
                if(before>8&&after>8)board.SetZoom(board.ZoomFactor*after/before,oldCenter,e.pressEventCamera);
                board.PanByScreenDelta(newCenter-oldCenter,e.pressEventCamera);return;
            }
            contact.Position=e.position;
            if(!navigating&&(contact.Position-contact.Start).sqrMagnitude<64*canvasScale*canvasScale)return;
            StartNavigation();if(!OwnsGate)return;
            e.eligibleForClick=false;suppressed.Block(e.pointerId);board.PanByScreenDelta(e.position-previous,e.pressEventCamera);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(suppressed.Contains(e.pointerId))e.eligibleForClick=false;
            if(!e.dragging)Release(e.pointerId);
        }
        public void OnEndDrag(PointerEventData e){if(suppressed.Contains(e.pointerId))e.eligibleForClick=false;Release(e.pointerId);}
        void Release(int id)
        {
            suppressed.End(id);contacts.Remove(id);
            if(contacts.Count==0){UiPointerGate.Release(this);navigating=false;return;}
            if(OwnsGate&&id==ownerPointer)
            {
                UiPointerGate.Release(this);foreach(var pair in contacts){ownerPointer=pair.Key;break;}
                UiPointerGate.Claim(this,ownerPointer);UiPointerGate.SuppressClick=true;
            }
        }
        public void OnPointerClick(PointerEventData e)
        {
            if(suppressed.Consume(e.pointerId)||!UiPointerGate.Allows(e.pointerId))return;
            if(e.button==PointerEventData.InputButton.Left&&board!=null)board.SelectGroundAt(e.position,e.pressEventCamera);
        }
        public void OnScroll(PointerEventData e)
        {
            if(board==null||!board.IsNavigable||UiPointerGate.Active)return;
            float wheel=Mathf.Clamp(e.scrollDelta.y,-6,6);if(Mathf.Abs(wheel)>.001f)board.SetZoom(board.ZoomFactor*Mathf.Exp(wheel*.12f),e.position,e.enterEventCamera);
        }
        public void CancelGesture(){foreach(var pair in contacts)suppressed.Block(pair.Key);suppressed.EndAll();contacts.Clear();UiPointerGate.Release(this);navigating=false;}
        public void OnCancel(BaseEventData e)=>CancelGesture();
        void OnDisable(){CancelGesture();suppressed.Clear();}
    }
}
