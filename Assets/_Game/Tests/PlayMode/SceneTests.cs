using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CampusPulse;

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
        Assert.LessOrEqual(Mathf.Abs(session.Rig.Target.x),76);
        Assert.IsNull(session.Pick(Vector2.zero,true));
        LogAssert.NoUnexpectedReceived();
    }
}
