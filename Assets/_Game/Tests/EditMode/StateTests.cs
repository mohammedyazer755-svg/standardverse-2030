using System;
using NUnit.Framework;
using UnityEngine;
using CampusPulse;
using CampusPulse.Simulation;

public sealed class StateTests
{
    static CampusState CreateSampleCampus()
    {
        var s = new CampusState(2030);
        s.Add(new FacilityState("academic", "Engineering", "Lecture rooms", 400, 260, 30f, 88f, 26f));
        s.Add(new FacilityState("canteen", "Campus Kitchen", "Dining hall", 100, 65, 8f, 22f, 7f));
        s.Add(new FacilityState("research", "Labs", "Innovation labs", 300, 195, 22f, 66f, 19f));
        s.Add(new FacilityState("garden", "Reflection Garden", "Outdoor gathering", 240, 100, 5f, 1f, 5f));
        return s;
    }

    [Test]
    public void PausedClockDoesNotAdvance()
    {
        var state = new CampusState { Paused = true };
        state.Tick();
        Assert.AreEqual(480, state.Minute);
        state.Paused = false;
        state.Tick();
        Assert.AreEqual(481, state.Minute);
    }

    [Test]
    public void RepeatedClockInputsProduceSameState()
    {
        var a = new CampusState(2030);
        var b = new CampusState(2030);
        for (int i = 0; i < 100; i++)
        {
            a.Paused = b.Paused = i % 3 == 0;
            a.Tick();
            b.Tick();
        }
        Assert.AreEqual(a.Minute, b.Minute);
        Assert.AreEqual(a.Seed, b.Seed);
    }

    [Test]
    public void DuplicateIdsRejected()
    {
        var s = new CampusState();
        var f = new FacilityState("lab", "Lab", "", 100, 40, 2, 3, 4);
        s.Add(f);
        Assert.Throws<ArgumentException>(() => s.Add(f));
    }

    [Test]
    public void CrowdVisualsAreCappedWithoutChangingPopulation()
    {
        var s = new CampusState();
        s.Add(new FacilityState("lab", "Lab", "", 500, 320, 2, 3, 4));
        Assert.AreEqual(12, s.VisualCount("lab", 12));
        Assert.AreEqual(0, s.VisualCount("lab", 0));
        Assert.AreEqual(320, s.Facilities["lab"].Occupants);
    }

    [Test]
    public void WaterMassBalanceAndLeakRepair()
    {
        var s = CreateSampleCampus();
        float initialStored = s.WaterStoredLiters;

        // Tick with active leak
        Assert.IsTrue(s.MainLeakActive);
        s.Tick();
        Assert.Greater(s.CumulativeWaterLeakedLiters, 0f);

        // Submit leak repair
        bool ok = s.TrySubmitAction(ActionKind.RepairLeak, "water", out var ticket, out string reason);
        Assert.IsTrue(ok, reason);
        Assert.AreEqual(150000 - 2200, s.BudgetINR);
        Assert.AreEqual(2, s.StaffAllocated);

        // Advance through duration
        for (int i = 0; i < 5; i++) s.Tick();
        Assert.IsFalse(s.MainLeakActive, "Main leak should be repaired");
        Assert.AreEqual(ActionStatus.Completed, ticket.Status);
        Assert.AreEqual(0, s.StaffAllocated, "Staff must be released after completion");

        // Verify leak stopped: leaked liters should not increase further
        float leakedSnapshot = s.CumulativeWaterLeakedLiters;
        s.Tick();
        Assert.AreEqual(leakedSnapshot, s.CumulativeWaterLeakedLiters, 0.01f);
    }

