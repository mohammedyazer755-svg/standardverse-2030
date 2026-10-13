# Campus Pulse 2030 — Implementation Plan

Prepared: 13 October 2026. Status: proposed plan; implementation has not started under this task.

## 1. Outcome and planning basis

Build a Windows Unity 3D campus-management game in which players identify visible problems, choose interventions, and manage the consequences across water, electricity, waste, overcrowding, budget, staffing, satisfaction, and sustainability. The complete product includes a three-day campaign, LAN phone controller, deterministic Future Lens, and a reliable jury demonstration.

The primary recommendation is to establish one complete, enjoyable decision loop before expanding the campus or investing in cinematic production. Prove all four challenge categories in a modest campus, then raise visual quality without rewriting the simulation.

Sources reviewed:

- `docs/CAMPUS_PULSE_2030_CODEX_MASTER_PLAN.md`, identical by SHA-256 to the supplied Downloads file.
- The supplied `Pasted text.txt` foundation brief.
- Existing project assets, package manifest, project version, and graphics configuration.

The embedded development prompts describe intended future work; they are not authorization to execute those phases in this planning task. The original competition PDF was not supplied or independently verified. Treat its quoted requirements as source-reported requirements until checked against the original rules.

## 2. Actual starting point

| Item | Observed evidence | Planning consequence |
|---|---|---|
| Project location | Unity folders exist at repository root | Keep `C:\GameDev\CampusPulse2030` as the Unity project; do not create a nested `UnityProject/` |
| Editor target | `ProjectVersion.txt`: `6000.3.26f1` | Inspect installed editor executable and Windows build support before implementation; the version file alone does not prove installation |
| Rendering | URP `17.3.0` declared; graphics settings reference a pipeline asset | Verify pipeline asset resolves and scene renders in the editor |
| Input | Input System `1.20.0` and an input-actions asset | Inspect and extend the existing actions before replacing anything |
| Other packages | Timeline `1.8.13`, AI Navigation `2.0.15`, Test Framework `1.6.0`, uGUI `2.0.0` | Verify package resolution; use existing tools where suitable |
| Camera | Cinemachine absent from manifest | Resolve a compatible package through the installed editor during foundation |
| Content | `Assets/Scenes/SampleScene.unity` and tutorial scripts | No custom gameplay implementation was found in inspected files |
| Source control | `git status` reports no repository | Initialize Git and Unity ignore rules during foundation |
| Continuity | Master brief exists; no root `AGENTS.md` or `progress.md` found | Create continuity documents during foundation |
| Runtime evidence | No editor launch, compilation, build, phone, or performance tests performed for this plan | All runtime functionality remains unverified |

## 3. Scope and delivery boundaries

### First playable foundation

One menu, one campus scene, three selectable temporary facilities, strategic camera, contextual UI, deterministic clock/state shell, pause/restart, and a Windows build. This is the first implementation assignment and its stopping point.

### Core playable prototype

One compact campus with all four systems, at least two meaningful interventions per system, visible consequences, shared budget/staff constraints, three surprises, a short complete campaign, and multiple outcomes. Desktop controls must support the entire game.

### Full showcase target

Expand to all named campus facilities; add verified phone control, Future Lens, licensed cohesive art, sound, weather, cinematics, quality presets, save/load, and rehearsed 15-minute and 5-minute demos.

### Explicitly deferred extras

BIS cards, elaborate weather, spectator phones, live external AI, complex traffic simulation, and decorative architecture beyond the brief's named locations. The advisor starts as transparent rule-based guidance. No cloud dependency is needed for play.

The six-hour competition prototype and the full showcase are separate delivery targets. The complete showcase is not a credible six-hour commitment. Organizer clarification about permitted preparation and AI assistance is a submission dependency, not a blocker to drafting this plan.

## 4. Architecture that supports the whole game

