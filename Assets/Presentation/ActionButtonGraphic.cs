using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public enum ActionSymbol { Build, Damage, Range, Income, Sell, Speed, Close, Frost }

    /// <summary>Lightweight vector artwork on the animated visual child; the button hit rectangle never moves.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ActionButtonGraphic : MaskableGraphic
    {
        ActionSymbol symbol;
        PurchaseState state;
        int steps;
        bool showSteps;
        bool showVotes,localVote,otherVote;
        public ActionSymbol Symbol=>symbol;
        public PurchaseState State=>state;
        public int CompletedSteps=>steps;
        public void SetConsents(bool own,bool other)
        {if(showVotes&&localVote==own&&otherVote==other)return;showVotes=true;localVote=own;otherVote=other;SetVerticesDirty();}
        public void SetStyle(ActionSymbol value,PurchaseState availability,int completed,bool progress)
        {
            raycastTarget=false;
            if(symbol==value&&state==availability&&steps==completed&&showSteps==progress)return;
            symbol=value;state=availability;steps=completed;showSteps=progress;SetVerticesDirty();
        }
        Color Tint(uint value)
        {
            var c=UiKit.Hex(value);
            if(state!=PurchaseState.Available){float grey=c.grayscale;c=new Color(grey*.9f,grey*.93f,grey,1);}
            return c*color;
        }
        uint Accent=>symbol==ActionSymbol.Frost?0x71CAD1u:symbol==ActionSymbol.Damage?0xE58F50u:symbol==ActionSymbol.Range?0x71CAD1u:symbol==ActionSymbol.Income||symbol==ActionSymbol.Sell?0xF0C66Au:symbol==ActionSymbol.Speed?0x9EDC87u:0xA0CA78u;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float w=r.width,h=r.height;
            // A dark lower extrusion, metal rim, warm upper bevel and recessed painted face.
            Plate(vh,1,0,w-2,h-4,10,Tint(0x08131E));
            Plate(vh,1,5,w-2,h-6,10,Tint(0x55737B));
            Plate(vh,3,7,w-6,h-9,8,Tint(0xCDBB87));
            Plate(vh,4,7,w-8,h-13,7,Tint(0x142A3A));
            Plate(vh,7,11,w-14,h-20,5,Tint(0x234957));
            Line(vh,new Vector2(12,h-9),new Vector2(w-12,h-9),2,Tint(0x77ACAF));
            Circle(vh,9,12,2,Tint(0x102633));Circle(vh,w-9,12,2,Tint(0x102633));
            bool iconOnly=symbol==ActionSymbol.Close,compact=h<75;
            float cx=iconOnly?w*.5f:compact?26:26,cy=iconOnly?h*.5f:compact?h*.5f:h-27;
            float radius=iconOnly?18:compact?18:18;
            Circle(vh,cx,cy-2,radius+2,Tint(0x0D202E));Circle(vh,cx,cy,radius,Tint(Accent));
            Circle(vh,cx,cy,radius-3,Tint(0x173541));Circle(vh,cx,cy+1,radius-5,Tint(0x2A5360));
            Icon(vh,cx,cy,Tint(0xFFF2CE),Tint(Accent));
            if(compact&&showVotes)
            {Diamond(vh,55,12,4,Tint(localVote?0xA9E482u:0x697C86u));Diamond(vh,100,12,4,Tint(otherVote?0xA9E482u:0x697C86u));}
            if(!compact&&!iconOnly)
            {
                Plate(vh,10,10,w-20,23,5,Tint(0x112939));
                Plate(vh,11,12,w-22,22,4,Tint(0xF0C66A));
                Line(vh,new Vector2(17,31),new Vector2(w-17,31),1,Tint(0xFFF0B7));
                if(showSteps)for(int i=0;i<2;i++)
                {
                    float x=17+i*14;Diamond(vh,x,43,6,Tint(0x102633));
                    Diamond(vh,x,44,4,i<steps?Tint(Accent):Tint(0x536D77));
                    if(i<steps)Line(vh,new Vector2(x-2,45),new Vector2(x,47),1,Tint(0xFFF1CA));
                }
            }
        }
        void Icon(VertexHelper v,float x,float y,Color light,Color accent)
        {
            if(symbol==ActionSymbol.Frost)
            {
                for(int i=0;i<3;i++){float a=i*Mathf.PI/3;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*12;Line(v,new Vector2(x,y)-d,new Vector2(x,y)+d,2.5f,light);}
                Circle(v,x,y,3,accent);
            }
            else if(symbol==ActionSymbol.Damage)
            {
                Poly(v,light,new Vector2(x-8,y-6),new Vector2(x+8,y+14),new Vector2(x+14,y+15),new Vector2(x+13,y+9),new Vector2(x-4,y-10));
                Line(v,new Vector2(x-11,y-3),new Vector2(x-1,y-13),4,accent);Line(v,new Vector2(x-7,y-9),new Vector2(x-13,y-15),5,accent);
            }
            else if(symbol==ActionSymbol.Range)
            {
                Ring(v,x,y,12,2,light);Ring(v,x,y,6,2,accent);Circle(v,x,y,2,light);
                Line(v,new Vector2(x-16,y),new Vector2(x-9,y),2,accent);Line(v,new Vector2(x+9,y),new Vector2(x+16,y),2,accent);
                Line(v,new Vector2(x,y-16),new Vector2(x,y-9),2,accent);Line(v,new Vector2(x,y+9),new Vector2(x,y+16),2,accent);
            }
            else if(symbol==ActionSymbol.Income)
            {
                for(int i=0;i<3;i++){float a=x-8+i*7,b=y-7+i*6;Circle(v,a,b-2,7,Tint(0x94622F));Circle(v,a,b,7,accent);Line(v,new Vector2(a,b-4),new Vector2(a,b+4),2,light);}
            }
            else if(symbol==ActionSymbol.Sell)
            {
                Poly(v,accent,new Vector2(x-12,y-10),new Vector2(x-11,y+1),new Vector2(x-5,y+7),new Vector2(x-8,y+13),new Vector2(x+4,y+13),new Vector2(x+1,y+7),new Vector2(x+7,y+1),new Vector2(x+8,y-10));
                Line(v,new Vector2(x-6,y+6),new Vector2(x+2,y+6),2,light);
                Line(v,new Vector2(x,y-4),new Vector2(x+13,y-4),3,light);
                Poly(v,light,new Vector2(x+9,y+2),new Vector2(x+16,y-4),new Vector2(x+9,y-10));
            }
            else if(symbol==ActionSymbol.Speed)
            {
                Poly(v,light,new Vector2(x-11,y-10),new Vector2(x-11,y+10),new Vector2(x+1,y));
                Poly(v,accent,new Vector2(x+1,y-10),new Vector2(x+1,y+10),new Vector2(x+13,y));
            }
            else if(symbol==ActionSymbol.Close)
            {Line(v,new Vector2(x-7,y-7),new Vector2(x+7,y+7),3,light);Line(v,new Vector2(x-7,y+7),new Vector2(x+7,y-7),3,light);}
            else
            {
                Line(v,new Vector2(x-8,y-13),new Vector2(x+6,y+7),5,accent);
                Poly(v,light,new Vector2(x-7,y+9),new Vector2(x-2,y+16),new Vector2(x+14,y+5),new Vector2(x+9,y-2));
            }
        }
        Vector2 P(Vector2 point){var r=rectTransform.rect;return new Vector2(r.xMin+point.x,r.yMin+point.y);}
        void Poly(VertexHelper v,Color c,params Vector2[] points)
        {int first=v.currentVertCount;foreach(var p in points)v.AddVert(P(p),c,Vector2.zero);for(int i=1;i<points.Length-1;i++)v.AddTriangle(first,first+i,first+i+1);}
        void Plate(VertexHelper v,float x,float y,float w,float h,float cut,Color c)
        {Poly(v,c,new Vector2(x+cut,y),new Vector2(x+w-cut,y),new Vector2(x+w,y+cut),new Vector2(x+w,y+h-cut),new Vector2(x+w-cut,y+h),new Vector2(x+cut,y+h),new Vector2(x,y+h-cut),new Vector2(x,y+cut));}
        void Diamond(VertexHelper v,float x,float y,float r,Color c)=>Poly(v,c,new Vector2(x-r,y),new Vector2(x,y+r),new Vector2(x+r,y),new Vector2(x,y-r));
        void Line(VertexHelper v,Vector2 a,Vector2 b,float width,Color c)
        {Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Poly(v,c,a-n,a+n,b+n,b-n);}
        void Circle(VertexHelper v,float x,float y,float radius,Color c)
        {int start=v.currentVertCount;v.AddVert(P(new Vector2(x,y)),c,Vector2.zero);for(int i=0;i<=24;i++){float a=i*Mathf.PI/12;v.AddVert(P(new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius)),c,Vector2.zero);}for(int i=1;i<=24;i++)v.AddTriangle(start,start+i,start+i+1);}
        void Ring(VertexHelper v,float x,float y,float radius,float thickness,Color c)
        {for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Poly(v,c,new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),new Vector2(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),new Vector2(x+Mathf.Cos(b)*(radius-thickness),y+Mathf.Sin(b)*(radius-thickness)),new Vector2(x+Mathf.Cos(a)*(radius-thickness),y+Mathf.Sin(a)*(radius-thickness)));}}
    }
}
