using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>A route pictogram for menu cards. The playable board is always the 3D BoardView.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapThumbnailGraphic : Graphic
    {
        MapPoint[] path;
        MapPoint[] minePoints;
        int theme,mapWidth=1000,mapHeight=600;
        public void Initialize(MapDefinition map)
        {
            path=map.Waypoints;theme=map.Theme;mapWidth=map.Width;mapHeight=map.Height;
            var sites=map.BuildSlots;var mines=map.MineSlots;minePoints=new MapPoint[mines.Length];for(int i=0;i<mines.Length;i++)minePoints[i]=sites[mines[i]];
            raycastTarget=false;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(path==null)return;Rect r=rectTransform.rect;
            Color ground=theme==1?UiKit.Hex(0xDDB46F):theme==2?UiKit.Hex(0xAFDCE3):UiKit.Hex(0x8EBF59);
            Quad(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin),ground);
            for(int i=1;i<path.Length;i++)
            {
                Vector2 a=Point(path[i-1],r),b=Point(path[i],r),d=(b-a).normalized,n=new Vector2(-d.y,d.x)*2.5f;
                Quad(vh,a-n,a+n,b+n,b-n,UiKit.Hex(0xFFF4D0));
            }
            Mark(vh,Point(path[0],r),3.4f,UiKit.Hex(0xC66C35));Mark(vh,Point(path[path.Length-1],r),3.8f,UiKit.Hex(0x317948));
            for(int i=0;i<minePoints.Length;i++)Mark(vh,Point(minePoints[i],r),2.5f,UiKit.Hex(0xE6BA36));
        }
        Vector2 Point(MapPoint p,Rect r)=>new Vector2(r.xMin+6+(r.width-12)*p.X/mapWidth,r.yMax-5-(r.height-10)*p.Y/mapHeight);
        static void Mark(VertexHelper vh,Vector2 center,float size,Color c)=>Quad(vh,center+new Vector2(-size,-size),center+new Vector2(-size,size),center+new Vector2(size,size),center+new Vector2(size,-size),c);
        static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        {int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
