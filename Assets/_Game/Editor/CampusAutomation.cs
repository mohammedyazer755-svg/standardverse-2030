using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace CampusPulse.Editor
{
    // Local, explicit command file; no sockets, reflection, or arbitrary code execution.
    [InitializeOnLoad]
    public static class CampusAutomation
    {
        static double next;
        static TestRunnerApi runner;
        static CampusAutomation()
        {
            EditorApplication.update += Poll;
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            var results = ScriptableObject.CreateInstance<Results>();
            runner.RegisterCallbacks(results);
        }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            next = EditorApplication.timeSinceStartup + 1;
            const string request = "Library/CampusAutomation/request.txt";
            if (!File.Exists(request)) return;
            var command = File.ReadAllText(request).Trim(); File.Delete(request);
            Directory.CreateDirectory("Logs/Campus");
            try
            {
                switch (command)
                {
                    case "status":
                        var scenes = new System.Text.StringBuilder();
                        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
                            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                            scenes.AppendLine(scene.path + " dirty=" + scene.isDirty);
                        }
                        File.WriteAllText("Logs/Campus/editor-status.txt", scenes.ToString()); break;
                    case "foundation": CampusBuilder.Foundation(); break;
                    case "refresh": AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); break;
                    case "benchmark": LivingCampusBuilder.Benchmark(); break;
                    case "campus": LivingCampusBuilder.FullCampus(); break;
                    case "build": CampusBuilder.Build(); break;
                    case "edit-tests": Tests(TestMode.EditMode); return;
                    case "play-tests": Tests(TestMode.PlayMode); return;
                    default: throw new ArgumentException("Unknown campus command: " + command);
                }
                File.WriteAllText("Logs/Campus/automation-result.txt", command + ": OK");
            }
            catch (Exception ex) { File.WriteAllText("Logs/Campus/automation-result.txt", command + ": FAILED\n" + ex); Debug.LogException(ex); }
        }
        static void Tests(TestMode mode)
        {
            SessionState.SetString("CampusTestMode",mode.ToString());
            runner.Execute(new ExecutionSettings(new Filter { testMode = mode, assemblyNames = new[]{mode == TestMode.EditMode ? "Campus.EditTests" : "Campus.PlayTests"} }));
        }
        sealed class Results : ScriptableObject, ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                var mode = SessionState.GetString("CampusTestMode","Unknown");
                TestRunnerApi.SaveResultToFile(result, "Logs/Campus/" + mode + "-results.xml");
                File.WriteAllText("Logs/Campus/automation-result.txt", $"{mode}: {result.TestStatus}; pass {result.PassCount}; fail {result.FailCount}");
            }
        }
    }
}
