using System.Collections.Generic;
using NUnit.Framework;
namespace DefenseDuell.Tests
{
    public sealed class EconomyFlowEditModeTests
    {
        public static IEnumerable<TestCaseData> Scenarios()
        {foreach(var c in EconomyFlowScenarios.All())yield return new TestCaseData(c.Key).SetName(c.Key);}
        [TestCaseSource(nameof(Scenarios))]
        public void ActiveEconomyRules(string name)
        {foreach(var c in EconomyFlowScenarios.All())if(c.Key==name){c.Value();return;}Assert.Fail("Unknown scenario: "+name);}
    }
}
