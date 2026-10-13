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
        public Renderer Highlight;
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
            if (WaterSurface)
            {
                var p = WaterSurface.localPosition;
                p.y = .4f + 4f * state.WaterStoredLiters / state.WaterCapacityLiters;
                WaterSurface.localPosition = p;
            }
        }
    }
}
