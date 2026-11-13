using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed class PointerSafeButton : Button
    {
        // Ground targets need the real pointer coordinate; keyboard submit still uses onClick.
        public Action<PointerEventData> PointerClickOverride;
        // Read-only panels may cancel a held troop swipe before claiming their own press.
        public Action PreparePress;
        readonly PointerSuppressionHistory blockedPointers=new PointerSuppressionHistory();
        readonly HashSet<int> heldPointers=new HashSet<int>();
        public int RetainedPointerCount=>blockedPointers.RetainedCount;
        public override void OnPointerDown(PointerEventData e)
        {
            if(e.button==PointerEventData.InputButton.Left&&IsActive()&&IsInteractable()&&UiPointerGate.AllowsTarget(this))PreparePress?.Invoke();
            blockedPointers.Begin(e.pointerId);
            heldPointers.Add(e.pointerId);
            if(IsActive()&&IsInteractable()&&UiPointerGate.Allows(e.pointerId)&&UiPointerGate.AllowsTarget(this)){base.OnPointerDown(e);return;}
            blockedPointers.Block(e.pointerId);e.eligibleForClick=false;
        }
        public override void OnPointerUp(PointerEventData e){heldPointers.Remove(e.pointerId);if(blockedPointers.Contains(e.pointerId))e.eligibleForClick=false;blockedPointers.End(e.pointerId);base.OnPointerUp(e);}
        // A modal can open and fully close while another finger remains pressed. Remember that
        // interruption until this particular contact ends, independently of current modal state.
        public void InvalidatePointer(int id){blockedPointers.Block(id);if(!heldPointers.Contains(id))blockedPointers.End(id);}
        public void InvalidateHeldPointers(){foreach(int id in heldPointers)blockedPointers.Block(id);}
        public override void OnPointerClick(PointerEventData e)
        {
            if(blockedPointers.Consume(e.pointerId)){e.eligibleForClick=false;return;}
            if(!UiPointerGate.Allows(e.pointerId)||!UiPointerGate.AllowsTarget(this))return;
            if(PointerClickOverride==null){base.OnPointerClick(e);return;}
            if(e.button==PointerEventData.InputButton.Left&&IsActive()&&IsInteractable())PointerClickOverride(e);
        }
        public override void OnSubmit(BaseEventData e){if(IsActive()&&IsInteractable()&&UiPointerGate.AllowsTarget(this))PreparePress?.Invoke();if(!UiPointerGate.Active&&UiPointerGate.AllowsTarget(this))base.OnSubmit(e);}
        protected override void OnDisable(){heldPointers.Clear();blockedPointers.Clear();base.OnDisable();}
    }
}
