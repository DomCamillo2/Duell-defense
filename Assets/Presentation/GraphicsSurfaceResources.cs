using UnityEngine;

namespace DefenseDuell.Presentation
{
    // Draft integration: a shared data archive, with per-catalog application guards.
    // Geometry stays owned by the existing catalog/BoardView lifecycle.
    public sealed partial class DioramaResources
    {
        GraphicsSurfaceCache graphicsSurface;
        string graphicsMapId;
        readonly bool[,] graphicsTowerDone=new bool[VisualEvolution.TowerClassCapacity,11],graphicsUnitDone=new bool[DefenseDuell.Core.GameContent.UnitClassCount,11],graphicsPathDone=new bool[VisualEvolution.TowerClassCapacity,4];
        public int GraphicsSurfaceAppliedGroups=>graphicsSurface==null?0:graphicsSurface.AppliedGroups;
        public int GraphicsSurfaceMissingGroups=>graphicsSurface==null?0:graphicsSurface.MissingGroups;
        public int GraphicsSurfaceRejectedGroups=>graphicsSurface==null?0:graphicsSurface.RejectedGroups;
        void InitializeGraphicsSurfaceCache(string mapId)
        {
            graphicsMapId=mapId;var archive=GraphicsSurfaceAssets.Load();
            if(archive==null)return;
            graphicsSurface=new GraphicsSurfaceCache(archive);
            graphicsSurface.TryApplyOwned("terrain/"+mapId,new[]{Terrain},new[]{Vector3.zero},out _);
            for(int kind=0;kind<TowerBases.GetLength(0);kind++)for(int variant=0;variant<(kind==4?3:6);variant++)
                graphicsSurface.TryApplyOwned("legacy/tower/"+kind+"/v"+variant,
                    new[]{TowerBases[kind,variant],TowerHeads[kind,variant]},new[]{Vector3.zero,TowerHeadOffset(kind)},out _);
            for(int kind=0;kind<Units.Length;kind++)
            {
                graphicsSurface.TryApplyOwned("unit/"+kind+"/t0",new[]{Units[kind]},new[]{Vector3.zero},out _);
                graphicsUnitDone[kind,0]=true;
            }
        }
        void ApplyEvolutionTowerSurface(int kind,int tier,VisualEvolutionPath path,Mesh module)
        {
            if(graphicsSurface==null||tier==0)return;
            if(!graphicsTowerDone[kind,tier])
            {
                graphicsSurface.TryApplyOwned("tower/"+kind+"/t"+tier,new[]{evolutionBases[kind,tier],evolutionHeads[kind,tier]},
                    new[]{Vector3.zero,TowerHeadOffset(kind)},out _);
                graphicsTowerDone[kind,tier]=true;
            }
            // Path modules are shared between tiers. Their own AO must not depend on
            // whichever parent tier happened to be requested last.
            int p=(int)path;
            if(module!=null&&!graphicsPathDone[kind,p])
            {
                graphicsSurface.TryApplyOwned("path/"+kind+"/p"+p,new[]{module},new[]{Vector3.zero},out _);
                graphicsPathDone[kind,p]=true;
            }
        }
        void ApplyEvolutionUnitSurface(int kind,int tier)
        {
            if(graphicsSurface==null||graphicsUnitDone[kind,tier])return;
            graphicsSurface.TryApplyOwned("unit/"+kind+"/t"+tier,new[]{evolutionUnits[kind,tier]},new[]{Vector3.zero},out _);
            graphicsUnitDone[kind,tier]=true;
        }
        public void ApplyGraphicsBackdropSurface(Mesh ownedBackdrop)
        {
            if(graphicsSurface!=null)graphicsSurface.TryApplyOwned("backdrop/"+graphicsMapId,new[]{ownedBackdrop},new[]{Vector3.zero},out _);
        }
    }
    public static class GraphicsSurfaceAssets
    {
        static GraphicsSurfaceArchive archive;
        static bool loaded;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset(){archive=null;loaded=false;}
        public static GraphicsSurfaceArchive Load()
        {
            if(loaded)return archive;loaded=true;
            var asset=Resources.Load<TextAsset>("DefenseSurfaceOcclusion");if(asset==null)return null;
            try{archive=GraphicsSurfaceArchive.Read(asset.bytes);}
            catch(System.IO.IOException error){Debug.LogWarning("Surface shading data unavailable: "+error.Message);}
            finally{Resources.UnloadAsset(asset);}
            return archive;
        }
    }
}