```text
Desktop Input ─┐
               ├─> Input/Command Router ─> Validated Decisions ─> Simulation
Phone → Relay ─┘                                  │                 │
                                                 │            State snapshots
                                             Decision log          │
                                                        ┌──────────┼──────────┐
                                                        World      HUD      Advisor

Simulation snapshot + scenario + explicit policies → Future Lens branches
Simulation snapshot + pending work + RNG state     → Save/load
```

### Assembly and folder boundaries

Keep implementation under `Assets/_Game/` with `Scenes`, `Scripts`, `Prefabs`, `Art`, `Audio`, `Data`, and `Tests`. Put editor utilities in `Assets/_Game/Editor`. Keep `controller-server/`, `scripts/`, and `docs/` at project root.

- **Simulation assembly:** plain C# state, units, seeded randomness, tick processing, actions, incidents, scoring, snapshot serialization contracts. No scene, physics, UI, or network dependencies.
- **Runtime assembly:** bootstrap, scene flow, input routing, presentation, camera, interactions, UI, audio, persistence adapter, and networking adapter. Separate namespaces first; split assemblies further only when it helps.
- **Editor assembly:** idempotent scene/prefab generation, validation, test/build entry points. Preserve authored assets and stable references on reruns.
- **Test assemblies:** EditMode simulation and data tests; PlayMode scene/input/integration tests.

ScriptableObjects contain authored definitions and tuning. Convert these into immutable simulation configuration. Mutable session state belongs in plain C# objects, never in reusable asset instances. Facilities, zones, incidents, and actions have stable IDs rather than scene-name lookups.

Use a small explicit composition root, not a general dependency-injection framework. Prefer typed simulation events with cause IDs over an unrestricted global event bus. Keep `Boot`, `MainMenu`, and `Campus` scenes; additive scene streaming is unnecessary for this map size.

### Deterministic tick contract

Initial tuning proposal: one simulation tick per in-game minute, with adjustable mapping to real time. Rendering interpolates between snapshots. Pause stops authoritative ticks; fast-forward changes tick throughput, not model equations.

Every tick executes in a documented order:

1. Validate and accept queued commands in stable order; reserve money/staff once.
2. Complete scheduled actions and activate scheduled external events.
3. Compute occupancy, service demands, waste generation rates, and requested pump/load operation from the tick's starting state.
4. Allocate power; compute actual powered pumping and service availability.
5. Apply water, energy, waste, and crowd flows into a separate next-state buffer.
6. Derive satisfaction, sustainability, threshold incidents, objectives, and endings.
7. Publish a snapshot and causal change records; schedule feedback effects for subsequent ticks where appropriate.

Do not allow component update order to determine outcomes. Resolve circular effects explicitly: pumping requests power; delivered power limits actual pumping; any resulting demand response happens on a defined later tick. Avoid iterating until an accidental equilibrium.

Document units: liters and liters/minute; kW versus kWh; kilograms and kilograms/minute; people and service capacity per minute; rupees; staff slots. Use scaled integers where practical and record rounding rules. Demand shortfalls are recorded, not hidden by clamping. Population is conserved across zones, queues, and transit; water and waste balances account for all sources and sinks.

### Decision lifecycle

`Available → Accepted → In progress → Completed/Failed/Cancelled`.

Each action defines target, prerequisites, cost, staffing, duration, effect, cooldown, interruption/refund policy, and any seeded risk. Repeated input cannot charge or complete an action twice. Show immediate acceptance feedback and a later completion result. UI previews label uncertain estimates and cannot bypass authoritative validation.

## 5. Playable mechanics and evidence

All starting values are fictional educational tuning, not engineering predictions.

