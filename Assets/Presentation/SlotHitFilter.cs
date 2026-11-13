using UnityEngine;

namespace DefenseDuell.Presentation
{
    /// <summary>Nearest-pad disambiguation for overlapping 64px touch targets on the tilted board.</summary>
    public sealed class SlotHitFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        BoardView board;
        int index;
        public void Initialize(BoardView value,int slot){board=value;index=slot;}
        public bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera)=>board!=null&&board.IsClosestTouchTarget(index,screenPoint,eventCamera);
    }
}
