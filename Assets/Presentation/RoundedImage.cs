using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>Small code-drawn UI panels; no texture allocation or downloaded skin.</summary>
    public sealed class RoundedImage : Image
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=GetPixelAdjustedRect();float radius=Mathf.Min(12,Mathf.Min(r.width,r.height)*.22f);
            vh.AddVert(r.center,color,Vector2.zero);const int steps=5;
            for(int corner=0;corner<4;corner++)
            {
                Vector2 center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                for(int step=0;step<=steps;step++)
                {
                    float angle=(corner*90+step*90f/steps)*Mathf.Deg2Rad;
                    vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,color,Vector2.zero);
                }
            }
            int count=4*(steps+1);for(int i=0;i<count;i++)vh.AddTriangle(0,i+1,(i+1)%count+1);
        }
    }
}
