using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>
    /// Scene bootstrap: creates the persistent manager set and the UI root.
    /// The LumoMain scene only contains a camera + this component - everything
    /// else is built at runtime (fast transitions, robust persistence).
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        /// <summary>Dev-only: when _tools/autotest.txt holds a level id (or "qa"), a test driver runs.
        /// Batch 5 fix: player builds must never start the test drivers - this
        /// entry point is compiled out unless in the editor or a development
        /// build. Follow-up: move AutoTestDriver/QaTestDriver out of the
        /// runtime assembly (no .asmdef in the repo yet; they still ship in
        /// Assembly-CSharp) into an editor/test assembly.</summary>
        static void TryStartAutoTest()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            try
            {
                string flag = System.IO.Path.Combine(
                    System.IO.Directory.GetParent(Application.dataPath).FullName, "_tools", "autotest.txt");
                if (!System.IO.File.Exists(flag)) return;
                string content = System.IO.File.ReadAllText(flag).Trim();
                var go = new GameObject("AutoTestDriver");
                if (content == "qa")
                {
                    go.AddComponent<Lumo.Core.QaTestDriver>();
                }
                else if (content == "all")
                {
                    // "all": play levels 1..13 back to back through the real UI and
                    // restore the original save afterwards (Stage 1.1 full-run smoke test).
                    var drv = go.AddComponent<Lumo.Core.AutoTestDriver>();
                    drv.RunAllLevels = true;
                    drv.LevelId = 1;
                    Debug.Log("[AutoTest] auto-started for ALL 13 levels (flag file)");
                }
                else
                {
                    int levelId;
                    if (!int.TryParse(content, out levelId)) { Destroy(go); return; }
                    go.AddComponent<Lumo.Core.AutoTestDriver>().LevelId = levelId;
                    Debug.Log("[AutoTest] auto-started for level " + levelId + " (flag file)");
                }
                DontDestroyOnLoad(go);
            }
            catch (System.Exception e) { Debug.LogWarning("[AutoTest] flag check failed: " + e.Message); }
        }

        void Awake()
        {
            if (FindAnyObjectByType<UIManager>() != null) return; // already bootstrapped

            Application.runInBackground = true;   // keep the player loop alive without focus

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                DontDestroyOnLoad(es);
            }

            var managers = new GameObject("Managers");
            managers.AddComponent<SaveManager>();
            managers.AddComponent<SessionTimeManager>();
            managers.AddComponent<SessionTracker>(); // LXD Phase D1: session reflection snapshot
            managers.AddComponent<AudioManager>();
            managers.AddComponent<TimerManager>();
            managers.AddComponent<ScoreManager>();
            managers.AddComponent<LevelManager>();
            managers.AddComponent<HintManager>();
            managers.AddComponent<FeedbackManager>();
            managers.AddComponent<AdaptiveManager>();
            managers.AddComponent<GameManager>();
            DontDestroyOnLoad(managers);

            // content must be loaded BEFORE the UI builds (map screens read the bank)
            GameManager.Instance.LoadContent();

            var ui = new GameObject("UIRoot");
            ui.AddComponent<UIManager>();
            DontDestroyOnLoad(ui);

            Debug.Log("[GameRoot] bootstrapped - all managers ready");

            TryStartAutoTest();
        }
    }
}
