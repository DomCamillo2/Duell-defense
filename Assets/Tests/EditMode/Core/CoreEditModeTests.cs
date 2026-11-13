using System.Collections.Generic;
using NUnit.Framework;

namespace DefenseDuell.Tests
{
    public sealed class CoreEditModeTests
    {
        public static IEnumerable<TestCaseData> Scenarios()
        {
            foreach (var scenario in CoreScenarios.All()) yield return new TestCaseData(scenario.Key).SetName(scenario.Key);
        }
        [TestCaseSource(nameof(Scenarios))]
        public void AuthoritativeRules(string scenarioName)
        {
            foreach (var scenario in CoreScenarios.All())
                if (scenario.Key == scenarioName) { scenario.Value(); return; }
            Assert.Fail("Unknown scenario: " + scenarioName);
        }
    }
}