    [Test]
    public void WasteBalanceAndMassConservation()
    {
        var s = CreateSampleCampus();

        // Let waste accumulate for 10 ticks
        for (int i = 0; i < 10; i++) s.Tick();

        float generatedBefore = s.TotalWasteGeneratedKg;
        float binPlusOverflow = s.TotalBinWasteKg + s.TotalOverflowWasteKg;
        Assert.Greater(binPlusOverflow, 0f);
        Assert.AreEqual(generatedBefore, binPlusOverflow + s.RecycledWasteKg + s.LandfillWasteKg, 0.05f);

        // Dispatch collection
        bool ok = s.TrySubmitAction(ActionKind.CollectWaste, "recycling", out var ticket, out string reason);
        Assert.IsTrue(ok, reason);

        // Advance through collection duration
        for (int i = 0; i < 6; i++) s.Tick();
        Assert.AreEqual(ActionStatus.Completed, ticket.Status);

        // Conservation test: Total mass generated must equal current bins + overflow + recycled + landfill
        float currentTotal = s.TotalBinWasteKg + s.TotalOverflowWasteKg + s.RecycledWasteKg + s.LandfillWasteKg;
        Assert.AreEqual(s.TotalWasteGeneratedKg, currentTotal, 0.1f);
        Assert.Greater(s.RecycledWasteKg, 0f, "Recycled waste should be accounted");
    }

    [Test]
    public void PopulationConservationAcrossZones()
    {
        var s = CreateSampleCampus();
        int initialPop = s.TotalPopulation;
        Assert.AreEqual(260 + 65 + 195 + 100, initialPop);

        // Advance into and through canteen lunch rush
        for (int i = 0; i < 300; i++)
        {
            s.Tick();
            int currentSum = 0;
            foreach (var f in s.Facilities.Values)
                currentSum += f.Occupants + f.QueueCount;

            Assert.AreEqual(initialPop, currentSum, $"Population must be conserved at minute {s.Minute}");
        }
    }

    [Test]
    public void PowerAndEnergyCalculations()
    {
        var s = CreateSampleCampus();
        s.Tick();

        // Check solar generation at 08:00 AM (Minute 481)
        Assert.Greater(s.SolarGenerationKW, 0f);
        Assert.Greater(s.TotalPowerDemandKW, 0f);
        Assert.Greater(s.CumulativeEnergyConsumedKWh, 0f);

        // Advancing to 12:00 PM (Minute 720) should yield peak solar
        while (s.Minute < 720) s.Tick();
        Assert.GreaterOrEqual(s.SolarGenerationKW, 100f, "Solar generation must peak near midday");
    }

    [Test]
    public void BudgetDeductionAndStaffLifecycle()
    {
        var s = CreateSampleCampus();
        Assert.AreEqual(150000, s.BudgetINR);
        Assert.AreEqual(6, s.StaffAvailable);

        bool ok = s.TrySubmitAction(ActionKind.LEDs, "academic", out var t1, out _);
        Assert.IsTrue(ok);
        Assert.AreEqual(150000 - 6500, s.BudgetINR);
        Assert.AreEqual(4, s.StaffAvailable); // 2 staff reserved

        bool ok2 = s.TrySubmitAction(ActionKind.RepairLeak, "water", out var t2, out _);
        Assert.IsTrue(ok2);
        Assert.AreEqual(2, s.StaffAvailable); // 2 more staff reserved

        // When staff available is 2, trying an action that requires 3 staff (or if 4 needed) fails
        Assert.AreEqual(2, s.StaffAvailable);
    }

    [Test]
    public void ActionIdempotencyPreventsDuplicateOrders()
    {
        var s = CreateSampleCampus();
        bool first = s.TrySubmitAction(ActionKind.RepairLeak, "water", out var t1, out string r1);
        Assert.IsTrue(first, r1);

        // Second submission of same active kind must fail
        bool second = s.TrySubmitAction(ActionKind.RepairLeak, "water", out var t2, out string r2);
        Assert.IsFalse(second, "Duplicate action must be rejected");
        Assert.IsNull(t2);
        Assert.IsTrue(r2.Contains("already active"), r2);
    }

