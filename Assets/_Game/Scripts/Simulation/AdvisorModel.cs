using System;
using System.Collections.Generic;

namespace CampusPulse.Simulation
{
    public enum AdvisorPriority { Info, Recommendation, Warning, Critical }

    public sealed class BisStandardInfo
    {
        public string StandardCode { get; }
        public string Title { get; }
        public string BenchmarkRule { get; }
        public string DirectRelevance { get; }

        public BisStandardInfo(string code, string title, string benchmark, string relevance)
        {
            StandardCode = code;
            Title = title;
            BenchmarkRule = benchmark;
            DirectRelevance = relevance;
        }
    }

    public sealed class AdvisorAdvice
    {
        public AdvisorPriority Priority { get; }
        public ResourceCategory Category { get; }
        public string Headline { get; }
        public string Explanation { get; }
        public string SuggestedActionText { get; }
        public ActionKind SuggestedAction { get; }
        public BisStandardInfo BisReference { get; }

        public AdvisorAdvice(AdvisorPriority priority, ResourceCategory category, string headline,
            string explanation, string actionText, ActionKind action, BisStandardInfo bis = null)
        {
            Priority = priority;
            Category = category;
            Headline = headline;
            Explanation = explanation;
            SuggestedActionText = actionText;
            SuggestedAction = action;
            BisReference = bis;
        }
    }

    public static class BisCatalog
    {
        public static readonly BisStandardInfo WaterStandards = new BisStandardInfo(
            "IS 1172:1993 / NBC 2016 Part 9",
            "Basic Requirements for Water Supply, Drainage and Sanitation",
            "45 to 135 Liters/Head/Day for Day Colleges & Hostels. Leakage control mandatory.",
            "Repairing pipe leaks and deploying aerated low-flow taps ensures compliance with national institutional consumption norms.");

        public static readonly BisStandardInfo ElectricalStandards = new BisStandardInfo(
            "IS 732:2019 / NBC 2016 Part 8 (Sec 2)",
            "Code of Practice for Electrical Wiring Installations & Building Services",
            "Demand-side energy management, harmonic reduction, and solar PV integration.",
            "Shifting laboratory peak loads and retrofitting LED luminaires curbs thermal line losses and prevents transformer overloads.");

        public static readonly BisStandardInfo WasteStandards = new BisStandardInfo(
            "Solid Waste Management Rules 2016 / IS 10447",
            "Standards for Solid Waste Segregation, Storage and Collection",
            "Mandatory 3-stream source segregation: Biodegradable (Green), Recyclable (Blue), Domestic Hazardous (Red).",
            "Segregating campus food waste from plastics reduces landfill volume and elevates recyclable yield to >75%.");

        public static readonly BisStandardInfo OccupancyStandards = new BisStandardInfo(
            "NBC 2016 Part 4 (Fire & Life Safety)",
            "Occupant Load & Egress Capacity for Assembly / Educational Buildings",
            "1.5 m² per person in educational lecture halls; 0.65 m² per person in dining halls.",
            "Staggering meal intervals and opening overflow dining seating prevents dangerous corridor congestion during peak rush.");
    }

    public sealed class CampusAdvisor
    {
        public AdvisorAdvice CurrentAdvice { get; private set; }

