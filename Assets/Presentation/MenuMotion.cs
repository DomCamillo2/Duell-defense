using System;
using System.Collections.Generic;
using DefenseDuell.Core;
using UnityEngine;

namespace DefenseDuell.Presentation
{
    // Presentation only: never submits commands or advances a real match.
    public sealed class MenuMotion : MonoBehaviour
    {
        sealed class Item
        {
            public RectTransform Rect;public CanvasGroup Fade;public Vector2 Origin;
            public float Delay,Amplitude,Frequency;public bool Slide;
        }
        readonly List<Item> items=new List<Item>();
        LocalSettings settings;BoardView board;GameContent content;MatchState demo;Func<bool> blocked;
        float elapsed,clock,sample;float[] distance;bool applicationFocused=true,applicationPaused;
        public bool MotionAllowed=>settings!=null&&!settings.ReducedEffects&&!settings.PowerSaving&&applicationFocused&&!applicationPaused&&!(blocked?.Invoke()??false);
        public float AnimationTime=>clock;
        public void Initialize(LocalSettings config,BoardView view,GameContent data,MatchState state,Func<bool> isBlocked)
        {
            settings=config;board=view;content=data;demo=state;blocked=isBlocked;
            distance=new float[demo.Units.Count];for(int i=0;i<distance.Length;i++)distance[i]=demo.Units[i].Distance;
            ApplyPolicy();
        }
        public void AddEntrance(RectTransform rect,float delay,bool slide)
        {
            var group=rect.gameObject.AddComponent<CanvasGroup>();
            items.Add(new Item{Rect=rect,Fade=group,Origin=rect.anchoredPosition,Delay=delay,Slide=slide});
            group.alpha=MotionAllowed?.25f:1;
        }
        public void AddFloat(RectTransform rect,float amplitude,float frequency)
        {items.Add(new Item{Rect=rect,Origin=rect.anchoredPosition,Amplitude=amplitude,Frequency=frequency});}
        public void ApplyPolicy()
        {
            if(board!=null){board.ReducedEffects=!MotionAllowed;board.PowerSaving=settings.PowerSaving;}
            if(!MotionAllowed){elapsed=Mathf.Max(elapsed,1);Rest();}
        }
        void Update()
        {
            ApplyPolicy();if(!MotionAllowed)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);elapsed+=dt;clock+=dt;
            foreach(var item in items)
            {
                if(item.Rect==null)continue;
                float t=Mathf.Clamp01((elapsed-item.Delay)/.42f);float eased=1-Mathf.Pow(1-t,3);
                if(item.Fade!=null)item.Fade.alpha=Mathf.Lerp(.25f,1,eased);
                if(item.Slide)item.Rect.anchoredPosition=item.Origin+Vector2.down*(1-eased)*14;
                if(item.Amplitude>0)item.Rect.anchoredPosition=item.Origin+Vector2.up*Mathf.Sin(clock*item.Frequency)*item.Amplitude;
            }
            sample+=dt;if(sample<.05f||board==null)return;
            bool wrapped=false;
            for(int i=0;i<demo.Units.Count;i++)
            {
                distance[i]+=content.Units[demo.Units[i].Kind].SpeedPerSecond*sample*.45f;
                if(distance[i]>=content.Map.Length){distance[i]%=content.Map.Length;wrapped=true;}
                demo.Units[i].Distance=(int)distance[i];
            }
            // Reset the preview epoch at a loop boundary; never interpolate across the whole map.
            if(wrapped)demo.SessionId++;
            demo.Tick++;board.Display(demo,PlayerSlot.Zero);sample=0;
        }
        void Rest()
        {foreach(var item in items){if(item.Rect!=null)item.Rect.anchoredPosition=item.Origin;if(item.Fade!=null)item.Fade.alpha=1;}}
        void OnApplicationFocus(bool value){applicationFocused=value;ApplyPolicy();}
        void OnApplicationPause(bool value){applicationPaused=value;ApplyPolicy();}
        void OnDisable(){Rest();if(board!=null)board.ReducedEffects=true;}
        void OnDestroy(){blocked=null;board=null;demo=null;items.Clear();}
    }
}
