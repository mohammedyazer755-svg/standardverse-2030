using System;
using System.Collections.Generic;

namespace CampusPulse.Simulation
{
    [Serializable]
    public sealed class FacilityState
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Purpose;
        public readonly int Capacity;
        public int Occupants { get; private set; }
        public readonly float WaterLitersPerMinute;
        public readonly float PowerKW;
        public readonly float WasteKG;

        public FacilityState(string id, string name, string purpose, int capacity, int occupants,
            float water, float power, float waste)
        {
            if (string.IsNullOrWhiteSpace(id) || capacity <= 0 || occupants < 0 || occupants > capacity)
                throw new ArgumentException("Invalid facility definition");
            Id = id; Name = name; Purpose = purpose; Capacity = capacity; Occupants = occupants;
            WaterLitersPerMinute = water; PowerKW = power; WasteKG = waste;
        }
    }

    // Phase 1 is a state/clock shell. Resource interventions and flow accounting belong to Phase 2.
    public sealed class CampusState
    {
        readonly Dictionary<string, FacilityState> facilities = new Dictionary<string, FacilityState>();
        public IReadOnlyDictionary<string, FacilityState> Facilities => facilities;
        public int Minute { get; private set; } = 8 * 60;
        public int Seed { get; }
        public bool Paused { get; set; }
        public int BudgetINR => 150000;
        public float WaterStoredLiters => 36000;
        public float WaterCapacityLiters => 50000;
        public float PowerSupplyKW => 420;
        public int StaffAvailable => 6;
        public int Satisfaction => 78;
        public int Sustainability => 64;
        public CampusState(int seed = 2030) { Seed = seed; }
        public void Add(FacilityState facility)
        {
            if (facility == null || facilities.ContainsKey(facility.Id)) throw new ArgumentException("Duplicate/null facility");
            facilities.Add(facility.Id, facility);
        }
        public void Tick() { if (!Paused) Minute++; }
        public int VisualCount(string id, int cap = 12)
        {
            if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap));
            return facilities.TryGetValue(id, out var f) ? Math.Min(cap, (f.Occupants + 9) / 10) : 0;
        }
    }
}
