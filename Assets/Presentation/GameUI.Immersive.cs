using UnityEngine;
using DefenseDuell.Core;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        RectTransform fullScreenBoard;
        bool buildGuideActive,updatingOverviewInsets;
        void DestroyFullScreenBoard()
        {
            if(fullScreenBoard==null)return;
            fullScreenBoard.gameObject.SetActive(false);Destroy(fullScreenBoard.gameObject);fullScreenBoard=null;
        }
        // Safe-area controls stay in their own canvas region; the scenery fills the entire display.
        void RefreshBoardOverviewInsets()
        {
            if(fullScreenBoard==null||ownBoard==null||safe==null)return;
            var r=safe.rect;var board=fullScreenBoard.rect;
            if(board.width<=0||board.height<=0)return;
            var low=fullScreenBoard.InverseTransformPoint(safe.TransformPoint(new Vector3(r.xMin,r.yMin,0)));
            var high=fullScreenBoard.InverseTransformPoint(safe.TransformPoint(new Vector3(r.xMax,r.yMax,0)));
            var area=Rect.MinMaxRect(Mathf.Clamp01((low.x-board.xMin)/board.width),Mathf.Clamp01((low.y-board.yMin)/board.height),
                Mathf.Clamp01((high.x-board.xMin)/board.width),Mathf.Clamp01((high.y-board.yMin)/board.height));
            updatingOverviewInsets=true;
            try{ownBoard.SetOverviewSafeRect(area);}
            finally{updatingOverviewInsets=false;}
        }
        void RefreshPlacementGuide()
        {
            if(ownBoard==null)return;
            bool active=page=="game"&&!showEnemy&&modal==null&&!InfoBlocking&&session?.State!=null
                &&session.State.Phase==Core.MatchPhase.Playing&&selectedTower<0&&(buildGuideActive||dragging||hasSelectedPoint);
            if(active)ownBoard.ShowPlacementGuide(session.State,session.LocalSlot,dragging?dragKind:selectedKind);
            else ownBoard.HidePlacementGuide();
        }
        static string PlacementErrorText(ErrorCode error)
        {
            switch(error)
            {
                case ErrorCode.InsufficientGold:return "Gold reicht noch nicht.";
                case ErrorCode.ClassLimit:return "Hier ist kein weiterer Bauplatz.";
                case ErrorCode.OutsideMap:return "Mehr Abstand zum Inselrand.";
                case ErrorCode.OnPath:return "Mehr Abstand zum Weg.";
                case ErrorCode.BlockedArea:return "Gelände gesperrt · freie Wiese wählen.";
                case ErrorCode.TooClose:return "Mehr Abstand zum Nachbarturm.";
                case ErrorCode.Occupied:return "Bauplatz belegt.";
                case ErrorCode.InvalidTarget:return "Hier ist kein Bauplatz.";
                case ErrorCode.TowerLocked:return "Turm nicht freigeschaltet.";
                case ErrorCode.WrongPhase:return "Bauen momentan nicht möglich.";
                default:return ErrorText(error);
            }
        }
        Vector2 PlacementChipPosition(Vector2 pointerLocal)
        {
            var r=safe.rect;var half=dragChip.sizeDelta*.5f;const float margin=8;
            // Tooltips stay in the map's reading area, clear of the HUD and build dock.
            r.xMax-=Mathf.Min(156,r.width*.22f);
            r.yMax-=Mathf.Min(132,r.height*.22f);
            r.yMin+=Mathf.Min(116,r.height*.22f);
            // Keep the label beside the upright model instead of covering its head.
            float clearance=56+32*(ownBoard!=null?ownBoard.ZoomFactor:1);
            float right=pointerLocal.x+clearance+half.x,left=pointerLocal.x-clearance-half.x;
            bool rightFits=right+half.x<=r.xMax-margin,leftFits=left-half.x>=r.xMin+margin;
            float x=rightFits?right:leftFits?left:pointerLocal.x;
            float y=rightFits||leftFits?pointerLocal.y:pointerLocal.y-half.y-36;
            if(y-half.y<r.yMin+margin)y=pointerLocal.y+half.y+128;
            return new Vector2(Mathf.Clamp(x,r.xMin+half.x+margin,r.xMax-half.x-margin),
                Mathf.Clamp(y,r.yMin+half.y+margin,r.yMax-half.y-margin));
        }
    }
}