| System | Visible identification | Initial choices | Shared consequences | Acceptance evidence |
|---|---|---|---|---|
| Water | Leak effect and falling tank level | Repair leak; low-flow upgrade; temporary rationing | Budget/staff versus water savings; rationing reduces satisfaction; pumping consumes electricity | Tank changes match ledger; leak repair reduces measured loss; powered-off pump delivers no water |
| Electricity | Lit idle building, load indicator, outage | Reschedule lab load; install LEDs; activate limited backup | Service availability, recurring fuel expense, emissions; solar varies with scenario daylight | Supply/load balance determines delivered service; energy integrates power over time |
| Waste | Filling bins, litter, collection route | Dispatch collection; improve segregation; add capacity | Staff/budget, recycling yield, temporary route obstruction | Collection moves material to processing/disposal; recycling cannot create mass or unlimited income |
| Crowds | Queue length and dense corridor | Reroute; stagger timetable; open counter | Shifted congestion; schedule inconvenience; counter energy and waste | People remain conserved; waits respond to arrival/service rates; congestion cannot disappear by changing a label |

Begin with a water repair vertical slice: select leaking facility → compare choices → confirm → reserve crew and budget → progress → stop leak → observe tank trend → explain cause. Then reuse the same command, UI, and presentation contracts for the remaining systems.

Model crowds with authoritative zone/queue counts and a lightweight route graph. Animated students illustrate these counts; NavMesh agents do not own the simulation. Keep a configurable visual-agent cap and representative density so performance cannot change outcomes. Collection follows the same principle: the simulation schedules route progress; the truck visual follows it.

## 6. Delivery sequence and phase gates

The original phase numbering is retained. Small risk experiments move earlier; full feature implementation remains in its phase. The visual ambition of Phase 1 is intentionally capped until Phase 2 proves the game loop.

| Phase | Work and deliverables | Exit gate |
|---|---|---|
| **Foundation** | Inspect editor/modules, resolve packages, establish documents/Git, create three-facility scene, camera, selection, contextual UI, state shell, automation | Editor compiles; menu-to-campus works; facilities selectable; pause/restart works; Windows build launches; record genuine evidence, then stop |
| **1: Living campus** | Stable IDs for all named locations, readable layout, representative art/material kit, focus camera, basic walkers, lighting, indicators | Every location can be focused/selected; routes readable; one representative art scene reviewed; baseline build/profile captured |
| **2: Four systems** | Complete water slice first, then electricity, waste, crowd; shared budget/staff, action lifecycle, causal UI, data tuning | Complete each category through real world interaction; verify cross-system effect, accounting, repeatability, and visual-state correspondence |
| **3: Campaign** | Three-day director, incident schedule, tutorial, advisor, scoring, endings, snapshots/save/load, demo seed | Full playthrough; three distinct surprises; at least recovery, balanced-success, and failure trajectories; save/load preserves continuation |
| **4: Phone** | Production relay/client, pairing QR, command validation, mobile UI, reconnect, launcher integration | Phone controls actual Windows game; invalid/stale/duplicate messages safely handled; disconnect restores input; record each tested device/browser |
| **5: Future Lens and polish** | Shared-engine forecasts, matched world views, full art/audio pass, Timeline sequences, accessibility, quality presets | Forecast correctness tests pass; live game unaffected; all facility visuals respond correctly; target-hardware frame-time evidence recorded |
| **6: Release** | Acceptance sweep, fresh-machine/offline launch, distribution/runtime decision, rehearsals, bug fixes, release docs | Runnable package without Unity Editor; all mandatory acceptance rows pass, or release is explicitly labeled incomplete |

### Early risk experiments

- **After foundation:** prove a phone can load a minimal locally served control page and send one command through the relay to the Windows player. Timebox to half a developer-day; record firewall/AP-isolation issues. Keep desktop development moving if physical access is unavailable.
- **During Phase 2:** prove snapshot clone + ten deterministic ticks + unchanged live state. This protects both save/load and Future Lens.
- **During Phase 1:** profile one representative block with crowd density and intended lighting before producing the full art set.

These are planned future assignments, not permission to continue beyond the separately requested foundation stage.

## 7. Campaign, balance, and scoring

