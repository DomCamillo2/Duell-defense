using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public enum HudSymbol { Shield, Coin, Wave, Menu, Plus, Minus, Overview, Info, Close, Back, Chevron, Attack }

    /// <summary>Small vector symbols; no emoji/font fallback and no per-frame animation.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudIconGraphic : MaskableGraphic
    {
        HudSymbol symbol;
        public HudSymbol Symbol=>symbol;
        public void SetSymbol(HudSymbol value){symbol=value;raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();
            switch(symbol)
            {
                case HudSymbol.Shield:
                    Poly(v,new Vector2(5,27),new Vector2(16,30),new Vector2(27,27),new Vector2(25,12),new Vector2(16,3),new Vector2(7,12));
                    break;
                case HudSymbol.Coin:
                    Ring(v,16,16,12,3);Line(v,16,9,16,23,3);Line(v,12,21,20,21,2);Line(v,12,11,20,11,2);break;
                case HudSymbol.Wave:
                    Line(v,8,3,8,29,3);Poly(v,new Vector2(10,27),new Vector2(27,25),new Vector2(22,18),new Vector2(10,20));break;
                case HudSymbol.Menu:
                    Line(v,6,24,26,24,3);Line(v,6,16,26,16,3);Line(v,6,8,26,8,3);break;
                case HudSymbol.Plus:Line(v,16,6,16,26,3);Line(v,6,16,26,16,3);break;
                case HudSymbol.Minus:Line(v,6,16,26,16,3);break;
                case HudSymbol.Overview:
                    Line(v,5,12,5,5,3);Line(v,5,5,12,5,3);Line(v,20,5,27,5,3);Line(v,27,5,27,12,3);
                    Line(v,5,20,5,27,3);Line(v,5,27,12,27,3);Line(v,20,27,27,27,3);Line(v,27,27,27,20,3);break;
                case HudSymbol.Info:Ring(v,16,16,12,2);Line(v,16,9,16,17,3);Line(v,16,22,16,23,3);break;
                case HudSymbol.Close:Line(v,8,8,24,24,3);Line(v,8,24,24,8,3);break;
                case HudSymbol.Back:Line(v,24,16,7,16,3);Line(v,7,16,15,24,3);Line(v,7,16,15,8,3);break;
                case HudSymbol.Chevron:Line(v,11,6,21,16,3);Line(v,21,16,11,26,3);break;
                case HudSymbol.Attack:
                    Line(v,7,6,24,25,3);Line(v,25,6,8,25,3);Line(v,4,11,11,4,3);Line(v,21,4,28,11,3);break;
            }
        }
        Vector2 P(Vector2 point)
        {var r=GetPixelAdjustedRect();float s=Mathf.Min(r.width,r.height)/32;return r.center+(point-Vector2.one*16)*s;}
        void Poly(VertexHelper v,params Vector2[] points)
        {int start=v.currentVertCount;foreach(var p in points)v.AddVert(P(p),color,Vector2.zero);for(int i=1;i<points.Length-1;i++)v.AddTriangle(start,start+i,start+i+1);}
        void Line(VertexHelper v,float ax,float ay,float bx,float by,float width)
        {var a=new Vector2(ax,ay);var b=new Vector2(bx,by);var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Poly(v,a-n,a+n,b+n,b-n);}
        void Ring(VertexHelper v,float x,float y,float radius,float thickness)
        {for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Poly(v,new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),new Vector2(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),new Vector2(x+Mathf.Cos(b)*(radius-thickness),y+Mathf.Sin(b)*(radius-thickness)),new Vector2(x+Mathf.Cos(a)*(radius-thickness),y+Mathf.Sin(a)*(radius-thickness)));}}
    }
}
