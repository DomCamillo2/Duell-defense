// OFFLINE AUTHORING ONLY. Do not call this baker during match or purchase handling.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace DefenseDuell.Editor.GraphicsQuality
{
    /// <summary>Deterministic bounded geometric AO. Source meshes are never changed.</summary>
    public static class GraphicsMeshAoBake
    {
        [Serializable] public sealed class Options
        {
            public int Samples=8,MaxTriangles=100000,MaxVertices=400000,MaxUniqueSamples=120000,MaxRays=1000000,MaxNodeVisitsPerRay=256;
            public float Radius=.32f,Bias=.003f,Strength=.35f;
        }
        [Serializable] public sealed class Part
        {
            public string GeometrySha256;
            public Vector3 Offset;
            public byte[] Darkening;
        }
        [Serializable] public sealed class Result
        {
            public bool Success;
            public string Problem,PoseSha256;
            public Part[] Parts;
            public int Rays,TruncatedRays,UniqueSamples,Triangles,Nodes;
        }
        sealed class Source
        {
            public Vector3[] Vertices,Normals;
            public int[] Indices;
            public Vector3 Offset;
            public string Hash;
        }
        struct Triangle
        {
            public Vector3 A,E1,E2,Min,Max,Center;
        }
        struct Node
        {
            public Vector3 Min,Max;
            public int Start,Count,Left,Right;
        }
        sealed class AxisComparer:IComparer<int>
        {
            readonly List<Triangle> triangles;readonly int axis;
            public AxisComparer(List<Triangle> triangles,int axis){this.triangles=triangles;this.axis=axis;}
            public int Compare(int a,int b){int c=triangles[a].Center[axis].CompareTo(triangles[b].Center[axis]);return c!=0?c:a.CompareTo(b);}
        }
        readonly struct SampleKey:IEquatable<SampleKey>
        {
            readonly int x,y,z,nx,ny,nz;
            public SampleKey(Vector3 p,Vector3 n)
            {x=(int)Math.Round(p.x*100000);y=(int)Math.Round(p.y*100000);z=(int)Math.Round(p.z*100000);nx=(int)Math.Round(n.x*10000);ny=(int)Math.Round(n.y*10000);nz=(int)Math.Round(n.z*10000);}
            public bool Equals(SampleKey v)=>x==v.x&&y==v.y&&z==v.z&&nx==v.nx&&ny==v.ny&&nz==v.nz;
            public override bool Equals(object obj)=>obj is SampleKey v&&Equals(v);
            public override int GetHashCode(){unchecked{int h=x;h=h*397^y;h=h*397^z;h=h*397^nx;h=h*397^ny;return h*397^nz;}}
        }
        sealed class Tree
        {
            public readonly List<Triangle> Triangles;
            public readonly List<Node> Nodes=new List<Node>();
            readonly int[] order;
            readonly AxisComparer[] comparers;
            readonly int[] stack=new int[64];
            public Tree(List<Triangle> triangles)
            {
                Triangles=triangles;order=new int[triangles.Count];for(int i=0;i<order.Length;i++)order[i]=i;
                comparers=new[]{new AxisComparer(triangles,0),new AxisComparer(triangles,1),new AxisComparer(triangles,2)};
                if(order.Length>0)Build(0,order.Length);
            }
            int Build(int start,int count)
            {
                int index=Nodes.Count;Nodes.Add(default);
                Vector3 min=Triangles[order[start]].Min,max=Triangles[order[start]].Max;
                for(int i=start+1;i<start+count;i++){min=Vector3.Min(min,Triangles[order[i]].Min);max=Vector3.Max(max,Triangles[order[i]].Max);}
                var node=new Node{Min=min,Max=max,Start=start,Count=count,Left=-1,Right=-1};
                if(count>8)
                {
                    Vector3 size=max-min;int axis=size.x>=size.y&&size.x>=size.z?0:size.y>=size.z?1:2;
                    Array.Sort(order,start,count,comparers[axis]);int half=count/2;
                    node.Left=Build(start,half);node.Right=Build(start+half,count-half);node.Count=0;
                }
                Nodes[index]=node;return index;
            }
            static bool Box(Vector3 p,Vector3 d,Node node,float maxDistance)
            {
                float lo=0,hi=maxDistance;
                for(int a=0;a<3;a++)
                {
                    if(Math.Abs(d[a])<1e-8f){if(p[a]<node.Min[a]||p[a]>node.Max[a])return false;continue;}
                    float q=(node.Min[a]-p[a])/d[a],r=(node.Max[a]-p[a])/d[a];
                    if(q>r){float swap=q;q=r;r=swap;}lo=Math.Max(lo,q);hi=Math.Min(hi,r);if(hi<lo)return false;
                }
                return true;
            }
            static bool Hit(Vector3 p,Vector3 d,Triangle t,float maxDistance,out float distance)
            {
                distance=0;Vector3 h=Vector3.Cross(d,t.E2);float det=Vector3.Dot(t.E1,h);
                if(Math.Abs(det)<1e-9f)return false;
                float inv=1/det;Vector3 s=p-t.A;float u=Vector3.Dot(s,h)*inv;if(u<0||u>1)return false;
                Vector3 q=Vector3.Cross(s,t.E1);float v=Vector3.Dot(d,q)*inv;if(v<0||u+v>1)return false;
                distance=Vector3.Dot(t.E2,q)*inv;return distance>1e-5f&&distance<maxDistance;
            }
            public bool Ray(Vector3 origin,Vector3 direction,float radius,int visitLimit,out float hit,out bool truncated)
            {
                hit=radius;truncated=false;if(Nodes.Count==0)return false;
                int top=0,visited=0;bool found=false;stack[top++]=0;
                while(top>0)
                {
                    if(++visited>visitLimit){truncated=true;return false;}
                    Node node=Nodes[stack[--top]];if(!Box(origin,direction,node,hit))continue;
                    if(node.Count>0)
                    {
                        for(int i=node.Start;i<node.Start+node.Count;i++)
                            if(Hit(origin,direction,Triangles[order[i]],hit,out float distance)){hit=distance;found=true;}
                    }
                    else
                    {
                        if(top+2>stack.Length){truncated=true;return false;}
                        stack[top++]=node.Left;stack[top++]=node.Right;
                    }
                }
                return found;
            }
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
        static string Hex(byte[] bytes)=>BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();
        static string Hash(Source s)
        {
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))using(var sha=SHA256.Create())
            {
                writer.Write(s.Vertices.Length);
                foreach(var v in s.Vertices){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
                foreach(var n in s.Normals){writer.Write(n.x);writer.Write(n.y);writer.Write(n.z);}
                writer.Write(s.Indices.Length);foreach(int i in s.Indices)writer.Write(i);
                writer.Flush();return Hex(sha.ComputeHash(stream.ToArray()));
            }
        }
        static Source Read(Mesh mesh,Vector3 offset)
        {
            if(mesh==null)throw new ArgumentException("Null source mesh");
            var s=new Source{Vertices=mesh.vertices,Normals=mesh.normals,Indices=mesh.triangles,Offset=offset};
            if(s.Vertices.Length==0||s.Normals.Length!=s.Vertices.Length||s.Indices.Length%3!=0)throw new ArgumentException("Invalid source mesh attributes");
            if(!Finite(offset)||offset.sqrMagnitude>1000000)throw new ArgumentException("Invalid mesh offset");
            foreach(var v in s.Vertices)if(!Finite(v)||v.sqrMagnitude>1000000)throw new ArgumentException("Invalid source position");
            foreach(var n in s.Normals)if(!Finite(n))throw new ArgumentException("Invalid source normal");
            foreach(int index in s.Indices)if(index<0||index>=s.Vertices.Length)throw new ArgumentException("Invalid source triangle index");
            s.Hash=Hash(s);return s;
        }
        public static string GeometrySha256(Mesh source)=>Read(source,Vector3.zero).Hash;
        static string PoseHash(Source[] sources)
        {
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))using(var sha=SHA256.Create())
            {
                writer.Write(sources.Length);
                foreach(var s in sources){writer.Write(s.Hash);writer.Write(s.Offset.x);writer.Write(s.Offset.y);writer.Write(s.Offset.z);}
                writer.Flush();return Hex(sha.ComputeHash(stream.ToArray()));
            }
        }
        public static Result Bake(Mesh[] sourceParts,Vector3[] partOffsets,Options options=null)
        {
            var result=new Result();var o=options??new Options();
            try
            {
                if(sourceParts==null||partOffsets==null||sourceParts.Length==0||sourceParts.Length!=partOffsets.Length||sourceParts.Length>64)
                    throw new ArgumentException("Provide 1–64 meshes with one explicit offset each");
                if(o.Samples<4||o.Samples>32||o.MaxTriangles<1||o.MaxTriangles>200000||o.MaxVertices<1||o.MaxVertices>1000000
                    ||o.MaxUniqueSamples<1||o.MaxUniqueSamples>250000||o.MaxRays<1||o.MaxRays>4000000||o.MaxNodeVisitsPerRay<16||o.MaxNodeVisitsPerRay>4096
                    ||!Finite(o.Radius)||o.Radius<=0||o.Radius>5||!Finite(o.Bias)||o.Bias<=0||o.Bias>=o.Radius*.25f||!Finite(o.Strength)||o.Strength<0||o.Strength>.35f)
                    throw new ArgumentException("Invalid or unbounded bake options");
                var sources=new Source[sourceParts.Length];int vertices=0,indices=0;
                for(int part=0;part<sources.Length;part++)
                {
                    if(sourceParts[part]==null||sourceParts[part].vertexCount>o.MaxVertices-vertices)
                        throw new ArgumentException("Null mesh or source vertex budget exceeded before copying");
                    sources[part]=Read(sourceParts[part],partOffsets[part]);vertices+=sources[part].Vertices.Length;indices+=sources[part].Indices.Length;
                    if(vertices>o.MaxVertices||indices/3>o.MaxTriangles)throw new InvalidOperationException("Source geometry exceeds offline bake budget");
                }
                result.PoseSha256=PoseHash(sources);
                var triangles=new List<Triangle>(indices/3);
                foreach(var s in sources)for(int i=0;i<s.Indices.Length;i+=3)
                {
                    Vector3 a=s.Vertices[s.Indices[i]]+s.Offset,b=s.Vertices[s.Indices[i+1]]+s.Offset,c=s.Vertices[s.Indices[i+2]]+s.Offset;
                    Vector3 e1=b-a,e2=c-a;if(Vector3.Cross(e1,e2).sqrMagnitude<1e-14f)continue;
                    triangles.Add(new Triangle{A=a,E1=e1,E2=e2,Min=Vector3.Min(a,Vector3.Min(b,c)),Max=Vector3.Max(a,Vector3.Max(b,c)),Center=(a+b+c)/3});
                }
                var tree=new Tree(triangles);result.Triangles=triangles.Count;result.Nodes=tree.Nodes.Count;
                var cache=new Dictionary<SampleKey,byte>();var parts=new Part[sources.Length];
                for(int part=0;part<sources.Length;part++)
                {
                    Source s=sources[part];var darkening=new byte[s.Vertices.Length];
                    for(int v=0;v<s.Vertices.Length;v++)
                    {
                        Vector3 p=s.Vertices[v]+s.Offset,n=s.Normals[v].normalized;if(n.sqrMagnitude<.5f)continue;
                        var key=new SampleKey(p,n);if(cache.TryGetValue(key,out byte stored)){darkening[v]=stored;continue;}
                        result.UniqueSamples++;if(result.UniqueSamples>o.MaxUniqueSamples)throw new InvalidOperationException("Unique vertex sampling budget exceeded");
                        Vector3 tangent=Vector3.Cross(Math.Abs(n.y)<.9f?Vector3.up:Vector3.right,n).normalized,bitangent=Vector3.Cross(n,tangent);
                        Vector3 origin=p+n*o.Bias;double obscured=0;
                        for(int sample=0;sample<o.Samples;sample++)
                        {
                            result.Rays++;if(result.Rays>o.MaxRays)throw new InvalidOperationException("Ray budget exceeded");
                            double u=(sample+.5)/o.Samples,angle=sample*2.399963229728653;
                            Vector3 direction=tangent*(float)(Math.Sqrt(u)*Math.Cos(angle))+bitangent*(float)(Math.Sqrt(u)*Math.Sin(angle))+n*(float)Math.Sqrt(1-u);
                            bool hit=tree.Ray(origin,direction,o.Radius,o.MaxNodeVisitsPerRay,out float distance,out bool truncated);
                            if(truncated){result.TruncatedRays++;throw new InvalidOperationException("BVH traversal budget exceeded; no partial AO is accepted");}
                            if(hit)obscured+=1-distance/o.Radius;
                        }
                        byte value=(byte)Math.Round(Math.Min(.35,o.Strength*obscured/o.Samples)*255);
                        darkening[v]=value;cache.Add(key,value);
                    }
                    parts[part]=new Part{GeometrySha256=s.Hash,Offset=s.Offset,Darkening=darkening};
                }
                result.Parts=parts;result.Success=true;result.Problem="";
            }
            catch(Exception e) when(e is ArgumentException||e is InvalidOperationException||e is UnityException)
            {result.Success=false;result.Parts=null;result.Problem=e.Message;}
            return result;
        }
        public static Mesh ApplyToClone(Mesh source,Part baked)
        {
            if(source==null||baked==null||baked.Darkening==null||baked.Darkening.Length!=source.vertexCount||GeometrySha256(source)!=baked.GeometrySha256)
                throw new ArgumentException("AO factors do not match this exact geometry");
            var uv=new Vector2[baked.Darkening.Length];
            for(int i=0;i<uv.Length;i++)
            {if(baked.Darkening[i]>89)throw new ArgumentException("AO factor exceeds .35 darkening budget");uv[i]=new Vector2(baked.Darkening[i]/255f,0);}
            var clone=UnityEngine.Object.Instantiate(source);clone.name=source.name+" AO baked";clone.uv2=uv;return clone;
        }
    }
}
