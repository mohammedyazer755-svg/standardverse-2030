using UnityEngine;
using CampusPulse.Simulation;

namespace CampusPulse
{
    public sealed class FacilityView : MonoBehaviour
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Purpose;
        public int Capacity = 200;
        public int InitialOccupants = 80;
        public float Water = 12, Power = 35, Waste = 8;
        public Vector3 FocusOffset = new Vector3(0, 2, 0);
        public float FocusDistance = 46;
        public Renderer Highlight;
        public Renderer ActivityIndicator;
        public Renderer LeakIndicator;
        public Renderer StatusPinGlow;
        public Transform WaterSurface;
        public FacilityState CreateState() => new FacilityState(Id, DisplayName, Purpose, Capacity, InitialOccupants, Water, Power, Waste);
        MaterialPropertyBlock properties;
        public void SetHighlight(bool selected, bool hovered)
        {
            if (!Highlight) return;
            Highlight.gameObject.SetActive(selected || hovered);
            properties ??= new MaterialPropertyBlock();
            var c = selected ? new Color(.05f, .85f, 1f) : new Color(.8f, .9f, 1f);
            properties.SetColor("_BaseColor", c);
            properties.SetColor("_EmissionColor", c * 1.6f);
            Highlight.SetPropertyBlock(properties);
        }
        public void Present(CampusState state)
        {
            if (ActivityIndicator)
            {
                properties ??= new MaterialPropertyBlock();
                bool blackout = state.PowerDeficitKW > 5f;
                var color = blackout ? new Color(.92f, .18f, .12f) : new Color(.12f, .88f, .72f);
                properties.SetColor("_BaseColor", color);
                properties.SetColor("_EmissionColor", color * (blackout ? 1.2f : 2.0f));
                ActivityIndicator.SetPropertyBlock(properties);
            }
            if (WaterSurface)
            {
                var p = WaterSurface.localPosition;
                p.y = 1.0f + 4.8f * (state.WaterStoredLiters / state.WaterCapacityLiters);
                WaterSurface.localPosition = p;
            }
            if (LeakIndicator)
            {
                LeakIndicator.gameObject.SetActive(state.MainLeakActive);
            }
            if (StatusPinGlow)
            {
                properties ??= new MaterialPropertyBlock();
                Color pinColor = new Color(.12f, .88f, .72f);
                if (Id == "water" && state.MainLeakActive)
                    pinColor = new Color(.95f, .22f, .15f);
                else if (state.PowerDeficitKW > 5f)
                    pinColor = new Color(.95f, .22f, .15f);
                else if (state.Facilities.TryGetValue(Id, out var fac) && fac.BinOverflowKg > 2f)
                    pinColor = new Color(1f, .72f, .15f);
                else if (Id == "canteen" && fac != null && fac.QueueCount > 25)
                    pinColor = new Color(1f, .72f, .15f);

                properties.SetColor("_BaseColor", pinColor);
                properties.SetColor("_EmissionColor", pinColor * 2.2f);
                StatusPinGlow.SetPropertyBlock(properties);
            }
        }
    }
}