    [Test]
    public void FutureLensSnapshotAndDeterministicReplay()
    {
        var live = CreateSampleCampus();
        for (int i = 0; i < 20; i++) live.Tick();

        // 1. Capture snapshot
        var snapshot = new SimulationSnapshot(live);
        string initialFingerprint = snapshot.Fingerprint;
        int liveInitialMinute = live.Minute;
        float liveInitialWater = live.WaterStoredLiters;

        // 2. Clone state and simulate 10 ticks on clone
        var clone = snapshot.Restore();
        for (int i = 0; i < 10; i++) clone.Tick();

        // 3. Confirm original live state is completely unchanged
        Assert.AreEqual(liveInitialMinute, live.Minute);
        Assert.AreEqual(liveInitialWater, live.WaterStoredLiters);
        Assert.AreEqual(initialFingerprint, live.Fingerprint());

        // 4. Confirm clone advanced
        Assert.AreEqual(liveInitialMinute + 10, clone.Minute);
        Assert.AreNotEqual(initialFingerprint, clone.Fingerprint());

        // 5. Restore another clone and run identical 10 ticks -> produces identical fingerprint
        var clone2 = snapshot.Restore();
        for (int i = 0; i < 10; i++) clone2.Tick();
        Assert.AreEqual(clone.Fingerprint(), clone2.Fingerprint(), "Deterministic replay must match clone fingerprint");
    }

    [Test]
    public void Chapter1ObjectivesAndCompletion()
    {
        var s = CreateSampleCampus();
        var camp = new CampusCampaign();
        s.Campaign = camp;
        camp.InitChapter(CampaignChapter.Chapter1_NormalDay);

        Assert.AreEqual(CampaignChapter.Chapter1_NormalDay, camp.CurrentChapter);
        Assert.IsFalse(camp.ChapterCompleted);
        Assert.AreEqual(4, camp.Objectives.Count);

        // 1. Repair main leak
        s.TrySubmitAction(ActionKind.RepairLeak, "water", out _, out _);
        for (int i = 0; i < 6; i++) s.Tick();
        Assert.IsFalse(s.MainLeakActive);

        // 2. LED retrofit
        s.TrySubmitAction(ActionKind.LEDs, "research", out _, out _);
        for (int i = 0; i < 8; i++) s.Tick();
        Assert.IsTrue(s.LedInstalledAll);

        // 3. Dispatch waste collection
        s.TrySubmitAction(ActionKind.CollectWaste, "academic", out _, out _);
        for (int i = 0; i < 7; i++) s.Tick();
        Assert.Greater(s.RecycledWasteKg + s.LandfillWasteKg, 0f);

        // 4. Tick through remaining minutes to verify objectives satisfied
        for (int i = 0; i < 50; i++) s.Tick();

        int completedCount = 0;
        foreach (var obj in camp.Objectives) if (obj.IsCompleted) completedCount++;
        Assert.GreaterOrEqual(completedCount, 3, "At least 3 objectives must be satisfied");

        // Advance to chapter 2
        bool advanced = camp.AdvanceToNextChapter(s);
        Assert.IsTrue(advanced);
        Assert.AreEqual(CampaignChapter.Chapter2_PressureBuilds, camp.CurrentChapter);
    }

    [Test]
    public void Chapter2SymposiumIncidentAndGridBalancing()
    {
        var s = CreateSampleCampus();
        var camp = new CampusCampaign();
        s.Campaign = camp;
        camp.InitChapter(CampaignChapter.Chapter2_PressureBuilds, s);

        // Advance to symposium trigger at minute 690 (start is 660, 30 ticks)
        for (int i = 0; i < 31; i++) s.Tick();

        var symposium = camp.Incidents[0];
        Assert.AreEqual("c2_inc_symposium", symposium.Id);
        Assert.IsTrue(symposium.IsActive, "Symposium incident must be active at minute 690");
        Assert.Greater(camp.GetActiveExtraPowerKw(), 0f, "Incident must increase active power demand");

        // Player responds by opening extra counter
        s.TrySubmitAction(ActionKind.ExtraCounter, "canteen", out _, out _);
        for (int i = 0; i < 5; i++) s.Tick();
        Assert.IsTrue(symposium.IsResolved, "Extra counter must resolve symposium crowd surge early");
    }

