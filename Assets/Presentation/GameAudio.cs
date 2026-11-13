using System;
using UnityEngine;

namespace DefenseDuell.Presentation
{
    /// <summary>Original prebaked PCM, bounded voices and focus-safe presentation only.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        public const int VoiceCapacity=4;
        public const float EffectHeadroom=.20f,MusicHeadroom=.28f;
        readonly AudioSource[] voices=new AudioSource[VoiceCapacity];
        readonly double[] busyUntil=new double[VoiceCapacity];
        readonly AudioClip[] cues=new AudioClip[10];
        readonly Audio09CueGate gate=new Audio09CueGate();
        AudioSource music;
        AudioClip generatedMusic;
        LocalSettings settings;
        bool initialized,focused=true,paused,subscribed,musicPaused,musicStarted;
        public int ActiveVoiceCount
        {
            get {int count=0;double now=Time.realtimeSinceStartupAsDouble;
                for(int i=0;i<VoiceCapacity;i++)if(busyUntil[i]>now)count++;return count;}
        }
        public void Initialize(LocalSettings value)
        {
            if(value==null)throw new ArgumentNullException(nameof(value));
            ReleaseClips();settings=value;gate.Reset();
            if(music==null){music=CreateSource();music.loop=true;music.priority=192;}
            for(int i=0;i<VoiceCapacity;i++)if(voices[i]==null){voices[i]=CreateSource();voices[i].priority=96;}
            generatedMusic=MakeClip(Audio09Synth.Music());music.clip=generatedMusic;
            for(int i=0;i<cues.Length;i++)cues[i]=MakeClip(Audio09Synth.Effect((GameAudioCue)i));
            focused=Application.isFocused;initialized=true;Subscribe();
            music.volume=0;musicPaused=false;musicStarted=false;ReconcileMusic();
        }
        AudioSource CreateSource()
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
            source.spatialBlend=0;source.dopplerLevel=0;source.pitch=1;source.volume=0;
            source.bypassReverbZones=true;source.ignoreListenerPause=false;return source;
        }
        static AudioClip MakeClip(Audio09Pcm pcm)
        {
            var clip=AudioClip.Create(pcm.Name,pcm.Frames,pcm.Channels,pcm.Rate,false);
            if(!clip.SetData(pcm.Samples,0)){Destroy(clip);throw new InvalidOperationException("Cannot upload original audio PCM.");}
            return clip;
        }
        bool Foreground=>initialized && isActiveAndEnabled && focused && !paused && !AudioListener.pause;
        static float Level(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp01(value);
        bool EffectsAllowed=>Foreground && settings!=null && !settings.Muted && Level(settings.Effects)>0;
        public void PlayIncomeIncrease()=>Play(GameAudioCue.Income);
        public void Play(GameAudioCue cue)
        {
            int index=(int)cue;if(index<0||index>=cues.Length)return;
            PlayBounded(cues[index],cue);
        }
        // Caller-owned clips are never destroyed or altered. Null selects the new UI cue.
        public void Play(AudioClip clip)
        {if(clip==null)Play(GameAudioCue.Ui);else PlayBounded(clip,GameAudioCue.Ui);}
        void PlayBounded(AudioClip clip,GameAudioCue cue)
        {
            if(!EffectsAllowed || clip==null)return;
            double now=Time.realtimeSinceStartupAsDouble;
            int limit=settings.PowerSaving?2:VoiceCapacity,slot=-1;
            // No voice stealing or queue: spam cannot cut sounding clips or play late.
            for(int i=0;i<limit;i++)if(now>=busyUntil[i]){slot=i;break;}
            if(slot<0 || !gate.TryAccept(cue,now))return;
            // Original PCM already has a sample-accurate attack. Waiting for Update to raise
            // gain would swallow up to 33 ms of a short cue at 30 FPS.
            var source=voices[slot];source.Stop();source.clip=clip;source.pitch=1;
            source.volume=Level(settings.Effects)*EffectHeadroom;
            busyUntil[slot]=now+Math.Max(.025,clip.length)+.025;source.Play();
        }
        void Update()
        {
            if(!initialized)return;
            if(!Foreground){Suspend();return;}
            double now=Time.realtimeSinceStartupAsDouble;
            float dt=Mathf.Min(.10f,Time.unscaledDeltaTime);ReconcileMusic();
            float musicTarget=settings.Muted?0:Level(settings.Music)*MusicHeadroom;
            music.volume=Smooth(music.volume,musicTarget,dt,.16f);
            if(musicTarget==0 && music.volume<.0001f && musicStarted&&!musicPaused)
            {music.volume=0;music.Pause();musicPaused=true;}
            float effectTarget=settings.Muted?0:Level(settings.Effects)*EffectHeadroom;
            int limit=settings.PowerSaving?2:VoiceCapacity;
            for(int i=0;i<VoiceCapacity;i++)
            {
                if(voices[i]==null)continue;
                float target=i<limit?effectTarget:0;voices[i].volume=Smooth(voices[i].volume,target,dt,.015f);
                if(now>=busyUntil[i] || (target==0 && voices[i].volume<.0001f))
                {voices[i].Stop();voices[i].clip=null;busyUntil[i]=0;voices[i].volume=0;}
            }
        }
        static float Smooth(float value,float target,float delta,float seconds)
        {return Mathf.Abs(value-target)<.00001f?target:Mathf.Lerp(value,target,1-Mathf.Exp(-delta/seconds));}
        void ReconcileMusic()
        {
            if(!Foreground || music==null || settings==null || settings.Muted || Level(settings.Music)<=0)return;
            if(musicPaused){music.UnPause();musicPaused=false;}
            else if(!musicStarted || !music.isPlaying){music.Play();musicStarted=true;}
        }
        void Suspend()
        {
            if(music!=null && musicStarted&&!musicPaused){music.Pause();musicPaused=true;}
            if(music!=null)music.volume=0;StopEffects();gate.Reset();
        }
        void StopEffects()
        {for(int i=0;i<VoiceCapacity;i++){if(voices[i]!=null){voices[i].Stop();voices[i].clip=null;voices[i].volume=0;}busyUntil[i]=0;}}
        void OnApplicationFocus(bool value){focused=value;if(!value)Suspend();else ReconcileMusic();}
        void OnApplicationPause(bool value){paused=value;if(value)Suspend();else ReconcileMusic();}
        void OnEnable(){if(initialized){Subscribe();ReconcileMusic();}}
        void OnDisable(){Unsubscribe();Suspend();}
        void Subscribe(){if(!subscribed && isActiveAndEnabled){AudioSettings.OnAudioConfigurationChanged+=AudioConfigurationChanged;subscribed=true;}}
        void Unsubscribe(){if(subscribed){AudioSettings.OnAudioConfigurationChanged-=AudioConfigurationChanged;subscribed=false;}}
        void AudioConfigurationChanged(bool deviceChanged)
        {
            if(!initialized)return;
            StopEffects();gate.Reset();music.Stop();music.volume=0;musicStarted=false;musicPaused=false;ReconcileMusic();
        }
        void ReleaseClips()
        {
            StopEffects();if(music!=null){music.Stop();music.clip=null;}
            if(generatedMusic!=null){Destroy(generatedMusic);generatedMusic=null;}
            for(int i=0;i<cues.Length;i++)if(cues[i]!=null){Destroy(cues[i]);cues[i]=null;}
        }
        void OnDestroy()
        {
            initialized=false;Unsubscribe();ReleaseClips();
            if(music!=null)Destroy(music);
            for(int i=0;i<VoiceCapacity;i++)if(voices[i]!=null)Destroy(voices[i]);
        }
    }
}
