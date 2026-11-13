using DefenseDuell.Content;
using DefenseDuell.Networking;
using DefenseDuell.Presentation;
using DefenseDuell.Progression;
using UnityEngine;

namespace DefenseDuell.Platform
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public GameContentAsset Content;
        public SessionCoordinator Session;
        public GameUI Interface;

        void Start()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
            Session.Initialize(Content.CreateCatalog());
            // Explicit acceptance players carry their own test masks and never read or
            // award the user's persistent progression profile.
            if (!NetworkTestDriver.Requested)
            {
                var progression = gameObject.AddComponent<LocalProgress>();
                progression.Initialize(Session);
            }
            Interface.Initialize(Session);
            var performance = gameObject.AddComponent<PerformanceRecorder>();
            performance.Initialize(Session);
        }

        void OnApplicationQuit() { if (Session != null) Session.Shutdown(); }
    }
}
