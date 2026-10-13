using System;
using System.Collections.Generic;
using System.Text;

namespace CampusPulse.Simulation
{
    public enum CampaignChapter
    {
        Chapter1_NormalDay = 1,
        Chapter2_PressureBuilds = 2,
        Chapter3_CampusCrisis = 3,
        Completed = 4
    }

    public enum IncidentSeverity { Minor, Moderate, Severe, Critical }

    public enum CampaignEnding
    {
        SustainableChampion,
        BalancedManager,
        CostlyRecovery,
        CampusCrisis
    }

    [Serializable]
    public sealed class CampaignObjective
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ResourceCategory Category { get; set; }
        public bool IsCompleted { get; set; }

        public CampaignObjective() { }

        public CampaignObjective(string id, string title, string description, ResourceCategory category)
        {
            Id = id;
            Title = title;
            Description = description;
            Category = category;
            IsCompleted = false;
        }

        public CampaignObjective Clone()
        {
            return new CampaignObjective(Id, Title, Description, Category) { IsCompleted = IsCompleted };
        }
    }

    [Serializable]
    public sealed class CrisisIncident
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IncidentSeverity Severity { get; set; }
        public string AffectedFacilityId { get; set; }
        public int TriggerMinute { get; set; }
        public int DurationMinutes { get; set; }

        // Multipliers and direct modifiers applied while active
        public float ExtraLeakLpm { get; set; }
        public float ExtraPowerDemandKw { get; set; }
        public float ExtraWasteKgPerMin { get; set; }
        public int ExtraArrivalsPerMin { get; set; }

        public ActionKind RecommendedAction { get; set; }
        public bool IsActive { get; set; }
        public bool IsResolved { get; set; }

        public CrisisIncident() { }

        public CrisisIncident(string id, string title, string description, IncidentSeverity severity,
            string affectedFacility, int triggerMinute, int durationMinutes,
            float extraLeakLpm, float extraPowerKw, float extraWasteKg, int extraArrivals,
            ActionKind recommendedAction)
        {
            Id = id;
            Title = title;
            Description = description;
            Severity = severity;
            AffectedFacilityId = affectedFacility;
            TriggerMinute = triggerMinute;
            DurationMinutes = durationMinutes;
            ExtraLeakLpm = extraLeakLpm;
            ExtraPowerDemandKw = extraPowerKw;
            ExtraWasteKgPerMin = extraWasteKg;
            ExtraArrivalsPerMin = extraArrivals;
            RecommendedAction = recommendedAction;
            IsActive = false;
            IsResolved = false;
        }

        public CrisisIncident Clone()
        {
            return new CrisisIncident(Id, Title, Description, Severity, AffectedFacilityId,
                TriggerMinute, DurationMinutes, ExtraLeakLpm, ExtraPowerDemandKw, ExtraWasteKgPerMin,
                ExtraArrivalsPerMin, RecommendedAction)
            {
                IsActive = IsActive,
                IsResolved = IsResolved
            };
        }
    }

    [Serializable]
    public sealed class CampaignScoreReport
    {
        public int ResourceEfficiencyScore; // 0-100
        public int BudgetManagementScore;    // 0-100
        public int StudentSatisfactionScore; // 0-100
        public int CampusSustainabilityScore; // 0-100
        public int CampusResilienceScore;    // 0-100
        public int CompositeScore;           // 0-100

        public CampaignEnding Ending;
        public string EndingTitle;
        public string EndingSummary;

        public float WaterSavedPercent;
        public float SolarUtilizedPercent;
        public float WasteRecycledPercent;
        public int RemainingBudgetINR;
        public int ResolvedIncidentsCount;
        public int TotalIncidentsCount;
    }

    public sealed class CampusCampaign
    {
        public CampaignChapter CurrentChapter { get; private set; } = CampaignChapter.Chapter1_NormalDay;
        public int ChapterStartMinute { get; private set; } = 480; // 08:00
        public int ChapterDurationMinutes { get; } = 180; // 3 hours per chapter (480-660, 660-840, 840-1020)

        readonly List<CampaignObjective> objectives = new List<CampaignObjective>();
        public IReadOnlyList<CampaignObjective> Objectives => objectives;

        readonly List<CrisisIncident> incidents = new List<CrisisIncident>();
        public IReadOnlyList<CrisisIncident> Incidents => incidents;

        public CampaignScoreReport FinalReport { get; private set; }

        public bool ChapterCompleted { get; private set; }
        public bool CampaignCompleted => CurrentChapter == CampaignChapter.Completed;

        // Cumulative tracking for multi-criteria scoring
        public int TotalTicks { get; private set; }
        public int TotalSatisfactionAccumulator { get; private set; }
        public int TotalSustainabilityAccumulator { get; private set; }
        public int DeficitTicks { get; private set; }
        public int WaterShortageTicks { get; private set; }
        public int OverflowTicks { get; private set; }

        public CampusCampaign()
        {
            InitChapter(CampaignChapter.Chapter1_NormalDay);
        }

        public void InitChapter(CampaignChapter chapter, CampusState state = null)
        {
            CurrentChapter = chapter;
            ChapterCompleted = false;
            objectives.Clear();
            incidents.Clear();

            switch (chapter)
            {
                case CampaignChapter.Chapter1_NormalDay:
                    ChapterStartMinute = 480; // 08:00 AM
                    if (state != null) state.Minute = ChapterStartMinute;
                    objectives.Add(new CampaignObjective("c1_leak", "Stop Water Loss", "Repair the 85 L/min pipe leak at Pump Station.", ResourceCategory.Water));
                    objectives.Add(new CampaignObjective("c1_energy", "Efficiency Retrofit", "Complete LED lighting or Low-Flow fixtures retrofit.", ResourceCategory.Energy));
                    objectives.Add(new CampaignObjective("c1_waste", "Sanitation Routine", "Dispatch a waste collection round before bins overflow.", ResourceCategory.Waste));
                    objectives.Add(new CampaignObjective("c1_satisfaction", "Student Welfare", "Maintain student satisfaction at or above 70%.", ResourceCategory.Crowd));

                    // Minor incident: Lab power surge during morning setup
                    incidents.Add(new CrisisIncident("c1_inc_lab", "Morning Lab Calibration Surge",
                        "Research labs initiated high-current sensor calibration, increasing local load.",
                        IncidentSeverity.Minor, "research", 510, 30, 0f, 25f, 0f, 0, ActionKind.RescheduleLabs));
                    break;

                case CampaignChapter.Chapter2_PressureBuilds:
                    ChapterStartMinute = 660; // 11:00 AM
                    if (state != null) state.Minute = ChapterStartMinute;
                    objectives.Add(new CampaignObjective("c2_grid", "Maintain Grid Balance", "Prevent electrical power deficit during symposium hours.", ResourceCategory.Energy));
                    objectives.Add(new CampaignObjective("c2_crowd", "Relieve Dining Congestion", "Keep canteen lunch queue under 25 students.", ResourceCategory.Crowd));
                    objectives.Add(new CampaignObjective("c2_waste", "Clean Campus Standard", "Increase bin capacity or upgrade waste segregation.", ResourceCategory.Waste));
                    objectives.Add(new CampaignObjective("c2_budget", "Fiscal Discipline", "Retain at least INR 60,000 in emergency reserves.", ResourceCategory.Water));

                    // Incidents for Chapter 2
                    incidents.Add(new CrisisIncident("c2_inc_symposium", "Symposium Auditorium Surge",
                        "Inter-College Symposium causes dense student influx and rapid waste generation.",
                        IncidentSeverity.Moderate, "auditorium", 690, 60, 0f, 35f, 1.8f, 10, ActionKind.ExtraCounter));

                    incidents.Add(new CrisisIncident("c2_inc_heatwave", "Afternoon Heatwave Strain",
                        "High ambient temperature spikes cooling power demand and water consumption.",
                        IncidentSeverity.Severe, "academic", 750, 45, 0f, 45f, 0f, 0, ActionKind.ReduceLoad));
                    break;

                case CampaignChapter.Chapter3_CampusCrisis:
                    ChapterStartMinute = 840; // 14:00 PM
                    if (state != null) state.Minute = ChapterStartMinute;
                    objectives.Add(new CampaignObjective("c3_contain_burst", "Contain Main Pipeline Burst", "Repair critical pipeline rupture and engage rationing.", ResourceCategory.Water));
                    objectives.Add(new CampaignObjective("c3_stabilize_power", "Avert Grid Blackout", "Activate emergency backup generator or shed loads.", ResourceCategory.Energy));
                    objectives.Add(new CampaignObjective("c3_crowd_comfort", "Prevent Crowd Disorder", "Divert overflow students into reflection garden.", ResourceCategory.Crowd));
                    objectives.Add(new CampaignObjective("c3_sustainability", "Achieve Resilience", "Conclude campaign with Satisfaction >= 60% and Sustainability >= 60%.", ResourceCategory.Waste));

                    // Major cascading crisis incidents
                    incidents.Add(new CrisisIncident("c3_inc_burst", "Critical Mainline Pipeline Rupture",
                        "Underground supply conduit ruptured near Water Utility! Severe 120 L/min loss threatening reservoir collapse.",
                        IncidentSeverity.Critical, "water", 855, 60, 120f, 0f, 0f, 0, ActionKind.RepairLeak));

                    incidents.Add(new CrisisIncident("c3_inc_blackout", "Transformer Substation Trip",
                        "Emergency boost pumps and evening laboratory loads exceed grid safety threshold, tripping substation breaker.",
                        IncidentSeverity.Critical, "substation", 880, 50, 0f, 65f, 0f, 0, ActionKind.Backup));
                    break;

                case CampaignChapter.Completed:
                    break;
            }
        }

        public void Tick(CampusState state)
        {
            if (CampaignCompleted) return;

            TotalTicks++;
            TotalSatisfactionAccumulator += state.Satisfaction;
            TotalSustainabilityAccumulator += state.Sustainability;

            if (state.PowerDeficitKW > 2f) DeficitTicks++;
            if (state.WaterShortageLiters > 5f) WaterShortageTicks++;
            if (state.TotalOverflowWasteKg > 10f) OverflowTicks++;

            // 1. Process Crisis Incidents
            foreach (var inc in incidents)
            {
                if (!inc.IsResolved)
                {
                    if (state.Minute >= inc.TriggerMinute && state.Minute < inc.TriggerMinute + inc.DurationMinutes)
                    {
                        inc.IsActive = true;
                    }
                    else if (state.Minute >= inc.TriggerMinute + inc.DurationMinutes)
                    {
                        inc.IsActive = false;
                        inc.IsResolved = true;
                    }

                    // Check early resolution via player actions
                    if (inc.IsActive && CheckIncidentEarlyResolution(inc, state))
                    {
                        inc.IsActive = false;
                        inc.IsResolved = true;
                    }
                }
            }

            // 2. Evaluate Chapter Objectives
            EvaluateObjectives(state);

            // 3. Check Chapter Expiry / Completion
            int chapterElapsed = state.Minute - ChapterStartMinute;
            bool allObjectivesMet = objectives.Count > 0 && objectives.TrueForAll(o => o.IsCompleted);

            if (chapterElapsed >= ChapterDurationMinutes || (chapterElapsed >= 60 && allObjectivesMet))
            {
                ChapterCompleted = true;
            }
        }

        public bool AdvanceToNextChapter(CampusState state)
        {
            if (CurrentChapter == CampaignChapter.Chapter1_NormalDay)
            {
                InitChapter(CampaignChapter.Chapter2_PressureBuilds, state);
                return true;
            }
            if (CurrentChapter == CampaignChapter.Chapter2_PressureBuilds)
            {
                InitChapter(CampaignChapter.Chapter3_CampusCrisis, state);
                return true;
            }
            if (CurrentChapter == CampaignChapter.Chapter3_CampusCrisis)
            {
                ComputeFinalReport(state);
                CurrentChapter = CampaignChapter.Completed;
                return true;
            }
            return false;
        }

        bool CheckIncidentEarlyResolution(CrisisIncident inc, CampusState state)
        {
            switch (inc.Id)
            {
                case "c1_inc_lab":
                    return state.LabsRescheduled;
                case "c2_inc_symposium":
                    return state.ExtraCounterActive || state.StaggerArrivalsActive;
                case "c2_inc_heatwave":
                    return state.LoadSheddingUntilMinute > 0 || state.LedInstalledAll;
                case "c3_inc_burst":
                    return !state.MainLeakActive;
                case "c3_inc_blackout":
                    return state.BackupActive || state.PowerDeficitKW <= 1f;
                default:
                    return false;
            }
        }

        void EvaluateObjectives(CampusState state)
        {
            foreach (var obj in objectives)
            {
                if (obj.IsCompleted) continue;

                switch (obj.Id)
                {
                    case "c1_leak":
                        if (!state.MainLeakActive) obj.IsCompleted = true;
                        break;
                    case "c1_energy":
                        if (state.LedInstalledAll || state.LowFlowInstalledAll) obj.IsCompleted = true;
                        break;
                    case "c1_waste":
                        if (state.RecycledWasteKg > 10f || state.LandfillWasteKg > 10f) obj.IsCompleted = true;
                        break;
                    case "c1_satisfaction":
                        if (state.Satisfaction >= 70 && state.Minute >= ChapterStartMinute + 30) obj.IsCompleted = true;
                        break;

                    case "c2_grid":
                        if (state.Minute >= ChapterStartMinute + 60 && state.PowerDeficitKW <= 2f) obj.IsCompleted = true;
                        break;
                    case "c2_crowd":
                        if (state.Facilities.TryGetValue("canteen", out var cant) && cant.QueueCount < 25) obj.IsCompleted = true;
                        break;
                    case "c2_waste":
                        if (state.SegregationQuality >= 0.70f || state.Facilities["canteen"].BinCapacityKg > 80f) obj.IsCompleted = true;
                        break;
                    case "c2_budget":
                        if (state.BudgetINR >= 60000) obj.IsCompleted = true;
                        break;

                    case "c3_contain_burst":
                        if (!state.MainLeakActive || state.RationingUntilMinute > 0) obj.IsCompleted = true;
                        break;
                    case "c3_stabilize_power":
                        if (state.BackupActive || (state.Minute >= ChapterStartMinute + 45 && state.PowerDeficitKW <= 2f)) obj.IsCompleted = true;
                        break;
                    case "c3_crowd_comfort":
                        if (state.GardenDiversionActive || state.OverflowSeatingActive) obj.IsCompleted = true;
                        break;
                    case "c3_sustainability":
                        if (state.Satisfaction >= 60 && state.Sustainability >= 60) obj.IsCompleted = true;
                        break;
                }
            }
        }

        public void ComputeFinalReport(CampusState state)
        {
            var rep = new CampaignScoreReport();

            // 1. Resource Efficiency Score (Water, Energy, Waste)
            float waterEfficiency = 100f - Math.Min(100f, (state.CumulativeWaterLeakedLiters / Math.Max(1f, state.CumulativeWaterPumpedLiters + state.WaterStoredLiters)) * 100f);
            float energyEfficiency = state.LedInstalledAll ? 90f : 70f;
            if (state.LabsRescheduled) energyEfficiency += 10f;
            float wasteEfficiency = state.SegregationQuality * 100f;

            rep.ResourceEfficiencyScore = (int)Math.Round((waterEfficiency + energyEfficiency + wasteEfficiency) / 3f);
            rep.ResourceEfficiencyScore = Math.Max(0, Math.Min(100, rep.ResourceEfficiencyScore));

            // 2. Budget Score
            rep.RemainingBudgetINR = state.BudgetINR;
            rep.BudgetManagementScore = Math.Min(100, Math.Max(0, (int)(state.BudgetINR / 1500f)));

            // 3. Student Satisfaction Score
            int meanSat = TotalTicks > 0 ? TotalSatisfactionAccumulator / TotalTicks : state.Satisfaction;
            rep.StudentSatisfactionScore = Math.Min(100, Math.Max(0, (meanSat + state.Satisfaction) / 2));

            // 4. Sustainability Score
            int meanSust = TotalTicks > 0 ? TotalSustainabilityAccumulator / TotalTicks : state.Sustainability;
            rep.CampusSustainabilityScore = Math.Min(100, Math.Max(0, (meanSust + state.Sustainability) / 2));

            // 5. Resilience Score (Penalized by deficits, shortages, and overflow)
            float resilience = 100f;
            if (DeficitTicks > 5) resilience -= DeficitTicks * 2.5f;
            if (WaterShortageTicks > 5) resilience -= WaterShortageTicks * 3f;
            if (OverflowTicks > 5) resilience -= OverflowTicks * 2f;
            rep.CampusResilienceScore = Math.Min(100, Math.Max(0, (int)resilience));

            // 6. Composite Score
            rep.CompositeScore = (rep.ResourceEfficiencyScore * 25 + rep.BudgetManagementScore * 15 +
                rep.StudentSatisfactionScore * 25 + rep.CampusSustainabilityScore * 25 + rep.CampusResilienceScore * 10) / 100;

            // Incident Metrics
            rep.TotalIncidentsCount = 5;
            int resolved = 0;
            foreach (var inc in incidents) if (inc.IsResolved) resolved++;
            rep.ResolvedIncidentsCount = resolved + 3; // across chapters

            rep.WaterSavedPercent = Math.Min(100f, waterEfficiency);
            rep.SolarUtilizedPercent = Math.Min(100f, (state.SolarGenerationKW / Math.Max(1f, state.TotalPowerDemandKW)) * 100f);
            rep.WasteRecycledPercent = state.TotalWasteGeneratedKg > 0 ? (state.RecycledWasteKg / state.TotalWasteGeneratedKg) * 100f : 50f;

            // 7. Outcome Determination
            if (rep.CompositeScore >= 72 && rep.StudentSatisfactionScore >= 68 && rep.CampusSustainabilityScore >= 65 && state.BudgetINR >= 20000)
            {
                rep.Ending = CampaignEnding.SustainableChampion;
                rep.EndingTitle = "SUSTAINABLE CAMPUS CHAMPION";
                rep.EndingSummary = "Flawless leadership! The campus weathered peak symposium loads and emergency pipeline ruptures with zero blackouts. Indian Standards compliance preserved natural resources while keeping student morale at an all-time high.";
            }
            else if (rep.CompositeScore >= 55 && rep.StudentSatisfactionScore >= 50 && rep.CampusSustainabilityScore >= 48 && state.BudgetINR >= 15000)
            {
                rep.Ending = CampaignEnding.BalancedManager;
                rep.EndingTitle = "BALANCED CAMPUS MANAGER";
                rep.EndingSummary = "Solid, practical operations. Interventions contained major crises with acceptable trade-offs between student comfort and institutional budget. Essential utilities remained stable.";
            }
            else if (state.BudgetINR < 15000 && rep.StudentSatisfactionScore >= 45)
            {
                rep.Ending = CampaignEnding.CostlyRecovery;
                rep.EndingTitle = "COSTLY RECOVERY";
                rep.EndingSummary = "The campus survived the 3-day crisis, but at severe financial cost. Excessive emergency measures exhausted fiscal reserves, highlighting the need for preventative maintenance.";
            }
            else
            {
                rep.Ending = CampaignEnding.CampusCrisis;
                rep.EndingTitle = "CAMPUS CRISIS COLLAPSE";
                rep.EndingSummary = "Cascading utility failures overwhelmed management. Uncontained pipe bursts and electrical deficits disrupted classes and dining services, forcing an emergency campus shutdown.";
            }

            FinalReport = rep;
        }

        // --- Active Incident Modifiers ---
        public float GetActiveExtraLeakLpm()
        {
            float extra = 0f;
            foreach (var inc in incidents) if (inc.IsActive) extra += inc.ExtraLeakLpm;
            return extra;
        }

        public float GetActiveExtraPowerKw()
        {
            float extra = 0f;
            foreach (var inc in incidents) if (inc.IsActive) extra += inc.ExtraPowerDemandKw;
            return extra;
        }

        public float GetActiveExtraWasteKg()
        {
            float extra = 0f;
            foreach (var inc in incidents) if (inc.IsActive) extra += inc.ExtraWasteKgPerMin;
            return extra;
        }

        public int GetActiveExtraArrivals()
        {
            int extra = 0;
            foreach (var inc in incidents) if (inc.IsActive) extra += inc.ExtraArrivalsPerMin;
            return extra;
        }

        // --- Save and Load Serialization ---
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"chapter\":{(int)CurrentChapter},");
            sb.Append($"\"chapterStartMinute\":{ChapterStartMinute},");
            sb.Append($"\"totalTicks\":{TotalTicks},");
            sb.Append($"\"deficitTicks\":{DeficitTicks},");
            sb.Append($"\"waterShortageTicks\":{WaterShortageTicks},");
            sb.Append($"\"overflowTicks\":{OverflowTicks},");
            sb.Append($"\"satAcc\":{TotalSatisfactionAccumulator},");
            sb.Append($"\"sustAcc\":{TotalSustainabilityAccumulator}");
            sb.Append("}");
            return sb.ToString();
        }

        public void LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                int ch = ExtractInt(json, "\"chapter\":");
                if (ch >= 1 && ch <= 4)
                {
                    InitChapter((CampaignChapter)ch);
                    ChapterStartMinute = ExtractInt(json, "\"chapterStartMinute\":");
                    TotalTicks = ExtractInt(json, "\"totalTicks\":");
                    DeficitTicks = ExtractInt(json, "\"deficitTicks\":");
                    WaterShortageTicks = ExtractInt(json, "\"waterShortageTicks\":");
                    OverflowTicks = ExtractInt(json, "\"overflowTicks\":");
                    TotalSatisfactionAccumulator = ExtractInt(json, "\"satAcc\":");
                    TotalSustainabilityAccumulator = ExtractInt(json, "\"sustAcc\":");
                }
            }
            catch { }
        }

        static int ExtractInt(string json, string key)
        {
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return 0;
            int start = idx + key.Length;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
            return int.TryParse(json.Substring(start, end - start), out int val) ? val : 0;
        }
    }
}
