using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseDuell.Presentation
{
    /// <summary>One bounded mesh for snapshot-derived lightning and laser pulses.</summary>
    public sealed class EnergyBeamBatch : IDisposable
    {
        struct Pulse { public Vector3 From,To; public double Began,Until; public bool Jagged; public int Seed; }
        readonly Pulse[] pulses;
        readonly List<Vector3> vertices=new List<Vector3>(2304);
        readonly List<Color32> colors=new List<Color32>(2304);
        readonly List<int> indices=new List<int>(3456);
        readonly GameObject root;
        readonly Mesh mesh;
        readonly Material material;
        int serial;
        double lastTime=double.NegativeInfinity;
        public int ActiveCount { get; private set; }
        public int Capacity=>pulses.Length;
        public Mesh DisplayMesh=>mesh;

        public EnergyBeamBatch(Transform parent,int layer,bool preview)
        {
            pulses=new Pulse[preview?8:24];
            var shader=Resources.Load<Shader>("DefenseEnergy");
            if(shader==null)throw new InvalidOperationException("DefenseEnergy shader is missing.");
            material=new Material(shader){name="Shared lightning and laser glow",hideFlags=HideFlags.DontSave};
            mesh=new Mesh{name="Bounded lightning and laser strips",hideFlags=HideFlags.DontSave};mesh.MarkDynamic();
            root=new GameObject("Lightning and laser pulses",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};
            root.transform.SetParent(parent,false);root.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=root.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }

        public bool Trigger(Vector3 from,Vector3 to,bool lightning,double now,bool reduced)
        {
            if(!Finite(from)||!Finite(to)||now<0||double.IsNaN(now)||double.IsInfinity(now)||(to-from).sqrMagnitude<.00001f)return false;
            if(now<lastTime)Clear();
            int slot=0;
            for(int i=0;i<pulses.Length;i++)
            {if(pulses[i].Until<=now){slot=i;break;}if(pulses[i].Until<pulses[slot].Until)slot=i;}
            pulses[slot]=new Pulse{From=from,To=to,Began=now,Until=now+(reduced?.08:lightning?.13:.11),Jagged=lightning&&!reduced,Seed=++serial};
            return true;
        }

        public bool Update(double now,Camera camera,bool reduced)
        {
            if(now<0||double.IsNaN(now)||double.IsInfinity(now)){Clear();return false;}
            if(now<lastTime)Clear();lastTime=now;vertices.Clear();colors.Clear();indices.Clear();ActiveCount=0;
            var forward=camera==null?Vector3.forward:root.transform.InverseTransformDirection(camera.transform.forward);
            for(int i=0;i<pulses.Length;i++)
            {
                var p=pulses[i];if(p.Until<=now)continue;ActiveCount++;
                Vector3 direction=p.To-p.From;
                Vector3 side=Vector3.Cross(direction,forward).normalized;
                if(side.sqrMagnitude<.001f)side=Vector3.Cross(direction,Vector3.up).normalized;
                if(side.sqrMagnitude<.001f)side=Vector3.right;
                float fade=reduced?1:Mathf.Clamp01((float)((p.Until-now)/(p.Until-p.Began)));
                int segments=p.Jagged&&!reduced?8:1;Vector3 previous=p.From;
                for(int n=1;n<=segments;n++)
                {
                    float t=n/(float)segments;Vector3 point=Vector3.Lerp(p.From,p.To,t);
                    if(n<segments)
                    {
                        uint h=unchecked((uint)(p.Seed*73856093+n*19349663));h^=h>>13;
                        float offset=((h&1023)/511.5f-1)*Mathf.Min(.18f,direction.magnitude*.055f);
                        point+=side*offset;
                    }
                    Strip(previous,point,side,reduced?.035f:.095f,new Color(.20f,.86f,1,.42f*fade));
                    Strip(previous,point,side,.024f,new Color(.87f,1,1,.96f*fade));
                    previous=point;
                }
            }
            mesh.Clear();
            if(vertices.Count>0){mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();}
            return ActiveCount>0;
        }
        void Strip(Vector3 a,Vector3 b,Vector3 side,float halfWidth,Color color)
        {
            int start=vertices.Count;var offset=side*halfWidth;
            vertices.Add(a-offset);vertices.Add(a+offset);vertices.Add(b+offset);vertices.Add(b-offset);
            for(int i=0;i<4;i++)colors.Add(color);
            indices.Add(start);indices.Add(start+1);indices.Add(start+2);indices.Add(start);indices.Add(start+2);indices.Add(start+3);
        }
        static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.z);
        public void Clear(){Array.Clear(pulses,0,pulses.Length);ActiveCount=0;mesh.Clear();lastTime=double.NegativeInfinity;}
        public void Dispose(){Clear();root.SetActive(false);Destroy(root);Destroy(mesh);Destroy(material);}
        static void Destroy(UnityEngine.Object value){if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
    }
}
