using System;
using System.Collections.Generic;
using BigInteger = System.Numerics.BigInteger;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseDuell.Presentation
{
    public enum VisualEvolutionPath { None, Primary, Secondary, Mixed }

    // Only this small visual key is cached. Exact gameplay levels never become array indices.
    public static class VisualEvolution
    {
        public const int MaximumTier = 10; // Existing troop series.
        public const int TowerClassCapacity = 8, MaximumTowerTier = 34, PathBandCount = 9;
        static readonly int[] LateLevels = {40,50,75,100};
        public static int TowerTier(BigInteger level)
        {
            if(level.Sign<=0)return 0;
            if(level<=30)return (int)level;
            int tier=30;for(int i=0;i<LateLevels.Length;i++)if(level>=LateLevels[i])tier=31+i;
            return tier;
        }
        public static int LevelForTowerTier(int tier)=>tier<=30?Mathf.Clamp(tier,0,30):LateLevels[Mathf.Clamp(tier-31,0,3)];
        // Bounded independent path signatures. Geometry also changes at every total purchase through30.
        public static int PathBand(BigInteger level)=>level<=0?0:level<10?1:level<15?2:level<20?3:level<25?4:level<30?5:level<50?6:level<100?7:8;
        public static int Tier(BigInteger level) => level.Sign <= 0 ? 0 : level >= MaximumTier ? MaximumTier : (int)level;
        public static VisualEvolutionPath Path(BigInteger primary, BigInteger secondary)
            => primary.Sign > 0 ? (secondary.Sign > 0 ? VisualEvolutionPath.Mixed : VisualEvolutionPath.Primary)
                : secondary.Sign > 0 ? VisualEvolutionPath.Secondary : VisualEvolutionPath.None;
        public static int TowerKey(int kind, BigInteger primary, BigInteger secondary)
        {
            int tier = TowerTier(BigInteger.Max(BigInteger.Zero,primary) + BigInteger.Max(BigInteger.Zero,secondary));
            if(tier>10)return 1000+tier*100+PathBand(primary)*10+(kind==4?0:PathBand(secondary));
            return tier * 4 + (kind == 4 ? 0 : (int)Path(primary,secondary));
        }
    }

    public readonly struct VisualEvolutionTower
    {
        public readonly Mesh Base, Head, PathModule;
        public readonly int Tier, Key;
        public readonly VisualEvolutionPath PathStyle;
        public VisualEvolutionTower(Mesh foundation, Mesh head, Mesh module, int tier, VisualEvolutionPath path, int key=-1)
        { Base=foundation; Head=head; PathModule=module; Tier=tier; PathStyle=path; Key=key<0?tier*4+(int)path:key; }
    }

    public sealed partial class DioramaResources
    {
        // Legacy series plus finite high silhouettes. No exact BigInteger is an array index.
        // Tier zero aliases the original meshes. Path accents are tiny, shared head-local meshes.
        readonly Mesh[,] evolutionBases = new Mesh[VisualEvolution.TowerClassCapacity,11], evolutionHeads = new Mesh[VisualEvolution.TowerClassCapacity,11];
        readonly Mesh[,] evolutionPaths = new Mesh[VisualEvolution.TowerClassCapacity,4], evolutionUnits = new Mesh[DefenseDuell.Core.GameContent.UnitClassCount,11];
        readonly Mesh[,] highBases=new Mesh[VisualEvolution.TowerClassCapacity,24], highHeads=new Mesh[VisualEvolution.TowerClassCapacity,24];
        readonly Dictionary<int,Mesh> highPaths=new Dictionary<int,Mesh>();
        bool evolutionDisposed;
        int evolutionMeshCount;
        public const int MaximumAdditionalEvolutionMeshes = 221; // Low series upper bound including laser.
        public const int MaximumHighEvolutionMeshes = 8*24*2+7*(VisualEvolution.PathBandCount*VisualEvolution.PathBandCount-1);
        public const int MaximumTotalEvolutionMeshes = MaximumAdditionalEvolutionMeshes+MaximumHighEvolutionMeshes;
        public const float MaximumEvolutionTowerHeight = 3.3f;
        public const float MaximumEvolutionTowerRadius = 1.55f;
        public int EvolutionMeshCount => evolutionMeshCount;

        public VisualEvolutionTower GetTowerEvolution(int kind, BigInteger primaryLevel, BigInteger secondaryLevel)
        {
            if(evolutionDisposed)throw new ObjectDisposedException(nameof(DioramaResources));
            if(kind<0||kind>=VisualEvolution.TowerClassCapacity||kind>=TowerBases.GetLength(0))throw new ArgumentOutOfRangeException(nameof(kind));
            int tier=VisualEvolution.TowerTier(BigInteger.Max(BigInteger.Zero,primaryLevel)+BigInteger.Max(BigInteger.Zero,secondaryLevel));
            if(tier>10)return GetHighTowerEvolution(kind,primaryLevel,secondaryLevel,tier);
            var path=kind==4?VisualEvolutionPath.None:VisualEvolution.Path(primaryLevel,secondaryLevel);
            if(tier==0)return new VisualEvolutionTower(TowerBases[kind,0],TowerHeads[kind,0],null,0,VisualEvolutionPath.None);
            if(evolutionBases[kind,tier]==null)
            {
                var b=new DioramaMeshBuilder();EvolutionTowerBase(b,kind,tier);
                evolutionBases[kind,tier]=MergeEvolution(TowerBases[kind,0],b,"Evolution base "+kind+" tier "+tier);
            }
            if(evolutionHeads[kind,tier]==null)
            {
                var b=new DioramaMeshBuilder();EvolutionTowerHead(b,kind,tier);
                evolutionHeads[kind,tier]=MergeEvolution(TowerHeads[kind,0],b,"Evolution head "+kind+" tier "+tier);
            }
            Mesh module=null;
            if(path!=VisualEvolutionPath.None)
            {
                if(evolutionPaths[kind,(int)path]==null)
                {
                    var b=new DioramaMeshBuilder();EvolutionPathModule(b,kind,path);
                    module=Own(b,"Evolution path "+kind+" "+path);evolutionMeshCount++;
                    evolutionPaths[kind,(int)path]=module;
                }
                else module=evolutionPaths[kind,(int)path];
            }
            ApplyEvolutionTowerSurface(kind,tier,path,module);
            return new VisualEvolutionTower(evolutionBases[kind,tier],evolutionHeads[kind,tier],module,tier,path);
        }
        VisualEvolutionTower GetHighTowerEvolution(int kind,BigInteger primary,BigInteger secondary,int tier)
        {
            // Build from the actual shaded legacy10 meshes, not a mismatched archive entry.
            var legacy=GetTowerEvolution(kind,10,0);int slot=tier-11;
            if(highBases[kind,slot]==null)
            {
                var builder=new DioramaMeshBuilder();HighTowerBase(builder,kind,VisualEvolution.LevelForTowerTier(tier));
                highBases[kind,slot]=MergeEvolution(legacy.Base,builder,"High evolution base "+kind+" tier "+tier);
            }
            if(highHeads[kind,slot]==null)
            {
                var builder=new DioramaMeshBuilder();HighTowerHead(builder,kind,VisualEvolution.LevelForTowerTier(tier));
                highHeads[kind,slot]=MergeEvolution(legacy.Head,builder,"High evolution head "+kind+" tier "+tier);
            }
            Mesh module=null;var path=kind==4?VisualEvolutionPath.None:VisualEvolution.Path(primary,secondary);
            int p=VisualEvolution.PathBand(primary),r=VisualEvolution.PathBand(secondary);
            if(path!=VisualEvolutionPath.None)
            {
                int cacheKey=kind*100+p*10+r;
                if(!highPaths.TryGetValue(cacheKey,out module))
                {
                    var builder=new DioramaMeshBuilder();EvolutionPathModule(builder,kind,path);
                    HighTowerPath(builder,kind,p,r);
                    module=Own(builder,"High evolution path "+kind+" p"+p+" s"+r);
                    VisualEvolutionMeshQuality.SmoothTurnedSurfaces(module);
                    highPaths.Add(cacheKey,module);evolutionMeshCount++;
                }
            }
            return new VisualEvolutionTower(highBases[kind,slot],highHeads[kind,slot],module,tier,path,
                VisualEvolution.TowerKey(kind,primary,secondary));
        }
        public Mesh GetUnitEvolution(int kind, BigInteger researchLevel)
        {
            if(evolutionDisposed)throw new ObjectDisposedException(nameof(DioramaResources));
            if(kind<0||kind>=Units.Length)throw new ArgumentOutOfRangeException(nameof(kind));
            int tier=EliteUnitGeometry.VisualStage(kind,researchLevel);
            if(tier==0)return Units[kind];
            if(evolutionUnits[kind,tier]==null)
            {
                if(kind>=4){evolutionUnits[kind,tier]=EliteUnitGeometry.Build(kind,researchLevel);owned.Add(evolutionUnits[kind,tier]);evolutionMeshCount++;}
                else {var b=new DioramaMeshBuilder();EvolutionUnit(b,kind,tier);
                    evolutionUnits[kind,tier]=MergeEvolution(Units[kind],b,"Evolution troop "+kind+" tier "+tier);}
                ExpandUnitMotionBounds(evolutionUnits[kind,tier]);
            }
            ApplyEvolutionUnitSurface(kind,tier);
            return evolutionUnits[kind,tier];
        }
        public float UnitEvolutionHeight(int kind, BigInteger level)=>GetUnitEvolution(kind,level).bounds.max.y;
        // Add this value to the caller's desired world ground height (not an absolute board y).
        public float UnitEvolutionGroundOffset(int kind, BigInteger level)=>-GetUnitEvolution(kind,level).bounds.min.y;

        static void ExpandUnitMotionBounds(Mesh mesh)
        {var bounds=mesh.bounds;bounds.Expand(new Vector3(.05f,0,0));mesh.bounds=bounds;}

        Mesh MergeEvolution(Mesh original,DioramaMeshBuilder additions,string name)
        {
            // Preserve the exact original vertices, normals, colors and finish data. Only
            // append geometry; no collider, renderer, material, or per-entity instance.
            var extra=additions.Build(name+" additions");
            if(name.StartsWith("High evolution",StringComparison.Ordinal))VisualEvolutionMeshQuality.SmoothTurnedSurfaces(extra);
            try
            {
                var mesh=new Mesh{name=name,hideFlags=HideFlags.DontSave};
                if(original.vertexCount+extra.vertexCount>65535)mesh.indexFormat=IndexFormat.UInt32;
                mesh.CombineMeshes(new[]{new CombineInstance{mesh=original,transform=Matrix4x4.identity},new CombineInstance{mesh=extra,transform=Matrix4x4.identity}},true,false,false);
                mesh.RecalculateBounds();owned.Add(mesh);evolutionMeshCount++;return mesh;
            }
            finally { Release(extra); }
        }
        void DisposeEvolution()
        {
            evolutionDisposed=true;
            Array.Clear(evolutionBases,0,evolutionBases.Length);Array.Clear(evolutionHeads,0,evolutionHeads.Length);
            Array.Clear(evolutionPaths,0,evolutionPaths.Length);Array.Clear(evolutionUnits,0,evolutionUnits.Length);
            Array.Clear(highBases,0,highBases.Length);Array.Clear(highHeads,0,highHeads.Length);highPaths.Clear();
            evolutionMeshCount=0; // Actual Unity objects are released once by the common owned list.
        }
    }
}
