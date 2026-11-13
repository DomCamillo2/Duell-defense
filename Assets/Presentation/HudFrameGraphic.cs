using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public enum HudFrameStyle { Panel, Button, Card, Price, Well }

    /// <summary>Static, pixel-scaled facets. The graphic stays on the animated visual child,
    /// while purchase availability continues to tint the complete card.</summary>
    public sealed class HudFrameGraphic : Image
    {
        HudFrameStyle style;
        Color accent = new Color(.35f,.65f,.69f);
        public HudFrameStyle Style => style;
        public void SetStyle(HudFrameStyle value, Color trim)
        { style=value;accent=trim;raycastTarget=false;SetVerticesDirty(); }
        Color Trim(Color value)
        {
            // PurchaseAvailability desaturates every Graphic, including this background.
            if(Mathf.Abs(color.r-color.g)<.002f&&Mathf.Abs(color.g-color.b)<.002f)
                value=new Color(value.grayscale,value.grayscale,value.grayscale,value.a);
            value.a*=color.a;return value;
        }
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();var r=GetPixelAdjustedRect();if(r.width<2||r.height<2)return;
            float w=r.width,h=r.height,cut=Mathf.Min(style==HudFrameStyle.Card?9:7,Mathf.Min(w,h)*.2f);
            Color face=color;
            if(style==HudFrameStyle.Price)
            {
                Plate(v,r,0,0,w,h,4,Trim(UiKit.Hex(0x80562D)),Trim(UiKit.Hex(0x80562D)));
                Plate(v,r,0,2,w,h-2,4,Color.Lerp(face,Color.black,.12f),Color.Lerp(face,Color.white,.18f));
                return;
            }
            if(style==HudFrameStyle.Well)
            {
                Plate(v,r,0,0,w,h,cut,Trim(UiKit.Hex(0x304950)),Trim(UiKit.Hex(0x304950)));
                Plate(v,r,1,1,w-2,h-2,cut-1,Color.Lerp(face,Color.black,.45f),Color.Lerp(face,Trim(accent),.16f));
                return;
            }
            // Lower extrusion, restrained metal edge and a softly lit recessed face.
            Plate(v,r,0,0,w,h-2,cut,Trim(UiKit.Hex(0x08131E)),Trim(UiKit.Hex(0x08131E)));
            Plate(v,r,0,3,w,h-3,cut,Trim(UiKit.Hex(0x304A57)),Trim(UiKit.Hex(0x78938E)));
            Plate(v,r,1.5f,4,w-3,h-5.5f,cut-1,Color.Lerp(face,Color.black,.20f),Color.Lerp(face,Trim(UiKit.Hex(0x5A8A8E)),.20f));
            if(style==HudFrameStyle.Card)
            {
                // Class colour is a small top inlay, never a loud full-card border.
                Plate(v,r,12,h-5,w-24,2,1,Trim(accent),Trim(accent));
                Plate(v,r,6,8,2,10,1,Trim(accent),Trim(accent));
                Plate(v,r,w-8,8,2,10,1,Trim(accent),Trim(accent));
            }
            else if(style==HudFrameStyle.Button)
            {
                Plate(v,r,cut+3,h-4,Mathf.Min(w-2*cut-6,30),1,0,Trim(UiKit.Hex(0xD3BB7F)),Trim(UiKit.Hex(0xD3BB7F)));
            }
        }
        static void Plate(VertexHelper v,Rect r,float x,float y,float w,float h,float cut,Color low,Color high)
        {
            if(w<=0||h<=0)return;cut=Mathf.Clamp(cut,0,Mathf.Min(w,h)*.5f);
            int first=v.currentVertCount;
            Add(v,r,x+cut,y,low);Add(v,r,x+w-cut,y,low);Add(v,r,x+w,y+cut,Color.Lerp(low,high,cut/h));
            Add(v,r,x+w,y+h-cut,Color.Lerp(low,high,1-cut/h));Add(v,r,x+w-cut,y+h,high);Add(v,r,x+cut,y+h,high);
            Add(v,r,x,y+h-cut,Color.Lerp(low,high,1-cut/h));Add(v,r,x,y+cut,Color.Lerp(low,high,cut/h));
            for(int i=1;i<7;i++)v.AddTriangle(first,first+i,first+i+1);
        }
        static void Add(VertexHelper v,Rect r,float x,float y,Color c)=>v.AddVert(new Vector2(r.xMin+x,r.yMin+y),c,Vector2.zero);
    }
}
