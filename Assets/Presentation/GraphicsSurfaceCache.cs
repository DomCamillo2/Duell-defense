using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace DefenseDuell.Presentation
{
    // Apply once when an OWNED mesh group is constructed, before sharing it with views.
    // No raycasts/BVH/physics, no per-frame calls, no change to geometry/colors/UV0.
    public sealed class GraphicsSurfaceCache
    {
        readonly GraphicsSurfaceArchive archive;
        public int AppliedGroups{get;private set;}
        public int MissingGroups{get;private set;}
        public int RejectedGroups{get;private set;}
        public GraphicsSurfaceCache(GraphicsSurfaceArchive source){archive=source??throw new ArgumentNullException(nameof(source));}
        public bool TryApplyOwned(string key,Mesh[] parts,Vector3[] offsets,out string problem)
        {
            problem=null;
            if(parts==null||offsets==null||parts.Length!=offsets.Length){RejectedGroups++;problem="Invalid mesh group";return false;}
            if(!archive.TryGet(key,out var record)){MissingGroups++;problem="Missing baked AO: "+key;return false;}
            if(record.Parts.Length!=parts.Length){RejectedGroups++;problem="AO part count mismatch: "+key;return false;}
            // Validate the entire group first. An invalid part cannot half-shade a tower.
            for(int i=0;i<parts.Length;i++)
            {
                var p=record.Parts[i];var offset=offsets[i];
                if(parts[i]==null||parts[i].vertexCount!=p.Darkening.Length||
                    float.IsNaN(offset.sqrMagnitude)||float.IsInfinity(offset.sqrMagnitude)||
                    (offset-new Vector3(p.X,p.Y,p.Z)).sqrMagnitude>1e-10f||
                    GeometrySha256(parts[i])!=p.GeometrySha256)
                {RejectedGroups++;problem="AO geometry/pose mismatch: "+key+" part "+i;return false;}
            }
            for(int i=0;i<parts.Length;i++)
            {
                var samples=record.Parts[i].Darkening;var uv=new Vector2[samples.Length];
                for(int vertex=0;vertex<uv.Length;vertex++)uv[vertex]=new Vector2(samples[vertex]/255f,0);
                parts[i].uv2=uv;
            }
            AppliedGroups++;return true;
        }
        // Matches the offline baker: positions + normals + index order, little endian.
        // UV and color changes never invalidate geometric occlusion or multiply it twice.
        public static string GeometrySha256(Mesh mesh)
        {
            if(mesh==null)throw new ArgumentNullException(nameof(mesh));
            var v=mesh.vertices;var n=mesh.normals;var indices=mesh.triangles;
            if(n.Length!=v.Length)throw new InvalidDataException("Missing model normals");
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
            {
                writer.Write(v.Length);
                foreach(var p in v){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}
                foreach(var p in n){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}
                writer.Write(indices.Length);foreach(int index in indices)writer.Write(index);
                writer.Flush();using(var sha=SHA256.Create())
                {var bytes=sha.ComputeHash(stream.ToArray());return BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();}
            }
        }
    }
}
