using System;
using NUnit.Framework;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class SnapshotMotionTests
    {
        [Test] public void JitteredFiveHzSnapshotsRemainContinuousWithoutPrediction()
        {
            var clock=new SnapshotPlaybackClock();var path=new RouteMotionSamples();
            double last=0;int stopped=0,frames=0,next=0;
            for(int frame=0;frame<360;frame++)
            {
                double now=frame/60d;
                if(now>=next*.2+(next%2==0?0:.025))
                {
                    clock.Observe(next*4,20,now);path.Add(next*.2,next*20);next++;
                }
                double position=path.Sample(clock.Advance(now));
                Assert.That(position,Is.LessThanOrEqualTo((next-1)*20));
                if(now>1){Assert.That(position,Is.GreaterThanOrEqualTo(last));Assert.That(position-last,Is.LessThan(2.1));if(position-last<.01)stopped++;frames++;}
                last=position;
            }
            Assert.That(stopped,Is.LessThan(frames/20),"Snapshot cadence must not cause repeated stops");
            double final=path.Sample(clock.Advance(20));
            Assert.That(path.Sample(clock.Advance(21)),Is.EqualTo(final));
            Assert.That(clock.Observe(2000,20,22),Is.True,"Long suspension restarts history");
        }
        [Test] public void HistoryRejectsReorderingAndBoundsMemoryAndResetsTeleports()
        {
            var path=new RouteMotionSamples();
            for(int i=0;i<1000;i++)path.Add(i*.2,i*10);
            Assert.That(path.Count,Is.EqualTo(16));
            path.Add(0,50000);Assert.That(path.Sample(1000),Is.EqualTo(9990));
            path.Add(200,1);Assert.That(path.Count,Is.EqualTo(1));Assert.That(path.Sample(1000),Is.EqualTo(1));
            path.Reset();Assert.That(path.Count,Is.Zero);
            var clock=new SnapshotPlaybackClock();clock.Observe(100,20,0);Assert.That(clock.Observe(100,20,.1),Is.False);
            Assert.That(clock.Observe(1,20,.2),Is.True);clock.Reset();Assert.That(clock.Observe(1,20,.3),Is.True);
        }
    }
}
