using NUnit.Framework;
namespace DefenseDuell.Networking.Tests
{
    public sealed class DriverShutdown15Tests
    {
        [Test] public void PreservedFinalResultEndsGuestOnlyAfterBothCompletedRoundsAndZeroPeers()
        {
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.Finished,0,false,true,true,true),Is.True);
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(true,ConnectionState.Finished,0,false,true,true,true),Is.False,"host cannot count its own shutdown as observed remote shutdown");
            foreach(int peers in new[]{1,2,4,6})Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.Finished,peers,false,true,true,true),Is.False,"a displayed result with connected peers is not shutdown");
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.Finished,0,false,true,true,false),Is.False,"first round or incomplete second round");
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.Finished,0,false,false,true,true),Is.False,"required actions missing");
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.Finished,0,true,true,false,false),Is.False,"startup smoke cannot claim completed results");
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.Finished,0,false,true,false,true),Is.False,"legacy short scenario cannot bypass two-round contract");
        }
        [Test] public void StartedSmokeUsesConnectionLostOnlyAndRequiresCompletedScenario()
        {
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.ConnectionLost,0,true,true,false,false),Is.True);
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.ConnectionLost,0,true,false,false,false),Is.False);
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.ConnectionLost,0,false,true,true,false),Is.False);
            Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,ConnectionState.ConnectionLost,0,false,true,true,true),Is.True);
            foreach(var status in new[]{ConnectionState.Idle,ConnectionState.Connecting,ConnectionState.Lobby,ConnectionState.Loading,ConnectionState.Playing})
                Assert.That(NetworkTestDriver.ExpectedGuestShutdown(false,status,0,false,true,true,true),Is.False);
        }
    }
}
