using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DefenseDuell.Content;
using DefenseDuell.Core;
using DefenseDuell.Editor.GraphicsQuality;
using System.Security.Cryptography;
using DefenseDuell.Presentation;
using UnityEditor;
using UnityEngine;

namespace DefenseDuell.Editor
{
    // Root-only offline command. Nothing in this class is a gameplay operation.
    public static class GraphicsSurfaceBake
    {
        const string Destination="Assets/Presentation/Resources/DefenseSurfaceOcclusion.bytes";
        [Serializable] sealed class Row { public string key,poseSha256;public int triangles,rays,truncatedRays,uniqueSamples,parts,samples; }
        [Serializable] sealed class Report { public string status="passed";public int groups,bytes;public Row[] rows; public string archiveSha256; public string[] sourcePaths, sourceSha256; public GraphicsMeshAoBake.Options modelOptions,terrainOptions; }
        [MenuItem("Defense Duell/Graphics/Bake surface occlusion")]
        public static void Run()
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameContentAsset>("Assets/Content/Standard.asset");
            if(asset==null)throw new InvalidOperationException("Prepare content before surface bake");
            var contents=MapCatalog.CreateContents(asset.CreateContent());
            var records=new List<GraphicsSurfaceArchive.Record>();var rows=new List<Row>();
            for(int map=0;map<contents.Length;map++)using(var resources=new DioramaResources(contents[map]))
            {
                Add("terrain/"+contents[map].Map.Id,new[]{resources.Terrain},new[]{Vector3.zero},true,records,rows);
                var backdrop=Backdrop(contents[map].Map);
                try{Add("backdrop/"+contents[map].Map.Id,new[]{backdrop},new[]{Vector3.zero},true,records,rows);}
                finally{UnityEngine.Object.DestroyImmediate(backdrop);}
                if(map!=0)continue;
                for(int kind=0;kind<contents[map].Towers.Length;kind++)for(int variant=0;variant<(kind==4?3:6);variant++)
                    Add("legacy/tower/"+kind+"/v"+variant,new[]{resources.TowerBases[kind,variant],resources.TowerHeads[kind,variant]},
                        new[]{Vector3.zero,DioramaResources.TowerHeadOffset(kind)},false,records,rows);
                for(int kind=0;kind<contents[map].Towers.Length;kind++)for(int tier=1;tier<=10;tier++)
                {
                    var art=resources.GetTowerEvolution(kind,tier,0);
                    Add("tower/"+kind+"/t"+tier,new[]{art.Base,art.Head},new[]{Vector3.zero,DioramaResources.TowerHeadOffset(kind)},false,records,rows);
                }
                for(int kind=0;kind<contents[map].Towers.Length;kind++)
                {
                    if(kind==4)continue;
                    for(int path=1;path<=3;path++)
                    {
                        var art=resources.GetTowerEvolution(kind,path==2?0:1,path==1?0:1);
                        Add("path/"+kind+"/p"+path,new[]{art.PathModule},new[]{Vector3.zero},false,records,rows);
                    }
                }
                for(int kind=0;kind<contents[map].Units.Length;kind++)for(int tier=0;tier<EliteUnitGeometry.StageCount(kind);tier++)
                    Add("unit/"+kind+"/t"+tier,new[]{resources.GetUnitEvolution(kind,EliteUnitGeometry.LevelForVisualStage(kind,tier))},new[]{Vector3.zero},false,records,rows);
            }
            var bytes=GraphicsSurfaceArchive.Write(records);
            // Do not replace the last good archive until every group successfully bakes.
            Directory.CreateDirectory(Path.GetDirectoryName(Destination));File.WriteAllBytes(Destination,bytes);
            AssetDatabase.ImportAsset(Destination,ImportAssetOptions.ForceUpdate);GraphicsSurfaceAssets.Reset();
            string folder="Docs/ResearchEvidence/graphics-quality/native-v16";Directory.CreateDirectory(folder);
            var sources=new[]{"Assets/Editor/GraphicsMeshAoBake.cs","Assets/Editor/GraphicsSurfaceBake.cs","Assets/Presentation/GraphicsSurfaceArchive.cs","Assets/Presentation/GraphicsSurfaceCache.cs","Assets/Presentation/GraphicsSurfaceResources.cs","Assets/Presentation/DioramaMeshes.cs","Assets/Presentation/EliteUnitGeometry.cs","Assets/Presentation/VisualEvolutionGeometry.cs","Assets/Presentation/VisualEvolutionResources.cs","Assets/Presentation/ScenicTerrain.cs","Assets/Presentation/MapLandmarkArt.cs","Assets/Presentation/MapLandmarkArt.Expanded.cs","Assets/Core/MapCatalog.cs","Assets/Presentation/Resources/DefenseDiorama.shader"};
            var hashes=new string[sources.Length];for(int i=0;i<sources.Length;i++)hashes[i]=File.Exists(sources[i])?Sha(File.ReadAllBytes(sources[i])):"missing";
            File.WriteAllText(folder+"/surface-bake.json",JsonUtility.ToJson(new Report{groups=records.Count,bytes=bytes.Length,rows=rows.ToArray(),archiveSha256=Sha(bytes),sourcePaths=sources,sourceSha256=hashes,modelOptions=Options(false),terrainOptions=Options(true)},true));
            Debug.Log("GRAPHICS_SURFACE_BAKE groups="+records.Count+" bytes="+bytes.Length);
        }
        static string Sha(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static GraphicsMeshAoBake.Options Options(bool terrain)=>new GraphicsMeshAoBake.Options{Samples=8,Radius=terrain?.65f:.32f,Bias=.003f,Strength=.35f,MaxNodeVisitsPerRay=terrain?1024:4096};
        static Mesh Backdrop(MapDefinition map)
        {
            var assembly=typeof(DioramaResources).Assembly;
            var builderType=assembly.GetType("DefenseDuell.Presentation.DioramaMeshBuilder",true);
            var builder=Activator.CreateInstance(builderType,true);
            assembly.GetType("DefenseDuell.Presentation.ScenicTerrain",true).GetMethod("BuildBackdrop",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{builder,map});
            return (Mesh)builderType.GetMethod("Build").Invoke(builder,new object[]{"Backdrop surface bake"});
        }
        static void Add(string key,Mesh[] meshes,Vector3[] offsets,bool terrain,List<GraphicsSurfaceArchive.Record> records,List<Row> rows)
        {
            var options=Options(terrain);
            var baked=GraphicsMeshAoBake.Bake(meshes,offsets,options);
            if(!baked.Success)throw new InvalidOperationException("Surface bake "+key+": "+baked.Problem);
            var parts=new GraphicsSurfaceArchive.Part[meshes.Length];int samples=0;
            for(int i=0;i<parts.Length;i++)
            {
                var p=baked.Parts[i];
                if(GraphicsSurfaceCache.GeometrySha256(meshes[i])!=p.GeometrySha256)throw new InvalidDataException("Geometry hash implementations disagree");
                parts[i]=new GraphicsSurfaceArchive.Part{GeometrySha256=p.GeometrySha256,X=p.Offset.x,Y=p.Offset.y,Z=p.Offset.z,Darkening=p.Darkening};samples+=p.Darkening.Length;
            }
            records.Add(new GraphicsSurfaceArchive.Record{Key=key,Parts=parts});
            rows.Add(new Row{key=key,poseSha256=baked.PoseSha256,triangles=baked.Triangles,rays=baked.Rays,truncatedRays=baked.TruncatedRays,uniqueSamples=baked.UniqueSamples,parts=parts.Length,samples=samples});
        }
    }
}
