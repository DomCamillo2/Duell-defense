using System;
using System.Numerics;
namespace DefenseDuell.Core
{
    public static class LateGameRules
    {
        public const string Version="counter-profiles-boss10-pressure-v4+"+EncounterGrowth.Version;
        public const string Canonical="10-step:12,4,2,6;14,10,4,12;4,6,2,36;8,4,18,6;8,8,8,8;6,28,4,6;8,6,12,10;10,12,8,16;12,4,2,6;4,4,1,8|permille:850,1000,900,1100,1150,950,1050,1200,850,1000|trend100+12(w-1)+ceil(2max(0,w-10)^2/5)|boss1250+15min(90,w-10);century3/2;share45%";
        static readonly int[][] Counts={new[]{12,4,2,6},new[]{14,10,4,12},new[]{4,6,2,36},new[]{8,4,18,6},new[]{8,8,8,8},new[]{6,28,4,6},new[]{8,6,12,10},new[]{10,12,8,16},new[]{12,4,2,6},new[]{4,4,1,8}};
        static readonly int[] Permille={850,1000,900,1100,1150,950,1050,1200,850,1000};
        static readonly string[] Ids={"breather","mixed","swarm","armored","dense-mix","rush","escort","mixed-pressure","breather","boss"};
        static readonly string[] Names={"Umbauwelle","Mischangriff","Schwarmflut","Panzerkolonne","Verdichtete Mischung","Sprintstoß","Eskorte","Gemischter Druck","Umbauwelle","Boss & Begleitung"};
        internal static WaveRecipe Create(GameContent content,BigInteger wave)
        {
            if(wave<10)throw new ArgumentOutOfRangeException(nameof(wave));
            int index=(int)((wave-1)%10);
            var pressure=BossPressure.At(content,wave,Counts[index],Permille[index]);
            bool boss=pressure.Boss;
            var grown=EncounterGrowth.Apply(wave,pressure.Counts,pressure.HpPerKind);
            string hint=boss?"Boss: hohe Einzeltreffer, Frost und mehrere Schusszonen vorbereiten":index==2?"Flächenschaden und Ketten-/Linientreffer helfen gegen die große Gruppe":index==3?"Panzerbrecher aufrüsten; hohe Einzeltreffer durchdringen die Rüstung":index==5?"Frost und verteilte Wegabdeckung bremsen den Sprintstoß":index==0||index==8?"Kürzere Umbauwelle: Positionen, Forschung und Angriff vorbereiten":"Gegnerklassen prüfen und Einzelschaden mit Flächenschaden kombinieren";
            return new WaveRecipe(new WaveDefinition(grown.Counts,1),pressure.PressurePercent,grown.HpPerKind,
                boss?"boss":Ids[index],boss&&pressure.Century?"SUPERBOSS":Names[index],hint,boss,grown.TotalHp,pressure.Century);
        }
        public static bool IsBoss(GameContent content,UnitState unit)=>unit!=null&&unit.Origin==UnitOrigin.NeutralWave&&
            content.GetWave(unit.WaveId).IsBossWave&&unit.NeutralRecipeIndex==content.GetWave(unit.WaveId).BossUnitIndex;
    }
}
