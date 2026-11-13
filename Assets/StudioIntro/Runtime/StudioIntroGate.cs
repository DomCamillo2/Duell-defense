using System;

namespace DefenseDuell.Intro
{
    public enum IntroExitReason { None, Completed, Skipped, MissingClip, PrepareTimeout, PlaybackTimeout, VideoError, Automation }

    // Independent from Unity/video callbacks so duplicate endings and skip timing are testable.
    public sealed class StudioIntroGate
    {
        readonly double skipDelay, prepareTimeout, stallTimeout, maximumPlayback;
        public bool HasFrame { get; private set; }
        public double VisibleSeconds { get; private set; }
        public IntroExitReason ExitReason { get; private set; }
        public bool WantsExit => ExitReason != IntroExitReason.None;
        double startup, idle;
        public StudioIntroGate(double skipDelay = 1.5, double prepareTimeout = 8, double stallTimeout = 5, double maximumPlayback = 15)
        {
            this.skipDelay = Math.Max(0, skipDelay); this.prepareTimeout = Math.Max(1, prepareTimeout);
            this.stallTimeout = Math.Max(1, stallTimeout); this.maximumPlayback = Math.Max(1, maximumPlayback);
        }
        public void FrameDecoded() { if (WantsExit) return; HasFrame = true; idle = 0; }
        public void Advance(double seconds, bool paused)
        {
            if (paused || WantsExit || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            if (!HasFrame) { startup += seconds; if (startup >= prepareTimeout) Exit(IntroExitReason.PrepareTimeout); }
            else
            {
                VisibleSeconds += seconds; idle += seconds;
                if (idle >= stallTimeout || VisibleSeconds >= maximumPlayback) Exit(IntroExitReason.PlaybackTimeout);
            }
        }
        public bool CanSkip(bool enabled) => enabled && HasFrame && !WantsExit && VisibleSeconds >= skipDelay;
        public bool TrySkip(bool enabled) => CanSkip(enabled) && Exit(IntroExitReason.Skipped);
        public bool Exit(IntroExitReason reason)
        {
            if (reason == IntroExitReason.None || WantsExit) return false;
            ExitReason = reason; return true;
        }
        public static bool IsAutomationArgument(string[] arguments)
        {
            if (arguments == null) return false;
            foreach (string arg in arguments)
                if (arg == "--duel-skip-intro" || arg == "--duel-host" || arg == "--duel-client" || arg == "--duel-relay-host" || arg == "--duel-relay-client") return true;
            return false;
        }
    }
}
