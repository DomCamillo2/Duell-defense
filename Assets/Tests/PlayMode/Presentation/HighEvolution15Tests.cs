using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BigInteger=System.Numerics.BigInteger;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class HighEvolution15Tests
    {
        static string Digest(byte[] bytes){using var h=SHA256.Create();return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static string Geometry(Mesh m)
        {using var bytes=new MemoryStream();using var w=new BinaryWriter(bytes);foreach(var v in m.vertices){w.Write(v.x);w.Write(v.y);w.Write(v.z);}return Digest(bytes.ToArray());}
        static string Vertex(Vector3 p,Color32 c)=>Mathf.RoundToInt(p.x*100000)+":"+Mathf.RoundToInt(p.y*100000)+":"+Mathf.RoundToInt(p.z*100000)+":"+c.r+":"+c.g+":"+c.b;
        static void Subset(Mesh small,Mesh large)
        {
            var vv=large.vertices;var cc=large.colors32;var set=new HashSet<string>();for(int i=0;i<vv.Length;i++)set.Add(Vertex(vv[i],cc[i]));
            vv=small.vertices;cc=small.colors32;for(int i=0;i<vv.Length;i++)Assert.That(set.Contains(Vertex(vv[i],cc[i])),Is.True,"Mixed path lost purchased module vertex");
        }
        [Test] public void TowerStagesAndKeysUseExactLevelsWithFiniteUpperBound()
        {
            for(int i=0;i<=30;i++)Assert.That(VisualEvolution.TowerTier(i),Is.EqualTo(i));
            foreach(int n in new[]{40,50,75,100})Assert.That(VisualEvolution.LevelForTowerTier(VisualEvolution.TowerTier(n)),Is.EqualTo(n));
            var huge=BigInteger.Pow(10,1000);Assert.That(VisualEvolution.TowerTier(huge),Is.EqualTo(34));
            Assert.That(VisualEvolution.TowerKey(0,huge,0),Is.EqualTo(VisualEvolution.TowerKey(0,100,0)));
            Assert.That(VisualEvolution.TowerKey(0,20,10),Is.Not.EqualTo(VisualEvolution.TowerKey(0,10,20)));
            Assert.That(VisualEvolution.TowerKey(0,0,30),Is.Not.EqualTo(VisualEvolution.TowerKey(0,15,15)));
            Assert.That(VisualEvolution.Tier(huge),Is.EqualTo(10),"Troop series must retain its current independent limits");
        }
        [Test] public void EveryPurchaseThroughThirtyChangesAllClassGeometryAndMixedKeepsBothPaths()
        {
            using var r=new DioramaResources(GameContent.CreateDefault());int classes=r.TowerBases.GetLength(0);
            for(int kind=0;kind<classes;kind++)
            {
                var geometries=new HashSet<string>();
                for(int level=10;level<=30;level++)
                {
                    var art=r.GetTowerEvolution(kind,level,0);
                    Assert.That(geometries.Add(Geometry(art.Base)+Geometry(art.Head)),Is.True,"No geometry change class "+kind+" level "+level);
                }
                if(kind==4)continue;
                var a=r.GetTowerEvolution(kind,15,0);var b=r.GetTowerEvolution(kind,0,15);var mixed=r.GetTowerEvolution(kind,15,15);
                Subset(a.PathModule,mixed.PathModule);Subset(b.PathModule,mixed.PathModule);
                Assert.That(mixed.Key,Is.EqualTo(VisualEvolution.TowerKey(kind,15,15)));
            }
        }
        [UnityTest] public IEnumerator LateMilestonesAreDistinctBoundedAndResourcesAreReleased()
        {
            var r=new DioramaResources(GameContent.CreateDefault());Mesh last=null;
            try
            {
                for(int kind=0;kind<r.TowerBases.GetLength(0);kind++)
                {
                    var seen=new HashSet<string>();
                    foreach(int level in new[]{10,15,20,25,30,40,50,75,100})
                    foreach(int path in kind==4?new[]{0}:new[]{0,1,2})
                    {
                        int p=path==1?0:path==2?level/2:level,s=path==1?level:path==2?level-p:0;
                        var art=r.GetTowerEvolution(kind,p,s);last=art.Head;
                        Assert.That(seen.Add(path+":"+Geometry(art.Base)+Geometry(art.Head)+ (art.PathModule==null?"":Geometry(art.PathModule))),Is.True,"Repeated milestone "+kind+"/"+level+"/"+path);
                        int tris=0;
                        foreach(var mesh in new[]{art.Base,art.Head,art.PathModule})
                        {
                            if(mesh==null)continue;tris+=mesh.triangles.Length/3;
                            foreach(var vertex in mesh.vertices)
                            {
                                var world=vertex+(mesh==art.Base?Vector3.zero:DioramaResources.TowerHeadOffset(kind));
                                Assert.That(float.IsNaN(world.sqrMagnitude)||float.IsInfinity(world.sqrMagnitude),Is.False);
                                Assert.That(world.y,Is.InRange(-.001f,DioramaResources.MaximumEvolutionTowerHeight),mesh.name);
                                Assert.That(new Vector2(world.x,world.z).magnitude,Is.LessThanOrEqualTo(DioramaResources.MaximumEvolutionTowerRadius),mesh.name);
                            }
                        }
                        Assert.That(tris,Is.LessThanOrEqualTo(10000),"Combined high model triangle budget");
                    }
                }
                var huge=BigInteger.Pow(10,1000);r.GetTowerEvolution(0,huge,huge);int count=r.EvolutionMeshCount;
                for(int i=0;i<100;i++)r.GetTowerEvolution(0,huge+i,huge+i);
                Assert.That(r.EvolutionMeshCount,Is.EqualTo(count));Assert.That(count,Is.LessThanOrEqualTo(DioramaResources.MaximumTotalEvolutionMeshes));
                Debug.Log("HIGH_EVOLUTION_MODELS classes="+r.TowerBases.GetLength(0)+" warmedMeshes="+count+" finiteCacheBound="+DioramaResources.MaximumTotalEvolutionMeshes);
            }
            finally{r.Dispose();}
            yield return null;Assert.That(last==null,Is.True);Assert.Throws<ObjectDisposedException>(()=>r.GetTowerEvolution(0,30,0));
        }
        [Serializable] sealed class Capture{public int kind,level,primary,secondary,triangles,changedPixels;public string file,sha256;}
        [Serializable] sealed class Gallery{public string scope;public Capture[] pictures;}
        static TowerState Purchase(GameContent content,int kind,int primary,int secondary)
        {
            var sim=new MatchSimulation(content);sim.State.Phase=MatchPhase.Playing;sim.State.Players[0].Gold=BigInteger.Pow(10,30);sim.State.Players[0].AvailableTowerMask=(1<<content.Towers.Length)-1;
            MapPoint at=default;bool found=false;
            for(int y=50;y<content.Map.Height-50&&!found;y+=25)for(int x=50;x<content.Map.Width-50&&!found;x+=25)
                if(PlacementValidator.ValidateBuild(content,sim.State,PlayerSlot.Zero,kind,-1,new MapPoint(x,y),out _)==ErrorCode.None){at=new MapPoint(x,y);found=true;}
            Assert.That(found,Is.True,"No legal fixture building point");ulong sequence=0;
            CommandResult Send(CommandType type,int value,int target=-1)
            {sim.State.Tick+=content.TickRate;return sim.Execute(PlayerSlot.Zero,new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=++sequence,Type=type,Kind=value,Target=target,PositionX=type==CommandType.BuildTower?at.X:0,PositionY=type==CommandType.BuildTower?at.Y:0});}
            Assert.That(Send(CommandType.BuildTower,kind).Accepted,Is.True);var t=sim.State.Towers[0];
            for(int i=0;i<primary+secondary;i++)
            {
                var before=sim.State.Players[0].Gold;var path=i<primary?TowerUpgradeRules.PrimaryPath(content,kind):TowerUpgradeRules.SecondaryPath(content,kind);
                var quote=TowerUpgradeRules.Quote(content,t,path);Assert.That(Send(CommandType.UpgradeTower,(int)path,t.Id).Accepted,Is.True);
                Assert.That(before-sim.State.Players[0].Gold,Is.EqualTo(quote.Price),"Visual level must come from accepted exact gold purchase");
            }
            return t;
        }
        [UnityTest] public IEnumerator PurchasedHighModelsRenderDistinctVisiblePathsWithSharedProductionResolver()
        {
            var content=GameContent.CreateDefault();var root=new GameObject("High evolution purchase proof");root.transform.position=new Vector3(1000,1000,1000);
            var cameraGo=new GameObject("High evolution proof camera");var cam=cameraGo.AddComponent<Camera>();cam.orthographic=true;cam.aspect=1;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.055f,.09f,.13f);cam.cullingMask=1<<30;
            cam.transform.rotation=Quaternion.Euler(24,208,0);cam.nearClipPlane=.01f;cam.farClipPlane=100;
            var lightGo=new GameObject("High evolution key light");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(45,-30,0);light.cullingMask=1<<30;
            var target=new RenderTexture(384,384,24){antiAliasing=2};target.Create();cam.targetTexture=target;
            var image=new Texture2D(384,384,TextureFormat.RGB24,false);var old=RenderTexture.active;
            var records=new List<Capture>();var folder=Path.GetFullPath("TestResults/HighEvolution-v15");Directory.CreateDirectory(folder);
            using var resources=new DioramaResources(content);
            try
            {
                for(int kind=0;kind<content.Towers.Length;kind++)
                {
                    var variants=new List<TowerState>();
                    foreach(int level in new[]{10,15,20,25,30,40,50,75,100})variants.Add(Purchase(content,kind,level,0));
                    if(kind!=4){variants.Add(Purchase(content,kind,0,30));variants.Add(Purchase(content,kind,15,15));variants.Add(Purchase(content,kind,25,5));variants.Add(Purchase(content,kind,5,25));}
                    var bounds=new Bounds();bool first=true;
                    foreach(var tower in variants)
                    {
                        var art=resources.GetTowerEvolution(kind,kind==4?tower.Level:tower.DamageLevel,tower.RangeLevel);
                        foreach(var mesh in new[]{art.Base,art.Head,art.PathModule})if(mesh!=null)foreach(var p in mesh.vertices)
                        {var v=Quaternion.Inverse(cam.transform.rotation)*(p+(mesh==art.Base?Vector3.zero:DioramaResources.TowerHeadOffset(kind)));if(first){bounds=new Bounds(v,Vector3.zero);first=false;}else bounds.Encapsulate(v);}
                    }
                    cam.orthographicSize=Mathf.Max(bounds.extents.x,bounds.extents.y)*1.13f;cam.transform.position=root.transform.position+cam.transform.rotation*bounds.center-cam.transform.forward*20;
                    Color32[] previous=null;var hashes=new HashSet<string>();
                    foreach(var tower in variants)
                    {
                        var art=resources.GetTowerEvolution(kind,kind==4?tower.Level:tower.DamageLevel,tower.RangeLevel);var actors=new List<GameObject>();int tris=0;
                        foreach(var mesh in new[]{art.Base,art.Head,art.PathModule})
                        {
                            if(mesh==null)continue;tris+=mesh.triangles.Length/3;
                            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.layer=30;go.transform.SetParent(root.transform,false);go.transform.localPosition=mesh==art.Base?Vector3.zero:DioramaResources.TowerHeadOffset(kind);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=resources.Material;actors.Add(go);
                            foreach(var v in mesh.vertices){var p=cam.WorldToViewportPoint(go.transform.TransformPoint(v));Assert.That(p.x,Is.InRange(.001f,.999f));Assert.That(p.y,Is.InRange(.001f,.999f));}
                        }
                        cam.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,384,384),0,0);image.Apply();var pixels=image.GetPixels32();int changed=0;
                        if(previous!=null)for(int n=0;n<pixels.Length;n++)if(Math.Abs(pixels[n].r-previous[n].r)+Math.Abs(pixels[n].g-previous[n].g)+Math.Abs(pixels[n].b-previous[n].b)>12)changed++;
                        var png=image.EncodeToPNG();var hash=Digest(png);Assert.That(hashes.Add(hash),Is.True,"Rendered high upgrade duplicated class "+kind);
                        if(previous!=null)Assert.That(changed,Is.GreaterThanOrEqualTo(24),"Upgrade hidden in production portrait camera");previous=pixels;
                        string file="tower-"+kind+"-level-"+tower.Level+"-p"+tower.DamageLevel+"-s"+tower.RangeLevel+".png";File.WriteAllBytes(Path.Combine(folder,file),png);
                        records.Add(new Capture{kind=kind,level=(int)tower.Level,primary=(int)tower.DamageLevel,secondary=(int)tower.RangeLevel,triangles=tris,changedPixels=changed,file=file,sha256=hash});
                        foreach(var go in actors){go.SetActive(false);Object.Destroy(go);}
                    }
                    yield return null;
                }
                File.WriteAllText(Path.Combine(folder,"gallery.json"),JsonUtility.ToJson(new Gallery{scope="Actual accepted gold upgrades and shared production meshes, native Unity rendering; not hardware performance",pictures=records.ToArray()},true));
            }
            finally{RenderTexture.active=old;cam.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(image);Object.Destroy(root);Object.Destroy(cameraGo);Object.Destroy(lightGo);}
        }
    }
}