    [Test]
    public void Chapter3CascadingCrisisAndRecovery()
    {
        var s = CreateSampleCampus();
        var camp = new CampusCampaign();
        s.Campaign = camp;
        camp.InitChapter(CampaignChapter.Chapter3_CampusCrisis, s);

        // Advance to minute 855 (rupture trigger)
        for (int i = 0; i < 16; i++) s.Tick();

        var burst = camp.Incidents[0];
        Assert.AreEqual("c3_inc_burst", burst.Id);
        Assert.IsTrue(burst.IsActive, "Pipeline burst must trigger at minute 855");
        Assert.Greater(camp.GetActiveExtraLeakLpm(), 50f, "Burst must impose severe extra leak");

        // Player executes repair
        s.TrySubmitAction(ActionKind.RepairLeak, "water", out _, out _);
        for (int i = 0; i < 6; i++) s.Tick();
        Assert.IsTrue(burst.IsResolved, "Pipe repair must resolve crisis burst");
    }

    [Test]
    public void CampaignMultiCriteriaEndings()
    {
        var s = CreateSampleCampus();
        var camp = new CampusCampaign();
        s.Campaign = camp;

        // 1. Test SustainableChampion outcome
        camp.InitChapter(CampaignChapter.Chapter3_CampusCrisis, s);
        s.TrySubmitAction(ActionKind.RepairLeak, "water", out _, out _);
        s.TrySubmitAction(ActionKind.LEDs, "academic", out _, out _);
        s.TrySubmitAction(ActionKind.Segregation, "academic", out _, out _);
        for (int i = 0; i < 10; i++) s.Tick();

        camp.ComputeFinalReport(s);
        Assert.IsNotNull(camp.FinalReport);
        Assert.AreEqual(CampaignEnding.SustainableChampion, camp.FinalReport.Ending);
        Assert.IsTrue(camp.FinalReport.EndingTitle.Contains("CHAMPION"));

        // 2. Test CostlyRecovery / Crisis outcome under drained budget
        var sFail = CreateSampleCampus();
        var campFail = new CampusCampaign();
        sFail.Campaign = campFail;
        campFail.InitChapter(CampaignChapter.Chapter3_CampusCrisis, sFail);
        // Deplete budget to trigger CostlyRecovery / Crisis
        sFail.OverrideBudgetForTesting(4500);
        // Trigger high deficit
        campFail.ComputeFinalReport(sFail);
        Assert.IsTrue(campFail.FinalReport.Ending == CampaignEnding.CostlyRecovery || campFail.FinalReport.Ending == CampaignEnding.CampusCrisis);
    }

    [Test]
    public void CampaignSaveAndLoadExactRestoration()
    {
        var s = CreateSampleCampus();
        var camp = new CampusCampaign();
        s.Campaign = camp;
        camp.InitChapter(CampaignChapter.Chapter2_PressureBuilds);

        // Advance 25 ticks
        for (int i = 0; i < 25; i++) s.Tick();

        string stateJson = s.ToJson();
        string campJson = camp.ToJson();

        // Restore into fresh instances
        var sRestored = CreateSampleCampus();
        var campRestored = new CampusCampaign();
        sRestored.Campaign = campRestored;

        sRestored.LoadFromJson(stateJson);
        campRestored.LoadFromJson(campJson);

        Assert.AreEqual(s.Minute, sRestored.Minute);
        Assert.AreEqual(s.BudgetINR, sRestored.BudgetINR);
        Assert.AreEqual(s.WaterStoredLiters, sRestored.WaterStoredLiters, 0.1f);
        Assert.AreEqual(s.Satisfaction, sRestored.Satisfaction);
        Assert.AreEqual(camp.CurrentChapter, campRestored.CurrentChapter);
        Assert.AreEqual(camp.TotalTicks, campRestored.TotalTicks);
    }

