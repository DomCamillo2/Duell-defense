using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseDuell.Presentation
{
    /// <summary>
    /// Optional presentation decoration: at most two renderers, one material, fixed buffers.
    /// Caller owns update timing and feeds positions already traversed by BoardView.
    /// These soft ellipses are contact cues, not physically cast/terrain-projected shadows.
    /// </summary>
    public sealed class ContactFxBatch : IDisposable
    {
        readonly Transform parent;
        readonly ContactShadowSelection shadowSelection=new ContactShadowSelection();
        readonly ContactFxEvents events=new ContactFxEvents();
        readonly QuadBatch shadows,effects;
        readonly Material material;
        readonly bool smallView;
        Matrix4x4 viewProjection;
        bool collecting,disposed,reduced,power;
        double nextShadow,nextEffect,lastTime=-1;
        public int ShadowCount=>shadowSelection.Count;
        public int ActiveEffectCount=>events.ActiveCount;
        public const float OpenGroundPlane=.184f,RouteGroundPlane=.214f;

        public ContactFxBatch(Transform boardRoot,int layer,Shader shader,bool preview=false)
        {
            if(boardRoot==null)throw new ArgumentNullException(nameof(boardRoot));
            if(shader==null)throw new ArgumentNullException(nameof(shader));
            parent=boardRoot;smallView=preview;
            material=new Material(shader){name="Shared contact and feedback coverage",hideFlags=HideFlags.DontSave};
            shadows=new QuadBatch("Combined contact shadows",parent,layer,ContactShadowSelection.Capacity,material);
            effects=new QuadBatch("Combined bounded dust and sparks",parent,layer,ContactFxEvents.Capacity*6,material);
        }
        public void SetOptions(bool reducedMotion,bool lowPower)
        {
            if(disposed)return;
            bool changed=power!=lowPower;reduced=reducedMotion;power=lowPower;
            if(reduced||smallView){events.Clear();effects.Hide();}
            if(changed){nextShadow=nextEffect=0;}
        }
        public bool BeginShadows(double now,Camera camera)
        {
            collecting=false;
            if(disposed||camera==null||!ValidTime(now))return false;
            if(now<lastTime){Clear();}lastTime=now;
            if(now<nextShadow)return false;
            nextShadow=now+(smallView?1.0/12:power?1.0/15:1.0/30);
            viewProjection=camera.projectionMatrix*camera.worldToCameraMatrix*parent.localToWorldMatrix;
            shadowSelection.Begin(power||smallView);collecting=true;return true;
        }
        public void OfferShadow(long stableId,Vector3 groundCenter,Vector2 radii,float opacity,int priority=0)
        {
            if(!collecting||disposed)return;
            Vector4 clip=viewProjection*new Vector4(groundCenter.x,groundCenter.y,groundCenter.z,1);
            if(clip.w<=0||clip.z < -clip.w||clip.z > clip.w)return;
            float extentX=Mathf.Abs(viewProjection.m00)*radii.x+Mathf.Abs(viewProjection.m02)*radii.y;
            float extentY=Mathf.Abs(viewProjection.m10)*radii.x+Mathf.Abs(viewProjection.m12)*radii.y;
            if(Mathf.Abs(clip.x)>clip.w+extentX||Mathf.Abs(clip.y)>clip.w+extentY)return;
            shadowSelection.Offer(new ContactShadowSample{StableId=stableId,Priority=priority,X=groundCenter.x,Y=groundCenter.y,Z=groundCenter.z,
                RadiusX=radii.x,RadiusZ=radii.y,Opacity=opacity,ViewportX=clip.x/clip.w*.5f+.5f,ViewportY=clip.y/clip.w*.5f+.5f});
        }
        public bool EndShadows()
        {
            if(!collecting||disposed)return false;collecting=false;
            shadowSelection.Select();shadows.Begin();
            for(int i=0;i<shadowSelection.Count;i++)
            {
                var s=shadowSelection[i];
                shadows.Quad(new Vector3(s.X,s.Y,s.Z),Vector3.right*s.RadiusX,Vector3.forward*s.RadiusZ,
                    new Color(.075f,.105f,.09f,s.Opacity),0,.26f);
            }
            shadows.Upload();return true;
        }
        public bool Trigger(ContactFxKind kind,Vector3 position,Color color,double now,float radius=0)
        {
            if(disposed||smallView)return false;
            return events.Trigger(kind,position.x,position.y,position.z,color.r,color.g,color.b,now,reduced,power,radius);
        }
        public bool UpdateEffects(double now,Camera camera)
        {
            if(disposed||camera==null||!ValidTime(now))return false;
            if(reduced||smallView){events.Clear();effects.Hide();return false;}
            events.Tick(now,false,power);
            if(events.ActiveCount==0){bool wasVisible=effects.Visible;effects.Hide();return wasVisible;}
            if(now<nextEffect)return false;nextEffect=now+(power?1.0/20:1.0/30);
            Vector3 right=parent.InverseTransformDirection(camera.transform.right),up=parent.InverseTransformDirection(camera.transform.up);
            effects.Begin();
            for(int i=0;i<ContactFxEvents.Capacity;i++)
            {
                var e=events[i];if(!e.Active)continue;
                float t=Mathf.Clamp01((float)((now-e.Began)/e.Duration)),fade=(1-t)*(1-t);
                Vector3 center=new Vector3(e.X,e.Y,e.Z);Color color=new Color(e.R,e.G,e.B);
                if(e.Kind==ContactFxKind.Explosion)
                {
                    // The outer edge is the actual splash blastRadius from the shot snapshot.
                    // Only the inner burst expands; it never suggests a larger damage area.
                    float blastRadius=e.Radius;
                    effects.Quad(center,Vector3.right*blastRadius,Vector3.forward*blastRadius,
                        new Color(1,.61f,.18f,.70f*(1-t)),.91f,.98f);
                    float burst=blastRadius*(.18f+.70f*(1-Mathf.Pow(1-t,3)));
                    effects.Quad(center+Vector3.up*.002f,Vector3.right*burst,Vector3.forward*burst,
                        new Color(1,.41f,.08f,.30f*fade),0,.55f);
                    int sparks=power?2:4;
                    for(int spark=0;spark<sparks;spark++)
                    {
                        float angle=(spark/(float)sparks+.125f)*Mathf.PI*2;
                        Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        float size=blastRadius*(.035f+.025f*t);
                        effects.Quad(center+direction*(blastRadius*(.12f+t*.55f))+Vector3.up*(.08f+blastRadius*.12f*Mathf.Sin(t*Mathf.PI)),
                            right*size,up*size,new Color(1,.83f,.42f,.8f*fade),0,.35f);
                    }
                    continue;
                }
                if(e.Kind==ContactFxKind.Impact)
                {
                    for(int spark=0;spark<4;spark++)
                    {
                        float angle=(spark+.18f)*Mathf.PI*.5f;
                        Vector3 direction=right*Mathf.Cos(angle)+up*Mathf.Sin(angle);
                        Vector3 tangent=-right*Mathf.Sin(angle)+up*Mathf.Cos(angle);
                        effects.Quad(center+direction*(.035f+t*.22f),direction*(.035f+.025f*t),tangent*.016f,
                            new Color(color.r,color.g,color.b,.65f*fade),0,.3f);
                    }
                    continue;
                }
                bool upgrade=e.Kind==ContactFxKind.Upgrade;
                float radius=upgrade?.45f+t*.36f:.40f+t*.39f;
                Color rim=upgrade?new Color(.91f,.76f,.40f,.44f*fade):new Color(.80f,.72f,.53f,.26f*fade);
                effects.Quad(center+Vector3.up*.003f,Vector3.right*radius,Vector3.forward*radius,rim,.76f,.91f);
                int count=power?3:5;
                for(int particle=0;particle<count;particle++)
                {
                    float angle=(particle/(float)count+.12f)*Mathf.PI*2;
                    Vector3 spread=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    Vector3 p=center+spread*(.27f+t*.30f)+Vector3.up*(.06f+Mathf.Sin(t*Mathf.PI)*.18f);
                    float size=upgrade?.035f:.055f+t*.055f;
                    Color tint=upgrade?new Color(.96f,.82f,.47f,.65f*fade):new Color(.72f,.67f,.53f,.24f*fade);
                    effects.Quad(p,right*size,up*(upgrade?size*1.6f:size),tint,0,.14f);
                }
            }
            effects.Upload();return true;
        }
        public void Clear()
        {
            if(disposed)return;collecting=false;shadowSelection.Begin(power||smallView);events.Clear();
            shadows.Hide();effects.Hide();nextShadow=nextEffect=0;lastTime=-1;
        }
        public void CancelTransient()
        {if(disposed)return;events.Clear();effects.Hide();nextEffect=0;}
        static bool ValidTime(double now)=>!double.IsNaN(now)&&!double.IsInfinity(now)&&now>=0;
        public void Dispose()
        {
            if(disposed)return;Clear();disposed=true;shadows.Dispose();effects.Dispose();Release(material);
        }
        static void Release(UnityEngine.Object value)
        {if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}

        sealed class QuadBatch : IDisposable
        {
            readonly GameObject go;
            readonly Mesh mesh;
            readonly MeshRenderer renderer;
            readonly Vector3[] vertices;
            readonly Color32[] colors;
            readonly Vector2[] uv,shape;
            readonly int[] indices;
            int quads,lastQuads;
            Vector3 minimum,maximum;
            public bool Visible=>renderer!=null&&renderer.enabled;
            public QuadBatch(string name,Transform parent,int layer,int capacity,Material material)
            {
                vertices=new Vector3[capacity*4];colors=new Color32[capacity*4];uv=new Vector2[capacity*4];shape=new Vector2[capacity*4];indices=new int[capacity*6];
                for(int i=0;i<capacity;i++)
                {
                    int v=i*4,t=i*6;uv[v]=new Vector2(-1,-1);uv[v+1]=new Vector2(-1,1);uv[v+2]=new Vector2(1,1);uv[v+3]=new Vector2(1,-1);
                    indices[t]=v;indices[t+1]=v+1;indices[t+2]=v+2;indices[t+3]=v;indices[t+4]=v+2;indices[t+5]=v+3;
                }
                go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer,hideFlags=HideFlags.DontSave};go.transform.SetParent(parent,false);
                mesh=new Mesh{name=name,hideFlags=HideFlags.DontSave};mesh.MarkDynamic();
                // Fixed native buffer sizes: unused quads are degenerate, not repeatedly
                // allocated/reindexed as units enter and leave the visible selection.
                mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetUVs(1,shape);mesh.SetTriangles(indices,0,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;
                renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;renderer.enabled=false;
            }
            public void Begin(){quads=0;minimum=Vector3.one*float.PositiveInfinity;maximum=Vector3.one*float.NegativeInfinity;}
            public void Quad(Vector3 center,Vector3 right,Vector3 up,Color color,float innerRadius,float outerFadeStart)
            {
                if(quads>=vertices.Length/4)return;int v=quads++*4;
                vertices[v]=center-right-up;vertices[v+1]=center-right+up;vertices[v+2]=center+right+up;vertices[v+3]=center+right-up;
                Color32 c=color;var profile=new Vector2(innerRadius,outerFadeStart);
                for(int i=0;i<4;i++){colors[v+i]=c;shape[v+i]=profile;minimum=Vector3.Min(minimum,vertices[v+i]);maximum=Vector3.Max(maximum,vertices[v+i]);}
            }
            public void Upload()
            {
                if(quads==0){Hide();return;}
                for(int i=quads*4;i<lastQuads*4;i++){vertices[i]=Vector3.zero;colors[i]=default;}
                lastQuads=quads;mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(1,shape);
                mesh.bounds=new Bounds((minimum+maximum)*.5f,maximum-minimum+Vector3.one*.01f);renderer.enabled=true;
            }
            public void Hide(){if(renderer!=null)renderer.enabled=false;}
            public void Dispose(){Hide();Release(mesh);Release(go);}
        }
    }
}