        public AdvisorAdvice Evaluate(CampusState state, CampusCampaign campaign)
        {
            // Priority 1: Critical Emergency Situations
            if (state.PowerDeficitKW > 5f)
            {
                CurrentAdvice = new AdvisorAdvice(
                    AdvisorPriority.Critical,
                    ResourceCategory.Energy,
                    "CRITICAL: Electrical Grid Deficit",
                    $"Power demand exceeds available supply by {state.PowerDeficitKW:0} kW! Pumps and vital services will halt.",
                    "Engage Emergency Diesel Backup or Shed Nonessential Load.",
                    state.BackupFuelReserveKWh > 1f ? ActionKind.Backup : ActionKind.ReduceLoad,
                    BisCatalog.ElectricalStandards
                );
                return CurrentAdvice;
            }

            if (state.MainLeakActive && state.WaterStoredLiters < 20000f)
            {
                CurrentAdvice = new AdvisorAdvice(
                    AdvisorPriority.Critical,
                    ResourceCategory.Water,
                    "URGENT: Water Reservoir Depletion",
                    $"Active 85 L/min pipe rupture has drained storage below 20 kL. Water shortages imminent across campus.",
                    "Dispatch maintenance crew immediately to repair main pipe leak.",
                    ActionKind.RepairLeak,
                    BisCatalog.WaterStandards
                );
                return CurrentAdvice;
            }

            // Priority 2: Active Crisis Incidents
            if (campaign != null)
            {
                foreach (var inc in campaign.Incidents)
                {
                    if (inc.IsActive && !inc.IsResolved)
                    {
                        CurrentAdvice = new AdvisorAdvice(
                            AdvisorPriority.Warning,
                            ResourceCategory.Crowd,
                            $"ALERT: {inc.Title}",
                            inc.Description,
                            $"Execute recommended intervention: {inc.RecommendedAction}",
                            inc.RecommendedAction,
                            GetRelevantBis(inc)
                        );
                        return CurrentAdvice;
                    }
                }
            }

            // Priority 3: Waste & Crowd Warnings
            if (state.TotalOverflowWasteKg > 15f)
            {
                CurrentAdvice = new AdvisorAdvice(
                    AdvisorPriority.Warning,
                    ResourceCategory.Waste,
                    "WARNING: Bin Overflow & Campus Litter",
                    $"{state.TotalOverflowWasteKg:0} kg of uncollected waste is overflowing from facility bins, dragging down campus cleanliness.",
                    "Dispatch waste collection fleet or expand bin capacities.",
                    ActionKind.CollectWaste,
                    BisCatalog.WasteStandards
                );
                return CurrentAdvice;
            }

            if (state.Facilities.TryGetValue("canteen", out var canteen) && canteen.QueueCount > 25)
            {
                CurrentAdvice = new AdvisorAdvice(
                    AdvisorPriority.Warning,
                    ResourceCategory.Crowd,
                    "WARNING: Severe Dining Congestion",
                    $"Canteen queue has grown to {canteen.QueueCount} waiting students, causing frustration and corridor blockages.",
                    "Open additional serving counter or stagger meal arrivals.",
                    ActionKind.ExtraCounter,
                    BisCatalog.OccupancyStandards
                );
                return CurrentAdvice;
            }

            // Priority 4: Preventative Efficiency Recommendations
            if (state.MainLeakActive)
            {
                CurrentAdvice = new AdvisorAdvice(
                    AdvisorPriority.Recommendation,
                    ResourceCategory.Water,
                    "RECOMMENDATION: Unaddressed Pipeline Leak",
                    "A continuous 85 L/min leak at the pump station is wasting clean treated water every minute.",
                    "Repair the pipe leak (₹2,200, 2 staff).",
                    ActionKind.RepairLeak,
                    BisCatalog.WaterStandards
                );
                return CurrentAdvice;
            }

            if (!state.LedInstalledAll && state.BudgetINR >= 15000)
            {
                CurrentAdvice = new AdvisorAdvice(
                    AdvisorPriority.Recommendation,
                    ResourceCategory.Energy,
                    "RECOMMENDATION: Energy Efficiency Retrofit",
                    "Replacing obsolete fixtures with LED luminaires reduces base campus electricity demand by 25%.",
                    "Commission campus LED lighting upgrade.",
                    ActionKind.LEDs,
                    BisCatalog.ElectricalStandards
                );
                return CurrentAdvice;
            }

            if (state.SegregationQuality < 0.50f && state.BudgetINR >= 10000)
            {
                CurrentAdvice = new AdvisorAdvice(
                    AdvisorPriority.Recommendation,
                    ResourceCategory.Waste,
                    "RECOMMENDATION: Segregation at Source",
                    "Current waste sorting is at 25%. Upgrading to 3-stream segregation boosts recycling recovery to 75%.",
                    "Upgrade campus segregation infrastructure.",
                    ActionKind.Segregation,
                    BisCatalog.WasteStandards
                );
                return CurrentAdvice;
            }

            // Nominal / All Systems Clear
            CurrentAdvice = new AdvisorAdvice(
                AdvisorPriority.Info,
                ResourceCategory.Crowd,
                "CAMPUS SYSTEMS STABLE",
                "Telemetry across Water, Energy, Waste, and Student Movement is within safe design boundaries.",
                "Continue monitoring telemetry and maintain budgetary discipline.",
                ActionKind.RepairLeak,
                null
            );
            return CurrentAdvice;
        }

        static BisStandardInfo GetRelevantBis(CrisisIncident inc)
        {
            if (inc.ExtraLeakLpm > 0) return BisCatalog.WaterStandards;
            if (inc.ExtraPowerDemandKw > 0) return BisCatalog.ElectricalStandards;
            if (inc.ExtraWasteKgPerMin > 0) return BisCatalog.WasteStandards;
            return BisCatalog.OccupancyStandards;
        }
    }
}
