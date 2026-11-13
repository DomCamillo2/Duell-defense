using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>Original silhouettes matching the five 3D buildings and four troop bodies.</summary>
    public sealed class ClassIconGraphic : Graphic
    {
        int kind;bool tower;
        public void Initialize(int value,bool isTower){kind=value;tower=isTower;raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Color dark=UiKit.Hex(0x34483C),gold=UiKit.Hex(0xE6B93E),green=UiKit.Hex(0x57A85B),orange=UiKit.Hex(0xE28843),blue=UiKit.Hex(0x5DBBCF),purple=UiKit.Hex(0x926AB5);
            if(tower)
            {
                Rect(vh,.12f,.05f,.76f,.17f,UiKit.Hex(0x8B9B83));
                if(kind==4){Poly(vh,gold,new Vector2(.12f,.3f),new Vector2(.3f,.86f),new Vector2(.7f,.94f),new Vector2(.91f,.3f));Rect(vh,.28f,.18f,.42f,.38f,dark);Rect(vh,.2f,.18f,.1f,.47f,orange);Rect(vh,.69f,.18f,.1f,.47f,orange);Rect(vh,.2f,.61f,.6f,.12f,orange);Circle(vh,.78f,.78f,.15f,dark);Circle(vh,.78f,.78f,.07f,gold);}
                else if(kind==2){Rect(vh,.31f,.2f,.38f,.2f,blue);Poly(vh,blue,new Vector2(.22f,.59f),new Vector2(.5f,1),new Vector2(.78f,.59f),new Vector2(.5f,.28f));Poly(vh,Color.white,new Vector2(.5f,.94f),new Vector2(.64f,.6f),new Vector2(.5f,.37f));}
                else if(kind==1){Circle(vh,.32f,.28f,.2f,dark);Circle(vh,.72f,.28f,.2f,dark);Rect(vh,.25f,.3f,.5f,.37f,orange);Poly(vh,dark,new Vector2(.4f,.56f),new Vector2(.56f,.88f),new Vector2(.92f,.72f),new Vector2(.76f,.4f));Circle(vh,.81f,.73f,.17f,UiKit.Hex(0xA5B1A7));Circle(vh,.81f,.73f,.09f,dark);}
                else if(kind==3){Rect(vh,.26f,.22f,.5f,.43f,purple);Rect(vh,.31f,.52f,.13f,.43f,dark);Rect(vh,.59f,.52f,.13f,.43f,dark);Rect(vh,.42f,.59f,.18f,.13f,gold);}
                else{Circle(vh,.5f,.51f,.32f,green);Rect(vh,.25f,.56f,.12f,.37f,dark);Rect(vh,.44f,.62f,.12f,.38f,dark);Rect(vh,.63f,.56f,.12f,.37f,dark);Rect(vh,.29f,.28f,.43f,.14f,gold);}
            }
            else if(kind==2){Rect(vh,.12f,.2f,.15f,.58f,dark);Rect(vh,.73f,.2f,.15f,.58f,dark);Rect(vh,.25f,.3f,.5f,.48f,orange);Rect(vh,.36f,.58f,.29f,.25f,gold);}
            else if(kind==1){Poly(vh,blue,new Vector2(.2f,.26f),new Vector2(.47f,.98f),new Vector2(.82f,.38f),new Vector2(.49f,.14f));Poly(vh,orange,new Vector2(.1f,.2f),new Vector2(.2f,.59f),new Vector2(.34f,.2f));Rect(vh,.34f,.59f,.3f,.12f,Color.white);}
            else if(kind==3){Circle(vh,.28f,.3f,.22f,purple);Circle(vh,.72f,.3f,.22f,purple);Circle(vh,.5f,.72f,.22f,purple);Circle(vh,.24f,.36f,.035f,Color.white);Circle(vh,.68f,.36f,.035f,Color.white);Circle(vh,.46f,.78f,.035f,Color.white);}
            else{Circle(vh,.5f,.52f,.35f,green);Rect(vh,.22f,.09f,.23f,.18f,dark);Rect(vh,.57f,.09f,.23f,.18f,dark);Circle(vh,.39f,.64f,.09f,Color.white);Circle(vh,.64f,.64f,.09f,Color.white);Circle(vh,.4f,.65f,.04f,dark);Circle(vh,.65f,.65f,.04f,dark);}
        }
        Vector2 P(Vector2 p){var r=rectTransform.rect;return new Vector2(r.xMin+r.width*p.x,r.yMin+r.height*p.y);}
        void Poly(VertexHelper vh,Color c,params Vector2[] points){int n=vh.currentVertCount;for(int i=0;i<points.Length;i++)vh.AddVert(P(points[i]),c,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(n,n+i,n+i+1);}
        void Rect(VertexHelper vh,float x,float y,float w,float h,Color c)=>Poly(vh,c,new Vector2(x,y),new Vector2(x,y+h),new Vector2(x+w,y+h),new Vector2(x+w,y));
        void Circle(VertexHelper vh,float x,float y,float radius,Color c){int n=vh.currentVertCount;vh.AddVert(P(new Vector2(x,y)),c,Vector2.zero);for(int i=0;i<=16;i++){float a=i*Mathf.PI/8;vh.AddVert(P(new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius)),c,Vector2.zero);}for(int i=1;i<=16;i++)vh.AddTriangle(n,n+i,n+i+1);}
    }
}
