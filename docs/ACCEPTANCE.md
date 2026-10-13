# Acceptance evidence

Status starts unverified. Consult progress.md and actual Logs/Campus outputs; implemented code alone is not a pass.

## Recorded foundation result — 13 October 2026

Four EditMode and one PlayMode test passed. Windows development build succeeded with zero errors and zero warnings. Visible standalone smoke test passed for three facilities at 1920x1080, including ray selection, pause and screenshots. Evidence: `Logs/Campus/Foundation/`. This verifies the foundation only. Newly added Phase 1 tests have not run.

The separate benchmark C# compilation passed, but Unity import, benchmark screenshot review and Phase 1 performance testing are blocked by the Windows Security prompt described in progress.md. No Phase 1 completion claim is made.

## Foundation gate
- Editor compilation succeeds.
- At least three uniquely identified facilities exist in a saved scene.
- Menu starts session; select/highlight/focus and bounded camera work.
- UI pointer blocking prevents world selection through panels.
- Pause halts the clock; restart restores state.
- EditMode and PlayMode results captured.
- Windows BuildPipeline succeeds and executable launches without critical exceptions.

## Phase 1 gate
- Academic block, laboratories, library, administration, canteen, auditorium, hostel, parking, roads, paths, gardens, water/pumps, solar/electricity and recycling exist.
- Representative block is rendered and reviewed before expansion.
- All major facilities have stable unique IDs, state records and selection/focus.
- Initial resource metrics are explicitly labeled illustrative.
- Walkers, vehicle routes, wind and infrastructure presentation function without owning simulation state.
- Visual population derives from authoritative counts with a cap.
- Measured representative-block performance includes resolution, graphics device and sample duration.
- Updated full-scene PlayMode checks, Windows build, and actual screenshot reviewed.

Phase 2 gameplay is excluded from this gate.
