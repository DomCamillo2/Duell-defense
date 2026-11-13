using BigInteger = System.Numerics.BigInteger;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>Unlimited progression marker; the former two-purchase budget no longer exists.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UpgradeBudgetGraphic : MaskableGraphic
    {
        BigInteger completed;
        public BigInteger Completed=>completed;
        public void SetCompleted(BigInteger value){raycastTarget=false;if(completed==value)return;completed=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;Vector2 previous=default;
            for(int i=0;i<=48;i++)
            {
                float a=i*Mathf.PI*2/48;var p=r.center+new Vector2(Mathf.Cos(a)*21,Mathf.Sin(2*a)*6);
                if(i>0){var n=new Vector2(-(p-previous).y,(p-previous).x).normalized*1.5f;int v=vh.currentVertCount;var tint=UiKit.Hex(0xF0C66A);
                    vh.AddVert(previous-n,tint,Vector2.zero);vh.AddVert(previous+n,tint,Vector2.zero);vh.AddVert(p+n,tint,Vector2.zero);vh.AddVert(p-n,tint,Vector2.zero);
                    vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);}
                previous=p;
            }
        }
        static void Diamond(VertexHelper v,Vector2 p,float size,Color c)
        {int a=v.currentVertCount;v.AddVert(p+Vector2.left*size,c,Vector2.zero);v.AddVert(p+Vector2.up*size,c,Vector2.zero);v.AddVert(p+Vector2.right*size,c,Vector2.zero);v.AddVert(p+Vector2.down*size,c,Vector2.zero);v.AddTriangle(a,a+1,a+2);v.AddTriangle(a,a+2,a+3);}
    }
}
