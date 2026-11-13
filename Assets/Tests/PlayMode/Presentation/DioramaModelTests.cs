using System.Collections;
using System.Reflection;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class DioramaModelTests
    {
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static void CheckMesh(Mesh mesh)
        {
            Assert.That(mesh,Is.Not.Null);var vertices=mesh.vertices;var normals=mesh.normals;var colors=mesh.colors32;var finish=mesh.uv;var triangles=mesh.triangles;
            Assert.That(vertices.Length,Is.GreaterThan(0),mesh.name);Assert.That(normals.Length,Is.EqualTo(vertices.Length),mesh.name);
            Assert.That(colors.Length,Is.EqualTo(vertices.Length),mesh.name);Assert.That(finish.Length,Is.EqualTo(vertices.Length),mesh.name);
            Assert.That(triangles.Length%3,Is.Zero,mesh.name);bool polished=false,matte=false;
            for(int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i];var n=normals[i];var f=finish[i];
                if(!Finite(p.x)||!Finite(p.y)||!Finite(p.z))Assert.Fail(mesh.name+" contains a non-finite vertex "+i);
                if(!Finite(n.sqrMagnitude)||Mathf.Abs(n.sqrMagnitude-1)>.005f)Assert.Fail(mesh.name+" contains an invalid normal "+i+": "+n);
                if(colors[i].a!=255)Assert.Fail(mesh.name+" uses material alpha instead of opaque geometry");
                if(!Finite(f.x)||f.x<0||f.x>1||f.y!=0)Assert.Fail(mesh.name+" has invalid surface finish data");
                polished|=f.x>=.5f;matte|=f.x<.5f;
            }
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
                if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-14f)Assert.Fail(mesh.name+" contains a degenerate model triangle "+i/3);
            }
            Assert.That(matte||polished,Is.True);
        }
        [UnityTest]
        public IEnumerator AllModelStatesHaveFiniteOpaqueGeometryAndBoundedBudgets()
        {
            var content=GameContent.CreateDefault();var catalog=new DioramaResources(content);var sample=catalog.Units[0];var material=catalog.Material;
            try
            {
                Assert.That(material.shader.name,Is.EqualTo("DefenseDuell/DioramaVertexLit"));
                int states=0,maxTowerTriangles=0,maxUnitTriangles=0;
                for(int kind=0;kind<7;kind++)for(int variant=0;variant<(kind==4?3:6);variant++)
                {
                    var basis=catalog.TowerBases[kind,variant];var head=catalog.TowerHeads[kind,variant];CheckMesh(basis);CheckMesh(head);
                    int count=(basis.triangles.Length+head.triangles.Length)/3;maxTowerTriangles=Mathf.Max(maxTowerTriangles,count);
                    Assert.That(count,Is.LessThanOrEqualTo(2200),"Tower budget: class "+kind+" variant "+variant);
                    foreach(var part in new[]{basis,head})
                    {
                        var vertices=part.vertices;
                        for(int i=0;i<vertices.Length;i++)
                        {
                            var point=vertices[i]+(part==head?DioramaResources.TowerHeadOffset(kind):Vector3.zero);
                            if(point.y>catalog.MaximumTowerHeight+.0001f||new Vector2(point.x,point.z).magnitude>catalog.MaximumTowerRadius+.0001f)
                                Assert.Fail("Cached overview bounds do not contain class "+kind+" variant "+variant+", "+part.name+" vertex "+i+" at "+point.ToString("F6"));
                            if(point.y<-.001f)Assert.Fail("Tower model extends below its authoritative ground plane: class "+kind+" variant "+variant+", "+part.name+" vertex "+i+" at "+point.ToString("F6"));
                        }
                    }
                    states++;
                }
                for(int kind=0;kind<4;kind++)
                {
                    var mesh=catalog.Units[kind];CheckMesh(mesh);int count=mesh.triangles.Length/3;maxUnitTriangles=Mathf.Max(maxUnitTriangles,count);
                    Assert.That(count,Is.LessThanOrEqualTo(1100),"Troop budget: class "+kind);
                    Assert.That(catalog.UnitVisualHeight(kind),Is.EqualTo(mesh.bounds.max.y).Within(.0001f));
                    Assert.That(catalog.UnitVisualHeight(kind),Is.GreaterThan(.5f));
                }
                // The tank stays lower and wider than the pointed sprinter, and the swarm is
                // a smaller member of the same expressive golem family as the runner.
                Assert.That(catalog.Units[2].bounds.size.x,Is.GreaterThan(catalog.Units[1].bounds.size.x));
                Assert.That(catalog.UnitVisualHeight(2),Is.LessThan(catalog.UnitVisualHeight(1)));
                Assert.That(catalog.UnitVisualHeight(3),Is.LessThan(catalog.UnitVisualHeight(0)));
                Assert.That(catalog.TowerHeads[2,3].bounds.size.x,Is.GreaterThan(catalog.TowerHeads[2,1].bounds.size.x*1.10f),"Frost Damage II needs a broader crown than Damage I");
                foreach(var finish in catalog.Terrain.uv)if(finish.x!=0||finish.y!=0)Assert.Fail("Terrain must retain a matte material mask");
                Assert.That(catalog.Range.bounds.extents.x,Is.EqualTo(1).Within(.0001f));Assert.That(catalog.Range.bounds.extents.z,Is.EqualTo(1).Within(.0001f));
                var rangeColors=catalog.Range.colors32;bool darkRange=false,lightRange=false;
                foreach(var color in rangeColors){darkRange|=color.r<75&&color.g<75&&color.b<75;lightRange|=color.r>220&&color.g>220&&color.b>150;}
                Assert.That(darkRange&&lightRange,Is.True,"Range must have contrasting bands while retaining its exact outside radius");
                Assert.That(states,Is.EqualTo(39));Debug.Log("DIORAMA_MODEL_BUDGET_PASS towers=39 units=4 max_tower_triangles="+maxTowerTriangles+" max_unit_triangles="+maxUnitTriangles);
            }
            finally{catalog.Dispose();}
            yield return null;Assert.That(sample==null,Is.True,"Owned model mesh must be released");Assert.That(material==null,Is.True,"Shared material must be released");
        }
        sealed class MeshProbe
        {
            readonly Vector3[] vertices;
            readonly int[] triangles;
            readonly Color32[] colors;
            readonly Vector2[] finish;
            public MeshProbe(Mesh mesh){vertices=mesh.vertices;triangles=mesh.triangles;colors=mesh.colors32;finish=mesh.uv;}
            public bool Cast(Vector3 origin,Vector3 direction,out Color32 color,out float surfaceFinish,out float distance,out bool conflictingSurface)
            {
                color=default;surfaceFinish=0;distance=float.PositiveInfinity;conflictingSurface=false;bool hit=false;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    int ia=triangles[i],ib=triangles[i+1],ic=triangles[i+2];var a=vertices[ia];var edge1=vertices[ib]-a;var edge2=vertices[ic]-a;
                    var p=Vector3.Cross(direction,edge2);float determinant=Vector3.Dot(edge1,p);
                    if(determinant<=.00000001f)continue; // Same front-face culling as the opaque production shader.
                    var s=origin-a;float u=Vector3.Dot(s,p)/determinant;if(u<-.00001f||u>1.00001f)continue;
                    var q=Vector3.Cross(s,edge1);float v=Vector3.Dot(direction,q)/determinant;if(v<-.00001f||u+v>1.00001f)continue;
                    float t=Vector3.Dot(edge2,q)/determinant;if(t<0)continue;
                    if(t<distance-.00001f){hit=true;distance=t;color=colors[ia];surfaceFinish=finish[ia].x;conflictingSurface=false;}
                    else if(Mathf.Abs(t-distance)<=.00001f&&(colors[ia].r!=color.r||colors[ia].g!=color.g||colors[ia].b!=color.b||Mathf.Abs(finish[ia].x-surfaceFinish)>.001f))conflictingSurface=true;
                }
                return hit;
            }
        }
        static void CheckDarkCavity(MeshProbe mesh,Vector3 origin,Vector3 direction,string detail,out Vector3 hit)
        {
            Assert.That(mesh.Cast(origin,direction,out var color,out float finish,out float distance,out bool overlap),Is.True,detail+": missing cavity surface");
            Assert.That(overlap,Is.False,detail+": differently colored coplanar surfaces");
            Assert.That(color.r<75&&color.g<75&&color.b<75,Is.True,detail+": the opening is blocked by a bright rim, brace or housing");
            Assert.That(finish,Is.Zero,detail+": the bore must be matte");hit=origin+direction*distance;
        }
        static void CheckMuzzle(MeshProbe mesh,Vector3 center,float length,Quaternion rotation,string detail)
        {
            var axis=rotation*Vector3.up;var opening=center+axis*(length*.5f+.015f);
            foreach(var view in new[]{axis,new Vector3(4,3.6f,6).normalized})
            {
                CheckDarkCavity(mesh,opening+view*.5f,-view,detail+" front/portrait ray",out var hit);
                Assert.That(Vector3.Dot(opening-hit,axis),Is.InRange(.005f,.12f),detail+": opening must have an actual recessed dark surface");
            }
            foreach(var side in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                CheckDarkCavity(mesh,opening-axis*.02f,rotation*side,detail+" interior wall ray",out _);
        }
        [UnityTest]
        public IEnumerator RapidAndCannonMuzzlesHaveDarkUnobstructedNonOverlappingCavities()
        {
            var catalog=new DioramaResources(GameContent.CreateDefault());int[,] paths={{0,0},{1,0},{0,1},{2,0},{1,1},{0,2}};int openings=0;
            try
            {
                // Fixed art fixtures for the real muzzle positions. Sampling from both
                // the bore axis and the portrait camera also catches decorative braces
                // crossing an otherwise valid opening, especially Rapid Damage II.
                for(int variant=0;variant<6;variant++)
                {
                    int range=paths[variant,1];var rapid=new MeshProbe(catalog.TowerHeads[0,variant]);float z=.35f+range*.12f,length=.58f+range*.24f;
                    foreach(var center in new[]{new Vector3(0,.39f,z),new Vector3(-.175f,.13f,z),new Vector3(.175f,.13f,z)})
                    {CheckMuzzle(rapid,center,length,Quaternion.Euler(90,0,0),"Rapid variant "+variant+" muzzle "+center);openings++;}
                    CheckMuzzle(new MeshProbe(catalog.TowerHeads[1,variant]),new Vector3(0,.31f,.32f+range*.16f),.72f+range*.32f,Quaternion.Euler(80,0,0),"Cannon variant "+variant);openings++;
                }
                Assert.That(openings,Is.EqualTo(24));
            }
            finally{catalog.Dispose();}
            yield return null;
        }
        [UnityTest]
        public IEnumerator PooledModelsKeepSharedGeometryGroundContactAndCleanEffects()
        {
            var content=GameContent.CreateDefault();var canvas=new GameObject("Model lifecycle canvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var root=new GameObject("Model lifecycle board",typeof(RectTransform));root.transform.SetParent(canvas.transform,false);((RectTransform)root.transform).sizeDelta=new Vector2(1280,720);
            var board=root.AddComponent<BoardView>();board.Initialize(content,null,null,false,null);board.ReducedEffects=true;
            var world=(GameObject)typeof(BoardView).GetField("world",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(board);
            var catalog=(DioramaResources)typeof(BoardView).GetField("resources",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(board);
            var state=new MatchSimulation(content).State;state.Phase=MatchPhase.Playing;
            GameObject unit=null;
            try
            {
                state.Units.Add(new UnitState{Id=41,Kind=0,Hp=content.Units[0].Hp,Distance=100,Defender=PlayerSlot.Zero,SlowPermille=1000});board.Display(state,PlayerSlot.Zero);
                foreach(Transform child in world.transform.Find("Unit pool"))if(child.gameObject.activeSelf){unit=child.gameObject;break;}
                Assert.That(unit,Is.Not.Null);var renderer=unit.GetComponent<MeshRenderer>();var filter=unit.GetComponent<MeshFilter>();
                Assert.That(unit.GetComponents<MeshRenderer>().Length,Is.EqualTo(1));Assert.That(filter.sharedMesh,Is.SameAs(catalog.Units[0]));Assert.That(renderer.sharedMaterial,Is.SameAs(catalog.Material));
                Assert.That(unit.transform.localPosition.y+filter.sharedMesh.bounds.min.y,Is.EqualTo(.214f).Within(.001f));
                var stale=new MaterialPropertyBlock();stale.SetFloat("_Flash",1);stale.SetColor("_Tint",Color.red);renderer.SetPropertyBlock(stale);unit.transform.localScale=Vector3.one*.7f;
                state.Units.Clear();board.Display(state,PlayerSlot.Zero);Assert.That(unit.activeSelf,Is.False);Assert.That(filter.sharedMesh,Is.Null);Assert.That(unit.transform.localScale,Is.EqualTo(Vector3.one));
                state.Units.Add(new UnitState{Id=42,Kind=3,Hp=content.Units[3].Hp,Distance=160,Defender=PlayerSlot.Zero,SlowPermille=1000});board.Display(state,PlayerSlot.Zero);
                Assert.That(unit.activeSelf,Is.True);Assert.That(filter.sharedMesh,Is.SameAs(catalog.Units[3]));Assert.That(unit.transform.localScale,Is.EqualTo(Vector3.one));
                var fresh=new MaterialPropertyBlock();renderer.GetPropertyBlock(fresh);Assert.That(fresh.GetFloat("_Flash"),Is.Zero);
                Assert.That(unit.transform.Find("Camera-facing health").localPosition.y,Is.EqualTo(catalog.UnitVisualHeight(3)+.15f).Within(.001f));
                Assert.That(unit.transform.localPosition.y+filter.sharedMesh.bounds.min.y,Is.EqualTo(.214f).Within(.001f));
                foreach(Transform tower in world.transform.Find("Tower views"))if(tower.name.StartsWith("Tower render slot"))Assert.That(tower.GetComponentsInChildren<MeshRenderer>(true).Length,Is.EqualTo(3));
                yield return null;
            }
            finally{Object.Destroy(canvas);}
            yield return null;Assert.That(world==null,Is.True);
        }
    }
}
