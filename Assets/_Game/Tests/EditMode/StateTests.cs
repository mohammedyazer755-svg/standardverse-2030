using System;
using NUnit.Framework;
using CampusPulse.Simulation;

public sealed class StateTests
{
    [Test] public void PausedClockDoesNotAdvance()
    {
        var state = new CampusState { Paused = true }; state.Tick(); Assert.AreEqual(480, state.Minute);
        state.Paused = false; state.Tick(); Assert.AreEqual(481, state.Minute);
    }
    [Test] public void RepeatedClockInputsProduceSameState()
    {
        var a = new CampusState(2030); var b = new CampusState(2030);
        for(int i=0;i<100;i++) { a.Paused = b.Paused = i % 3 == 0; a.Tick(); b.Tick(); }
        Assert.AreEqual(a.Minute,b.Minute); Assert.AreEqual(a.Seed,b.Seed);
    }
    [Test] public void DuplicateIdsRejected()
    {
        var s = new CampusState(); var f = new FacilityState("lab","Lab","",100,40,2,3,4); s.Add(f);
        Assert.Throws<ArgumentException>(() => s.Add(f));
    }
    [Test] public void CrowdVisualsAreCappedWithoutChangingPopulation()
    {
        var s = new CampusState(); s.Add(new FacilityState("lab","Lab","",500,320,2,3,4));
        Assert.AreEqual(12,s.VisualCount("lab")); Assert.AreEqual(0,s.VisualCount("lab",0)); Assert.AreEqual(320,s.Facilities["lab"].Occupants);
    }
}
