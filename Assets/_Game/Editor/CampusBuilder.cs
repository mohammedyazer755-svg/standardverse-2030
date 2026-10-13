using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

namespace CampusPulse.Editor
{
    public static class CampusBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Campus.unity";
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        [MenuItem("Campus Pulse/Generate Foundation")]
        public static void Foundation()
        {
            BeginScene();
            Box("Campus ground", Vector3.zero, new Vector3(130, .3f, 100), Material("Lawn", new Color(.23f,.35f,.24f)));
            var list = new List<FacilityView>();
            list.Add(FoundationFacility("academic", "Academic block", new Vector3(-24, 0, 8)));
            list.Add(FoundationFacility("library", "Library", new Vector3(12, 0, 15)));
            list.Add(FoundationFacility("canteen", "Canteen", new Vector3(18, 0, -20)));
            Complete(list, 110);
        }
        static FacilityView FoundationFacility(string id, string name, Vector3 position)
        {
            var root = new GameObject(name); root.transform.position = position;
            var view = root.AddComponent<FacilityView>(); view.Id = id; view.DisplayName = name;
            view.Purpose = "Foundation facility. Initial illustrative resource profile.";
            Box("Temporary block", position + new Vector3(0, 6, 0), new Vector3(22, 12, 16), Material("Foundation stone", new Color(.68f,.72f,.71f)), root.transform);
            var ring = Box("Selection", position + new Vector3(0,.23f,0), new Vector3(24,.1f,18), Material("Selection cyan", new Color(.05f,.8f,.95f)), root.transform);
            UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());
            view.Highlight = ring.GetComponent<Renderer>(); ring.SetActive(false);
            return view;
        }
        public static void BeginScene()
        {
            for (int i=0;i<SceneManager.sceneCount;i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your dirty scene before running Campus generation.");
            Directory.CreateDirectory("Assets/_Game/Scenes"); Directory.CreateDirectory("Assets/_Game/Art/Materials");
            materials.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.61f,.72f,.82f);
            RenderSettings.ambientEquatorColor = new Color(.45f,.5f,.48f);
            RenderSettings.ambientGroundColor = new Color(.24f,.28f,.25f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.69f,.79f,.85f); RenderSettings.fogDensity = .0014f;
            var sun = new GameObject("Late morning sun").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(48,-35,0); sun.color = new Color(1,.93f,.8f);
            sun.intensity = 2.1f; sun.shadows = LightShadows.Soft; RenderSettings.sun = sun;
            var volume = new GameObject("Campus atmosphere").AddComponent<Volume>(); volume.isGlobal = true;
            var profilePath = "Assets/_Game/Art/CampusVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, profilePath);
                var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES); AssetDatabase.AddObjectToAsset(tone, profile);
                var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(.16f); bloom.threshold.Override(1.3f); AssetDatabase.AddObjectToAsset(bloom, profile);
                var adjust = profile.Add<ColorAdjustments>(true); adjust.postExposure.Override(.25f); adjust.contrast.Override(8); adjust.saturation.Override(-5); AssetDatabase.AddObjectToAsset(adjust, profile);
            }
            volume.sharedProfile = profile;
        }
        public static void Complete(List<FacilityView> facilities, float distance)
        {
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CinemachineBrain)); cam.tag = "MainCamera";
            var camera = cam.GetComponent<Camera>(); camera.nearClipPlane = .3f; camera.farClipPlane = 650;
            camera.backgroundColor = new Color(.63f,.76f,.84f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var virtualCamera = new GameObject("Strategic Cinemachine Camera").AddComponent<CinemachineCamera>();
            virtualCamera.Lens.FieldOfView = 45;
            var rig = new GameObject("Strategic camera rig").AddComponent<StrategicCamera>();
            rig.VirtualCamera = virtualCamera; rig.OverviewDistance = distance;
            var rotation = Quaternion.Euler(48,-28,0); virtualCamera.transform.SetPositionAndRotation(-(rotation * Vector3.forward) * distance, rotation);
            cam.transform.SetPositionAndRotation(virtualCamera.transform.position, rotation);
            var session = new GameObject("Campus session").AddComponent<CampusSession>();
            session.Facilities = facilities.ToArray(); session.WorldCamera = camera; session.Rig = rig;
            var hud = session.gameObject.AddComponent<CampusHud>(); hud.Session = session; session.Hud = hud;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(ScenePath, true)};
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("CAMPUS_GENERATED " + facilities.Count + " facilities");
        }
        public static Material Material(string name, Color color, float metallic = 0, float smoothness = .35f)
        {
            if (materials.TryGetValue(name, out var result)) return result;
            string path = "Assets/_Game/Art/Materials/" + name + ".mat";
            result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!result) { result = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(result, path); }
            result.SetColor("_BaseColor", color); result.SetFloat("_Metallic", metallic); result.SetFloat("_Smoothness", smoothness);
            if (name.Contains("glow") || name.Contains("cyan")) { result.EnableKeyword("_EMISSION"); result.SetColor("_EmissionColor", color * .6f); }
            materials[name] = result; EditorUtility.SetDirty(result); return result;
        }
        public static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, true); go.transform.position = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        [MenuItem("Campus Pulse/Build Windows")]
        public static void Build()
        {
            Directory.CreateDirectory("builds/Windows"); Directory.CreateDirectory("Logs/Campus");
            PlayerSettings.companyName = "Campus Pulse"; PlayerSettings.productName = "Campus Pulse 2030";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[]{ScenePath},
                locationPathName = "builds/Windows/CampusPulse2030.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            var summary = $"{report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nDuration: {report.summary.totalTime}\nBytes: {report.summary.totalSize}";
            File.WriteAllText("Logs/Campus/build-result.txt", summary);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception(summary);
            Debug.Log("CAMPUS_BUILD_SUCCEEDED " + summary);
        }
    }
}
