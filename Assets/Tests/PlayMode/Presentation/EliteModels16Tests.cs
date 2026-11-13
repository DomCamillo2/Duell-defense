using System;
using System.Collections;
using System.IO;
using BigInteger=System.Numerics.BigInteger;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace DefenseDuell.Presentation.Tests
{
    public sealed class EliteModelsTests
    {
        [UnityTest] public IEnumerator TwoDistinctBossFamiliesHaveBoundedResearchStages()
        {
            foreach(int kind in new[]{4,5})foreach(int level in new[]{0,1,5,10,20,30})
            {
                var mesh=EliteUnitGeometry.Build(kind,level);
                try
                {
                    Assert.That(mesh.triangles.Length/3,Is.LessThanOrEqualTo(EliteUnitGeometry.MaximumTriangles));
                    Assert.That(mesh.bounds.min.y,Is.GreaterThanOrEqualTo(0));Assert.That(mesh.bounds.max.y,Is.LessThanOrEqualTo(EliteUnitGeometry.MaximumHeight));
                    foreach(var v in mesh.vertices)Assert.That(new Vector2(v.x,v.z).magnitude,Is.LessThanOrEqualTo(EliteUnitGeometry.MaximumRadius));
                    Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.colors32.Length,Is.EqualTo(mesh.vertexCount));
                }
                finally{Object.Destroy(mesh);}
            }
            Assert.That(EliteUnitGeometry.Stage(BigInteger.Pow(10,100)),Is.EqualTo(5));yield return null;
        }
        [UnityTest] public IEnumerator BossCatalogueRendersEveryResearchStage()
        {
            var content=DefenseDuell.Core.GameContent.CreateDefault();
            using var resources=new DioramaResources(content);
            var root=new GameObject("Boss preview");root.layer=30;root.transform.position=new Vector3(2000,2000,2000);
            var cameraGo=new GameObject("Boss catalogue camera");var cam=cameraGo.AddComponent<Camera>();
            cam.orthographic=true;cam.aspect=1;cam.orthographicSize=1.30f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.05f,.08f,.12f);cam.cullingMask=1<<30;
            cam.transform.rotation=Quaternion.Euler(24,208,0);cam.transform.position=root.transform.position+Vector3.up*.95f-cam.transform.forward*20;
            var lightGo=new GameObject("Boss catalogue key light");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(40,-30,0);
            var target=new RenderTexture(384,384,24);target.Create();cam.targetTexture=target;var image=new Texture2D(384,384,TextureFormat.RGB24,false);var old=RenderTexture.active;
            var filter=root.AddComponent<MeshFilter>();root.AddComponent<MeshRenderer>().sharedMaterial=resources.Material;
            string folder=Path.GetFullPath("TestResults/EliteBoss-v16");Directory.CreateDirectory(folder);
            try
            {
                foreach(int kind in new[]{4,5})
                {
                    var hashes=new System.Collections.Generic.HashSet<string>();
                    foreach(int level in new[]{0,1,5,10,20,30})
                    {
                        var mesh=EliteUnitGeometry.Build(kind,level);filter.sharedMesh=mesh;
                        try
                        {
                            foreach(var v in mesh.vertices){var view=cam.WorldToViewportPoint(root.transform.TransformPoint(v));Assert.That(view.x,Is.InRange(0f,1f));Assert.That(view.y,Is.InRange(0f,1f));}
                            cam.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,384,384),0,0);image.Apply();byte[] png=image.EncodeToPNG();
                            using var sha=System.Security.Cryptography.SHA256.Create();string digest=BitConverter.ToString(sha.ComputeHash(png));
                            Assert.That(hashes.Add(digest),Is.True,"Research appearance did not change");
                            File.WriteAllBytes(Path.Combine(folder,"boss-"+kind+"-research-"+level+".png"),png);
                        }
                        finally{filter.sharedMesh=null;Object.Destroy(mesh);}
                    }
                }
            }
            finally{RenderTexture.active=old;cam.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(image);Object.Destroy(root);Object.Destroy(cameraGo);Object.Destroy(lightGo);}
            yield return null;
        }
    }
}
