using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using CampusPulse.Simulation;

namespace CampusPulse
{
    public enum SessionMode { Menu, Playing, Paused }
    public sealed class CampusSession : MonoBehaviour
    {
        public StrategicCamera Rig;
        public Camera WorldCamera;
        public CampusHud Hud;
        public FacilityView[] Facilities;
        public CampusState State { get; private set; }
        public SessionMode Mode { get; private set; } = SessionMode.Menu;
        public FacilityView Selected { get; private set; }
        public FacilityView Hovered { get; private set; }
        public CampusCampaign Campaign { get; private set; }
        public CampusAdvisor Advisor { get; private set; }
        public event Action<FacilityView> SelectionChanged;
        float clock;
        void Awake()
        {
            ResetState();
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
        }
        public void ResetState()
        {
            Campaign = new CampusCampaign();
            Advisor = new CampusAdvisor();
            State = new CampusState();
            State.Campaign = Campaign;
            foreach (var facility in Facilities) State.Add(facility.CreateState());
            clock = 0;
            Select(null);
            State.Paused = Mode != SessionMode.Playing;
        }
        public void Begin() { Mode = SessionMode.Playing; State.Paused = false; }
        public void TogglePause()
        {
            if (Mode == SessionMode.Menu) return;
            Mode = Mode == SessionMode.Playing ? SessionMode.Paused : SessionMode.Playing;
            State.Paused = Mode != SessionMode.Playing;
        }
        public void Restart() { Mode = SessionMode.Playing; ResetState(); Rig.ResetView(); }
        public void PlayChapter(CampaignChapter ch)
        {
            ResetState();
            Campaign.InitChapter(ch);
            Mode = SessionMode.Playing;
            State.Paused = false;
        }
        public void AdvanceChapter()
        {
            Campaign.AdvanceToNextChapter(State);
        }
        public string GetSavePath() => System.IO.Path.Combine(Application.persistentDataPath, "campus-save.json");
        public void SaveGame()
        {
            try
            {
                var json = "{\"state\":" + State.ToJson() + ",\"campaign\":" + Campaign.ToJson() + "}";
                System.IO.File.WriteAllText(GetSavePath(), json);
                Hud?.TriggerFeedback("✔ GAME SAVED: Progress preserved to disk.");
            }
            catch (Exception ex) { Debug.LogWarning("Save failed: " + ex.Message); }
        }
        public bool LoadGame()
        {
            try
            {
                var path = GetSavePath();
                if (!System.IO.File.Exists(path)) { Hud?.TriggerFeedback("❌ No save file found."); return false; }
                var json = System.IO.File.ReadAllText(path);
                int sIdx = json.IndexOf("\"state\":", StringComparison.Ordinal);
                int cIdx = json.IndexOf("\"campaign\":", StringComparison.Ordinal);
                if (sIdx >= 0 && cIdx >= 0)
                {
                    ResetState();
                    string stateJson = json.Substring(sIdx + 8, cIdx - (sIdx + 8) - 1);
                    string campJson = json.Substring(cIdx + 11).TrimEnd('}');
                    State.LoadFromJson(stateJson);
                    Campaign.LoadFromJson(campJson);
                    Hud?.TriggerFeedback("✔ GAME LOADED: State restored successfully.");
                    return true;
                }
            }
            catch (Exception ex) { Debug.LogWarning("Load failed: " + ex.Message); }
            return false;
        }
        public void Select(FacilityView facility)
        {
            Selected = facility;
            foreach (var f in Facilities) f.SetHighlight(f == Selected, f == Hovered);
            SelectionChanged?.Invoke(facility);
        }
        public void FocusSelected() { if (Selected) Rig.Focus(Selected); }
        public void Overview() { Select(null); Rig.ResetView(); }
        public FacilityView Pick(Vector2 screenPosition, bool blocked)
        {
            if (blocked || !WorldCamera) return null;
            return Physics.Raycast(WorldCamera.ScreenPointToRay(screenPosition), out var hit, 1000)
                ? hit.collider.GetComponentInParent<FacilityView>() : null;
        }
        void Update()
        {
            if (Mode == SessionMode.Menu) return;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
                if (Keyboard.current.fKey.wasPressedThisFrame) FocusSelected();
            }
            bool blocked = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Rig.Apply(StrategicCamera.ReadDesktop(blocked), Time.unscaledDeltaTime);
            if (Mouse.current != null)
            {
                var hover = Pick(Mouse.current.position.ReadValue(), blocked);
                if (hover != Hovered)
                {
                    Hovered = hover;
                    foreach (var f in Facilities) f.SetHighlight(f == Selected, f == Hovered);
                }
                if (!blocked && Mouse.current.leftButton.wasPressedThisFrame) Select(hover);
            }
            if (Mode == SessionMode.Playing)
            {
                clock += Time.deltaTime;
                while (clock >= 1) { State.Tick(); clock -= 1; }
            }
            foreach (var f in Facilities) f.Present(State);
        }
    }
}
