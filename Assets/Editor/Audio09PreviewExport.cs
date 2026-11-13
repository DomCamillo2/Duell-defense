using System;
using System.IO;
using DefenseDuell.Presentation;
using UnityEditor;
using UnityEngine;

namespace DefenseDuell.Editor
{
    public static class Audio09PreviewExport
    {
        [MenuItem("Defense Duell/Audio 09/Export original WAV previews")]
        public static void Export()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Docs","ResearchEvidence","audio-v09","PreviewUnity"));
            Directory.CreateDirectory(root);
            foreach(GameAudioCue cue in Enum.GetValues(typeof(GameAudioCue)))
                Audio09WaveFile.Write(Path.Combine(root,cue.ToString().ToLowerInvariant()+".wav"),Audio09Synth.Effect(cue));
            Audio09WaveFile.Write(Path.Combine(root,"music-loop.wav"),Audio09Synth.Music());
            Debug.Log("AUDIO09_PREVIEW "+root);
        }
    }
}
