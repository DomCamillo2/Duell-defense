using System;
using System.Globalization;
using System.IO;
using DefenseDuell.Networking;
using Unity.Profiling;
using UnityEngine;

namespace DefenseDuell.Platform
{
    /// <summary>Opt-in local CSV evidence, enabled with --duel-performance or PlayerPrefs duel.performance=1.</summary>
    public sealed class PerformanceRecorder : MonoBehaviour
    {
        readonly float[] frameTimes = new float[3600];
        SessionCoordinator session;
        StreamWriter output;
        ProfilerRecorder allocations;
        int count;
        long maxGc;
        double windowStart;
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public void Initialize(SessionCoordinator coordinator)
        {
            session = coordinator;
            bool requested = PlayerPrefs.GetInt("duel.performance", 0) == 1;
            foreach (var arg in Environment.GetCommandLineArgs()) if (arg == "--duel-performance") requested = true;
            if (!requested) { enabled = false; return; }
            string path = Path.Combine(Application.persistentDataPath, "performance-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".csv");
            output = new StreamWriter(path);
            output.WriteLine("# model=" + SystemInfo.deviceModel + "; os=" + SystemInfo.operatingSystem + "; unity=" + Application.unityVersion + "; game=" + Application.version);
            output.WriteLine("# graphics=" + SystemInfo.graphicsDeviceName + "; content=" + session.Content.ContentHash);
            output.WriteLine("elapsed_s,frames,mean_ms,p95_ms,p99_ms,max_ms,max_gc_bytes,managed_bytes,units,towers,sent_bytes,received_bytes,target_fps");
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            windowStart = Time.realtimeSinceStartupAsDouble;
            Debug.Log("DEFENSE_PERFORMANCE_CSV " + path);
        }

        void Update()
        {
            if (output == null) return;
            if (count < frameTimes.Length) frameTimes[count++] = Time.unscaledDeltaTime * 1000;
            if (allocations.Valid) maxGc = Math.Max(maxGc, allocations.LastValue);
            if (Time.realtimeSinceStartupAsDouble - windowStart >= 10 || count == frameTimes.Length) FlushWindow();
        }
        void FlushWindow()
        {
            if (count == 0 || output == null) return;
            double total = 0;
            for (int i = 0; i < count; i++) total += frameTimes[i];
            Array.Sort(frameTimes, 0, count);
            var state = session.State;
            output.WriteLine(string.Join(",", Time.realtimeSinceStartupAsDouble.ToString("F2", Invariant), count,
                (total / count).ToString("F2", Invariant), frameTimes[Math.Min(count-1,(int)(count*.95))].ToString("F2", Invariant),
                frameTimes[Math.Min(count-1,(int)(count*.99))].ToString("F2", Invariant), frameTimes[count-1].ToString("F2", Invariant),
                maxGc, GC.GetTotalMemory(false), state?.Units.Count ?? 0, state?.Towers.Count ?? 0, session.SentBytes, session.ReceivedBytes, Application.targetFrameRate));
            output.Flush(); count = 0; maxGc = 0; windowStart = Time.realtimeSinceStartupAsDouble;
        }
        void OnDestroy() { FlushWindow(); output?.Dispose(); if (allocations.Valid) allocations.Dispose(); }
    }
}
