using System.Numerics;
using System.Text;
using DefenseDuell.Core;
namespace DefenseDuell.Presentation
{
    public static class WavePreview16
    {
        public static string Describe(GameContent content,BigInteger first)
        {
            var copy=new StringBuilder();var units=content.Units;
            for(int i=0;i<3;i++)
            {
                var id=first+i;if(!content.ContainsWave(id))break;var wave=content.GetWave(id);
                if(copy.Length>0)copy.Append("\n\n");
                copy.Append("Welle ").Append(GameNumbers.Exact(id)).Append(wave.IsSuperBossWave?" · SUPERBOSS · ":wave.IsBossWave?" · BOSS · ":" · ").Append(wave.Title);
                var counts=wave.Counts;
                for(int kind=0;kind<counts.Length;kind++)if(counts[kind]>0)
                    copy.Append("\n").Append(counts[kind]).Append(" × ").Append(units[kind].Name).Append(" · ").Append(GameNumbers.Exact(wave.HpForKind(kind))).Append(" LP je Truppe");
                if(!string.IsNullOrEmpty(wave.CounterHint))copy.Append("\nTipp: ").Append(wave.CounterHint);
            }
            return copy.ToString();
        }
    }
}
