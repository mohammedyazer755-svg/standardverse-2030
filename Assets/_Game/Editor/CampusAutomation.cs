using System;
using System.IO;
using UnityEditor;
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
            runner.RegisterCallbacks(new Results());
        }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            next = EditorApplication.timeSinceStartup + 1;
            const string request = "Library/CampusAutomation/request.txt";
            if (!File.Exists(request)) return;
            var command = File.ReadAllText(request).Trim(); File.Delete(request);
            Directory.CreateDirectory("Logs/Campus");
            try
            {
                switch (command)
                {
                    case "foundation": CampusBuilder.Foundation(); break;
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
        sealed class Results : ICallbacks
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
