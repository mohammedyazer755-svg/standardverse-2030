using System.Collections.Generic;
using UnityEngine;

namespace CampusPulse
{
    public sealed class CampusLife : MonoBehaviour
    {
        public CampusSession Session;
        public GameObject WalkerPrefab;
        public Material[] OutfitMaterials;
        public Transform[] Trees;
        public Transform[] Rotors;
        public Transform[] SolarTables;
        public Renderer[] Lamps;
        public Transform Vehicle;
        public Vector3[] VehicleRoute;
        public int MaximumWalkers = 144;
        public int ActiveWalkers { get; private set; }
        readonly List<Walker> pool = new List<Walker>();
        readonly List<Quaternion> treeRotations = new List<Quaternion>();
        MaterialPropertyBlock lampProperties;
        float visualTime;
        int vehicleSegment;
        sealed class Walker
        {
            public Transform Root, LeftLeg, RightLeg, LeftArm, RightArm;
            public Vector3[] Route;
            public int Segment;
            public float Speed, Phase;
        }
        void Start()
        {
            foreach (var t in Trees) treeRotations.Add(t.localRotation);
            lampProperties = new MaterialPropertyBlock();
            ReconcilePopulation();
        }
        public void ReconcilePopulation()
        {
            int desired = 0;
            foreach (var f in Session.Facilities)
            {
                int count = Session.State.VisualCount(f.Id, 18);
                for (int j = 0; j < count && desired < MaximumWalkers; j++, desired++)
                {
                    Walker walker;
                    if (desired < pool.Count) walker = pool[desired];
                    else
                    {
                        var body = Instantiate(WalkerPrefab, transform);
                        walker = new Walker { Root=body.transform, LeftLeg=body.transform.Find("Left leg"), RightLeg=body.transform.Find("Right leg"),
                            LeftArm=body.transform.Find("Left arm"),RightArm=body.transform.Find("Right arm") };
                        pool.Add(walker);
                        if (OutfitMaterials.Length > 0) body.transform.Find("Torso").GetComponent<Renderer>().sharedMaterial = OutfitMaterials[desired % OutfitMaterials.Length];
                    }
                    walker.Root.gameObject.SetActive(true);
                    // Facility-local footpath rectangle; independent of decorative agent counts.
                    Vector3 p = f.transform.position;
                    var bounds = f.GetComponent<BoxCollider>();
                    p += new Vector3(bounds.center.x,0,bounds.center.z);
                    var ext = bounds.size * .5f;
                    float x = ext.x + 3.2f, z = ext.z + 3.2f;
                    walker.Route = new[] {p+new Vector3(-x,.28f,-z),p+new Vector3(x,.28f,-z),p+new Vector3(x,.28f,z),p+new Vector3(-x,.28f,z)};
                    walker.Segment = j % 4;
                    walker.Root.position = Vector3.Lerp(walker.Route[walker.Segment], walker.Route[(walker.Segment+1)%4], (j*.173f)%1);
                    walker.Speed = 1.15f + (desired % 5)*.12f; walker.Phase = desired * 1.7f;
                }
            }
            for (int i=desired;i<pool.Count;i++) pool[i].Root.gameObject.SetActive(false);
            ActiveWalkers = desired;
        }
        void Update()
        {
            if (Session.Mode != SessionMode.Playing) return;
            float dt = Mathf.Min(Time.deltaTime,.1f); visualTime += dt;
            for(int i=0;i<ActiveWalkers;i++)
            {
                var w = pool[i]; var target = w.Route[(w.Segment+1)%w.Route.Length]; var delta = target-w.Root.position;
                w.Root.position = Vector3.MoveTowards(w.Root.position,target,w.Speed*dt);
                if(delta.sqrMagnitude>.01f) w.Root.rotation = Quaternion.Slerp(w.Root.rotation,Quaternion.LookRotation(delta),dt*8);
                if(delta.sqrMagnitude<.04f) w.Segment=(w.Segment+1)%w.Route.Length;
                float swing = Mathf.Sin(visualTime*w.Speed*6+w.Phase)*24;
                w.LeftLeg.localRotation=Quaternion.Euler(swing,0,0); w.RightLeg.localRotation=Quaternion.Euler(-swing,0,0);
                w.LeftArm.localRotation=Quaternion.Euler(-swing*.7f,0,0); w.RightArm.localRotation=Quaternion.Euler(swing*.7f,0,0);
            }
            for(int i=0;i<Trees.Length;i++) Trees[i].localRotation=treeRotations[i]*Quaternion.Euler(Mathf.Sin(visualTime*.8f+i)*.65f,0,Mathf.Cos(visualTime*.6f+i)*.5f);
            foreach(var rotor in Rotors) rotor.Rotate(0,0,dt*65,Space.Self);
            if(SolarTables!=null) foreach(var table in SolarTables)
                table.localRotation=Quaternion.Euler(-22+Mathf.Sin(Session.State.Minute/1440f*Mathf.PI*2)*7,0,0);
            if(Vehicle && VehicleRoute.Length>1)
            {
                var target = VehicleRoute[(vehicleSegment+1)%VehicleRoute.Length]; var delta=target-Vehicle.position;
                Vehicle.position=Vector3.MoveTowards(Vehicle.position,target,6*dt);
                if(delta.sqrMagnitude>.01f) Vehicle.rotation=Quaternion.Slerp(Vehicle.rotation,Quaternion.LookRotation(delta),dt*3);
                if(delta.sqrMagnitude<.1f) vehicleSegment=(vehicleSegment+1)%VehicleRoute.Length;
            }
            bool evening = Session.State.Minute % 1440 >= 18*60 || Session.State.Minute % 1440 < 6*60;
            lampProperties.SetColor("_EmissionColor",new Color(1,.78f,.38f)*(evening?3:.3f));
            foreach(var lamp in Lamps) lamp.SetPropertyBlock(lampProperties);
        }
    }
}
