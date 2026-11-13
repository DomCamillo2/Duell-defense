using System;
using System.Reflection;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
namespace DefenseDuell.Presentation.Tests
{
    public sealed class GraphicsSurfaceCoverageTests
    {
        static void Baked(Mesh mesh)
        {
            var values=mesh.uv2;Assert.That(values.Length,Is.EqualTo(mesh.vertexCount),mesh.name);
            foreach(var value in values)if(float.IsNaN(value.x)||value.x<0||value.x>89/255f||value.y!=0)Assert.Fail("Invalid AO UV2: "+mesh.name);
        }
        [Test] public void EveryCatalogAndSharedPathUsesMatchingBakedArchiveExactlyOnce()
        {
            GraphicsSurfaceAssets.Reset();var archive=GraphicsSurfaceAssets.Load();
            Assert.That(archive,Is.Not.Null,"Run offline AO bake before acceptance");
            var basis=GameContent.CreateDefault();int legacy=0;
            for(int kind=0;kind<basis.Towers.Length;kind++)legacy+=kind==4?3:6;
            int sharedGroups=legacy+basis.Towers.Length*10+(basis.Towers.Length-1)*3+4*11+2*6;
            Assert.That(archive.Count,Is.EqualTo(MapCatalog.Count*2+sharedGroups));
            foreach(var content in MapCatalog.CreateContents(GameContent.CreateDefault()))using(var r=new DioramaResources(content))
            {
                Baked(r.Terrain);
                var assembly=typeof(DioramaResources).Assembly;var builderType=assembly.GetType("DefenseDuell.Presentation.DioramaMeshBuilder",true);
                var builder=Activator.CreateInstance(builderType,true);
                assembly.GetType("DefenseDuell.Presentation.ScenicTerrain",true).GetMethod("BuildBackdrop",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{builder,content.Map});
                var backdrop=(Mesh)builderType.GetMethod("Build").Invoke(builder,new object[]{"AO coverage backdrop"});
                try{r.ApplyGraphicsBackdropSurface(backdrop);Baked(backdrop);}finally{UnityEngine.Object.DestroyImmediate(backdrop);}
                for(int kind=0;kind<content.Towers.Length;kind++)for(int variant=0;variant<(kind==4?3:6);variant++){Baked(r.TowerBases[kind,variant]);Baked(r.TowerHeads[kind,variant]);}
                for(int kind=0;kind<content.Towers.Length;kind++)for(int tier=10;tier>=1;tier--)
                {
                    var art=r.GetTowerEvolution(kind,tier,0);Baked(art.Base);Baked(art.Head);
                    if(kind==4)continue;
                    Baked(art.PathModule);Baked(r.GetTowerEvolution(kind,0,tier).PathModule);Baked(r.GetTowerEvolution(kind,1,1).PathModule);
                }
                for(int kind=0;kind<GameContent.UnitClassCount;kind++)for(int tier=0;tier<EliteUnitGeometry.StageCount(kind);tier++)Baked(r.GetUnitEvolution(kind,EliteUnitGeometry.LevelForVisualStage(kind,tier)));
                Assert.That(r.GraphicsSurfaceMissingGroups,Is.Zero,content.Map.Id);Assert.That(r.GraphicsSurfaceRejectedGroups,Is.Zero,content.Map.Id);
                Assert.That(r.GraphicsSurfaceAppliedGroups,Is.EqualTo(2+sharedGroups),content.Map.Id);
                int before=r.GraphicsSurfaceAppliedGroups;
                for(int kind=0;kind<content.Towers.Length;kind++){r.GetTowerEvolution(kind,5,5);r.GetTowerEvolution(kind,1,0);}
                Assert.That(r.GraphicsSurfaceAppliedGroups,Is.EqualTo(before),"Shared meshes may not reapply per view/path/order");
            }
        }
    }
}
