using System;
using System.Collections.Generic;

namespace CampusPulse.Simulation
{
    public enum ResourceCategory { Water, Energy, Waste, Crowd }
    public enum ScenarioKind { Water, Energy, Waste, Crowd }
    public enum ActionKind
    {
        RepairLeak, LowFlow, Ration, BoostPump, SchedulePump,
        RescheduleLabs, LEDs, Backup, ReduceLoad,
        CollectWaste, Segregation, MoreBins, CollectionRoute,
        Stagger, Reroute, ExtraCounter, MoreCapacity
    }
    public enum ActionStatus { Available, Accepted, InProgress, Completed, Failed, Cancelled }
    public enum PumpPolicy { Standard, Boost, OffPeak }

    public sealed class ActionDefinition
    {
        public readonly ActionKind Kind;
        public readonly ResourceCategory Category;
        public readonly string Id, Name, Effect;
        public readonly int Cost, Staff, Duration, Cooldown;

        public ActionDefinition(ActionKind kind, ResourceCategory category, string name, int cost, int staff, int duration, string effect, int cooldown = 0)
        {
            Kind = kind;
            Category = category;
            Id = category.ToString().ToLowerInvariant() + "." + kind.ToString().ToLowerInvariant();
            Name = name;
            Cost = cost;
            Staff = staff;
            Duration = duration;
            Effect = effect;
            Cooldown = cooldown;
        }
    }

    public static class ActionCatalog
    {
        static readonly ActionDefinition[] definitions = {
            new ActionDefinition(ActionKind.RepairLeak, ResourceCategory.Water, "Repair Main Leak", 2200, 2, 4, "Stops the 85 L/min pipe leak permanently. Requires 2 staff."),
            new ActionDefinition(ActionKind.LowFlow, ResourceCategory.Water, "Campus Low-Flow Retrofit", 8000, 2, 7, "Cuts all building water demand by 35% permanently."),
            new ActionDefinition(ActionKind.Ration, ResourceCategory.Water, "Emergency Rationing", 400, 1, 2, "Cuts consumption by 40% for 30 minutes; temporary satisfaction cost.", 32),
            new ActionDefinition(ActionKind.BoostPump, ResourceCategory.Water, "Boost Pumping", 800, 1, 2, "Pumps up to 300 L/min for 30 minutes (requires 35 kW power).", 32),
            new ActionDefinition(ActionKind.SchedulePump, ResourceCategory.Water, "Off-Peak Pump Schedule", 600, 1, 3, "Halts pumping during 08:00–10:00 peak hours; saves peak energy."),

            new ActionDefinition(ActionKind.RescheduleLabs, ResourceCategory.Energy, "Reschedule Lab Loads", 600, 1, 3, "Moves lab equipment load outside 08:00–10:00 morning peak."),
            new ActionDefinition(ActionKind.LEDs, ResourceCategory.Energy, "Campus LED Retrofit", 6500, 2, 6, "Reduces building electricity demand by 25% permanently."),
            new ActionDefinition(ActionKind.Backup, ResourceCategory.Energy, "Activate Emergency Backup", 1800, 1, 2, "Provides up to 150 kW from finite 60 kWh fuel reserve."),
            new ActionDefinition(ActionKind.ReduceLoad, ResourceCategory.Energy, "Shed Nonessential Load", 300, 1, 1, "Cuts building load by 25% for 25 minutes; minor comfort penalty.", 26),

            new ActionDefinition(ActionKind.CollectWaste, ResourceCategory.Waste, "Dispatch Collection", 1200, 2, 5, "Collects up to 450 kg of waste and transfers it to the recovery yard.", 7),
            new ActionDefinition(ActionKind.Segregation, ResourceCategory.Waste, "Improve Segregation", 2800, 1, 4, "Raises recycling recovery yield from 25% to 75% permanently."),
            new ActionDefinition(ActionKind.MoreBins, ResourceCategory.Waste, "Increase Bin Capacity", 3600, 1, 4, "Adds 40 kg bin capacity at every facility across campus."),
            new ActionDefinition(ActionKind.CollectionRoute, ResourceCategory.Waste, "Reorganize Collection Route", 1600, 1, 3, "Shortens future collection duration from 5 to 3 minutes."),

            new ActionDefinition(ActionKind.Stagger, ResourceCategory.Crowd, "Stagger Lunch Arrivals", 800, 1, 3, "Smooths lunch departures into canteen from 12 to 4 students/min."),
            new ActionDefinition(ActionKind.Reroute, ResourceCategory.Crowd, "Open Garden Diversion", 900, 1, 2, "Diverts waiting students into the reflection garden.", 12),
            new ActionDefinition(ActionKind.ExtraCounter, ResourceCategory.Crowd, "Open Additional Counter", 4200, 2, 4, "Adds 8 students/min service capacity (+28 kW demand, +meals waste)."),
            new ActionDefinition(ActionKind.MoreCapacity, ResourceCategory.Crowd, "Open Overflow Seating", 1800, 1, 3, "Adds 80 seats to canteen, relieving congestion.")
        };

        public static IReadOnlyList<ActionDefinition> All { get; } = Array.AsReadOnly(definitions);

        public static ActionDefinition Get(ActionKind kind)
        {
            foreach (var d in definitions) if (d.Kind == kind) return d;
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public sealed class ActionTicket
    {
        public string CommandId { get; internal set; }
        public string FacilityId { get; internal set; }
        public ActionKind Kind { get; internal set; }
        public ActionStatus Status { get; internal set; }
        public string Message { get; internal set; }
        public int AcceptedMinute { get; internal set; }
        public int StartMinute { get; internal set; }
        public int FinishMinute { get; internal set; }
        public int ReservedCost { get; internal set; }
        public int ReservedStaff { get; internal set; }

        public bool Active => Status == ActionStatus.Accepted || Status == ActionStatus.InProgress;

        internal ActionTicket Clone()
        {
            return (ActionTicket)MemberwiseClone();
        }
    }

    public sealed class SimulationSnapshot
    {
        readonly CampusState saved;
        public SimulationSnapshot(CampusState state) { saved = state.DeepClone(); }
        public CampusState Restore() => saved.DeepClone();
        public string Fingerprint => saved.Fingerprint();
    }
}
