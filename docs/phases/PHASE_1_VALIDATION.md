# Phase 1 validation log — in progress

13 October 2026, Asia/Calcutta.

## Benchmark pass 1

- Unity imported the pending Phase 1 scripts and generated the three-building academic benchmark.
- Windows build succeeded: zero errors/warnings; 169,728,394 bytes.
- Visible 1920x1080 standalone smoke passed all three facility picks, pause and guarded selection. 43 capped student visuals were active.
- Twelve-second uncapped diagnostic: median 2.70 ms, p95 4.47 ms on RTX 4060 Laptop / i7-13650HX. The editor/test runner was also active; this is an early diagnostic, not sustained profiling.
- Reviewed actual academic render against campus reference: façade depth, courtyard, service details and landscaping are present. The kit is stylized; it is not photorealistic reference parity.
- Found mirrored/oversized world signage and roof surface occlusion by the top slab. Corrected generator rotation/character size and roof height; regenerate and review before expansion.
- PlayMode: one passed, one failed. Walking population/movement/pause passed. Header-raycast test used a fixed pixel point outside the header at small Game-view sizes; changed it to the actual header center. Rerun pending.
- Evidence: `docs/evidence/Benchmark-Initial/`. These are the initial render and measurements, not evidence for corrected source.

## Exact next action

Regenerate the corrected benchmark, run the scene tests, rebuild and capture. Only then expand the remaining campus facilities. Phase 2 implementation waits for the Phase 1 gate.

## Benchmark correction accepted

- Corrected generator compiled/imported and regenerated successfully.
- Two PlayMode tests passed: menu/selection/focus/pause/reset/bounds/UI raycast and population-derived walker movement/pause.
- Windows build passed with zero errors/warnings. Standalone smoke passed; actual 1080p render reviewed: roof surface and signage defects corrected.
- Evidence: `docs/evidence/Benchmark-Corrected/`. 43 walkers; twelve-second diagnostic median 2.99 ms, p95 4.75 ms. Same hardware and diagnostic caveats as above.
- Expansion now authored: seven campus buildings, water/pump area, substation, solar array, recycling yard, parking and garden. Full-campus generation and verification in progress.
