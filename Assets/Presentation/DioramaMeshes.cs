using System;
using System.Collections.Generic;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseDuell.Presentation
{
    // Original, small low-poly geometry. No physics, asset downloads, or runtime mesh mutation.
    internal sealed class DioramaMeshBuilder
    {
        readonly List<Vector3> vertices=new List<Vector3>(2048);
        readonly List<Vector3> normals=new List<Vector3>(2048);
        readonly List<Color32> colors=new List<Color32>(2048);
        readonly List<Vector2> finishes=new List<Vector2>(2048);
        readonly List<int> triangles=new List<int>(3072);
        // Terrain leaves this at zero. Model parts share one material and opt into a
        // restrained painted-metal/crystal highlight through UV0.x, never alpha.
        public float Finish;
        void Triangle(Vector3 a,Vector3 b,Vector3 c,Color32 color)
        {
            int n=vertices.Count;Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);normals.Add(normal);normals.Add(normal);normals.Add(normal);
            colors.Add(color);colors.Add(color);colors.Add(color);AddFinish();AddFinish();AddFinish();triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
        }
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color32 color){Triangle(a,b,c,color);Triangle(a,c,d,color);}
        internal void SurfaceTriangle(Vector3 a,Vector3 b,Vector3 c,Color color)=>Triangle(a,b,c,color);
        internal void SurfaceQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)=>Quad(a,b,c,d,color);
        internal void GradientTriangle(Vector3 a,Vector3 b,Vector3 c,Color ca,Color cb,Color cc)
        {
            int n=vertices.Count;Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);normals.Add(normal);normals.Add(normal);normals.Add(normal);
            colors.Add(ca);colors.Add(cb);colors.Add(cc);AddFinish();AddFinish();AddFinish();triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
        }
        void AddFinish()=>finishes.Add(new Vector2(Mathf.Clamp01(Finish),0));
        void FacingTriangle(Vector3 a,Vector3 b,Vector3 c,Vector3 outward,Color color)
        {if(Vector3.Dot(Vector3.Cross(b-a,c-a),outward)<0)Triangle(a,c,b,color);else Triangle(a,b,c,color);}
        void FacingQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward,Color color)
        {FacingTriangle(a,b,c,outward,color);FacingTriangle(a,c,d,outward,color);}
        static Vector3 Axis(int axis,float value){var v=Vector3.zero;v[axis]=value;return v;}
        public void BevelBox(Vector3 center,Vector3 size,float bevel,Color color,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            Vector3 h=size*.5f;float b=Mathf.Clamp(bevel,.001f,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.8f);Vector3 inner=h-Vector3.one*b;
            Vector3 P(Vector3 v)=>center+rotation*v;
            for(int axis=0;axis<3;axis++)for(int side=-1;side<=1;side+=2)
            {
                int u=(axis+1)%3,v=(axis+2)%3;Vector3 normal=Axis(axis,side),face=Axis(axis,h[axis]*side);
                FacingQuad(P(face+Axis(u,-inner[u])+Axis(v,-inner[v])),P(face+Axis(u,inner[u])+Axis(v,-inner[v])),
                    P(face+Axis(u,inner[u])+Axis(v,inner[v])),P(face+Axis(u,-inner[u])+Axis(v,inner[v])),rotation*normal,color);
            }
            for(int along=0;along<3;along++)for(int su=-1;su<=1;su+=2)for(int sv=-1;sv<=1;sv+=2)
            {
                int u=(along+1)%3,v=(along+2)%3;
                Vector3 a=Axis(u,su*h[u])+Axis(v,sv*inner[v]),c=Axis(u,su*inner[u])+Axis(v,sv*h[v]),edge=Axis(along,inner[along]);
                FacingQuad(P(a-edge),P(a+edge),P(c+edge),P(c-edge),rotation*(Axis(u,su)+Axis(v,sv)),Color.Lerp(color,Color.white,.06f));
            }
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                var sign=new Vector3(x,y,z);
                FacingTriangle(P(new Vector3(x*h.x,y*inner.y,z*inner.z)),P(new Vector3(x*inner.x,y*h.y,z*inner.z)),P(new Vector3(x*inner.x,y*inner.y,z*h.z)),rotation*sign,Color.Lerp(color,Color.white,.08f));
            }
        }
        void BodyTriangle(Vector3 a,Vector3 b,Vector3 c,Vector3 na,Vector3 nb,Vector3 nc,Color color)
        {
            int n=vertices.Count;Vector3 face=Vector3.Cross(b-a,c-a).normalized;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);
            float smoothing=Finish>=.15f?.92f:.65f;
            normals.Add(Vector3.Lerp(face,na,smoothing).normalized);normals.Add(Vector3.Lerp(face,nb,smoothing).normalized);normals.Add(Vector3.Lerp(face,nc,smoothing).normalized);
            colors.Add(color);colors.Add(color);colors.Add(color);AddFinish();AddFinish();AddFinish();triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
        }
        public void Body(Vector3 center,Vector3 scale,Color color,int sides=10,int rings=5,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            Vector3 P(Vector3 v)=>center+rotation*Vector3.Scale(v,scale);
            Vector3 N(Vector3 v)=>rotation*new Vector3(v.x/scale.x,v.y/scale.y,v.z/scale.z).normalized;
            for(int r=0;r<rings;r++)for(int s=0;s<sides;s++)
            {
                float a=-Mathf.PI*.5f+Mathf.PI*r/rings,b=-Mathf.PI*.5f+Mathf.PI*(r+1)/rings,c=s*Mathf.PI*2/sides,d=(s+1)*Mathf.PI*2/sides;
                var p=Sphere(a,c);var q=Sphere(a,d);var u=Sphere(b,d);var v=Sphere(b,c);
                if(r<rings-1)BodyTriangle(P(p),P(v),P(u),N(p),N(v),N(u),color);
                if(r>0)BodyTriangle(P(p),P(u),P(q),N(p),N(u),N(q),color);
            }
        }
        public void Lathe(Vector3 center,Vector2[] profile,Color color,int sides=12,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            for(int j=1;j<profile.Length;j++)for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 p=new Vector3(Mathf.Cos(a)*profile[j-1].x,profile[j-1].y,Mathf.Sin(a)*profile[j-1].x),q=new Vector3(Mathf.Cos(b)*profile[j-1].x,profile[j-1].y,Mathf.Sin(b)*profile[j-1].x),
                    u=new Vector3(Mathf.Cos(b)*profile[j].x,profile[j].y,Mathf.Sin(b)*profile[j].x),v=new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x);
                if(Finish>=.4f)
                {
                    // Smooth only around a turned surface. Separate profile segments keep
                    // muzzle lips, shoulders and end caps crisp; stone stays faceted.
                    float dy=profile[j].y-profile[j-1].y,dr=profile[j].x-profile[j-1].x;
                    Vector3 na=rotation*new Vector3(dy*Mathf.Cos(a),-dr,dy*Mathf.Sin(a)).normalized;
                    Vector3 nb=rotation*new Vector3(dy*Mathf.Cos(b),-dr,dy*Mathf.Sin(b)).normalized;
                    if(profile[j].x>0)BodyTriangle(center+rotation*p,center+rotation*v,center+rotation*u,na,na,nb,color);
                    if(profile[j-1].x>0)BodyTriangle(center+rotation*p,center+rotation*u,center+rotation*q,na,nb,nb,color);
                }
                else
                {
                    if(profile[j].x>0)Triangle(center+rotation*p,center+rotation*v,center+rotation*u,color);
                    if(profile[j-1].x>0)Triangle(center+rotation*p,center+rotation*u,center+rotation*q,color);
                }
            }
        }
        public void Tube(Vector3 center,float outer,float inner,float length,Color color,int sides=12,Quaternion rotation=default)
        {
            Lathe(center,new[]{new Vector2(inner,-length*.5f),new Vector2(outer,-length*.5f),new Vector2(outer,length*.5f),new Vector2(inner,length*.5f),new Vector2(inner,-length*.5f)},color,sides,rotation);
        }
        public void Shard(Vector3 center,Vector3 scale,Color color,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            Vector3 top=center+rotation*Vector3.up*scale.y,bottom=center-rotation*Vector3.up*scale.y*.48f;
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;Vector3 p=center+rotation*new Vector3(Mathf.Cos(a)*scale.x,0,Mathf.Sin(a)*scale.z),q=center+rotation*new Vector3(Mathf.Cos(b)*scale.x,0,Mathf.Sin(b)*scale.z);
                Color face=Color.Lerp(color,Color.white,i%3==0?.24f:i%3==1?.04f:.12f);
                Triangle(top,q,p,face);Triangle(bottom,p,q,Color.Lerp(color,Color.black,.12f));
            }
        }
        public void Box(Vector3 center,Vector3 size,Color color,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;Vector3 h=size*.5f;
            Vector3[] v={new Vector3(-h.x,-h.y,-h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,-h.y,h.z),new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(h.x,h.y,h.z),new Vector3(-h.x,h.y,h.z)};
            for(int i=0;i<v.Length;i++)v[i]=center+rotation*v[i];
            Quad(v[0],v[1],v[2],v[3],color);Quad(v[4],v[7],v[6],v[5],color);
            Quad(v[0],v[4],v[5],v[1],color);Quad(v[1],v[5],v[6],v[2],color);Quad(v[2],v[6],v[7],v[3],color);Quad(v[3],v[7],v[4],v[0],color);
        }
        public void Cylinder(Vector3 center,float radius,float height,Color color,int sides=12,float topRadius=-1,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;if(topRadius<0)topRadius=radius;
            Vector3 bottom=center+rotation*(Vector3.down*height*.5f),top=center+rotation*(Vector3.up*height*.5f);
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 dirA=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),dirB=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                Vector3 p=bottom+rotation*dirA*radius,q=bottom+rotation*dirB*radius,r=top+rotation*dirB*topRadius,s=top+rotation*dirA*topRadius;
                if(Finish>=.4f)
                {
                    Vector3 na=rotation*new Vector3(height*dirA.x,radius-topRadius,height*dirA.z).normalized;
                    Vector3 nb=rotation*new Vector3(height*dirB.x,radius-topRadius,height*dirB.z).normalized;
                    BodyTriangle(p,s,r,na,na,nb,color);BodyTriangle(p,r,q,na,nb,nb,color);
                }
                else Quad(p,s,r,q,color);
                Triangle(top,r,s,color);Triangle(bottom,p,q,color);
            }
        }
        public void Ball(Vector3 center,Vector3 scale,Color color,int sides=10,int rings=6)
        {
            for(int r=0;r<rings;r++)
            {
                float a=-Mathf.PI*.5f+Mathf.PI*r/rings,b=-Mathf.PI*.5f+Mathf.PI*(r+1)/rings;
                for(int s=0;s<sides;s++)
                {
                    float c=s*Mathf.PI*2/sides,d=(s+1)*Mathf.PI*2/sides;
                    Vector3 p=Sphere(a,c),q=Sphere(a,d),u=Sphere(b,d),v=Sphere(b,c);
                    Quad(center+Vector3.Scale(p,scale),center+Vector3.Scale(v,scale),center+Vector3.Scale(u,scale),center+Vector3.Scale(q,scale),color);
                }
            }
        }
        static Vector3 Sphere(float lat,float lon)=>new Vector3(Mathf.Cos(lat)*Mathf.Cos(lon),Mathf.Sin(lat),Mathf.Cos(lat)*Mathf.Sin(lon));
        public void Crystal(Vector3 center,Vector3 scale,Color color)
        {
            Vector3 top=center+Vector3.up*scale.y,bottom=center-Vector3.up*scale.y*.55f;
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;Vector3 p=center+new Vector3(Mathf.Cos(a)*scale.x,0,Mathf.Sin(a)*scale.z),q=center+new Vector3(Mathf.Cos(b)*scale.x,0,Mathf.Sin(b)*scale.z);
                Triangle(top,q,p,color);Triangle(bottom,p,q,color);
            }
        }
        public void Ribbon(MapPoint[] points,float radius,float y,Color color)
        {
            for(int i=1;i<points.Length;i++)
            {
                Vector3 a=DioramaResources.Point(points[i-1],y),b=DioramaResources.Point(points[i],y),n=Vector3.Cross(Vector3.up,(b-a).normalized)*radius;
                Quad(a-n,b-n,b+n,a+n,color);
            }
            for(int i=0;i<points.Length;i++)Cylinder(DioramaResources.Point(points[i],y-.006f),radius,.012f,color,16);
        }
        public void Ring(Vector3 center,float radius,float thickness,Color color,int sides=48)
        {
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                Quad(center+p*(radius-thickness),center+q*(radius-thickness),center+q*radius,center+p*radius,color);
            }
        }
        public Mesh Build(string name)
        {
            var mesh=new Mesh{name=name,hideFlags=HideFlags.DontSave};if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetUVs(0,finishes);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
    }

    public sealed partial class DioramaResources : IDisposable
    {
        internal static readonly Color Green=C(0x79B54B), Orange=C(0xD98545), Blue=C(0x56BDCE), Purple=C(0x9574BC), Gold=C(0xE7BC59), Dark=C(0x203C35), Cream=C(0xFFF3D8), Stone=C(0xB5A089);
        static readonly Color Steel=C(0x56636C),Deep=C(0x17262C),Wood=C(0xA96E3F),LightWood=C(0xC98E4F);
        readonly List<Mesh> owned=new List<Mesh>();
        public readonly Material Material;
        public readonly Mesh Terrain, Pad, MinePad, Selection, Range, Bullet, Burst, HealthBar, HealthTrack;
        public readonly Mesh[] Units=new Mesh[GameContent.UnitClassCount];
        readonly float[] unitHeights=new float[GameContent.UnitClassCount];
        public readonly Mesh[,] TowerBases, TowerHeads;
        public readonly float MaximumTowerHeight,MaximumTowerRadius;
        public readonly Color Sky;
        public readonly Vector3 MapCenter;
        public readonly Bounds WorldBounds;
        public static Color C(uint v)=>new Color(((v>>16)&255)/255f,((v>>8)&255)/255f,(v&255)/255f,1);
        public static Vector3 Point(MapPoint p,float y=.28f)=>new Vector3((p.X-500)/50f,y,(300-p.Y)/50f);
        public static Vector3 TowerHeadOffset(int kind)=>kind==7?new Vector3(0,.72f,0):kind>=5?new Vector3(0,.42f,0):kind==4?new Vector3(0,1.28f,.34f):new Vector3(0,.65f,0);
        public float UnitVisualHeight(int kind)
        {if(kind<0||kind>=unitHeights.Length)throw new ArgumentOutOfRangeException(nameof(kind));return unitHeights[kind];}
        Mesh Own(DioramaMeshBuilder b,string name){var m=b.Build(name);owned.Add(m);return m;}
        public DioramaResources(GameContent content)
        {
            var shader=Resources.Load<Shader>("DefenseDiorama");if(shader==null)throw new InvalidOperationException("DefenseDiorama shader missing from Resources.");
            Material=new Material(shader){name="Shared mobile diorama material",enableInstancing=true,hideFlags=HideFlags.DontSave};
            int theme=content.Map.Theme;Sky=theme==1?C(0xD9BD91):theme==2?C(0xB4CED5):C(0x9DB38C);
            MapCenter=Point(new MapPoint(content.Map.Width/2,content.Map.Height/2),.15f);
            Terrain=BuildTerrain(content.Map,theme);
            var bounds=Terrain.bounds;
            foreach(var slot in content.Map.BuildSlots)
            {
                var point=Point(slot,.43f);bounds.Encapsulate(point+new Vector3(-1.2f,0,-1.2f));
                bounds.Encapsulate(point+new Vector3(1.2f,2.4f,1.2f));
            }
            WorldBounds=bounds;
            var b=new DioramaMeshBuilder();b.Cylinder(new Vector3(0,.025f,0),.64f,.1f,C(0x65715A),12);b.Cylinder(new Vector3(0,.11f,0),.54f,.12f,C(0x8F9980),12);b.Cylinder(new Vector3(0,.18f,0),.41f,.035f,C(0xC6A16C),12);
            for(int i=0;i<10;i++)
            {
                float angle=i*Mathf.PI/5;b.Box(new Vector3(Mathf.Cos(angle)*.51f,.17f,Mathf.Sin(angle)*.51f),new Vector3(.18f,.13f,.27f),Color.Lerp(C(0xA8AF98),C(0xD1D2B5),(i%3)*.22f),Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0));
            }
            Pad=Own(b,"Segmented stone build platform");
            b=new DioramaMeshBuilder();
            b.Cylinder(new Vector3(0,.025f,0),.66f,.1f,C(0x6C5D42),8);
            b.Cylinder(new Vector3(0,.11f,0),.57f,.12f,C(0xB18B42),8);
            b.Cylinder(new Vector3(0,.18f,0),.45f,.035f,C(0xE5C068),8);
            b.Ring(new Vector3(0,.185f,0),.57f,.085f,C(0xF1D38A),8);
            for(int i=0;i<3;i++)b.Crystal(new Vector3(-.36f+i*.31f,.25f,.46f),new Vector3(.13f,.22f+(i%2)*.08f,.12f),Gold);
            MinePad=Own(b,"Dedicated gold ore foundation");
            b=new DioramaMeshBuilder();b.Ring(Vector3.zero,.68f,.085f,Gold);Selection=Own(b,"Selected pad rim");
            b=new DioramaMeshBuilder();b.Ring(Vector3.zero,1,.024f,Deep,64);b.Ring(Vector3.zero,.976f,.022f,C(0xFFF5C4),64);Range=Own(b,"Ground range ring");
            b=new DioramaMeshBuilder();b.Ball(Vector3.zero,Vector3.one*.1f,Color.white,6,4);Bullet=Own(b,"Projectile bead");
            b=new DioramaMeshBuilder();for(int i=0;i<6;i++){float a=i*Mathf.PI/3;b.Crystal(new Vector3(Mathf.Cos(a)*.3f,.12f,Mathf.Sin(a)*.3f),Vector3.one*.08f,Color.white);}Burst=Own(b,"Impact sparks");
            b=new DioramaMeshBuilder();b.Box(Vector3.zero,new Vector3(.65f,.07f,.03f),C(0x75EC69));HealthBar=Own(b,"Health fill");
            b=new DioramaMeshBuilder();b.Box(Vector3.zero,new Vector3(.7f,.11f,.035f),Dark);HealthTrack=Own(b,"Health track");
            for(int i=0;i<Units.Length;i++){Units[i]=BuildUnit(i);ExpandUnitMotionBounds(Units[i]);unitHeights[i]=Units[i].bounds.max.y;}
            int towerCount=content.Towers.Length;TowerBases=new Mesh[towerCount,6];TowerHeads=new Mesh[towerCount,6];
            float height=0,radius=0;
            for(int k=0;k<towerCount;k++)for(int variant=0;variant<(k==4?3:6);variant++)
            {
                TowerBases[k,variant]=BuildTowerBase(k,variant);TowerHeads[k,variant]=BuildTowerHead(k,variant);
                foreach(var point in TowerBases[k,variant].vertices){height=Mathf.Max(height,point.y);radius=Mathf.Max(radius,new Vector2(point.x,point.z).magnitude);}
                foreach(var point in TowerHeads[k,variant].vertices)
                {
                    var p=point+TowerHeadOffset(k);
                    height=Mathf.Max(height,p.y);radius=Mathf.Max(radius,new Vector2(p.x,p.z).magnitude);
                }
            }
            MaximumTowerHeight=height*1.05f;MaximumTowerRadius=radius*1.05f;
            InitializeGraphicsSurfaceCache(content.Map.Id);
        }
        Mesh BuildTerrain(MapDefinition map,int theme)
        {
            var builder=new DioramaMeshBuilder();
            ScenicTerrain.Build(builder,map,map.Width,map.Height);
            return Own(builder,"Sculpted island, layered scenery and authoritative route");
        }
        Mesh BuildUnit(int kind)
        {
            if(kind>=4){var mesh=EliteUnitGeometry.Build(kind,0);owned.Add(mesh);return mesh;}
            var b=new DioramaMeshBuilder();
            if(kind==2)BuildTank(b);
            else if(kind==1)BuildSprinter(b);
            else BuildGolem(b,kind==3);
            return Own(b,"Toy troop class "+kind);
        }
        static void BuildGolem(DioramaMeshBuilder b,bool swarm)
        {
            float s=swarm?.76f:1;Color paint=swarm?Purple:Green,shade=Color.Lerp(paint,Dark,.25f);
            Vector3 P(float x,float y,float z)=>new Vector3(x,y,z)*s;
            b.Finish=.18f;b.Body(P(0,.52f,0),P(.31f,.42f,.27f),paint,10,5);
            for(int side=-1;side<=1;side+=2)
            {
                b.Finish=.12f;b.BevelBox(P(side*.145f,.11f,side*.025f+.05f),P(.24f,.22f,.32f),.045f*s,Steel);
                b.Finish=.2f;b.Body(P(side*.32f,.46f,.01f),P(.115f,.17f,.12f),shade,8,4);
                b.Body(P(side*.365f,.41f,.09f),P(.10f,.11f,.105f),paint,8,3);
                b.BevelBox(P(side*.235f,.77f,-.03f),P(.15f,.3f,.28f),.055f*s,shade,Quaternion.Euler(0,0,-side*18));
            }
            b.Finish=.22f;b.BevelBox(P(0,.86f,-.04f),P(.34f,.16f,.31f),.055f*s,paint,Quaternion.Euler(-10,0,0));
            b.Finish=.65f;b.Lathe(P(0,.32f,0),new[]{new Vector2(0,-.05f*s),new Vector2(.29f*s,-.05f*s),new Vector2(.33f*s,0),new Vector2(.29f*s,.055f*s),new Vector2(0,.055f*s)},Gold,10);
            b.BevelBox(P(0,.3f,.29f),P(.18f,.15f,.065f),.035f*s,Gold);
            Face(b,P(0,.615f,.246f),s,swarm?.82f:1);
            if(swarm){b.Finish=.8f;b.Shard(P(0,1.01f,-.02f),P(.11f,.24f,.12f),Gold,Quaternion.Euler(-12,0,0));}
            else
            {
                b.Finish=.15f;b.BevelBox(P(0,.22f,.19f),P(.27f,.16f,.12f),.045f*s,shade,Quaternion.Euler(-12,0,0));
                // A broad smile is readable without a face texture or a separate renderer.
                b.Finish=0;
                b.SurfaceQuad(P(-.07f,.425f,.279f),P(-.025f,.405f,.29f),P(-.022f,.424f,.291f),P(-.066f,.443f,.28f),Deep);
                b.SurfaceQuad(P(-.025f,.405f,.29f),P(.035f,.408f,.289f),P(.035f,.427f,.29f),P(-.022f,.424f,.291f),Deep);
                b.SurfaceQuad(P(.035f,.408f,.289f),P(.075f,.445f,.276f),P(.067f,.458f,.278f),P(.035f,.427f,.29f),Deep);
            }
        }
        static void Face(DioramaMeshBuilder b,Vector3 center,float scale,float eyeWidth=1)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 eye=center+new Vector3(side*.105f,0,0)*scale;
                b.Finish=.25f;b.Body(eye,new Vector3(.108f*eyeWidth,.151f,.055f)*scale,Cream,8,4);
                b.Finish=.45f;b.Body(eye+new Vector3(-side*.008f,-.002f,.052f)*scale,new Vector3(.043f,.079f,.014f)*scale,Deep,8,3);
                b.Finish=.6f;b.Body(eye+new Vector3(-side*.014f,.033f,.065f)*scale,Vector3.one*(.017f*scale),Color.white,5,3);
                b.Finish=.2f;b.BevelBox(eye+new Vector3(0,.149f,.016f)*scale,new Vector3(.21f,.064f,.07f)*scale,.018f*scale,Dark,Quaternion.Euler(0,0,side*16));
            }
        }
        static void BuildSprinter(DioramaMeshBuilder b)
        {
            // Broad racing helmet with a swept-back crest, rather than an upright cone.
            b.Finish=.72f;b.Shard(new Vector3(0,.69f,-.09f),new Vector3(.34f,.62f,.24f),Blue,Quaternion.Euler(-62,0,0));
            b.Body(new Vector3(0,.58f,0),new Vector3(.34f,.30f,.33f),Blue,10,5);
            b.BevelBox(new Vector3(0,.79f,-.055f),new Vector3(.48f,.14f,.38f),.045f,Blue,Quaternion.Euler(-20,0,0));
            for(int side=-1;side<=1;side+=2)
            {
                bool forward=side<0;
                var boot=new Vector3(side*.14f,forward?.105f:.20f,forward?.20f:-.19f);
                b.Finish=.15f;b.BevelBox(boot,new Vector3(.20f,.19f,.33f),.045f,Steel,Quaternion.Euler(forward?-8:20,side*5,0));
                b.Finish=.42f;b.BevelBox(new Vector3(side*.14f,forward?.27f:.34f,forward?.12f:-.12f),new Vector3(.13f,.22f,.14f),.03f,Blue,Quaternion.Euler(forward?20:-25,0,0));
                b.Finish=.7f;b.Cylinder(new Vector3(side*.14f,forward?.225f:.32f,forward?.175f:-.17f),.10f,.055f,Gold,8,-1,Quaternion.Euler(forward?12:-20,0,0));
                b.Shard(new Vector3(side*.32f,.64f,-.16f),new Vector3(.11f,.46f,.115f),Orange,Quaternion.Euler(-72,0,-side*12));
                b.Cylinder(new Vector3(side*.332f,.59f,.015f),.14f,.07f,Gold,10,-1,Quaternion.Euler(0,0,90));
                b.Finish=.4f;b.Cylinder(new Vector3(side*.375f,.59f,.015f),.085f,.024f,Steel,8,-1,Quaternion.Euler(0,0,90));
            }
            b.Finish=.35f;b.Body(new Vector3(0,.56f,.285f),new Vector3(.29f,.21f,.065f),Cream,10,4);
            Face(b,new Vector3(0,.575f,.335f),1.08f,1);
            b.Finish=.65f;b.BevelBox(new Vector3(0,.38f,.32f),new Vector3(.46f,.075f,.095f),.025f,Gold);
            b.BevelBox(new Vector3(0,.365f,.385f),new Vector3(.14f,.12f,.065f),.03f,Gold,Quaternion.Euler(0,0,45));
            b.Finish=.72f;b.Shard(new Vector3(0,.30f,.19f),new Vector3(.17f,.21f,.13f),Blue,Quaternion.Euler(180,0,0));
        }
        static void BuildTank(DioramaMeshBuilder b)
        {
            // A low, broad transport silhouette: deliberately no gun or turret.
            b.Finish=.38f;b.BevelBox(new Vector3(0,.33f,0),new Vector3(.82f,.42f,.91f),.09f,Orange);
            b.BevelBox(new Vector3(0,.55f,-.025f),new Vector3(.65f,.18f,.68f),.065f,Gold);
            b.BevelBox(new Vector3(0,.66f,-.07f),new Vector3(.43f,.10f,.43f),.035f,Orange);
            for(int side=-1;side<=1;side+=2)
            {
                b.Finish=.1f;b.BevelBox(new Vector3(side*.43f,.235f,0),new Vector3(.22f,.43f,1.09f),.085f,Deep);
                for(int wheel=0;wheel<4;wheel++)
                {
                    float z=-.35f+wheel*.235f;
                    b.Finish=.3f;b.Cylinder(new Vector3(side*.55f,.235f,z),.137f,.035f,Steel,8,-1,Quaternion.Euler(0,0,90));
                    b.Finish=.55f;b.Cylinder(new Vector3(side*.574f,.235f,z),.061f,.018f,Gold,6,-1,Quaternion.Euler(0,0,90));
                }
                b.Finish=.18f;
                for(int tread=0;tread<8;tread++)b.Box(new Vector3(side*.43f,.452f,-.44f+tread*.126f),new Vector3(.245f,.035f,.043f),Steel);
            }
            b.Finish=.2f;b.BevelBox(new Vector3(0,.36f,.469f),new Vector3(.62f,.12f,.045f),.02f,Cream);
            b.Finish=0;b.Box(new Vector3(0,.37f,.495f),new Vector3(.39f,.035f,.012f),Deep);
            b.Finish=.1f;for(int i=0;i<3;i++)b.Box(new Vector3(-.16f+i*.16f,.721f,-.1f),new Vector3(.07f,.012f,.23f),Steel);
        }
        static void Branches(int variant,out int damage,out int range)
        {
            damage=variant==1||variant==4?1:variant==3?2:0;
            range=variant==2||variant==4?1:variant==5?2:0;
        }
        static Color ClassPaint(int kind)=>kind==0?Green:kind==1?Orange:kind==2?Blue:kind==3?Purple:Gold;
        static void Foundation(DioramaMeshBuilder b,Color stone,Color accent)
        {
            b.Finish=.08f;
            b.Lathe(Vector3.zero,new[]{new Vector2(0,0),new Vector2(.53f,0),new Vector2(.57f,.07f),new Vector2(.53f,.22f),new Vector2(.43f,.30f),new Vector2(0,.30f)},stone,10);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;var rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
                b.BevelBox(new Vector3(Mathf.Cos(a)*.43f,.19f,Mathf.Sin(a)*.43f),new Vector3(.22f,.21f,.20f),.035f,Color.Lerp(stone,Cream,(i%3)*.05f),rotation);
            }
            b.Finish=.24f;b.BevelBox(new Vector3(0,.30f,.448f),new Vector3(.27f,.23f,.07f),.04f,Cream);
            b.BevelBox(new Vector3(0,.305f,.489f),new Vector3(.19f,.16f,.025f),.025f,accent);
        }
        Mesh ImportedTower(int kind,int variant,string part)
        {
            string key="DefenseUnlockModels/"+(kind==5?"tesla":"ballista")+"_v"+variant+"_"+part;
            var source=Resources.Load<Mesh>(key);
            if(source==null)throw new InvalidOperationException("Missing field model: "+key+". Run Prepare.");
            var mesh=UnityEngine.Object.Instantiate(source);mesh.hideFlags=HideFlags.DontSave;VisualEvolutionMeshQuality.SmoothTurnedSurfaces(mesh);owned.Add(mesh);return mesh;
        }
        Mesh BuildLaserBase(int variant)
        {
            var b=new DioramaMeshBuilder();var teal=C(0x38CFC1);var graphite=C(0x263C50);
            Foundation(b,C(0x8C9FA5),teal);
            b.Finish=.72f;b.Cylinder(new Vector3(0,.42f,0),.35f,.36f,graphite,12,.28f);
            for(int side=-1;side<=1;side+=2)
            {
                b.BevelBox(new Vector3(side*.34f,.43f,0),new Vector3(.14f,.42f,.44f),.035f,Steel,Quaternion.Euler(0,0,-side*12));
                b.Finish=.8f;b.Box(new Vector3(side*.36f,.47f,.23f),new Vector3(.065f,.25f,.035f),teal);
            }
            b.Cylinder(new Vector3(0,.64f,0),.31f,.09f,Gold,16);
            if(variant>0)b.Ring(new Vector3(0,.69f,0),.3f,.045f,Cream,24);
            if(variant>2)for(int i=0;i<4;i++)
            {float a=i*Mathf.PI*.5f;b.Shard(new Vector3(Mathf.Cos(a)*.38f,.48f,Mathf.Sin(a)*.38f),new Vector3(.07f,.15f,.07f),teal);}
            return Own(b,"Laser prism foundation v"+variant);
        }
        Mesh BuildLaserHead(int variant)
        {
            var b=new DioramaMeshBuilder();var teal=C(0x5EF4DE);var graphite=C(0x263C50);int power=variant<3?variant:variant==5?2:variant==3?1:0;
            b.Finish=.7f;b.BevelBox(new Vector3(0,.12f,.08f),new Vector3(.52f,.25f,.68f),.055f,graphite);
            b.BevelBox(new Vector3(0,.29f,.16f),new Vector3(.42f,.19f,.51f),.04f,Steel);
            b.Finish=.88f;b.Shard(new Vector3(0,.32f,.27f),new Vector3(.18f,.37f+power*.055f,.18f),teal,Quaternion.Euler(90,0,0));
            var turn=Quaternion.Euler(90,0,0);
            b.Tube(new Vector3(0,.32f,.58f),.26f+power*.02f,.19f,.10f,Gold,16,turn);
            b.Tube(new Vector3(0,.32f,.76f),.21f+power*.02f,.15f,.09f,Cream,16,turn);
            for(int side=-1;side<=1;side+=2)
            {
                b.Finish=.7f;b.BevelBox(new Vector3(side*.28f,.23f,.33f),new Vector3(.10f,.30f,.63f),.025f,graphite);
                b.Box(new Vector3(side*.285f,.23f,.66f),new Vector3(.055f,.18f,.07f),teal);
                if(variant>=3)b.Tube(new Vector3(side*.33f,.39f,-.04f),.12f,.065f,.09f,Gold,12,turn);
            }
            return Own(b,"Laser optical emitter v"+variant);
        }

        Mesh BuildTowerBase(int kind,int variant)
        {
            if(kind==7)return BuildLaserBase(variant);
            if(kind>=5)return ImportedTower(kind,variant,"Base");
            var b=new DioramaMeshBuilder();Color paint=ClassPaint(kind);
            if(kind==4)BuildMineBase(b,variant);
            else
            {
                Branches(variant,out int damage,out int range);
                Foundation(b,kind==2?C(0x829BA8):Stone,paint);
                b.Finish=.4f;b.Lathe(new Vector3(0,.29f,0),new[]{new Vector2(0,0),new Vector2(.32f,0),new Vector2(.31f,.11f),new Vector2(.25f,.28f),new Vector2(.3f,.34f),new Vector2(0,.34f)},paint,10);
                b.Finish=.65f;b.Lathe(new Vector3(0,.57f,0),new[]{new Vector2(0,0),new Vector2(.33f,0),new Vector2(.35f,.045f),new Vector2(.32f,.10f),new Vector2(0,.10f)},kind==2?Cream:Gold,12);
                if(kind==1)
                {
                    for(int side=-1;side<=1;side+=2)
                    {
                        b.Finish=.12f;b.Lathe(new Vector3(side*.49f,.31f,0),new[]{new Vector2(0,-.07f),new Vector2(.22f,-.07f),new Vector2(.28f,-.03f),new Vector2(.28f,.055f),new Vector2(.22f,.08f),new Vector2(0,.08f)},Steel,10,Quaternion.Euler(0,0,90));
                        b.Finish=.6f;b.Cylinder(new Vector3(side*.59f,.31f,0),.115f,.055f,Gold,8,-1,Quaternion.Euler(0,0,90));
                    }
                }
                if(kind==2)for(int i=0;i<4;i++)
                {
                    float a=i*Mathf.PI*.5f;b.Finish=.7f;
                    b.Shard(new Vector3(Mathf.Cos(a)*.29f,.43f,Mathf.Sin(a)*.29f),new Vector3(.095f,.19f,.095f),Blue);
                }
                b.Finish=.6f;for(int i=0;i<damage+range;i++)b.BevelBox(new Vector3(-.08f+i*.16f,.34f,-.30f),new Vector3(.09f,.1f,.035f),.012f,Gold);
            }
            return Own(b,"Toy tower base "+kind+" variant "+variant);
        }
        static void BuildMineBase(DioramaMeshBuilder b,int level)
        {
            b.Finish=.08f;b.Lathe(Vector3.zero,new[]{new Vector2(0,0),new Vector2(.53f,0),new Vector2(.57f,.07f),new Vector2(.48f,.16f),new Vector2(0,.16f)},Stone,10);
            for(int side=-1;side<=1;side+=2)
            {
                b.Body(new Vector3(side*.36f,.43f,-.08f),new Vector3(.26f,.43f,.37f),Stone,8,4);
                b.Body(new Vector3(side*.45f,.19f,.22f),new Vector3(.22f,.19f,.23f),Color.Lerp(Stone,Cream,.12f),7,3);
            }
            b.Body(new Vector3(0,.61f,-.23f),new Vector3(.46f,.3f,.25f),Color.Lerp(Stone,Steel,.18f),8,4);
            b.Finish=0;b.BevelBox(new Vector3(0,.42f,.23f),new Vector3(.55f,.65f,.075f),.055f,Deep);
            for(int side=-1;side<=1;side+=2)
            {
                b.Finish=.18f;b.BevelBox(new Vector3(side*.34f,.49f,.30f),new Vector3(.16f,.94f,.20f),.035f,Wood,Quaternion.Euler(0,0,-side*8));
                b.Finish=.3f;b.BevelBox(new Vector3(side*.34f,.48f,.414f),new Vector3(.19f,.10f,.035f),.015f,Steel);
                b.Finish=.65f;b.Cylinder(new Vector3(side*.34f,.48f,.444f),.036f,.016f,Gold,6,-1,Quaternion.Euler(90,0,0));
            }
            b.Finish=.18f;b.BevelBox(new Vector3(0,.99f,.29f),new Vector3(.96f,.19f,.24f),.045f,LightWood);
            for(int side=-1;side<=1;side+=2)b.BevelBox(new Vector3(side*.19f,1.17f,.29f),new Vector3(.10f,.28f,.13f),.025f,Wood,Quaternion.Euler(0,0,-side*12));
            b.Finish=.32f;b.Cylinder(new Vector3(0,.80f,.44f),.022f,.44f,Gold,6);
            b.BevelBox(new Vector3(0,.58f,.45f),new Vector3(.14f,.13f,.12f),.025f,Steel);
            for(int i=0;i<3+level;i++)
            {
                b.Finish=.8f;b.Shard(new Vector3(-.41f+(i%2)*.16f,.24f+(i/2)*.14f,.29f-(i/2)*.13f),new Vector3(.105f,.23f,.11f),Gold,Quaternion.Euler(0,i*33,-15+i*7));
            }
            MineCart(b,new Vector3(.41f,.12f,.48f));
            if(level>0)
            {
                b.Finish=.65f;b.BevelBox(new Vector3(0,1.015f,.425f),new Vector3(.36f,.09f,.035f),.018f,Gold);
                for(int side=-1;side<=1;side+=2)b.BevelBox(new Vector3(side*.43f,.81f,.16f),new Vector3(.13f,.28f,.14f),.025f,Gold);
            }
            if(level>1)MineCart(b,new Vector3(-.37f,.12f,-.39f));
        }
        static void MineCart(DioramaMeshBuilder b,Vector3 p)
        {
            b.Finish=.22f;b.BevelBox(p+Vector3.up*.12f,new Vector3(.34f,.25f,.40f),.045f,Steel);
            b.Finish=.45f;b.BevelBox(p+Vector3.up*.26f,new Vector3(.39f,.065f,.45f),.02f,Stone);
            for(int side=-1;side<=1;side+=2)for(int axle=-1;axle<=1;axle+=2)
            {
                b.Finish=.25f;b.Cylinder(p+new Vector3(side*.19f,0,axle*.13f),.10f,.045f,Wood,8,-1,Quaternion.Euler(0,0,90));
                b.Finish=.55f;b.Cylinder(p+new Vector3(side*.217f,0,axle*.13f),.039f,.016f,Steel,6,-1,Quaternion.Euler(0,0,90));
            }
            b.Finish=.8f;for(int i=0;i<3;i++)b.Shard(p+new Vector3(-.10f+i*.095f,.32f,(i%2)*.11f-.04f),new Vector3(.075f,.12f,.08f),Gold,Quaternion.Euler(0,i*30,i*12-10));
        }
        Mesh BuildTowerHead(int kind,int variant)
        {
            if(kind==7)return BuildLaserHead(variant);
            if(kind>=5)return ImportedTower(kind,variant,"Head");
            var b=new DioramaMeshBuilder();Branches(variant,out int damage,out int range);
            if(kind==0)RapidHead(b,damage,range);
            else if(kind==1)CannonHead(b,damage,range);
            else if(kind==2)FrostHead(b,damage,range);
            else if(kind==3)BreakerHead(b,damage,range);
            else MineWheel(b,variant);
            return Own(b,"Toy tower head "+kind+" variant "+variant);
        }
        static void Barrel(DioramaMeshBuilder b,Vector3 center,float length,float radius,Quaternion rotation,Color ring)
        {
            float ringBack=length*.5f-.085f,ringFront=length*.5f+.015f,bore=radius*.68f;
            // Join the three surfaces at their edges. Two complete tubes at the same
            // inner radius produced coplanar steel/cream walls inside the muzzle.
            b.Finish=.5f;b.Lathe(center,new[]{new Vector2(0,-length*.5f),new Vector2(radius,-length*.5f),new Vector2(radius,ringBack),new Vector2(0,ringBack)},Steel,10,rotation);
            b.Finish=.62f;b.Lathe(center,new[]{new Vector2(radius,ringBack),new Vector2(radius*1.13f,ringBack),new Vector2(radius*1.13f,ringFront),new Vector2(bore,ringFront)},ring,10,rotation);
            // The matte cavity closes in front of the turret shell, below the lip.
            // Its back plane is separated from the steel body cap by .012 world units.
            b.Finish=0;b.Lathe(center,new[]{new Vector2(bore,ringFront),new Vector2(bore,ringBack+.012f),new Vector2(0,ringBack+.012f)},Deep,10,rotation);
        }
        static void RapidHead(DioramaMeshBuilder b,int damage,int range)
        {
            b.Finish=.45f;b.Body(new Vector3(0,.24f,-.08f),new Vector3(.42f,.38f,.40f),Green,10,5);
            for(int side=-1;side<=1;side+=2)
            {
                b.BevelBox(new Vector3(side*.37f,.25f,.01f),new Vector3(.19f,.48f,.51f),.06f,Green);
                b.Finish=.6f;b.Cylinder(new Vector3(side*.485f,.24f,-.025f),.155f,.055f,Gold,10,-1,Quaternion.Euler(0,0,90));
                b.Cylinder(new Vector3(side*.52f,.24f,-.025f),.098f,.025f,Cream,8,-1,Quaternion.Euler(0,0,90));
            }
            b.Finish=.65f;b.BevelBox(new Vector3(0,.625f,-.05f),new Vector3(.44f,.14f,.38f),.045f,Gold);
            b.Finish=.42f;b.BevelBox(new Vector3(0,-.015f,.28f),new Vector3(.69f,.12f,.38f),.04f,Green);
            float length=.58f+range*.24f,radius=.112f+damage*.018f,z=.35f+range*.12f;
            var axis=Quaternion.Euler(90,0,0);
            Barrel(b,new Vector3(0,.39f,z),length,radius,axis,Cream);
            Barrel(b,new Vector3(-.175f,.13f,z),length,radius,axis,Cream);
            Barrel(b,new Vector3(.175f,.13f,z),length,radius,axis,Cream);
            if(damage>0)for(int side=-1;side<=1;side+=2)
            {
                b.Finish=.4f;b.BevelBox(new Vector3(side*.46f,.20f,-.24f),new Vector3(.19f,.31f,.38f),.04f,Green);
                b.Finish=.65f;for(int i=0;i<2+damage;i++)b.Cylinder(new Vector3(side*.465f,.42f,-.37f+i*.08f),.033f,.15f,Gold,6);
            }
            if(damage>1)
            {
                b.Finish=.35f;b.BevelBox(new Vector3(0,-.14f,.42f),new Vector3(.75f,.13f,.29f),.035f,Steel);
                for(int i=0;i<3;i++)b.Box(new Vector3(0,-.063f,.18f+i*.12f),new Vector3(.66f,.035f,.036f),Gold);
            }
            if(range>0)Scope(b,new Vector3(0,.79f,-.02f),range);
        }
        static void CannonHead(DioramaMeshBuilder b,int damage,int range)
        {
            b.Finish=.5f;b.Body(new Vector3(0,.28f,-.11f),new Vector3(.47f,.43f,.45f),Orange,10,6);
            var axis=Quaternion.Euler(80,0,0);float radius=.255f+damage*.045f,length=.72f+range*.32f;
            Barrel(b,new Vector3(0,.31f,.32f+range*.16f),length,radius,axis,Cream);
            b.Finish=.68f;b.BevelBox(new Vector3(0,.69f,-.11f),new Vector3(.34f,.13f,.43f),.045f,Gold);
            for(int side=-1;side<=1;side+=2)
            {
                b.Finish=.5f;b.BevelBox(new Vector3(side*.39f,.16f,.12f),new Vector3(.16f,.40f,.40f),.055f,Gold,Quaternion.Euler(-13,0,0));
                b.Cylinder(new Vector3(side*.49f,.30f,.08f),.135f,.045f,Gold,10,-1,Quaternion.Euler(0,0,90));
            }
            if(damage>0)for(int side=-1;side<=1;side+=2)
            {
                b.Finish=.38f;b.BevelBox(new Vector3(side*.49f,.17f,-.26f),new Vector3(.18f,.31f,.54f),.035f,Orange);
                b.Finish=.65f;b.Cylinder(new Vector3(side*.49f,.39f,-.35f),.10f,.09f,Gold,8);
            }
            if(damage>1)
            {
                b.Finish=.4f;b.BevelBox(new Vector3(0,.29f,-.48f),new Vector3(.77f,.40f,.23f),.06f,Steel);
                b.Finish=.65f;b.BevelBox(new Vector3(0,.51f,-.47f),new Vector3(.59f,.09f,.26f),.025f,Gold);
            }
            if(range>0)Scope(b,new Vector3(.30f,.69f,.12f),range);
        }
        static void FrostHead(DioramaMeshBuilder b,int damage,int range)
        {
            b.Finish=.75f;
            b.Shard(new Vector3(0,.59f+range*.09f+damage*.04f,0),new Vector3(.27f+damage*.035f,.88f+range*.15f+damage*.07f,.27f+damage*.035f),Blue);
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f;var p=new Vector3(Mathf.Cos(a)*.42f,.19f,Mathf.Sin(a)*.42f);
                b.Finish=.28f;b.BevelBox(p-Vector3.up*.10f,new Vector3(.20f,.16f,.20f),.03f,Cream);
                b.Finish=.75f;b.Shard(p+Vector3.up*.10f,new Vector3(.11f,.30f,.11f),Color.Lerp(Blue,Cream,.3f));
            }
            b.Finish=.45f;b.Tube(new Vector3(0,.09f,0),.55f,.42f,.13f,Cream,12);
            if(damage==1)
            {
                b.Finish=.8f;for(int side=-1;side<=1;side+=2)b.Shard(new Vector3(side*.39f,.56f,-.04f),new Vector3(.18f,.66f,.18f),Blue,Quaternion.Euler(0,0,-side*19));
            }
            if(damage==2)
            {
                // A five-pronged crown changes the outline substantially from the twin
                // side crystals of Damage I, including at the overview camera distance.
                for(int i=0;i<5;i++)
                {
                    float a=(i/5f+.05f)*Mathf.PI*2;var direction=new Vector3(Mathf.Cos(a)*.38f,1,Mathf.Sin(a)*.38f);
                    b.Finish=.82f;b.Shard(new Vector3(Mathf.Cos(a)*.38f,.62f,Mathf.Sin(a)*.38f),new Vector3(.22f,1.05f,.22f),i%2==0?Blue:Color.Lerp(Blue,Cream,.28f),Quaternion.FromToRotation(Vector3.up,direction));
                    b.Finish=.5f;b.BevelBox(new Vector3(Mathf.Cos(a)*.52f,.18f,Mathf.Sin(a)*.52f),new Vector3(.22f,.16f,.20f),.03f,Gold,Quaternion.Euler(0,-a*Mathf.Rad2Deg,0));
                }
            }
            for(int focus=0;focus<range;focus++)
            {
                float y=.94f+focus*.52f,radius=.56f-focus*.11f;
                b.Finish=.65f;b.Tube(new Vector3(0,y,0),radius,radius-.075f,.09f,focus==0?Gold:Cream,16);
                for(int i=0;i<3;i++)
                {
                    float a=i*Mathf.PI*2/3;b.Finish=.8f;b.Shard(new Vector3(Mathf.Cos(a)*radius,y+.05f,Mathf.Sin(a)*radius),new Vector3(.06f,.12f,.06f),Blue);
                }
            }
        }
        static void BreakerHead(DioramaMeshBuilder b,int damage,int range)
        {
            b.Finish=.48f;b.BevelBox(new Vector3(0,.25f,-.08f),new Vector3(.76f,.50f,.61f),.11f,Purple);
            float length=.92f+range*.31f,front=.25f+length*.5f;
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*(.21f+damage*.025f);
                b.Finish=.45f;b.BevelBox(new Vector3(x,.26f,.29f+range*.15f),new Vector3(.16f+damage*.025f,.18f+damage*.02f,length),.035f,Steel);
                b.Finish=.6f;b.Tube(new Vector3(x,.26f,front+range*.15f),.135f+damage*.012f,.079f,.13f,Cream,4,Quaternion.Euler(90,0,45));
                b.Finish=0;b.Box(new Vector3(x,.26f,front+range*.15f-.035f),new Vector3(.11f,.11f,.012f),Deep);
                b.Finish=.6f;b.Cylinder(new Vector3(side*.40f,.23f,-.12f),.145f,.055f,Gold,8,-1,Quaternion.Euler(0,0,90));
            }
            b.Finish=.8f;b.BevelBox(new Vector3(0,.30f,.26f),new Vector3(.19f,.20f,.67f),.035f,Gold);
            for(int i=0;i<3;i++)b.BevelBox(new Vector3(0,.42f,-.02f+i*.18f),new Vector3(.16f,.055f,.07f),.015f,Cream);
            if(damage>0)for(int side=-1;side<=1;side+=2)
            {
                b.Finish=.55f;b.Lathe(new Vector3(side*.49f,.23f,-.22f),new[]{new Vector2(0,-.20f),new Vector2(.14f+damage*.025f,-.20f),new Vector2(.17f+damage*.025f,-.14f),new Vector2(.17f+damage*.025f,.16f),new Vector2(.13f+damage*.025f,.23f),new Vector2(0,.23f)},Purple,8);
                b.Finish=.75f;b.Cylinder(new Vector3(side*.49f,.48f,-.22f),.14f+damage*.025f,.055f,Gold,8);
            }
            if(damage>1)
            {
                b.Finish=.45f;b.BevelBox(new Vector3(0,.10f,.40f),new Vector3(.86f,.16f,.73f),.045f,Purple);
                b.Finish=.7f;for(int side=-1;side<=1;side+=2)b.BevelBox(new Vector3(side*.37f,.25f,.62f),new Vector3(.10f,.20f,.16f),.02f,Gold);
            }
            if(range>0)Scope(b,new Vector3(0,.67f,-.03f),range);
        }
        static void MineWheel(DioramaMeshBuilder b,int level)
        {
            var axis=Quaternion.Euler(90,0,0);
            b.Finish=.25f;b.Tube(Vector3.zero,.28f,.215f,.105f,LightWood,12,axis);
            for(int i=0;i<6;i++)b.Box(Vector3.zero,new Vector3(.055f,.45f,.075f),Wood,Quaternion.Euler(0,0,i*30));
            b.Finish=.5f;b.Cylinder(new Vector3(0,0,.065f),.085f,.05f,Steel,10,-1,axis);
            if(level>0){b.Finish=.65f;b.Tube(new Vector3(0,0,-.055f),.23f,.19f,.035f,Gold,12,axis);}
            if(level>1)
            {
                b.Finish=.6f;b.Tube(new Vector3(0,0,.06f),.29f,.26f,.035f,Gold,12,axis);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;b.Box(new Vector3(Mathf.Cos(a)*.285f,Mathf.Sin(a)*.285f,.04f),new Vector3(.075f,.075f,.07f),Gold,Quaternion.Euler(0,0,a*Mathf.Rad2Deg));}
            }
        }
        static void Scope(DioramaMeshBuilder b,Vector3 point,int range)
        {
            b.Finish=.4f;b.BevelBox(point-Vector3.up*.12f,new Vector3(.12f,.22f,.16f),.025f,Steel);
            b.Finish=.65f;b.Tube(point,.10f,.065f,.33f+range*.09f,Gold,8,Quaternion.Euler(90,0,0));
            b.Finish=.85f;b.Cylinder(point+Vector3.forward*(.17f+range*.045f),.069f,.024f,Blue,8,-1,Quaternion.Euler(90,0,0));
            if(range>1)
            {
                b.Finish=.5f;b.BevelBox(point+new Vector3(0,.13f,.04f),new Vector3(.44f,.09f,.14f),.025f,Cream);
                b.Finish=.8f;for(int side=-1;side<=1;side+=2)b.Shard(point+new Vector3(side*.20f,.20f,.04f),new Vector3(.045f,.11f,.045f),Blue);
            }
        }
        public void Dispose(){DisposeEvolution();for(int i=0;i<owned.Count;i++)Release(owned[i]);owned.Clear();if(Material!=null)Release(Material);}
        static void Release(UnityEngine.Object value){if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
    }
}
