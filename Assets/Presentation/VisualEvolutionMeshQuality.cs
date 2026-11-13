using System.Collections.Generic;
using UnityEngine;

namespace DefenseDuell.Presentation
{
    // Normal-only refinement on owned imported meshes. No source asset mutation,
    // extra vertices, material instances, or rounding of crystal/stone silhouettes.
    public static class VisualEvolutionMeshQuality
    {
        public static int SmoothTurnedSurfaces(Mesh mesh)
        {
            var vertices=mesh.vertices;var original=mesh.normals;var colors=mesh.colors32;
            if(original.Length!=vertices.Length||colors.Length!=vertices.Length)return 0;
            var groups=new Dictionary<Key,List<int>>();
            for(int i=0;i<vertices.Length;i++)
            {
                if(!TurnedMaterial(colors[i]))continue;
                var key=new Key(vertices[i],colors[i]);
                if(!groups.TryGetValue(key,out var list)){list=new List<int>(8);groups.Add(key,list);}list.Add(i);
            }
            var result=(Vector3[])original.Clone();var seen=new List<Vector3>(12);int changed=0;
            foreach(var group in groups.Values)foreach(int i in group)
            {
                Vector3 sum=Vector3.zero;seen.Clear();
                foreach(int j in group)
                {
                    var n=original[j];if(Vector3.Dot(original[i],n)<.72f)continue;
                    bool duplicate=false;foreach(var old in seen)if(Vector3.Dot(n,old)>.9999f){duplicate=true;break;}
                    if(duplicate)continue;seen.Add(n);sum+=n;
                }
                if(sum.sqrMagnitude<.0001f)continue;
                var normal=sum.normalized;if((normal-original[i]).sqrMagnitude>.000001f)changed++;
                result[i]=normal;
            }
            mesh.normals=result;return changed;
        }
        static bool TurnedMaterial(Color32 c)=>
            (c.r==231&&c.g==188&&c.b==89)|| // gold
            (c.r==255&&c.g==243&&c.b==216)|| // ceramic/ivory metal
            (c.r==86&&c.g==99&&c.b==108)|| // steel
            (c.r==217&&c.g==133&&c.b==69)|| // copper/paint
            (c.r==84&&c.g==168&&c.b==154)|| // coil housing
            (c.r==56&&c.g==207&&c.b==193); // laser optical housing
        readonly struct Key:System.IEquatable<Key>
        {
            readonly int x,y,z,rgb;
            public Key(Vector3 p,Color32 c){x=Mathf.RoundToInt(p.x*100000);y=Mathf.RoundToInt(p.y*100000);z=Mathf.RoundToInt(p.z*100000);rgb=(c.r<<16)|(c.g<<8)|c.b;}
            public bool Equals(Key other)=>x==other.x&&y==other.y&&z==other.z&&rgb==other.rgb;
            public override bool Equals(object other)=>other is Key key&&Equals(key);
            public override int GetHashCode(){unchecked{return (((x*397)^y)*397^z)*397^rgb;}}
        }
    }
}
