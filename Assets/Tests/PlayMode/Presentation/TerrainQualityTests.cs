using System;
using System.Collections;
using System.Reflection;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class TerrainQualityTests
    {
        static readonly Assembly Presentation=typeof(DioramaResources).Assembly;
        static readonly Type Builder=Presentation.GetType("DefenseDuell.Presentation.DioramaMeshBuilder",true);
        static readonly Type Terrain=Presentation.GetType("DefenseDuell.Presentation.ScenicTerrain",true);
        static readonly Type Landmarks=Presentation.GetType("DefenseDuell.Presentation.MapLandmarkArt",true);
        static Mesh Build(MapDefinition map,bool backdrop=false)
        {
            var b=Activator.CreateInstance(Builder,true);
            var method=Terrain.GetMethod(backdrop?"BuildBackdrop":"Build",BindingFlags.NonPublic|BindingFlags.Static);
            method.Invoke(null,backdrop?new object[]{b,map}:new object[]{b,map,map.Width,map.Height});
            return (Mesh)Builder.GetMethod("Build").Invoke(b,new object[]{"Terrain quality fixture"});
        }
        static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
        [TestCase(0,49940,11030)] [TestCase(1,47138,6651)] [TestCase(2,54404,15163)]
        [TestCase(3,72000,16000)] [TestCase(4,66000,12000)] [TestCase(5,78000,22000)]
        public void LandscapeStaysOpaqueDeterministicAndWithinPerMapBudget(int index,int budget,int backdropBudget)
        {
            var map=MapCatalog.Create(index);Mesh first=null,second=null,backdrop=null;
            try
            {
                first=Build(map);second=Build(map);backdrop=Build(map,true);
                Assert.That(first.triangles.Length/3,Is.LessThanOrEqualTo(budget));
                Assert.That(backdrop.triangles.Length/3,Is.LessThanOrEqualTo(backdropBudget));
                Assert.That(first.bounds.min.y,Is.EqualTo(-1.85f).Within(.001f));
                CollectionAssert.AreEqual(first.vertices,second.vertices,"Repeated construction must not move details or paths");
                CollectionAssert.AreEqual(first.colors32,second.colors32);
                bool finite=true,opaque=true,matte=true;
                foreach(var v in first.vertices)finite&=Finite(v.x)&&Finite(v.y)&&Finite(v.z);
                foreach(var c in first.colors32)opaque&=c.a==255;
                foreach(var uv in first.uv)matte&=uv.x==0&&uv.y==0;
                Assert.That(finite&&opaque&&matte,Is.True,"Terrain must remain finite, opaque and matte");
                Debug.Log("TERRAIN_QUALITY map="+map.Id+" triangles="+first.triangles.Length/3+" backdrop="+backdrop.triangles.Length/3);
            }
            finally
            {
                if(first!=null)Object.DestroyImmediate(first);if(second!=null)Object.DestroyImmediate(second);
                if(backdrop!=null)Object.DestroyImmediate(backdrop);
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void EveryLandmarkKeepsItsPhysicalAndProjectedFootprintInsideTheExistingBlocker(int index)
        {
            var map=MapCatalog.Create(index);int landmarks=0,triangles=0;float highest=0;
            var create=Landmarks.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic);
            for(int areaIndex=0;areaIndex<map.BlockedAreas.Length;areaIndex++)
            {
                var faces=(IEnumerable)create.Invoke(null,new object[]{map,areaIndex});if(faces==null)continue;
                var area=map.BlockedAreas[areaIndex];landmarks++;
                foreach(var face in faces)
                {
                    var type=face.GetType();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                    var a=(Vector3)type.GetField("A",flags).GetValue(face);var b=(Vector3)type.GetField("B",flags).GetValue(face);var c=(Vector3)type.GetField("C",flags).GetValue(face);
                    if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-14f)Assert.Fail("Degenerate landmark triangle on "+map.Id+"/"+areaIndex);
                    foreach(var v in new[]{a,b,c})
                    {
                        highest=Math.Max(highest,v.y);
                        if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||v.y<.18f)Assert.Fail("Invalid landmark position");
                        foreach(float elevation in new[]{36f,43f,45f})foreach(float yaw in new[]{-3.68f,0,3.68f})
                        {
                            double distance=(v.y-.18)/Math.Tan(elevation*Math.PI/180);
                            double x=v.x*50+500-distance*Math.Sin(yaw*Math.PI/180)*50;
                            double y=300-v.z*50-distance*Math.Cos(yaw*Math.PI/180)*50;
                            if(x<area.MinX+7.9||x>area.MaxX-7.9||y<area.MinY+7.9||y>area.MaxY-7.9)
                                Assert.Fail("Landmark projects outside original blocker "+map.Id+"/"+areaIndex);
                        }
                    }
                    triangles++;
                }
            }
            Assert.That(landmarks,Is.EqualTo(index<3?2:map.BlockedAreas.Length));
            Assert.That(triangles,Is.LessThanOrEqualTo(index<3?3000:9000));
            if(index>=3)Assert.That(highest,Is.GreaterThan(2.7f),"Expanded landscape must provide real elevation");
        }
    }
}
