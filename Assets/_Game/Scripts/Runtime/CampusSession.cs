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
            State = new CampusState();
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
