using System;
using System.Collections;
using System.Linq;
using DefenseDuell.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DefenseDuell.Tests
{
    public sealed class Audio09Tests
    {
        sealed class SettingsLease:IDisposable
        {
            public readonly LocalSettings Value;
            readonly float music,effects;readonly bool muted,power;
            readonly int fps,vSync;readonly bool listenerPaused;
            public SettingsLease()
            {
                fps=Application.targetFrameRate;vSync=QualitySettings.vSyncCount;listenerPaused=AudioListener.pause;AudioListener.pause=false;
                Value=new LocalSettings();music=Value.Music;effects=Value.Effects;muted=Value.Muted;power=Value.PowerSaving;
                Value.SetMuted(false);Value.SetPower(false);Value.SetMusic(.22f);Value.SetEffects(.6f);
            }
            public void Dispose()
            {
                Value.SetMusic(music);Value.SetEffects(effects);Value.SetMuted(muted);Value.SetPower(power);
                Application.targetFrameRate=fps;QualitySettings.vSyncCount=vSync;AudioListener.pause=listenerPaused;
            }
        }
        static void Focus(GameAudio audio,bool value)=>audio.SendMessage("OnApplicationFocus",value);
        static void Pause(GameAudio audio,bool value)=>audio.SendMessage("OnApplicationPause",value);
        [TestCase(GameAudioCue.Ui)] [TestCase(GameAudioCue.Back)] [TestCase(GameAudioCue.Build)]
        [TestCase(GameAudioCue.Upgrade)] [TestCase(GameAudioCue.Sell)] [TestCase(GameAudioCue.Send)]
        [TestCase(GameAudioCue.Income)] [TestCase(GameAudioCue.Error)] [TestCase(GameAudioCue.Unlock)] [TestCase(GameAudioCue.Research)]
        public void OriginalCueHasBoundedFinitePcmAndAnImmediateSoftAttack(GameAudioCue cue)
        {
            var pcm=Audio09Synth.Effect(cue);double peak=0,energy=0,step=0,firstEnergy=0;bool finite=true;
            for(int i=0;i<pcm.Samples.Length;i++)
            {
                float x=pcm.Samples[i];finite&=!float.IsNaN(x)&&!float.IsInfinity(x);
                peak=Math.Max(peak,Math.Abs(x));energy+=x*x;
                if(i>0)step=Math.Max(step,Math.Abs(x-pcm.Samples[i-1]));
                if(i<pcm.Rate*.05)firstEnergy+=x*x*.6*.6*GameAudio.EffectHeadroom*GameAudio.EffectHeadroom;
            }
            Assert.That(finite,Is.True);Assert.That(peak,Is.InRange(.1,.851));Assert.That(energy,Is.GreaterThan(0));
            Assert.That(step,Is.LessThan(.35));Assert.That(pcm.Samples[0],Is.Zero);Assert.That(pcm.Samples[pcm.Samples.Length-1],Is.Zero);
            Assert.That(Math.Sqrt(firstEnergy/(pcm.Rate*.05)),Is.GreaterThan(.001));
            CollectionAssert.AreEqual(pcm.Samples,Audio09Synth.Effect(cue).Samples);
        }
        [Test]
        public void MusicIsTwentySecondStereoLoopWithContinuousSeamAndHeadroom()
        {
            var pcm=Audio09Synth.Music();Assert.That(pcm.Duration,Is.EqualTo(20));Assert.That(pcm.Channels,Is.EqualTo(2));
            double peak=0,mean=0;bool finite=true;
            foreach(float x in pcm.Samples){finite&=!float.IsNaN(x)&&!float.IsInfinity(x);peak=Math.Max(peak,Math.Abs(x));mean+=x;}
            Assert.That(finite,Is.True);Assert.That(peak,Is.LessThanOrEqualTo(.451));Assert.That(Math.Abs(mean/pcm.Samples.Length),Is.LessThan(.005));
            for(int c=0;c<2;c++)Assert.That(Math.Abs(pcm.Samples[c]-pcm.Samples[pcm.Samples.Length-2+c]),Is.LessThan(.025));
            Assert.That(GameAudio.VoiceCapacity*GameAudio.EffectHeadroom+.45*GameAudio.MusicHeadroom,Is.LessThan(1));
        }
        [Test]
        public void IncomeCooldownAndGlobalBucketDropSpamWithoutDeferredPlayback()
        {
            var gate=new Audio09CueGate();Assert.That(gate.TryAccept(GameAudioCue.Income,0),Is.True);
            for(int i=1;i<500;i++)Assert.That(gate.TryAccept(GameAudioCue.Income,i*.001),Is.False);
            Assert.That(gate.TryAccept(GameAudioCue.Income,.5),Is.True);
            Assert.That(gate.TryAccept(GameAudioCue.Ui,.1),Is.False);
            gate.Reset();int count=0;for(int i=0;i<1000;i++)if(gate.TryAccept((GameAudioCue)(i%10),i*.001))count++;
            Assert.That(count,Is.InRange(4,11));
        }
        [UnityTest]
        public IEnumerator PoolIsBoundedAndFirstCueHasGainBeforeAnyUpdateAtThirtyFps()
        {
            using(var settings=new SettingsLease())
            {
                var go=new GameObject("Audio09 pool");var audio=go.AddComponent<GameAudio>();
                try
                {
                    audio.Initialize(settings.Value);Focus(audio,true);Pause(audio,false);
                    Assert.That(go.GetComponents<AudioSource>().Length,Is.EqualTo(5));
                    for(int i=0;i<10;i++)audio.Play((GameAudioCue)i);
                    Assert.That(audio.ActiveVoiceCount,Is.EqualTo(4));
                    foreach(var source in go.GetComponents<AudioSource>().Where(s=>!s.loop && s.clip!=null))
                        Assert.That(source.volume,Is.EqualTo(.6f*GameAudio.EffectHeadroom).Within(.0001f));
                    yield return new WaitForSecondsRealtime(1.05f);
                    Assert.That(audio.ActiveVoiceCount,Is.Zero);
                    settings.Value.SetPower(true);
                    for(int i=0;i<10;i++)audio.Play((GameAudioCue)i);
                    Assert.That(audio.ActiveVoiceCount,Is.EqualTo(2));
                    settings.Value.SetMuted(true);audio.Play(GameAudioCue.Unlock);
                    yield return new WaitForSecondsRealtime(.35f);
                    Assert.That(audio.ActiveVoiceCount,Is.Zero);
                    audio.Play(GameAudioCue.Build);Assert.That(audio.ActiveVoiceCount,Is.Zero);
                }
                finally{Object.Destroy(go);}
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator FocusPauseDisableAndDeviceResetNeverReplayOldEffects()
        {
            using(var settings=new SettingsLease())
            {
                var go=new GameObject("Audio09 lifecycle");var audio=go.AddComponent<GameAudio>();
                try
                {
                    audio.Initialize(settings.Value);Focus(audio,true);audio.Play(GameAudioCue.Unlock);
                    Assert.That(audio.ActiveVoiceCount,Is.EqualTo(1));
                    Focus(audio,false);Assert.That(audio.ActiveVoiceCount,Is.Zero);
                    audio.Play(GameAudioCue.Build);Assert.That(audio.ActiveVoiceCount,Is.Zero);
                    Pause(audio,true);Focus(audio,true);audio.Play(GameAudioCue.Build);Assert.That(audio.ActiveVoiceCount,Is.Zero);
                    Pause(audio,false);Assert.That(audio.ActiveVoiceCount,Is.Zero);
                    audio.Play(GameAudioCue.Build);Assert.That(audio.ActiveVoiceCount,Is.EqualTo(1));
                    audio.enabled=false;Assert.That(audio.ActiveVoiceCount,Is.Zero);
                    audio.enabled=true;audio.Play(GameAudioCue.Send);Assert.That(audio.ActiveVoiceCount,Is.EqualTo(1));
                    audio.SendMessage("AudioConfigurationChanged",true);Assert.That(audio.ActiveVoiceCount,Is.Zero);
                    Assert.That(go.GetComponents<AudioSource>().Length,Is.EqualTo(5));
                    settings.Value.SetEffects(0);audio.PlayIncomeIncrease();Assert.That(audio.ActiveVoiceCount,Is.Zero);
                }
                finally{Object.Destroy(go);}
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator MusicMuteAndGainChangesFadeWithoutRegeneratingTheLoop()
        {
            using(var settings=new SettingsLease())
            {
                var go=new GameObject("Audio09 music settings");var audio=go.AddComponent<GameAudio>();
                try
                {
                    settings.Value.SetMusic(1);audio.Initialize(settings.Value);Focus(audio,true);
                    var music=go.GetComponents<AudioSource>().Single(s=>s.loop);var clip=music.clip;
                    yield return new WaitForSecondsRealtime(.4f);
                    Assert.That(music.volume,Is.GreaterThan(.10f));
                    settings.Value.SetMuted(true);
                    yield return new WaitForSecondsRealtime(1.6f);
                    Assert.That(music.volume,Is.LessThan(.0002f));
                    settings.Value.SetMusic(.4f);settings.Value.SetMuted(false);
                    yield return new WaitForSecondsRealtime(.4f);
                    Assert.That(music.volume,Is.GreaterThan(.05f));Assert.That(music.clip,Is.SameAs(clip));
                    Focus(audio,false);Assert.That(music.volume,Is.Zero);
                }
                finally{Object.Destroy(go);}
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator ReinitializeAndDestroyReleaseOwnedSourcesAndClipsButNotLegacyClip()
        {
            using(var settings=new SettingsLease())
            {
                var go=new GameObject("Audio09 ownership");var audio=go.AddComponent<GameAudio>();
                var foreign=AudioClip.Create("Caller owns this clip",3200,1,32000,false);
                try
                {
                    audio.Initialize(settings.Value);var oldMusic=go.GetComponents<AudioSource>().Single(s=>s.loop).clip;
                    audio.Initialize(settings.Value);Focus(audio,true);audio.Play(foreign);
                    Assert.That(audio.ActiveVoiceCount,Is.EqualTo(1));
                    yield return null;
                    Assert.That(oldMusic==null,Is.True);Assert.That(go.GetComponents<AudioSource>().Length,Is.EqualTo(5));
                    Object.Destroy(audio);yield return null;yield return null;
                    Assert.That(go.GetComponents<AudioSource>().Length,Is.Zero);Assert.That(foreign!=null,Is.True);
                }
                finally{Object.Destroy(foreign);Object.Destroy(go);}
            }
            yield return null;
        }
    }
}
