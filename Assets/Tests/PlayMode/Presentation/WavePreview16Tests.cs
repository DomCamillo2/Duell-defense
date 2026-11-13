using DefenseDuell.Core;
using NUnit.Framework;
namespace DefenseDuell.Presentation.Tests
{
 public sealed class WavePreview16Tests
 {
  [Test] public void FinitePreviewStopsAtFinalWaveAndUsesActualHp()
  {
   var content=GameContent.CreateDefault().WithMatchRules(new MatchRules(10,180));
   string text=WavePreview16.Describe(content,9);
   StringAssert.Contains("Welle 9",text);StringAssert.Contains("Welle 10",text);StringAssert.DoesNotContain("Welle 11",text);
   var recipe=content.GetWave(10);StringAssert.Contains(recipe.Title,text);
   for(int k=0;k<4;k++)if(recipe.Counts[k]>0)StringAssert.Contains(GameNumbers.Exact(recipe.HpForKind(k))+" LP je Truppe",text);
  }
  [Test] public void EndlessPreviewRemainsThreeRecipes()
  {
   var content=GameContent.CreateDefault().WithMatchRules(new MatchRules(10,180,true));
   string text=WavePreview16.Describe(content,100);
   StringAssert.Contains("Welle 100",text);StringAssert.Contains("Welle 102",text);StringAssert.DoesNotContain("Welle 103",text);
  }
 }
}
