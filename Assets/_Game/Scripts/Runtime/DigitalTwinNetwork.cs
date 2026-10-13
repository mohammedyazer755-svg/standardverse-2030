using System.Collections.Generic;
using UnityEngine;
using CampusPulse.Simulation;

namespace CampusPulse
{
    /// <summary>
    /// Creates the smart campus holographic data network directly inspired by
    /// the Digital Twin reference image: glowing curved data arcs connecting facilities
    /// with animated pulsing telemetry packets and status beacons.
    /// </summary>
    public sealed class DigitalTwinNetwork : MonoBehaviour
    {
        public CampusSession Session;

        sealed class DataArc
        {
            public string SourceId;
            public string TargetId;
            public LineRenderer Line;
            public Vector3[] Points;
        }

        readonly List<DataArc> arcs = new List<DataArc>();
        Material lineMaterial;
        float pulseOffset = 0f;

        void Start()
        {
            if (!Session) Session = FindFirstObjectByType<CampusSession>();

            CreateArcMaterial();
            BuildNetworkConnections();
        }

        void CreateArcMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
            {
                var ren = FindFirstObjectByType<Renderer>();
                if (ren && ren.sharedMaterial) shader = ren.sharedMaterial.shader;
            }

            if (shader != null)
            {
                lineMaterial = new Material(shader);
                lineMaterial.name = "DigitalTwinDataArc";
                lineMaterial.SetColor("_BaseColor", new Color(0.12f, 0.85f, 1.0f, 0.85f));
                if (lineMaterial.HasProperty("_EmissionColor"))
                {
                    lineMaterial.EnableKeyword("_EMISSION");
                    lineMaterial.SetColor("_EmissionColor", new Color(0.12f, 0.85f, 1.0f) * 2.2f);
                }
            }
        }

        public int ActiveConnectionsCount => arcs.Count;

        void BuildNetworkConnections()
        {
            if (Session == null || Session.Facilities == null) return;

            // Connect primary infrastructural hubs to facilities
            var connections = new (string src, string dst)[]
            {
                ("solar", "research"),
                ("solar", "academic"),
                ("solar", "administration"),
                ("water", "academic"),
                ("water", "canteen"),
                ("water", "hostel"),
                ("recycling", "canteen"),
                ("recycling", "library"),
                ("power", "academic"),
                ("power", "auditorium")
            };

            foreach (var conn in connections)
            {
                var srcFac = FindFacility(conn.src);
                var dstFac = FindFacility(conn.dst);
                if (srcFac != null && dstFac != null)
                {
                    CreateDataArc(srcFac, dstFac);
                }
            }
        }

        FacilityView FindFacility(string id)
        {
            foreach (var f in Session.Facilities)
            {
                if (f.Id.Equals(id, System.StringComparison.OrdinalIgnoreCase)) return f;
            }
            return null;
        }

        void CreateDataArc(FacilityView src, FacilityView dst)
        {
            var go = new GameObject($"Arc_{src.Id}_{dst.Id}");
            go.transform.SetParent(transform, false);

            var line = go.AddComponent<LineRenderer>();
            line.material = lineMaterial;
            line.startWidth = 0.45f;
            line.endWidth = 0.45f;
            line.numCapVertices = 4;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            int segments = 24;
            line.positionCount = segments + 1;

            Vector3 p0 = src.transform.position + Vector3.up * 8f;
            Vector3 p2 = dst.transform.position + Vector3.up * 8f;
            Vector3 mid = (p0 + p2) * 0.5f;
            // Arch upwards proportional to distance
            float dist = Vector3.Distance(p0, p2);
            Vector3 p1 = mid + Vector3.up * Mathf.Clamp(dist * 0.28f, 6f, 22f);

            Vector3[] pts = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                // Quadratic Bezier
                pts[i] = (1f - t) * (1f - t) * p0 + 2f * (1f - t) * t * p1 + t * t * p2;
            }
            line.SetPositions(pts);

            arcs.Add(new DataArc
            {
                SourceId = src.Id,
                TargetId = dst.Id,
                Line = line,
                Points = pts
            });
        }

        void Update()
        {
            if (arcs.Count == 0 || lineMaterial == null || Session == null) return;

            pulseOffset -= Time.deltaTime * 0.8f;
            var s = Session.State;

            // Shift arc color depending on grid stability
            Color arcColor = new Color(0.12f, 0.85f, 1.0f);
            if (s != null && s.PowerDeficitKW > 5f)
            {
                // Amber/Red during electrical strain
                arcColor = new Color(1.0f, 0.35f, 0.15f);
            }
            else if (s != null && s.Satisfaction >= 90 && s.Sustainability >= 90)
            {
                // Radiant green in high sustainability / Future Lens
                arcColor = new Color(0.18f, 0.95f, 0.55f);
            }

            lineMaterial.SetColor("_BaseColor", arcColor);
            if (lineMaterial.HasProperty("_EmissionColor"))
            {
                lineMaterial.SetColor("_EmissionColor", arcColor * 2.0f);
            }
        }
    }
}
