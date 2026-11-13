using NUnit.Framework;
using UnityEngine;
using DefenseDuell.Presentation;
namespace DefenseDuell.Tests
{
    public sealed class GraphicsSurfaceCacheTests
    {
        static Mesh Triangle()=>new Mesh{vertices=new[]{Vector3.zero,Vector3.right,Vector3.forward},normals=new[]{Vector3.up,Vector3.up,Vector3.up},triangles=new[]{0,2,1},uv=new[]{Vector2.zero,Vector2.right,Vector2.up}};
        static GraphicsSurfaceArchive.Part Part(Mesh mesh)=>new GraphicsSurfaceArchive.Part{GeometrySha256=GraphicsSurfaceCache.GeometrySha256(mesh),Darkening=new byte[]{0,40,89}};
        [Test] public void ApplicationPreservesGeometryAndUv0AndIsIdempotent()
        {
            var mesh=Triangle();try
            {
                string hash=GraphicsSurfaceCache.GeometrySha256(mesh);var uv=mesh.uv;
                var source=GraphicsSurfaceArchive.Read(GraphicsSurfaceArchive.Write(new[]{new GraphicsSurfaceArchive.Record{Key="one",Parts=new[]{Part(mesh)}}}));
                var cache=new GraphicsSurfaceCache(source);
                Assert.That(cache.TryApplyOwned("one",new[]{mesh},new[]{Vector3.zero},out _),Is.True);
                Assert.That(mesh.uv2[2].x,Is.EqualTo(89/255f).Within(1e-6));
                var baked=mesh.uv2;
                Assert.That(cache.TryApplyOwned("one",new[]{mesh},new[]{Vector3.zero},out _),Is.True);
                CollectionAssert.AreEqual(baked,mesh.uv2);CollectionAssert.AreEqual(uv,mesh.uv);
                Assert.That(GraphicsSurfaceCache.GeometrySha256(mesh),Is.EqualTo(hash));
            }finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void AnyMismatchedPartRejectsWholeGroupBeforeUvWrite()
        {
            var a=Triangle();var b=Triangle();try
            {
                var source=GraphicsSurfaceArchive.Read(GraphicsSurfaceArchive.Write(new[]{new GraphicsSurfaceArchive.Record{Key="pair",Parts=new[]{Part(a),Part(b)}}}));
                var cache=new GraphicsSurfaceCache(source);var verts=b.vertices;verts[0]=Vector3.up;b.vertices=verts;
                Assert.That(cache.TryApplyOwned("pair",new[]{a,b},new[]{Vector3.zero,Vector3.zero},out _),Is.False);
                Assert.That(a.uv2.Length,Is.Zero);Assert.That(b.uv2.Length,Is.Zero);
                Assert.That(cache.RejectedGroups,Is.EqualTo(1));
                Assert.That(cache.TryApplyOwned("missing",new[]{a},new[]{Vector3.zero},out _),Is.False);
                Assert.That(cache.MissingGroups,Is.EqualTo(1));
            }finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
        }
    }
}
