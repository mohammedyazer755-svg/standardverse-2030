using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CampusPulse
{
    /// <summary>
    /// Manages the cinematic opening sequence, letterboxing, typography overlays,
    /// and dynamic crisis cut-in cameras.
    /// </summary>
    public sealed class CampusCinematics : MonoBehaviour
    {
        public CampusSession Session;
        public StrategicCamera StrategicCam;

        public bool IsPlayingIntro { get; private set; } = true;
        public bool IsPlayingCrisisCutin { get; private set; } = false;
        public bool IsCinematicPlaying => IsPlayingIntro || IsPlayingCrisisCutin;
        public void SkipCinematic() => SkipIntro();

        GameObject cinemaCanvas;
        RectTransform topBar, bottomBar;
        Text titleText, subtitleText, skipText, crisisBannerText;
        CanvasGroup canvasGroup;

        Vector3 introStartPos = new Vector3(-70f, 95f, -125f);
        Vector3 introEndPos;
        Quaternion introStartRot = Quaternion.Euler(34f, 28f, 0f);
        Quaternion introEndRot;

        float introDuration = 7.5f;
        float introElapsed = 0f;

        void Start()
        {
            if (!Session) Session = FindFirstObjectByType<CampusSession>();
            if (!StrategicCam && Session) StrategicCam = Session.Rig;

            BuildCinemaUi();

            // Set up initial camera orientation for flyover
            if (StrategicCam && StrategicCam.VirtualCamera)
            {
                introEndPos = StrategicCam.Target - Quaternion.Euler(48f, -28f, 0f) * Vector3.forward * StrategicCam.Distance;
                introEndRot = Quaternion.Euler(48f, -28f, 0f);

                StrategicCam.VirtualCamera.transform.SetPositionAndRotation(introStartPos, introStartRot);
            }

            CampusAudioManager.Instance?.PlayIntroTheme();
        }

        void BuildCinemaUi()
        {
            cinemaCanvas = new GameObject("Cinematics Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            cinemaCanvas.transform.SetParent(transform, false);
            var canvas = cinemaCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = cinemaCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = cinemaCanvas.GetComponent<CanvasGroup>();

            // Letterbox Bars
            topBar = MakeBar(cinemaCanvas.transform, "Top Bar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), new Vector2(0, 0));
            bottomBar = MakeBar(cinemaCanvas.transform, "Bottom Bar", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 90));

            // Typography Container
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Crisis Banner (Middle Top)
            var cbGo = new GameObject("Crisis Banner", typeof(RectTransform), typeof(Text));
            cbGo.transform.SetParent(cinemaCanvas.transform, false);
            var cbRt = (RectTransform)cbGo.transform;
            cbRt.anchorMin = new Vector2(0.5f, 1f); cbRt.anchorMax = new Vector2(0.5f, 1f);
            cbRt.pivot = new Vector2(0.5f, 1f);
            cbRt.anchoredPosition = new Vector2(0, -110);
            cbRt.sizeDelta = new Vector2(1100, 50);
            crisisBannerText = cbGo.GetComponent<Text>();
            crisisBannerText.font = font; crisisBannerText.fontSize = 20;
            crisisBannerText.alignment = TextAnchor.MiddleCenter;
            crisisBannerText.color = new Color(0.96f, 0.22f, 0.18f);
            crisisBannerText.text = "";
            cbGo.SetActive(false);

            // Intro Title
            var titleGo = new GameObject("Intro Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(cinemaCanvas.transform, false);
            var tRt = (RectTransform)titleGo.transform;
            tRt.anchorMin = new Vector2(0.5f, 0.5f); tRt.anchorMax = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = new Vector2(0, 30);
            tRt.sizeDelta = new Vector2(1200, 80);
            titleText = titleGo.GetComponent<Text>();
            titleText.font = font; titleText.fontSize = 42;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.18f, 0.88f, 0.96f);
            titleText.text = "CAMPUS // PULSE 2030";

            // Intro Subtitle
            var subGo = new GameObject("Intro Subtitle", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(cinemaCanvas.transform, false);
            var sRt = (RectTransform)subGo.transform;
            sRt.anchorMin = new Vector2(0.5f, 0.5f); sRt.anchorMax = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = new Vector2(0, -25);
            sRt.sizeDelta = new Vector2(1000, 40);
            subtitleText = subGo.GetComponent<Text>();
            subtitleText.font = font; subtitleText.fontSize = 18;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.92f, 0.95f, 0.98f);
            subtitleText.text = "SMART ECO-CAMPUS DIGITAL TWIN SIMULATION";

            // Skip Hint
            var skipGo = new GameObject("Skip Hint", typeof(RectTransform), typeof(Text));
            skipGo.transform.SetParent(cinemaCanvas.transform, false);
            var skRt = (RectTransform)skipGo.transform;
            skRt.anchorMin = new Vector2(0.5f, 0f); skRt.anchorMax = new Vector2(0.5f, 0f);
            skRt.anchoredPosition = new Vector2(0, 115);
            skRt.sizeDelta = new Vector2(600, 30);
            skipText = skipGo.GetComponent<Text>();
            skipText.font = font; skipText.fontSize = 13;
            skipText.alignment = TextAnchor.MiddleCenter;
            skipText.color = new Color(0.7f, 0.8f, 0.9f, 0.8f);
            skipText.text = "[ SPACE / ESC TO SKIP INTRO ]";
        }

        RectTransform MakeBar(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
            go.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.96f);
            return rt;
        }

        void Update()
        {
            if (IsPlayingIntro)
            {
                introElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(introElapsed / introDuration);
                // Smooth hermite curve
                float k = t * t * (3f - 2f * t);

                // Smooth flyover camera position and rotation
                if (StrategicCam && StrategicCam.VirtualCamera)
                {
                    Vector3 camPos = Vector3.Lerp(introStartPos, introEndPos, k);
                    Quaternion camRot = Quaternion.Slerp(introStartRot, introEndRot, k);
                    StrategicCam.VirtualCamera.transform.SetPositionAndRotation(camPos, camRot);
                }

                // Fade out typography during last 2 seconds
                if (introElapsed > introDuration - 2.0f)
                {
                    float fade = (introDuration - introElapsed) / 2.0f;
                    titleText.color = new Color(0.18f, 0.88f, 0.96f, fade);
                    subtitleText.color = new Color(0.92f, 0.95f, 0.98f, fade);
                    skipText.color = new Color(0.7f, 0.8f, 0.9f, fade * 0.8f);
                }

                // Check skip inputs
                if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
                {
                    SkipIntro();
                }
                else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && introElapsed > 0.5f)
                {
                    SkipIntro();
                }

                if (introElapsed >= introDuration)
                {
                    FinishIntro();
                }
            }
        }

        public void SkipIntro()
        {
            if (!IsPlayingIntro) return;
            FinishIntro();
        }

        void FinishIntro()
        {
            IsPlayingIntro = false;
            titleText.gameObject.SetActive(false);
            subtitleText.gameObject.SetActive(false);
            skipText.gameObject.SetActive(false);

            // Animate letterbox retraction
            StartCoroutine(RetractBarsRoutine());

            if (StrategicCam)
            {
                StrategicCam.ResetView();
            }
        }

        IEnumerator RetractBarsRoutine()
        {
            float elapsed = 0f;
            float dur = 0.6f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = elapsed / dur;
                topBar.anchoredPosition = new Vector2(0, 90f * k);
                bottomBar.anchoredPosition = new Vector2(0, -90f * k);
                yield return null;
            }
            topBar.gameObject.SetActive(false);
            bottomBar.gameObject.SetActive(false);
        }

        public void TriggerCrisisCutin(string headline, FacilityView targetFacility, float duration = 3.0f)
        {
            if (IsPlayingIntro || IsPlayingCrisisCutin) return;
            StartCoroutine(CrisisCutinRoutine(headline, targetFacility, duration));
        }

        IEnumerator CrisisCutinRoutine(string headline, FacilityView targetFacility, float duration)
        {
            IsPlayingCrisisCutin = true;

            // Flash crisis banner
            if (crisisBannerText)
            {
                crisisBannerText.text = $"⚠ {headline.ToUpperInvariant()} ⚠";
                crisisBannerText.gameObject.SetActive(true);
            }

            CampusAudioManager.Instance?.SetAlarmActive(true);

            // Temporarily focus camera
            if (targetFacility && StrategicCam)
            {
                StrategicCam.Focus(targetFacility);
            }

            yield return new WaitForSecondsRealtime(duration);

            if (crisisBannerText) crisisBannerText.gameObject.SetActive(false);
            CampusAudioManager.Instance?.SetAlarmActive(false);

            if (StrategicCam) StrategicCam.ResetView();
            IsPlayingCrisisCutin = false;
        }
    }
}