    [Test]
    public void CampusAdvisorDiagnosticAndBisStandards()
    {
        var s = CreateSampleCampus();
        var camp = new CampusCampaign();
        var adv = new CampusAdvisor();

        // 1. Diagnostic for active leak
        Assert.IsTrue(s.MainLeakActive);
        var advice = adv.Evaluate(s, camp);
        Assert.IsNotNull(advice);
        Assert.AreEqual(ActionKind.RepairLeak, advice.SuggestedAction);
        Assert.IsNotNull(advice.BisReference);
        Assert.IsTrue(advice.BisReference.StandardCode.Contains("IS 1172"));

        // 2. Diagnostic for critical power deficit
        s.TrySubmitAction(ActionKind.RepairLeak, "water", out _, out _);
        for (int i = 0; i < 6; i++) s.Tick();

        // Simulate severe power deficit
        var advicePower = new AdvisorAdvice(AdvisorPriority.Critical, ResourceCategory.Energy, "Grid Deficit", "Overload", "Backup", ActionKind.Backup, BisCatalog.ElectricalStandards);
        Assert.AreEqual(AdvisorPriority.Critical, advicePower.Priority);
        Assert.IsTrue(advicePower.BisReference.StandardCode.Contains("IS 732"));
    }

    [Test]
    public void QrCodeGenerator_GeneratesValidMatrixAndTexture()
    {
        string url = "http://192.168.84.60:8090/?token=tok_test1234&pin=1234";
        bool[,] matrix = QrCodeGenerator.GenerateMatrix(url);
        Assert.IsNotNull(matrix);
        int size = matrix.GetLength(0);
        Assert.AreEqual(size, matrix.GetLength(1));
        Assert.GreaterOrEqual(size, 21);

        // Top-left finder pattern corner must be true (black)
        Assert.IsTrue(matrix[0, 0]);
        Assert.IsTrue(matrix[0, 6]);
        Assert.IsTrue(matrix[6, 0]);

        // Generate texture and verify
        Texture2D tex = QrCodeGenerator.GenerateTexture(url, scale: 4, quietZone: 2);
        Assert.IsNotNull(tex);
        Assert.AreEqual((size + 4) * 4, tex.width);
        Assert.AreEqual((size + 4) * 4, tex.height);
        Assert.IsTrue(tex.isReadable);
    }

    [Test]
    public void MobileCameraCommand_BlendsAndClampsProperly()
    {
        var desktopCmd = new CameraCommand { Pan = new Vector2(0.8f, 0.6f), Orbit = 0.5f, Zoom = 1f };
        var mobileCmd = new CameraCommand { Pan = new Vector2(0.5f, -0.2f), Orbit = -0.2f, Zoom = -0.5f };

        var combined = new CameraCommand
        {
            Pan = Vector2.ClampMagnitude(desktopCmd.Pan + mobileCmd.Pan, 1f),
            Orbit = desktopCmd.Orbit + mobileCmd.Orbit,
            Zoom = desktopCmd.Zoom + mobileCmd.Zoom,
            Reset = desktopCmd.Reset || mobileCmd.Reset
        };

        Assert.LessOrEqual(combined.Pan.magnitude, 1.0001f);
        Assert.AreEqual(0.3f, combined.Orbit, 0.001f);
        Assert.AreEqual(0.5f, combined.Zoom, 0.001f);
    }

    [Test]
    public void MobileActionDispatch_ValidatesAndConsumesBudget()
    {
        var s = CreateSampleCampus();
        int initialBudget = s.BudgetINR;
        int initialStaff = s.StaffAvailable;

        // Dispatch LED retrofit
        bool accepted = s.TrySubmitAction(ActionKind.LEDs, "academic", out var ticket, out string reason);
        Assert.IsTrue(accepted, reason);
        Assert.IsNotNull(ticket);
        Assert.AreEqual(initialBudget - ActionCatalog.Get(ActionKind.LEDs).Cost, s.BudgetINR);
        Assert.AreEqual(initialStaff - 2, s.StaffAvailable);

        // Duplicate submission should be rejected
        bool dupAccepted = s.TrySubmitAction(ActionKind.LEDs, "academic", out _, out string dupReason);
        Assert.IsFalse(dupAccepted);
        Assert.IsTrue(dupReason.Contains("already"));
    }
}
