using System;
using System.Collections;
using System.Collections.Generic;
using BigInteger = System.Numerics.BigInteger;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class VisualEvolutionModelTests
    {
        static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        static void MeshContract(Mesh mesh,int triangleBudget)
        {
            var v=mesh.vertices;var n=mesh.normals;var c=mesh.colors32;var uv=mesh.uv;var t=mesh.triangles;
            Assert.That(v.Length,Is.GreaterThan(0),mesh.name);Assert.That(n.Length,Is.EqualTo(v.Length));
            Assert.That(c.Length,Is.EqualTo(v.Length));Assert.That(uv.Length,Is.EqualTo(v.Length));
            Assert.That(t.Length/3,Is.LessThanOrEqualTo(triangleBudget),mesh.name);
            for(int i=0;i<v.Length;i++)
            {
                if(!Finite(v[i].x)||!Finite(v[i].y)||!Finite(v[i].z))Assert.Fail(mesh.name+" nonfinite vertex");
                if(!Finite(n[i].sqrMagnitude)||Mathf.Abs(n[i].sqrMagnitude-1)>.005f)Assert.Fail(mesh.name+" invalid normal");
                if(c[i].a!=255||!Finite(uv[i].x)||uv[i].x<0||uv[i].x>1||uv[i].y!=0)Assert.Fail(mesh.name+" invalid opaque material/finish");
            }
            for(int i=0;i<t.Length;i+=3)
                if(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).sqrMagnitude<1e-14f)Assert.Fail(mesh.name+" degenerate triangle "+i/3);
        }
        static void PreservedOriginal(Mesh original,Mesh evolved)
        {
            var a=original.vertices;var b=evolved.vertices;
            Assert.That(b.Length,Is.GreaterThan(a.Length));
            for(int i=0;i<a.Length;i++)if(a[i]!=b[i])Assert.Fail("Base geometry was replaced at "+i);
        }
        [Test]
        public void HugeLevelsHaveBoundedStableKeysAndMineHasNoCombatPath()
        {
            var huge=BigInteger.Pow(10,1000);
            Assert.That(VisualEvolution.Tier(huge),Is.EqualTo(10));Assert.That(VisualEvolution.Tier(-huge),Is.Zero);
            Assert.That(VisualEvolution.TowerKey(0,huge,huge),Is.EqualTo(VisualEvolution.TowerKey(0,100,100)));
            Assert.That(VisualEvolution.TowerKey(4,huge,0),Is.EqualTo(VisualEvolution.TowerKey(4,100,0)));
            Assert.That(VisualEvolution.TowerKey(2,1,1),Is.EqualTo(11));
            Assert.That(VisualEvolution.Path(0,huge),Is.EqualTo(VisualEvolutionPath.Secondary));
        }
        [UnityTest]
        public IEnumerator AllEvolutionStagesKeepIdentityGroundAndBoundedOpaqueGeometry()
        {
            var resources=new DioramaResources(GameContent.CreateDefault());
            int maximumTower=0,maximumUnit=0,maximumModule=0;long vertices=0;
            var unique=new HashSet<Mesh>();Mesh last=null;
            try
            {
                for(int kind=0;kind<resources.TowerBases.GetLength(0);kind++)for(int tier=0;tier<=10;tier++)
                {
                    var model=resources.GetTowerEvolution(kind,tier,0);
                    Assert.That(model.Tier,Is.EqualTo(tier));
                    if(tier==0){Assert.That(model.Base,Is.SameAs(resources.TowerBases[kind,0]));Assert.That(model.Head,Is.SameAs(resources.TowerHeads[kind,0]));Assert.That(model.PathModule,Is.Null);}
                    else {PreservedOriginal(resources.TowerBases[kind,0],model.Base);PreservedOriginal(resources.TowerHeads[kind,0],model.Head);Assert.That(model.Head,Is.Not.SameAs(last));}
                    last=model.Head;
                    int triangles=(model.Base.triangles.Length+model.Head.triangles.Length)/3;maximumTower=Mathf.Max(maximumTower,triangles);
                    Assert.That(triangles,Is.LessThanOrEqualTo(5200),"Combined tower "+kind+" tier "+tier);
                    foreach(var part in new[]{model.Base,model.Head})
                    {
                        MeshContract(part,4200);unique.Add(part);
                        foreach(var v in part.vertices)
                        {
                            var p=v+(part==model.Head?DioramaResources.TowerHeadOffset(kind):Vector3.zero);
                            Assert.That(p.y,Is.InRange(-.001f,DioramaResources.MaximumEvolutionTowerHeight),part.name);
                            Assert.That(new Vector2(p.x,p.z).magnitude,Is.LessThanOrEqualTo(DioramaResources.MaximumEvolutionTowerRadius),part.name);
                        }
                    }
                    foreach(var levels in new[]{new[]{tier,0},new[]{0,tier},new[]{tier/2,tier-tier/2}})
                    {
                        var branch=resources.GetTowerEvolution(kind,levels[0],levels[1]);
                        Assert.That(branch.Base,Is.SameAs(model.Base));Assert.That(branch.Head,Is.SameAs(model.Head));
                        if(branch.PathModule==null)continue;
                        MeshContract(branch.PathModule,800);unique.Add(branch.PathModule);
                        maximumModule=Mathf.Max(maximumModule,branch.PathModule.triangles.Length/3);
                        foreach(var p0 in branch.PathModule.vertices)
                        {
                            var p=p0+DioramaResources.TowerHeadOffset(kind);
                            Assert.That(p.y,Is.InRange(0,DioramaResources.MaximumEvolutionTowerHeight));
                            Assert.That(new Vector2(p.x,p.z).magnitude,Is.LessThanOrEqualTo(DioramaResources.MaximumEvolutionTowerRadius));
                        }
                    }
                }
                for(int kind=0;kind<4;kind++)for(int tier=0;tier<=10;tier++)
                {
                    var unit=resources.GetUnitEvolution(kind,tier);MeshContract(unit,2800);unique.Add(unit);
                    maximumUnit=Mathf.Max(maximumUnit,unit.triangles.Length/3);
                    if(tier==0)Assert.That(unit,Is.SameAs(resources.Units[kind]));else PreservedOriginal(resources.Units[kind],unit);
                    Assert.That(unit.bounds.min.y,Is.EqualTo(resources.Units[kind].bounds.min.y).Within(.0001f),"Added armor must not lift the feet");
                    Assert.That(resources.UnitEvolutionHeight(kind,tier),Is.EqualTo(unit.bounds.max.y));
                    Assert.That(unit.bounds.min.y+resources.UnitEvolutionGroundOffset(kind,tier),Is.EqualTo(0).Within(.00001f));
                    Assert.That(unit.bounds.max.y,Is.LessThanOrEqualTo(1.6f));
                    Assert.That(unit.bounds.size.x,Is.LessThanOrEqualTo(1.4f));
                }
                foreach(var mesh in unique)vertices+=mesh.vertexCount;
                Assert.That(resources.EvolutionMeshCount,Is.LessThanOrEqualTo(DioramaResources.MaximumAdditionalEvolutionMeshes));
                var huge=BigInteger.Pow(10,1000);
                resources.GetTowerEvolution(2,100,100);int count=resources.EvolutionMeshCount;
                for(int i=0;i<100;i++)
                {
                    var a=resources.GetTowerEvolution(2,huge+i,huge*(i+1)+1);var b=resources.GetTowerEvolution(2,100,100);
                    Assert.That(a.Head,Is.SameAs(b.Head));Assert.That(a.PathModule,Is.SameAs(b.PathModule));
                    Assert.That(resources.GetUnitEvolution(2,huge+i),Is.SameAs(resources.GetUnitEvolution(2,10)));
                }
                Assert.That(resources.EvolutionMeshCount,Is.EqualTo(count));
                Assert.That(vertices,Is.LessThan(1500000),"Finite full-cache geometry budget");
                Debug.Log("VISUAL_EVOLUTION_BUDGET towers="+maximumTower+" unit="+maximumUnit+" module="+maximumModule+" totalVertices="+vertices+" additionalMeshes="+count);
            }
            finally{resources.Dispose();}
            yield return null;
            Assert.That(last==null,Is.True,"Evolution meshes must be destroyed with the common resources");
            Assert.Throws<ObjectDisposedException>(()=>resources.GetUnitEvolution(0,1));
        }
        // Geometry hashes cannot catch an accent hidden inside another mesh: compare actual pixels.
        [UnityTest]
        public IEnumerator CannonMixedPathRemainsVisibleInProductionPortraitDirection()
        {
            var root=new GameObject("Cannon path visibility regression");root.transform.position=new Vector3(100,100,100);
            var target=new RenderTexture(128,128,24);var pixels=new Texture2D(128,128,TextureFormat.RGBA32,false);
            var prior=RenderTexture.active;
            using(var resources=new DioramaResources(GameContent.CreateDefault()))
            try
            {
                GameObject Child(string name)
                {var child=new GameObject(name);child.layer=31;child.transform.SetParent(root.transform,false);return child;}
                var camera=Child("Camera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;
                camera.orthographicSize=1.45f;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=20;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                camera.cullingMask=1<<31;camera.allowHDR=false;camera.allowMSAA=false;camera.targetTexture=target;
                var focus=root.transform.position+Vector3.up*.85f;
                camera.transform.position=focus+new Vector3(4,3.6f,6).normalized*5;
                camera.transform.rotation=Quaternion.LookRotation(focus-camera.transform.position,Vector3.up);
                var light=Child("Light").AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<31;
                light.intensity=1.2f;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(48,-34,0);
                MeshFilter Part(string name,Vector3 offset)
                {
                    var child=Child(name);child.transform.localPosition=offset;
                    var filter=child.AddComponent<MeshFilter>();var renderer=child.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial=resources.Material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows=false;return filter;
                }
                var foundation=Part("Base",Vector3.zero);var head=Part("Head",DioramaResources.TowerHeadOffset(1));
                var accent=Part("Path",DioramaResources.TowerHeadOffset(1));
                Color32[] Capture(int primary,int secondary)
                {
                    var model=resources.GetTowerEvolution(1,primary,secondary);
                    foundation.sharedMesh=model.Base;head.sharedMesh=model.Head;accent.sharedMesh=model.PathModule;
                    camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,128,128),0,0);pixels.Apply();
                    return pixels.GetPixels32();
                }
                foreach(int tier in new[]{2,5,10})
                {
                    var secondary=Capture(0,tier);var mixed=Capture(tier/2,tier-tier/2);int changed=0;
                    for(int i=0;i<secondary.Length;i++)
                    {
                        var a=secondary[i];var b=mixed[i];
                        if(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)+Mathf.Abs(a.a-b.a)>24)changed++;
                    }
                    Assert.That(changed,Is.GreaterThan(12),"Cannon primary accent is occluded at tier "+tier);
                }
            }
            finally
            {
                RenderTexture.active=prior;UnityEngine.Object.Destroy(root);target.Release();
                UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator ImportedQualitySmoothsMetalWithoutChangingStoneOrGeometry()
        {
            using(var resources=new DioramaResources(GameContent.CreateDefault()))
            {
                foreach(var name in new[]{"tesla","ballista"})
                {
                    int kind=name=="tesla"?5:6;int changed=0;
                    foreach(var part in new[]{"Base","Head"})
                    {
                        var source=Resources.Load<Mesh>("DefenseUnlockModels/"+name+"_v0_"+part);
                        var target=part=="Base"?resources.TowerBases[kind,0]:resources.TowerHeads[kind,0];
                        var a=source.vertices;var b=target.vertices;var old=source.normals;var current=target.normals;var colors=source.colors32;
                        Assert.That(a.Length,Is.EqualTo(b.Length));CollectionAssert.AreEqual(source.triangles,target.triangles);
                        for(int i=0;i<a.Length;i++)
                        {
                            Assert.That(b[i],Is.EqualTo(a[i]));
                            bool stone=colors[i].r==181&&colors[i].g==160&&colors[i].b==137;
                            bool crystal=colors[i].r==86&&colors[i].g==189&&colors[i].b==206;
                            if(stone||crystal)Assert.That(current[i],Is.EqualTo(old[i]));
                            if((current[i]-old[i]).sqrMagnitude>.000001f)changed++;
                        }
                    }
                    Assert.That(changed,Is.GreaterThan(0),name+" must actually improve round material shading");
                }
            }
            yield return null;
        }
    }
}
