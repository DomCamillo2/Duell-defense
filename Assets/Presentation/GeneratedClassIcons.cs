using UnityEngine;
using System.Collections.Generic;
using BigInteger=System.Numerics.BigInteger;

namespace DefenseDuell.Presentation
{
    /// <summary>Baked portraits of the field meshes. Concept images remain a missing-asset fallback.</summary>
    public static class GeneratedClassIcons
    {
        static readonly string[] TowerNames={"rapid","cannon","frost","armorbreaker","mine","tesla","ballista","laser"};
        static readonly string[] UnitNames={"runner","sprinter","tank","swarm","stormcolossus","siegecolossus"};
        static Sprite[] towers,units;
        static Sprite[,] variants;
        static bool[,] variantLoaded;
        static readonly Dictionary<int,Sprite> evolutionTowers=new Dictionary<int,Sprite>();
        const int EvolutionTowerCacheCapacity=64;
        static readonly LinkedList<int> evolutionOrder=new LinkedList<int>();
        static readonly Dictionary<int,LinkedListNode<int>> evolutionNodes=new Dictionary<int,LinkedListNode<int>>();
        public static int CachedEvolutionTowerCount=>evolutionTowers.Count;
        public static void TrimEvolutionCache(){evolutionTowers.Clear();evolutionOrder.Clear();evolutionNodes.Clear();}
        static void TouchTower(int id)
        {
            if(evolutionNodes.TryGetValue(id,out var node)){evolutionOrder.Remove(node);evolutionOrder.AddLast(node);return;}
            evolutionNodes[id]=evolutionOrder.AddLast(id);
            while(evolutionTowers.Count>EvolutionTowerCacheCapacity)
            {
                int oldest=evolutionOrder.First.Value;evolutionOrder.RemoveFirst();evolutionNodes.Remove(oldest);evolutionTowers.Remove(oldest);
                // Native sprites can still be displayed by an Image. Never destroy them here.
            }
        }
        static readonly Dictionary<int,Sprite> evolutionUnits=new Dictionary<int,Sprite>();
        public static int EvolutionTier(BigInteger level)=>(int)BigInteger.Min(10,BigInteger.Max(0,level));
        public static int EvolutionKey(BigInteger primary,BigInteger secondary)
        {
            int tier=EvolutionTier(primary+secondary);
            return tier*4+(tier==0?0:primary>0&&secondary>0?3:secondary>0?2:1);
        }
        public static Sprite GetTowerEvolution(int kind,BigInteger primary,BigInteger secondary)
        {
            Load();if(kind<0||kind>=TowerNames.Length)return null;
            int key=VisualEvolution.TowerKey(kind,primary,secondary),id=kind*10000+key;
            if(!evolutionTowers.TryGetValue(id,out var portrait))
            {
                portrait=Resources.Load<Sprite>("DefenseModelPortraits/evolution_tower_"+TowerNames[kind]+"_k"+key);
                evolutionTowers[id]=portrait;
            }
            TouchTower(id);return portrait!=null?portrait:towers[kind];
        }
        public static Sprite GetUnitEvolution(int kind,BigInteger level)
        {
            Load();if(kind<0||kind>=UnitNames.Length)return null;
            int tier=EliteUnitGeometry.VisualStage(kind,level),id=kind*11+tier;
            if(!evolutionUnits.TryGetValue(id,out var portrait))
            {
                portrait=Resources.Load<Sprite>("DefenseModelPortraits/evolution_unit_"+UnitNames[kind]+"_s"+tier);
                evolutionUnits[id]=portrait;
            }
            return portrait!=null?portrait:units[kind];
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { towers=null;units=null;variants=null;variantLoaded=null;TrimEvolutionCache();evolutionUnits.Clear(); }

        static void Load()
        {
            if(towers!=null)return;
            towers=new Sprite[TowerNames.Length];units=new Sprite[UnitNames.Length];variants=new Sprite[TowerNames.Length,6];
            variantLoaded=new bool[TowerNames.Length,6];
            for(int i=0;i<towers.Length;i++)
            {
                towers[i]=Resources.Load<Sprite>("DefenseModelPortraits/evolution_tower_"+TowerNames[i]+"_k0");
                if(towers[i]==null)
                {
                    variants[i,0]=Resources.Load<Sprite>("DefenseModelPortraits/tower_"+TowerNames[i]+"_v0");
                    variantLoaded[i,0]=true;
                    towers[i]=variants[i,0]!=null?variants[i,0]:Resources.Load<Sprite>("DefenseClassIcons/tower_"+TowerNames[i]);
                }
            }
            for(int i=0;i<units.Length;i++)
            {
                units[i]=Resources.Load<Sprite>("DefenseModelPortraits/evolution_unit_"+UnitNames[i]+"_s0");
                if(units[i]==null)units[i]=Resources.Load<Sprite>("DefenseModelPortraits/unit_"+UnitNames[i]);
                if(units[i]==null)units[i]=Resources.Load<Sprite>("DefenseClassIcons/unit_"+UnitNames[i]);
            }
        }
        public static Sprite Get(int kind,bool tower)
        {Load();var values=tower?towers:units;return kind>=0&&kind<values.Length?values[kind]:null;}

        public static Sprite GetTowerVariant(int kind,int visualVariant)
        {
            Load();
            if(kind<0||kind>=towers.Length)return null;
            int count=kind==4?3:6;
            if(visualVariant<0||visualVariant>=count)return towers[kind];
            // Load legacy art only on explicit request; current base cards use eleven evolution textures.
            if(!variantLoaded[kind,visualVariant])
            {
                variants[kind,visualVariant]=Resources.Load<Sprite>("DefenseModelPortraits/tower_"+TowerNames[kind]+"_v"+visualVariant);
                variantLoaded[kind,visualVariant]=true;
            }
            return variants[kind,visualVariant]!=null?variants[kind,visualVariant]:towers[kind];
        }
    }
}
