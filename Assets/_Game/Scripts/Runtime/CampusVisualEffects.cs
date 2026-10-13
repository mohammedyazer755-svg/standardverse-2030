using System.Collections.Generic;
using UnityEngine;
using CampusPulse.Simulation;

namespace CampusPulse
{
    /// <summary>
    /// Governs simulation-driven visual effects: water leak spray particles,
    /// substation electrical arcing, window brownout flickering, and pump rotation.
    /// </summary>
    public sealed class CampusVisualEffects : MonoBehaviour
    {
        public CampusSession Session;

        // Water VFX
        ParticleSystem waterLeakParticles;
        GameObject leakPuddle;
        FacilityView waterFacility;

        // Electric VFX
        ParticleSystem electricSparksParticles;
        FacilityView solarFacility;
        readonly List<Renderer> illuminatedWindows = new List<Renderer>();

        // Pump rotors
        readonly List<Transform> pumpRotors = new List<Transform>();

        void Start()
        {
            if (!Session) Session = FindFirstObjectByType<CampusSession>();
            FindKeyFacilities();
            BuildWaterVfx();
            BuildElectricVfx();
            FindRotorsAndWindows();
        }

        void FindKeyFacilities()
        {
            if (Session == null || Session.Facilities == null) return;
            foreach (var f in Session.Facilities)
            {
                if (f.Id == "water") waterFacility = f;
                else if (f.Id == "solar") solarFacility = f;
            }
        }

        void BuildWaterVfx()
        {
            if (!waterFacility) return;

            var go = new GameObject("Water Leak Spray VFX");
            go.transform.SetParent(waterFacility.transform, false);
            go.transform.localPosition = new Vector3(2.5f, 2.2f, 0f);

            waterLeakParticles = go.AddComponent<ParticleSystem>();
            var main = waterLeakParticles.main;
            main.startColor = new Color(0.4f, 0.85f, 1.0f, 0.7f);
            main.startSize = 0.45f;
            main.startSpeed = 6f;
            main.startLifetime = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;

            var emission = waterLeakParticles.emission;
            emission.rateOverTime = 35f;

            var shape = waterLeakParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.15f;
            go.transform.localRotation = Quaternion.Euler(-30f, 45f, 0f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader) renderer.material = new Material(shader);

            // Ground leak puddle
            leakPuddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leakPuddle.name = "Water Leak Puddle";
            leakPuddle.transform.SetParent(waterFacility.transform, false);
            leakPuddle.transform.localPosition = new Vector3(3.2f, 0.05f, 0.8f);
            leakPuddle.transform.localScale = new Vector3(4.5f, 0.02f, 4.5f);
            Destroy(leakPuddle.GetComponent<Collider>());

            var litShader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            if (litShader == null)
            {
                var ren = waterFacility.GetComponentInChildren<Renderer>();
                if (ren && ren.sharedMaterial) litShader = ren.sharedMaterial.shader;
            }
            if (litShader != null)
            {
                var puddleMat = new Material(litShader);
                puddleMat.SetColor("_BaseColor", new Color(0.15f, 0.45f, 0.65f, 0.9f));
                puddleMat.SetFloat("_Smoothness", 0.95f);
                leakPuddle.GetComponent<Renderer>().material = puddleMat;
            }

            waterLeakParticles.gameObject.SetActive(false);
            leakPuddle.SetActive(false);
        }

        void BuildElectricVfx()
        {
            if (!solarFacility) return;

            var go = new GameObject("Substation Electrical Arcs VFX");
            go.transform.SetParent(solarFacility.transform, false);
            go.transform.localPosition = new Vector3(0f, 3.5f, 0f);

            electricSparksParticles = go.AddComponent<ParticleSystem>();
            var main = electricSparksParticles.main;
            main.startColor = new Color(0.3f, 0.9f, 1.0f, 0.95f);
            main.startSize = 0.6f;
            main.startSpeed = 8f;
            main.startLifetime = 0.4f;
            main.loop = true;

            var emission = electricSparksParticles.emission;
            emission.rateOverTime = 40f;

            var shape = electricSparksParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2.2f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader)
            {
                var mat = new Material(shader);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.4f, 0.95f, 1.0f) * 3f);
                renderer.material = mat;
            }

            electricSparksParticles.gameObject.SetActive(false);
        }

        void FindRotorsAndWindows()
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name.Contains("Pump rotor") || t.name.Contains("Turbine rotor"))
                {
                    pumpRotors.Add(t);
                }
            }

            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.name.Contains("Glazing") || r.name.Contains("Window") || r.name.Contains("Entrance glazing"))
                {
                    illuminatedWindows.Add(r);
                }
            }
        }

        void Update()
        {
            if (Session == null || Session.State == null) return;
            var s = Session.State;

            // 1. Water Leak & Puddle response
            bool leak = s.MainLeakActive;
            if (waterLeakParticles && waterLeakParticles.gameObject.activeSelf != leak)
            {
                waterLeakParticles.gameObject.SetActive(leak);
                if (leak) waterLeakParticles.Play();
            }
            if (leakPuddle && leakPuddle.activeSelf != leak)
            {
                leakPuddle.SetActive(leak);
            }

            // 2. Electrical Substation Arcs & Blackout Flicker
            bool severePowerDeficit = s.PowerDeficitKW > 10f;
            if (electricSparksParticles && electricSparksParticles.gameObject.activeSelf != severePowerDeficit)
            {
                electricSparksParticles.gameObject.SetActive(severePowerDeficit);
                if (severePowerDeficit)
                {
                    electricSparksParticles.Play();
                    CampusAudioManager.Instance?.PlayElectricSpark();
                }
            }

            // Window brownout flickering
            if (severePowerDeficit && illuminatedWindows.Count > 0)
            {
                float flicker = (Random.value > 0.35f) ? Random.Range(0.2f, 1.6f) : 0.05f;
                // Subtle flicker during grid overload
                Color emission = new Color(1f, 0.85f, 0.55f) * flicker;
                foreach (var w in illuminatedWindows)
                {
                    if (w && w.material.HasProperty("_EmissionColor"))
                    {
                        w.material.SetColor("_EmissionColor", emission);
                    }
                }
            }

            // 3. Pump rotation when powered and pumping
            if (pumpRotors.Count > 0 && s.PumpDeliveredLpm > 0f)
            {
                float rotSpeed = (s.PumpPolicy == PumpPolicy.Boost ? 720f : 360f) * Time.deltaTime;
                foreach (var rotor in pumpRotors)
                {
                    if (rotor) rotor.Rotate(Vector3.up, rotSpeed, Space.Self);
                }
            }
        }
    }
}
