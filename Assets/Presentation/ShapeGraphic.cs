using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed class ShapeGraphic : Graphic
    {
        public int Sides = 24;
        public float Hole = 0;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=rectTransform.rect; var c=r.center; float rx=r.width*.5f,ry=r.height*.5f;
            int count=Mathf.Clamp(Sides,3,40);
            if (Hole<=0)
            {
                vh.AddVert(c,color,Vector2.zero);
                for(int i=0;i<=count;i++) { float a=(90f+i*360f/count)*Mathf.Deg2Rad; vh.AddVert(c+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),color,Vector2.zero); }
                for(int i=1;i<=count;i++) vh.AddTriangle(0,i,i+1);
            }
            else
            {
                for(int i=0;i<=count;i++) { float a=(90f+i*360f/count)*Mathf.Deg2Rad; var v=new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry); vh.AddVert(c+v,color,Vector2.zero);vh.AddVert(c+v*Hole,color,Vector2.zero); }
                for(int i=0;i<count;i++) { int k=i*2;vh.AddTriangle(k,k+2,k+1);vh.AddTriangle(k+1,k+2,k+3); }
            }
        }
        internal static ShapeGraphic Create(Transform parent, string name, float size, Color color, int sides=24)
        {
            var r=UiKit.Rect(name,parent);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=Vector2.one*size;
            var shape=r.gameObject.AddComponent<ShapeGraphic>();shape.color=color;shape.Sides=sides;shape.raycastTarget=false;return shape;
        }
    }
}
