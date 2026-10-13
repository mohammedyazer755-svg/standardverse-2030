using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CampusPulse;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public sealed class SceneTests
{
    [UnityTest] public IEnumerator FoundationInteractionAndPause()
    {
        yield return SceneManager.LoadSceneAsync("Campus"); yield return null;
        var session = Object.FindFirstObjectByType<CampusSession>();
        Assert.NotNull(session); Assert.GreaterOrEqual(session.Facilities.Length,3);
        Assert.AreEqual(SessionMode.Menu,session.Mode); session.Begin();
        foreach(var facility in session.Facilities)
        {
            session.Select(facility); session.FocusSelected();
            Assert.AreSame(facility,session.Selected);
            Assert.IsTrue(facility.Highlight.gameObject.activeSelf);
        }
        session.TogglePause(); int minute = session.State.Minute;
        yield return new WaitForSeconds(1.1f); Assert.AreEqual(minute, session.State.Minute);
        session.Restart(); Assert.AreEqual(480,session.State.Minute); Assert.IsNull(session.Selected);
        session.Rig.Apply(new CameraCommand {Pan = new Vector2(10000,10000),Zoom=10000},1);
        yield return new WaitForSeconds(1);
        Assert.LessOrEqual(session.Rig.Distance,171); Assert.GreaterOrEqual(session.Rig.Distance,29);
        Assert.LessOrEqual(Mathf.Abs(session.Rig.Target.x),session.Rig.Bounds.x+1);
        Assert.IsNull(session.Pick(Vector2.zero,true));
        var uiHits = new List<RaycastResult>();
        var header=(RectTransform)session.Hud.transform.Find("Campus Interface/Header");
        var headerPixel=RectTransformUtility.WorldToScreenPoint(null,header.TransformPoint(header.rect.center));
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = headerPixel },uiHits);
        Assert.Greater(uiHits.Count,0,"Header must receive UI raycasts");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator LivingCampusPresentationFollowsState()
    {
        yield return SceneManager.LoadSceneAsync("Campus"); yield return null;
        var session=Object.FindFirstObjectByType<CampusSession>();
        var life=Object.FindFirstObjectByType<CampusLife>();
        Assert.NotNull(life,"Generate the Phase 1 benchmark/campus before running this suite");
        int expected=0;
        foreach(var f in session.Facilities) expected+=session.State.VisualCount(f.Id,18);
        Assert.AreEqual(Mathf.Min(life.MaximumWalkers,expected),life.ActiveWalkers);
        Assert.Greater(life.ActiveWalkers,0);
        var walker=life.transform.GetChild(0); var initial=walker.position;
        session.Begin(); yield return new WaitForSeconds(.3f);
        Assert.Greater(Vector3.Distance(initial,walker.position),.01f);
        session.TogglePause(); var paused=walker.position; yield return new WaitForSeconds(.2f);
        Assert.AreEqual(paused,walker.position);
        var ids=new HashSet<string>();
        foreach(var f in session.Facilities) { Assert.IsTrue(ids.Add(f.Id)); Assert.IsTrue(session.State.Facilities.ContainsKey(f.Id)); }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator FullCampusHasEveryRequiredFacility()
    {
        yield return SceneManager.LoadSceneAsync("Campus"); yield return null;
        var session=Object.FindFirstObjectByType<CampusSession>();
        foreach(var id in new[]{"academic","research","library","administration","canteen","auditorium","hostel","water","power","solar","recycling","parking","garden"})
            Assert.IsTrue(session.State.Facilities.ContainsKey(id),"Missing facility: "+id);
        Assert.AreEqual(13,session.Facilities.Length);
        var life=Object.FindFirstObjectByType<CampusLife>();
        Assert.Greater(life.SolarTables.Length,0); Assert.Greater(life.Rotors.Length,0); Assert.NotNull(life.Vehicle);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator PlayableResourceInterventionsAndStateSync()
    {
        yield return SceneManager.LoadSceneAsync("Campus"); yield return null;
        var session = Object.FindFirstObjectByType<CampusSession>();
        Assert.NotNull(session);
        session.Begin();
        Assert.IsTrue(session.State.MainLeakActive);

        // Submit Repair Main Leak action
        bool ok = session.State.TrySubmitAction(CampusPulse.Simulation.ActionKind.RepairLeak, "water", out var ticket, out string reason);
        Assert.IsTrue(ok, reason);
        Assert.AreEqual(CampusPulse.Simulation.ActionStatus.Accepted, ticket.Status);

        // Advance 5 simulation ticks
        for (int i = 0; i < 5; i++) session.State.Tick();
        Assert.AreEqual(CampusPulse.Simulation.ActionStatus.Completed, ticket.Status);
        Assert.IsFalse(session.State.MainLeakActive, "Leak must be resolved");

        // Submit LED Retrofit
        bool okLed = session.State.TrySubmitAction(CampusPulse.Simulation.ActionKind.LEDs, "academic", out var ledTicket, out _);
        Assert.IsTrue(okLed);
        for (int i = 0; i < 7; i++) session.State.Tick();
        Assert.IsTrue(session.State.LedInstalledAll);

        yield return null;
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator CampaignProgressionAdvisorAndSaveLoad()
    {
        yield return SceneManager.LoadSceneAsync("Campus"); yield return null;
        var session = Object.FindFirstObjectByType<CampusSession>();
        Assert.NotNull(session);
        session.Begin();

        Assert.NotNull(session.Campaign);
        Assert.NotNull(session.Advisor);
        Assert.AreEqual(CampusPulse.Simulation.CampaignChapter.Chapter1_NormalDay, session.Campaign.CurrentChapter);

        // Advance chapter to Chapter 2
        session.PlayChapter(CampusPulse.Simulation.CampaignChapter.Chapter2_PressureBuilds);
        Assert.AreEqual(CampusPulse.Simulation.CampaignChapter.Chapter2_PressureBuilds, session.Campaign.CurrentChapter);

        // Advisor test
        var advice = session.Advisor.Evaluate(session.State, session.Campaign);
        Assert.NotNull(advice);
        Assert.NotNull(advice.Headline);

        // Save & Load
        session.SaveGame();
        Assert.IsTrue(System.IO.File.Exists(session.GetSavePath()));
        bool loaded = session.LoadGame();
        Assert.IsTrue(loaded);

        yield return null;
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator MobileControllerClientInitializationAndQrModal()
    {
        yield return SceneManager.LoadSceneAsync("Campus"); yield return null;
        var session = Object.FindFirstObjectByType<CampusSession>();
        Assert.NotNull(session);
        session.Begin();

        // 1. Verify ControllerClient attached
        Assert.NotNull(session.ControllerClient);
        Assert.AreEqual(8090, session.ControllerClient.ServerPort);

        // 2. Open QR Modal
        Assert.NotNull(session.Hud);
        session.Hud.ToggleQrModal();
        yield return null;

        // 3. Verify camera command integration
        var initialCamTarget = session.Rig.Target;
        session.Rig.Apply(new CameraCommand { Pan = new Vector2(1f, 0f) }, 0.5f);
        yield return null;
        Assert.AreNotEqual(initialCamTarget.x, session.Rig.Target.x);

        session.Hud.ToggleQrModal();
        yield return null;
        LogAssert.NoUnexpectedReceived();
    }
}