Target a 10–12 minute active campaign, adjustable after playtesting, with three approximately equal days. The jury presentation additionally includes setup and explanation.

- **Day 1 — discovery:** guided leak diagnosis and repair, idle electricity load, first waste issue. Teach inspect/choose/observe and shared staffing.
- **Day 2 — pressure:** scheduled auditorium gathering creates arrivals, queues, waste, and load; a seeded disruption changes the planned response.
- **Day 3 — crisis:** heat or supply interruption meets elevated demand. Earlier upgrades and operating decisions determine recovery options.

Schedule at least three distinct surprise incidents with explicit IDs, causes, durations, and recovery paths. Guarantee coverage in `JuryDemo`; allow bounded variation in the normal campaign. Avoid unwinnable incident combinations until difficulty settings explicitly permit them.

Use separate hard outcome gates and explanatory scores. Balanced success requires completion of all four categories, viable finances, acceptable end-state satisfaction, and no sustained critical service failure. Sustainability draws from normalized cumulative water loss, grid/backup energy, recovered waste, and service outcomes; it must not be awarded simply for purchasing upgrades. Publish the score components and provisional thresholds in tuning data.

Balance using recorded policies: do nothing, spend aggressively, conserve aggressively, and balanced intervention. At least one affordable strategy must recover from each tutorial crisis. Time-averaged or cumulative penalties prevent last-second cosmetic fixes from erasing an entire day of neglect.

## 8. Phone controller design

Use one Node process on the laptop to serve bundled HTML/CSS/JS and relay WebSocket messages. Unity connects as an authenticated host client; the phone connects as a controller. Unity remains the only gameplay authority. Keep simulation objects off network threads; enqueue parsed commands for main-thread processing.

Planned pairing flow:

1. Launcher selects a reachable private interface, checks port availability, and starts the relay.
2. Unity establishes its host session using a separate ephemeral host credential.
3. Unity requests a short-lived controller invite; the QR contains the chosen LAN address and invite. Prefer a URL fragment for the invite so it is not included in ordinary HTTP request logs.
4. Phone exchanges the invite over the socket; host approves the device; relay grants a single-controller lease and invalidates the consumed invite.
5. Browser displays confirmed connection state. Unity publishes a small view model of selectable facilities and available actions.

Use camera axes plus facility cycling/list selection and confirm/back for the first mobile interface; screen-space ray targeting can follow only if usability testing justifies it. Desktop camera input overrides remote camera axes; accepted actions from either source use the same validation pipeline.

Version message schemas. Include session/lease IDs, sequence numbers, bounded payloads, and acknowledgements for discrete actions. Analog input is latest-state delivery; discrete decisions are idempotent commands. A proposed starting rate is 20 analog updates/second, with bounded server queues and a separate discrete-action limit. Tune after measurement.

Reset remote axes after 250 ms without fresh input and immediately on disconnect, lost focus, hidden browser tab, pause, or lease revocation. Use host receipt time rather than trusting the phone clock. Reconnection requests a fresh authoritative state; never replay an old input backlog.

The local HTTP/WS path assumes a trusted private LAN; it does not provide transport confidentiality. No public port forwarding. Do not depend on secure-context browser APIs; haptics are optional. Document private-profile Windows firewall rules and multi-NIC selection without disabling the firewall. Bundle local assets so airplane-mode-plus-Wi-Fi operation needs no CDN.

Required tests: invalid/expired invite, second controller, unauthorized host role, malformed/oversized/flooded messages, duplicate commands, stale sessions, screen lock, Wi-Fi loss, relay restart, and desktop fallback. Test actual Android Chrome and iOS Safari when available; browser emulation alone is insufficient evidence.

## 9. Future Lens without misleading forecasts

Capture an immutable snapshot including queued actions, time, configuration version, scenario schedule, and RNG states. Clone into branches A and B for a fixed displayed horizon.

