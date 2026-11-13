using DefenseDuell.Content;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DefenseDuell.Tests
{
    public sealed class ContentMigrationTests
    {
        static GameContent AuthoredMapContent(GameContent balance, int index)
            => new GameContent(balance.Rules, balance.Towers, balance.Units, MapCatalog.Create(index),
                balance.BaseContent.Waves, balance.Research, balance.WaveRules);

        [Test]
        public void ActualBuildAssetContainsCurrentAuthoredMaps()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameContentAsset>("Assets/Content/Standard.asset");
            Assert.That(asset, Is.Not.Null);
            var actual = asset.CreateCatalog();
            Assert.That(actual.Length, Is.EqualTo(MapCatalog.Count));
            for (int i = 0; i < actual.Length; i++)
                Assert.That(actual[i].ContentHash, Is.EqualTo(AuthoredMapContent(actual[i], i).ContentHash), "Actual serialized build map " + i);
        }

        [Test]
        public void MapRefreshRepairsStaleGeometryWithoutResettingBalanceAndIsIdempotent()
        {
            var asset = ScriptableObject.CreateInstance<GameContentAsset>();
            try
            {
                asset.SetEditorDefaults();
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("maps").GetArrayElementAtIndex(0).FindPropertyRelative("Width").intValue += 20;
                serialized.FindProperty("maps").GetArrayElementAtIndex(3).FindPropertyRelative("Name").stringValue = "Old map";
                serialized.FindProperty("towers").GetArrayElementAtIndex(0).FindPropertyRelative("Price").intValue = 123;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(asset.CreateContent(0).ContentHash, Is.Not.EqualTo(AuthoredMapContent(asset.CreateContent(0), 0).ContentHash), "Stale slot zero must be detected independently");
                Assert.That(asset.RefreshEditorMapCatalog(), Is.True);
                var current = asset.CreateCatalog();
                for (int i = 0; i < current.Length; i++)
                    Assert.That(current[i].ContentHash, Is.EqualTo(AuthoredMapContent(current[i], i).ContentHash), "Refreshed map " + i);
                Assert.That(asset.CreateContent(3).Map.Name, Is.EqualTo(MapCatalog.Create(3).Name));
                Assert.That(asset.CreateContent().Towers[0].Price, Is.EqualTo(123));
                var refreshed = EditorJsonUtility.ToJson(asset);
                Assert.That(asset.RefreshEditorMapCatalog(), Is.False);
                Assert.That(EditorJsonUtility.ToJson(asset), Is.EqualTo(refreshed));
            }
            finally { Object.DestroyImmediate(asset); }
        }

        [Test]
        public void SerializedCataloguePreservesAllAuthoritativeDefinitions()
        {
            var source = ScriptableObject.CreateInstance<GameContentAsset>();
            var restored = ScriptableObject.CreateInstance<GameContentAsset>();
            try
            {
                source.SetEditorDefaults();
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source), restored);
                var expected = MapCatalog.CreateContents(GameContent.CreateDefault());
                var actual = restored.CreateCatalog();
                Assert.That(actual.Length, Is.EqualTo(expected.Length));
                Assert.That(actual.Length, Is.EqualTo(6));
                for (int i = 0; i < actual.Length; i++)
                {
                    Assert.That(actual[i].ContentHash, Is.EqualTo(expected[i].ContentHash), "Map " + i);
                    Assert.That(actual[i].Map.BuildSlots.Length, Is.EqualTo(i<3?32:1));
                    Assert.That(actual[i].Map.MineSlots, Is.Empty);
                    Assert.That(actual[i].Map.Width, Is.EqualTo(i<3?1600:2400));
                    Assert.That(actual[i].Map.Height, Is.EqualTo(i<3?960:1440));
                    Assert.That(actual[i].Map.FreePlacement, Is.True);
                    Assert.That(actual[i].Map.BlockedAreas.Length, Is.GreaterThan(0));
                    Assert.That(actual[i].MaxSpawnOrdersPerPlayer, Is.EqualTo(80));
                    Assert.That(actual[i].Towers[0].MaxPerPlayer, Is.EqualTo(32));
                    Assert.That(actual[i].Units[0].KillBounty, Is.GreaterThan(0));
                    Assert.That(actual[i].PassiveGoldPerSecond, Is.EqualTo(4));
                    Assert.That(actual[i].WaveCount, Is.EqualTo(10));
                    Assert.That(actual[i].Towers.Length, Is.EqualTo(8));
                    Assert.That(actual[i].Towers[4].Upgrades[0].IncomeUnitsPerSecond, Is.EqualTo(1440));
                    Assert.That(actual[i].Towers[5].Mechanic, Is.EqualTo(WeaponMechanic.Chain));
                    Assert.That(actual[i].Towers[6].Mechanic, Is.EqualTo(WeaponMechanic.PierceLine));
                    Assert.That(actual[i].Towers[7].Mechanic, Is.EqualTo(WeaponMechanic.Beam));
                    Assert.That(actual[i].Towers[7].Price, Is.EqualTo(180));
                    Assert.That(actual[i].Towers[7].FireIntervalTicks, Is.EqualTo(8));
                    foreach (var tower in actual[i].Towers)
                    {
                        if (tower.IsEconomy) continue;
                        Assert.That(tower.Upgrades[0].Range, Is.EqualTo((tower.Range * 120 + 99) / 100));
                        Assert.That(tower.Upgrades[1].Range, Is.EqualTo((tower.Range * 140 + 99) / 100));
                        foreach (var upgrade in tower.Upgrades)
                            Assert.That(upgrade.FireIntervalTicks, Is.EqualTo(tower.FireIntervalTicks));
                    }
                }
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(restored); }
        }

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void EarlierSerializedPrototypeMigratesOnceToCurrentCatalogue(int oldSchema)
        {
            var content = ScriptableObject.CreateInstance<GameContentAsset>();
            try
            {
                content.SetEditorDefaults();
                var serialized = new SerializedObject(content);
                serialized.FindProperty("schemaVersion").intValue = oldSchema;
                serialized.FindProperty("towers").arraySize = 7;
                serialized.FindProperty("maps").arraySize = 3;
                serialized.FindProperty("rules.MaxSpawnOrdersPerPlayer").intValue = 8;
                serialized.FindProperty("maps").GetArrayElementAtIndex(0).FindPropertyRelative("Width").intValue = 0;
                var oldMineSites = serialized.FindProperty("maps").GetArrayElementAtIndex(0).FindPropertyRelative("MineSlots");
                oldMineSites.arraySize = 2;
                oldMineSites.GetArrayElementAtIndex(0).intValue = 0;
                oldMineSites.GetArrayElementAtIndex(1).intValue = 29;
                serialized.FindProperty("towers").GetArrayElementAtIndex(0).FindPropertyRelative("Upgrades").GetArrayElementAtIndex(0).FindPropertyRelative("FireIntervalTicks").intValue = 6;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(content.MigratePrototypeInEditor(), Is.True);
                Assert.That(content.MigratePrototypeInEditor(), Is.False);
                var expected = MapCatalog.CreateContents(GameContent.CreateDefault());
                var actual = content.CreateCatalog();
                Assert.That(new SerializedObject(content).FindProperty("schemaVersion").intValue, Is.EqualTo(11));
                for (int i = 0; i < expected.Length; i++) Assert.That(actual[i].ContentHash, Is.EqualTo(expected[i].ContentHash));
            }
            finally { Object.DestroyImmediate(content); }
        }

        [Test]
        public void SchemaNineBalanceMigrationPreservesOtherAuthoredContent()
        {
            var asset = ScriptableObject.CreateInstance<GameContentAsset>();
            try
            {
                asset.SetEditorDefaults();
                var data = new SerializedObject(asset);
                data.FindProperty("schemaVersion").intValue = 9;
                data.FindProperty("units").arraySize=4;data.FindProperty("research").arraySize=4;
                data.FindProperty("rules.SpawnIntervalTicks").intValue = 4;
                int[] old = {12,20,30,24};
                for (int i = 0; i < old.Length; i++) data.FindProperty("units").GetArrayElementAtIndex(i).FindPropertyRelative("CooldownTicks").intValue = old[i];
                var laser = data.FindProperty("towers").GetArrayElementAtIndex(7);
                laser.FindPropertyRelative("FireIntervalTicks").intValue = 5;
                for (int i = 0; i < 2; i++) laser.FindPropertyRelative("Upgrades").GetArrayElementAtIndex(i).FindPropertyRelative("FireIntervalTicks").intValue = 5;
                data.FindProperty("towers").GetArrayElementAtIndex(0).FindPropertyRelative("Price").intValue = 123;
                data.FindProperty("waves").GetArrayElementAtIndex(0).FindPropertyRelative("HpPercent").intValue = 111;
                data.FindProperty("maps").GetArrayElementAtIndex(0).FindPropertyRelative("Name").stringValue = "Authored map";
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(asset.MigratePrototypeInEditor(), Is.True);
                Assert.That(asset.MigratePrototypeInEditor(), Is.False);
                var content = asset.CreateContent();
                Assert.That(content.Rules.SpawnIntervalTicks, Is.EqualTo(2));
                int[] expected = {6,8,12,10};
                for (int i = 0; i < expected.Length; i++) Assert.That(content.Units[i].CooldownTicks, Is.EqualTo(expected[i]));
                Assert.That(content.Towers[7].FireIntervalTicks, Is.EqualTo(8));
                foreach (var upgrade in content.Towers[7].Upgrades) Assert.That(upgrade.FireIntervalTicks, Is.EqualTo(8));
                Assert.That(content.Towers[0].Price, Is.EqualTo(123));
                Assert.That(content.Waves[0].HpPercent, Is.EqualTo(111));
                Assert.That(content.Map.Name, Is.EqualTo("Authored map"));
                Assert.That(content.WaveRules.NeutralSpawnIntervalTicks, Is.EqualTo(10));
            }
            finally { Object.DestroyImmediate(asset); }
        }

        [Test]
        public void SerializedWaveResearchAndFractionalIncomeAreDataNotHiddenDefaults()
        {
            var source=ScriptableObject.CreateInstance<GameContentAsset>();
            var restored=ScriptableObject.CreateInstance<GameContentAsset>();
            try
            {
                source.SetEditorDefaults();
                var serialized=new SerializedObject(source);
                serialized.FindProperty("rules.MineSellRefundPercent").intValue=37;
                serialized.FindProperty("waves").GetArrayElementAtIndex(0).FindPropertyRelative("HpPercent").intValue=111;
                serialized.FindProperty("research").GetArrayElementAtIndex(0).FindPropertyRelative("HpPerLevel").intValue=5;
                serialized.FindProperty("towers").GetArrayElementAtIndex(4).FindPropertyRelative("Upgrades").GetArrayElementAtIndex(0).FindPropertyRelative("IncomeUnitsPerSecond").intValue=1520;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source),restored);
                var actual=restored.CreateContent();
                Assert.That(actual.Rules.MineSellRefundPercent,Is.EqualTo(37));
                Assert.That(actual.Waves[0].HpPercent,Is.EqualTo(111));
                Assert.That(actual.Research[0].HpPerLevel,Is.EqualTo(5));
                Assert.That(actual.Towers[4].Upgrades[0].IncomeUnitsPerSecond,Is.EqualTo(1520));
                Assert.That(actual.ContentHash,Is.EqualTo(source.CreateContent().ContentHash));
                Assert.That(actual.ContentHash,Is.Not.EqualTo(GameContent.CreateDefault().ContentHash));
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(restored); }
        }

        [Test]
        public void BootCatalogueMatchesCurrentCoreDefaults()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContentAsset>("Assets/Content/Standard.asset");
            Assert.That(content, Is.Not.Null);
            var actual = content.CreateCatalog();
            var expected = MapCatalog.CreateContents(GameContent.CreateDefault());
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++) Assert.That(actual[i].ContentHash, Is.EqualTo(expected[i].ContentHash), "Serialized map " + i + " was not migrated by Prepare.");
        }
    }
}
