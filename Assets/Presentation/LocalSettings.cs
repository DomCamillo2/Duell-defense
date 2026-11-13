using UnityEngine;

namespace DefenseDuell.Presentation
{
    public sealed class LocalSettings
    {
        public float Music { get; private set; }
        public float Effects { get; private set; }
        public bool ReducedEffects { get; private set; }
        public bool PowerSaving { get; private set; }
        public bool Muted { get; private set; }
        public LocalSettings()
        {
            Music=PlayerPrefs.GetFloat("duell.music",.22f); Effects=PlayerPrefs.GetFloat("duell.effects",.6f);
            ReducedEffects=PlayerPrefs.GetInt("duell.reduced",0)==1; PowerSaving=PlayerPrefs.GetInt("duell.power",0)==1;
            Muted=PlayerPrefs.GetInt("duell.muted",0)==1; Apply();
        }
        public void SetMusic(float value) { Music=Mathf.Clamp01(value);PlayerPrefs.SetFloat("duell.music",Music);Save(); }
        public void SetEffects(float value) { Effects=Mathf.Clamp01(value);PlayerPrefs.SetFloat("duell.effects",Effects);Save(); }
        public void SetReduced(bool value) { ReducedEffects=value;PlayerPrefs.SetInt("duell.reduced",value?1:0);Save(); }
        public void SetPower(bool value) { PowerSaving=value;PlayerPrefs.SetInt("duell.power",value?1:0);Apply();Save(); }
        public void SetMuted(bool value) { Muted=value;PlayerPrefs.SetInt("duell.muted",value?1:0);Save(); }
        static void Save() => PlayerPrefs.Save();
        void Apply() { Application.targetFrameRate=PowerSaving?30:60;QualitySettings.vSyncCount=0; }
    }

}