- **A:** continue current operating policies and already accepted actions; make no additional discretionary investments.
- **B:** apply an explicitly displayed, legal alternative policy with the same money/staff constraints.

Use the same external weather and incident schedule for both branches. A shared seed alone is insufficient if one policy consumes extra random draws: precompute external events or use independent keyed random streams. Policy-dependent incidents may differ and must retain their causal explanations.

Run both branches through the same tick engine. Display differences in water shortage, energy, waste, waiting, satisfaction, and cost, with the alternative actions listed. Label these as model projections. Never mutate the live state or advance its clock while forecasting.

Start with one camera and a toggle between matched forecast snapshots. Add dual render textures or a slider after profiling. Test identical-policy equality, reproducibility, legal action budgets, projection isolation, and matching results between offline projection and normal simulation over the same inputs.

## 10. Visual quality, accessibility, and performance

Choose a cohesive stylized campus kit with consistent scale, palette, signage, and material treatment. Art approval starts with one representative block, tank, bin, and path. Use temporary geometry during foundation; replace visible placeholders before the full showcase gate.

World feedback must communicate gameplay: tank height, lamp/window state, bin fill, queue density, collection progress, and incident markers. Keep markers readable at strategic zoom and avoid conveying state by color alone. UI must explain costs, duration, staffing, and consequences before confirmation.

Use restrained lighting/post-processing, pooled effects, LODs where appropriate, capped animated crowds, and configurable shadow settings. Cinematics are skippable and cannot accept unintended gameplay input. Include subtitles for essential spoken cues, readable text, reduced motion, and independent audio controls.

Performance goal: approximately 60 FPS at 1080p on the specified laptop. Record development/release build type, quality preset, power mode, crowd count, resolution, CPU/GPU frame times, and frame-time spikes. A proposed acceptance target is median frame time at or below 16.7 ms and 95th percentile below 22 ms over a representative five-minute run after warmup; revise only with an explicit quality decision. Profile Future Lens separately. These are targets, not observed results.

## 11. Validation and release evidence

| Layer | Required checks |
|---|---|
| Simulation | Fixed-input determinism; water/waste/population balance; kW-to-kWh conversion; action cost exactly once; staff release; outage/pump causality; threshold and ending logic |
| Persistence | Round trip; pending action restoration; RNG continuation; schema-version handling; missing/corrupt save recovery |
| Forecasts | Equal policies yield equal results; matched external events; isolated clones; normal-run equivalence |
| Unity integration | Menu flow; camera bounds and focus; UI blocks world clicks; selection; pause/restart; state-driven visual updates; scene transitions |
| Controller | Pairing lifecycle; role validation; schema/rate limits; duplicate/stale input; reconnect and loss-of-focus safety |
| Release | Windows build launch; no critical exceptions; packaged runtime; no internet dependency; phone and desktop walkthroughs |

Store evidence under a documented location with test result XML, build logs, screenshots where actually captured, and manual test records. Use `Planned`, `Implemented—not verified`, `Verified`, and `Blocked` consistently. A missing physical phone test cannot be converted into a passing acceptance item by documenting it.

Save files should include schema/config versions and use atomic replacement with a recoverable previous copy. Demo checkpoints are actual snapshots, not arbitrary UI flags; skipping to a checkpoint must reconstruct the corresponding world state.

The launcher must detect port conflicts, show connection errors, permit desktop-only launch, and shut down only relay processes it owns. Decide whether to bundle a redistributable Node runtime or require installation before Phase 6; test the actual distributed choice on a clean Windows user profile or machine.

## 12. Effort and six-hour event variant

Planning estimates for the full showcase, assuming experienced contributors and a cohesive licensed art kit:

| Work | Developer-days |
|---|---:|
| Foundation | 2–3 |
| Living campus and early feasibility experiments | 3–5 |
| Four interconnected systems | 5–8 |
| Campaign, tuning, persistence | 3–5 |
| Production phone controller and device verification | 3–5 |
| Future Lens, art/audio, performance | 5–8 |
| Integration, release, rehearsal | 3–4 |
| **Base total** | **24–38** |

