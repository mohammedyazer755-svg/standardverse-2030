using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CampusPulse
{
    // Opt-in player smoke test. Normal players never run this coroutine.
    public sealed class RuntimeEvidence : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--campus-smoke") >= 0)
                new GameObject("Runtime evidence").AddComponent<RuntimeEvidence>();
        }
        [Serializable] public sealed class Report
        {
            public string graphicsDevice, processor, unityVersion, timestamp;
            public int width, height, facilities, frames, visualWalkers;
            public float medianMS, p95MS, seconds;
            public bool passed;
            public string error;
        }
        IEnumerator Start()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Evidence")); Directory.CreateDirectory(folder);
            var report = new Report { graphicsDevice=SystemInfo.graphicsDeviceName, processor=SystemInfo.processorType,
                unityVersion=Application.unityVersion, timestamp=DateTime.UtcNow.ToString("O") };
            Application.logMessageReceived += (message, stack, type) =>
            { if (type == LogType.Error || type == LogType.Exception) report.error += message + "\n"; };
            yield return new WaitForSeconds(2);
            var s = FindFirstObjectByType<CampusSession>();
            if (!s) { report.error = "Session missing"; Save(folder,report); yield break; }
            s.Begin(); report.facilities = s.Facilities.Length;
            foreach (var f in s.Facilities)
            {
                s.Select(f); s.FocusSelected(); yield return new WaitForSeconds(.8f);
                var pixel = s.WorldCamera.WorldToScreenPoint(f.transform.position + new Vector3(0,4,0));
                if (s.Pick(pixel,false) != f) report.error += "Ray selection failed: " + f.Id + "\n";
                if (s.Pick(pixel,true) != null) report.error += "UI block failed\n";
            }
            s.TogglePause(); var minute = s.State.Minute; yield return new WaitForSeconds(1.2f);
            if (minute != s.State.Minute) report.error += "Pause failed\n";
            s.Restart(); s.Overview(); yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"overview.png"));
            s.Select(s.Facilities[0]); s.FocusSelected(); yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"academic-block.png"));
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var times = new List<float>(); float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 12) { yield return null; times.Add(Time.unscaledDeltaTime * 1000); }
            report.seconds = Time.realtimeSinceStartup-start; times.Sort(); report.frames=times.Count;
            report.width = Screen.width; report.height = Screen.height;
            var life = FindFirstObjectByType<CampusLife>(); report.visualWalkers = life ? life.ActiveWalkers : 0;
            report.medianMS=times[times.Count/2]; report.p95MS=times[Mathf.Min(times.Count-1,(int)(times.Count*.95f))];
            report.passed=string.IsNullOrEmpty(report.error); Save(folder,report);
            Application.Quit(report.passed ? 0 : 1);
        }
        static void Save(string folder, Report report) { File.WriteAllText(Path.Combine(folder,"runtime-report.json"),JsonUtility.ToJson(report,true)); }
    }
}
