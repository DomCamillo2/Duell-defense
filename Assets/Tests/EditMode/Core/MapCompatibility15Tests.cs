#if UNITY_EDITOR
using DefenseDuell.Core;
using NUnit.Framework;

namespace DefenseDuell.Tests
{
    public sealed class MapCompatibility15Tests
    {
        [Test] public void UnknownAuthoredTemplateRetainsItsGeometryAndConfiguredRulesAtSlotZero()
        {
            var source=GameContent.CreateDefault();var map=source.Map;
            var custom=new MapDefinition(map.Waypoints,map.BuildSlots,id:"custom-fixture",name:"Custom fixture",
                theme:map.Theme,width:map.Width,height:map.Height,mineSlots:map.MineSlots,
                freePlacement:map.FreePlacement,blockedAreas:map.BlockedAreas);
            var template=new GameContent(source.Rules,source.Towers,source.Units,custom,source.Waves,source.Research,source.WaveRules)
                .WithMatchRules(new MatchRules(200,500,endless:true,incomeMultiplier:3,format:MatchFormat.ThreeVsThree));
            var catalog=MapCatalog.CreateContents(template);
            Assert.That(catalog.Length,Is.EqualTo(6));Assert.That(catalog[0].Map,Is.SameAs(custom));
            foreach(var content in catalog)
            {
                Assert.That(content.MatchRules.Endless,Is.True);Assert.That(content.MatchRules.IncomeMultiplier,Is.EqualTo(3));
                Assert.That(content.MatchRules.Format,Is.EqualTo(MatchFormat.ThreeVsThree));
                Assert.That(content.MatchRules.MapId,Is.EqualTo(content.Map.Id));
                Assert.That(content.MatchStartGold.ToString(),Is.EqualTo("500"));
            }
        }
    }
}

#endif