Reserve a further 20–30% for integration, asset issues, networking, and playtest revisions. Developer-days are effort, not elapsed days; dependencies prevent perfect division across a team. Re-estimate after the first complete water slice. If working as a team, useful ownership areas are simulation/campaign, Unity interaction/presentation, and controller/release. This is a staffing recommendation, not a request to spawn agents.

For a strict six-hour event, first confirm which templates, assets, prior code, and AI assistance are permitted. Use only allowed starting materials. Prioritize a compact desktop game demonstrating all four categories; describe omitted showcase features honestly.

| Event time | Delivery target |
|---|---|
| 00:00–00:30 | Agree minimal data/contracts, verify permitted project, build immediately |
| 00:30–01:30 | Camera, selection, contextual choices, shared state, first water loop |
| 01:30–03:00 | Electricity, waste, crowd choices and visible consequences |
| 03:00–04:00 | Three scripted surprises, shared constraints, short win/loss loop |
| 04:00–04:45 | Readability, feedback, basic audio; optional phone proof only if core already passes |
| 04:45–05:15 | Freeze features, fix integration, build and test |
| 05:15–06:00 | Package, verify fresh launch, rehearse, preserve submission buffer |

This schedule is an aggressive prototype timebox, not a promise of the full master brief. If behind, remove cinematics, elaborate crowds, forecasting presentation, and phone control before removing a required challenge category. Do not advertise the reduced prototype as a completed showcase.

## 13. Main risks and decisions

| Risk | Early mitigation | Fallback |
|---|---|---|
| Visual production consumes gameplay time | Approve one art block; prove core loop before expanding | Cohesive smaller campus for prototype |
| Network blocks live pairing | Early real-device LAN experiment | Full desktop demo, with phone marked unavailable |
| Non-deterministic forecasting | Pure tick engine, explicit event streams, early clone test | Delay visual comparison until calculations pass |
| Crowds dominate CPU cost | Zone-based authority and capped visual agents | Reduce visible density while preserving actual counts |
| Player cannot understand causality | Decision preview and cause-linked outcomes | Reduce simultaneous incidents and tutorial complexity |
| Six-hour scope mismatch | Separate event and showcase targets | Cut optional features transparently |
| Asset licensing or package incompatibility | Record provenance; lock resolved versions | Authored procedural kit and already verified packages |

## 14. Concrete first implementation assignment

Execute **Foundation only** when development is requested:

1. Read the master brief and this plan; inspect any newly added instructions or files.
2. Locate the installed editor, verify Windows build support and package resolution, and capture a baseline compile. Avoid batch-opening the project while it is already open in another editor instance.
3. Create `AGENTS.md`, `progress.md`, and a canonical specification reference; preserve the original master brief. Document the root-level Unity layout explicitly.
4. Initialize Git with Unity ignore rules; preserve `.meta`, package lock, and project settings files. Add architecture, acceptance, asset-license, and launch documentation.
5. Create simulation/runtime/editor/test boundaries and the session lifecycle.
6. Generate/save a minimal campus scene with three stable-ID facilities using actual editor APIs.
7. Implement desktop camera, selection/highlight, contextual UI, pause, restart, and menu flow.
8. Add initial deterministic state and tick shell plus focused tests for pause, repeatability, and command rejection.
9. Run compilation, relevant EditMode/PlayMode checks, and a Windows build smoke test. Record exact executable paths, commands, result files, and blockers.
10. Update `progress.md` with verified versus unverified work, the build location, known issues, and the next three actions. Stop before Phase 1, consistent with the supplied foundation scope.

Foundation is complete only when its real runtime checks pass. If editor/device access blocks a check, preserve useful work and mark the gate incomplete rather than claiming success.
