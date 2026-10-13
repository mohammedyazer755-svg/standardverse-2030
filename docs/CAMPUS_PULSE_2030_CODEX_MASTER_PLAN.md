# CAMPUS PULSE 2030 — THE CAMPUS CRISIS SIMULATOR
## Unity 3D Game Master Brief + Codex Astra Foundation Prompt + Six Phase Prompts

**Tagline:** One Campus. Four Crises. Every Decision Shapes Tomorrow.
**Selected official CODE2PLAY statement:** Problem Statement 1 — SAVE THE CAMPUS.
**Target PC:** Lenovo Legion 5i, Intel Core i7-13700HX, NVIDIA RTX 4060 Laptop GPU, Windows.
**Target controller:** Android/iPhone using QR-scanned, browser-based touch controller over local Wi-Fi/hotspot.
**Game engine:** Installed stable Unity 6 LTS release, URP, C#, Input System, Cinemachine, Timeline, Shader Graph and carefully selected URP-compatible post-processing.
**Primary player display:** Unity Windows standalone game on laptop or projector. **The phone is a remote controller, not a promise of running the complete 3D game inside a mobile browser.**

## A. SOURCE-FAITHFUL OFFICIAL REQUIREMENTS — NON-NEGOTIABLE
The supplied CODE2PLAY PDF (p. 4) says:

> College campuses face everyday challenges such as excessive water and electricity consumption, poor waste management, overcrowding, and inefficient use of resources. These problems can be turned into an engaging game where players take responsibility for managing a virtual campus. Players must make decisions, allocate resources, and respond to unexpected situations while keeping students happy and the campus sustainable. The objective is to create a fun game that subtly teaches players about responsible resource management and sustainable campus practices.

> Challenge: Create a game that allows players to identify and solve common campus problems while maintaining a balance between resources, sustainability, and student satisfaction.

Convert EVERY part of this into playable functionality, not static informational panels:
1. Water use and scarcity, including visible leaks, tanks, consumption and conservation solutions.
2. Electricity demand and supply, power-hungry equipment, efficiency upgrades and solar generation.
3. Waste management: bin fill levels, segregation, recycling and collection routes.
4. Overcrowding: queues, dense pathways, canteen/auditorium congestion and rerouting.
5. Inefficient resource use: limited budget, staffing, capacity, time and tradeoffs.
6. Player-directed problem identification, physical selection, interventions and consequence monitoring.
7. Unexpected situations that affect one or multiple campus systems.
8. Student satisfaction that reacts to living conditions and player actions.
9. Sustainability and resource stewardship as measurable, meaningful victory conditions.

Other supplied rules (PDF p. 2): teams 2–3; ONE selected statement; development in the 6-hour event; originality required; use any suitable engine; submit runnable prototype/demo; judging involves innovation, creativity, functionality, problem solving and presentation. **Do not misrepresent pre-event work as competition-period development. Ask organizers what preparation and AI coding assistance are allowed before submitting.** This plan is a design/development resource, not permission to bypass competition rules.

## B. DETAILED ABSTRACT
**CAMPUS PULSE 2030: The Campus Crisis Simulator** is an interactive, cinematic 3D sustainability and resource-management game designed around the official CODE2PLAY “Save the Campus” challenge. The player takes responsibility for running a living virtual college campus faced with excessive water consumption, rising electrical demand, unmanaged waste, overcrowding, and constrained resources. The game turns everyday campus management problems into decisions with visible consequences, creating an engaging way to learn sustainable practices while protecting student satisfaction.

Instead of navigating an ordinary web dashboard, the player explores a professionally rendered campus with teaching blocks, laboratories, hostels, a canteen, an auditorium, solar installations, substations, water tanks, waste processing and pedestrian paths. Students move between locations, queues expand or shrink, rooftop solar panels generate simulated energy, tanks visibly fill and drain, bins overflow if left unmanaged, and building lighting changes as electricity conditions evolve. An animated campus director interface and a diegetic digital assistant notify the player of problems without removing their control of the world.

Gameplay is built around a single, interconnected campus simulation. Each decision affects a shared budget, water reserves and consumption, power supply and demand, waste accumulation, pedestrian congestion, sustainability score, and student satisfaction. Repairing leaks saves water but costs funds; operating more pumps can relieve a shortage while increasing power demand; scheduling a waste collection can clear bins but consume money and temporarily increase congestion; redirecting students can relieve one corridor while burdening another. Unpredictable but reproducible events such as sudden heat, power disruption, a major college event or water-system failure compel players to plan, prioritize and respond. The user wins by maintaining a functioning, environmentally responsible, student-friendly campus over an in-game multi-day challenge, not merely by clicking the most expensive upgrades.

The game's visual direction is a polished, stylized-realistic 3D management simulation with real-time building illumination, animated crowds, moving service vehicles, water and electrical infrastructure effects, dynamic weather, cinematic camera transitions and responsive environmental sound. All important gameplay actions are expressed through genuine world-state changes. Interactive tutorials, short missions, camera highlights, contextual cards and progress feedback make gameplay understandable in minutes and engaging to watch during a jury demo.

Its key innovations add depth while remaining directly tied to the official problem. **Chain Reaction Crisis** makes campus systems causally dependent, turning a water intervention into a potential power problem, for example. **Campus Pulse** is a contextual advisor explaining why problems occur and showing possible tradeoffs without automatically making decisions. **Future Lens** compares two simulated futures of the SAME campus: continuation of the player's current strategy and an alternative responsible management strategy under comparable events, using matched 3D views and actual simulation calculations. An optional **BIS Learning Layer** surfaces verified Indian Standards where genuinely relevant—for example drinking-water quality—without replacing the core campus game or claiming fictional activities confer official BIS certification.

A distinctive live-presentation feature is a **QR-paired smartphone controller**. The Unity 3D game runs natively on the player's Windows laptop and can be projected to a screen. A judge scans an on-screen QR code with an Android or iPhone browser while on the same private local network, connects through a short-lived session, and controls the camera, selects campus facilities and makes resource decisions from touch controls. This system provides a tangible, interactive demonstration with no game installation on the phone. Keyboard/mouse support remains available if network connectivity fails.

The result should be a complete game with three short in-game days, four core resource challenge categories, a connected decision engine, meaningful success and failure paths, high-quality cinematic presentation, a working mobile controller, and a 15-minute guided jury demo. The game is intended to demonstrate that sustainable campus management requires systems thinking: interventions must balance resource efficiency, social wellbeing, time and economic constraints. All numerical models are clearly presented as fictional educational simulations rather than calibrated predictions of a real college.

## C. REQUIRED GAME EXPERIENCE AND CREATIVE DIRECTION
### World and camera
- Single cohesive campus with academic block, labs, hostel, canteen, auditorium, garden, solar array, substation, two water tanks/pump house, recycling yard and roads/walkways.
- One polished art direction: stylized-realistic, premium PBR materials, varied architecture, trees and foliage, readable signage, purposeful props, believable proportions. Avoid cubes as final assets.
- Strategic isometric orbit/zoom/pan camera, with smooth guided close-up/focus to specific facilities. Optional guided cinematic camera flythrough, NOT an uncontrolled open-world project.
- Students follow nav paths, forming queues, clustering during events and reacting to disruptions. At minimum use convincing animated crowd agents or optimization-friendly impostors with real congestion state.
- Early morning, midday, evening changes during three in-game days; visible solar/lighting changes; localized weather effects.

### Main game loop
Inspect campus -> spot a problem (environment and indicator) -> travel/focus to facility -> examine 2–3 tradeoff-based interventions -> spend resources -> animate intervention -> show cross-system effect -> handle follow-on event -> reach day summary and final campus outcome.

### Four playable systems (all required)
- **Water:** reservoir level, daily consumption, leak locations, pumping energy usage, leak repairs, low-flow installations, water rationing with satisfaction costs.
- **Energy:** building loads, peak demand, solar generation, power availability, lab schedules, LED efficiency, backup and power failure impacts.
- **Waste:** waste generation, capacity of bins, segregation quality, collection logistics, recycling yield and litter/student satisfaction.
- **Crowd:** per-zone occupancy/capacity, queue times, hall transitions, timetable staggering, alternate routing, canteen extra counters with energy/budget impacts.

### Shared state and model
Central deterministic seeded simulation in C# with pure/calculable systems; do not tie numeric state directly to animations. Suggested state: simulatedDay, timeOfDay, budgetINR, studentSatisfaction0to100, sustainability0to100, waterStoredLiters, waterDemandLitersPerTick, powerSupplyKW, powerDemandKW, renewableEnergyShare, wasteKG, wasteCapacityKG, segregationRate, crowdDensityPerZone, averageWaitMinutes, staffCapacity, openIncidents, decisionHistory. Choose reasonable documented illustrative units and values; NEVER claim they are accurate to a real campus. Decisions require cost, delay, effect, cooldown, risk, feedback, and consequences.

### Story and events
- **Day 1 / Discovery:** obvious water leak, unnecessary lights and small overflowing bin, tutorial for detecting, resolving and observing.
- **Day 2 / Pressure:** auditorium crowd surge + canteen queue + waste buildup + extra electricity demand; multiple simultaneous priorities.
- **Day 3 / Crisis:** unexpected water shortage or power instability paired with high student demand, leading to a connected decision-based finale.
- A full playthrough should cover water, energy, waste and crowd and expose both sustainability and satisfaction tradeoffs.
- Win conditions must test balanced campus wellbeing and reasonable budget; multiple endings, not binary scripted video.

### Signature features
1. **Living Campus:** world actually responds (lights turn off/on, tank indicators, bin overflow, moving trucks, crowd density and pathing).
2. **Chain Reaction Crisis:** controlled seeded cross-system interactions and explicit cause/effect feedback.
3. **Future Lens:** same geometry and camera, compare actual projected outcome with an alternative trajectory; calculations must share the same simulation engine, not merely two textures.
4. **Campus Pulse Advisor:** contextual explanation of the tradeoffs, no false claim of an external AI model if rule-based.
5. **Optional BIS Learning Layer:** short cards referencing only verified applicable Indian Standards using official BIS portal, with accurate scope. It is an enhancement; the organizer's campus gameplay takes priority.

### Visual/audio quality bar
- Build real Unity scenes using URP (not HTML faux-3D or a single static render); use imported licensed models if available or well-authored procedural geometry as fallback.
- Camera rig via Cinemachine, story sequences via Timeline, scene/object animation via Animator and script-driven motion, shader effects via Shader Graph and URP-compatible effects.
- Proper color grading, soft shadows, SSAO if suitable, restrained bloom, reflection probes, volumetric-style atmospheric cues within URP feasibility, credible day/night lighting, environmental audio and subtle music.
- Signature animations: water leaks, dynamic tank gauges/water surface, solar arrays, animated power demand/glowing lines, waste truck pickup, crowds dispersing, emergency light flicker, weather and final sustainable campus restoration.
- Do not blindly add screen shake, excessive bloom, arbitrary particles or unlicensed assets. No invented visual-verification claims.
- Quality presets High/Medium/Low, default 1080p targeting stable ~60 FPS on the supplied Legion laptop, with profiler evidence and fallback settings.

### Controls and mobile pairing
- Keyboard/mouse desktop fallback always works.
- Unity Windows player + local Node.js HTTP/WebSocket relay in `controller-server/` (or a proven equivalent). Phone connects to the relay and relay sends validated commands to Unity. Choose one connection architecture and document it explicitly.
- QR overlay generated from **current reachable LAN IPv4 + controller port + short-lived random pairing token**. Do not encode `localhost` or fake IPs. Allow manually selecting the active IP if multiple NICs exist.
- Phone browser responsive interface works with current Android Chrome and iOS Safari; no dedicated application needed. Simple touch joystick/camera pan, facility selection, inspect/interact, confirm/back and pause; visible connection state and low-latency input.
- On host device start service bound to intended private interface; phones on the same Wi-Fi or laptop hotspot. Explicit private-network Windows firewall setup instructions. No public port forwarding, no permanent secret in QR, no outside cloud dependence.
- One active controller; second device may be a spectator only if explicitly implemented. Validate token, expire/rotate sessions, reject malformed input, rate-limit messages. On disconnect, release held controls and revert to mouse/keyboard; no stuck joystick.
- Test with a real Android phone AND iOS Safari if available; otherwise document untested device rather than claiming success. Add connection-loss and stale-command tests.
- Mobile webpage is for CONTROLLING the laptop's 3D game; mobile full-game browser rendering is NOT in scope.

### 15-minute jury demonstration
- Show a short city intro and state goal (1–2 min); open QR and let judge connect (1 min); repair a water leak with visible power/budget tradeoff (2 min); control power and waste (2 min); solve a crowd emergency and unexpected chain reaction (3 min); show Future Lens comparing two simulated futures (2 min); show end scores/standards insight, technical architecture and answer Q&A (remaining time).
- Provide deterministic `JuryDemo` presets, fast-forward, skip cutscenes, quick reset, no network fallback and a 5-minute condensed walkthrough.

## D. ARCHITECTURE AND ACCEPTANCE CHECKLIST
### Proposed repo structure
```
CampusPulse2030/
  README.md
  progress.md
  docs/GAME_SPEC.md
  docs/ARCHITECTURE.md
  docs/ACCEPTANCE.md
  docs/DEMO_SCRIPT.md
  docs/ASSET_LICENSES.md
  UnityProject/
    Assets/_Game/Scenes/
    Assets/_Game/Scripts/Core/
    Assets/_Game/Scripts/Simulation/
    Assets/_Game/Scripts/Missions/
    Assets/_Game/Scripts/Interaction/
    Assets/_Game/Scripts/Visuals/
    Assets/_Game/Scripts/Network/
    Assets/_Game/Prefabs/
    Assets/_Game/Art/
    Assets/_Game/Audio/
    Assets/_Game/Tests/
    Assets/Editor/
    Packages/
    ProjectSettings/
  controller-server/
    package.json
    src/
    public/
    tests/
  scripts/
  builds/ (ignored or selectively distributed)
```
Align Unity paths with the actual editor version and project creation process. Never fake Unity-generated scene or meta files. Use valid Editor scripts to generate and save scenes/prefabs programmatically where helpful. Git-ignore `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, binaries and secrets, but keep all Unity Assets and ProjectSettings.

### Definition of DONE
1. Windows Unity build launches into a visually coherent 3D campus.
2. Camera controls and facility interactions work, with mouse/keyboard and mobile controller.
3. ALL FOUR official challenge categories are playable with multiple meaningful choices.
4. Each system has visual feedback and impacts shared resources.
5. At least three meaningful unexpected events occur with controlled triggers.
6. Student satisfaction, sustainability, resource limits and budget influence ending.
7. Decisions create reproducible cross-system consequences.
8. QR from Windows UI opens controller on physical phone and controls live Unity game, or explicitly records a blocker if not verified.
9. Controller reconnect/disconnect does not leave inputs stuck.
10. Future Lens compares outcomes using simulation state.
11. Professional, performant animations/lighting/audio are functioning.
12. Save/load/restart and demo-mode shortcuts work.
13. EditMode/PlayMode tests pass, Windows build succeeds, no critical exceptions.
14. The 15-minute demonstration can be performed offline on a private LAN/hotspot.
15. All source and asset licenses documented; fake BIS certification never claimed.

## E. MANDATORY CONTINUITY RULES FOR CODEX ASTRA
Always maintain root `progress.md` **before modifying the project, after meaningful milestones, and at the end of every work session**. At every phase, read it and the repository before editing. Maintain:
```
# CAMPUS PULSE 2030 — PROGRESS
Last updated: [date/time]
Repo root / Unity project path:
Unity Editor exact version + installed modules:
Current phase: Foundation | 1 | 2 | 3 | 4 | 5 | 6
Status: NOT STARTED | IN PROGRESS | VERIFIED | BLOCKED
Implemented and verified (include file paths):
Currently implemented but NOT verified:
Commands run + results / Unity logs:
Build location + last build result:
Current known bugs + reproduction steps:
Decision log / architecture changes:
Mobile network info (omit tokens/secrets and real private addresses):
Assets used + license/provenance:
Next THREE concrete steps:
How a fresh AI resumes:
```
Never mark a feature VERIFIED without a real test. Never claim Unity Editor has been launched, screenshots inspected or a phone tested unless actually performed. Write to `docs/` for details. At context-limit risk, stop new features and write a handoff with exact next command and current failure. Commit at stable points if Git configured. Keep the game buildable each phase.

---
# PROMPT 0 — FOUNDATION / BASE PROJECT
**Paste this first in Codex Astra with THIS WHOLE MASTER BRIEF accessible in the repo as `docs/GAME_SPEC.md`.**

You are the lead Unity game developer for CAMPUS PULSE 2030. Read `docs/GAME_SPEC.md` completely, especially the source-faithful official 'Save the Campus' requirements. Before writing gameplay code, inspect the actual Windows host to check Unity Hub, exact installed Unity Editor version, required Windows build support, Node.js and available project files. Do not assume these tools are installed. If Unity is missing, give exact minimal install steps and stop only the blocked operation; do not hallucinate that the Editor ran.

Create a REAL Unity 6 LTS URP project under `UnityProject/`, with a coherent folder structure, C# assemblies as appropriate, New Input System and Cinemachine if installed/compatible. Do not produce a React/Three.js substitute. Establish keyboard/mouse camera input, a minimal isometric camera over a terrain plane with recognizable prototype campus facilities, a test interaction (select building to inspect), basic Canvas/UI Toolkit HUD with budget, happiness and sustainability, valid game states `Menu/Playing/Paused/Ending`, and a deterministic tick-based simulation shell. Add a reliable Editor automation helper that programmatically creates/opens scenes and prefabs, and a Windows build helper; use only documented commands/packages supported by installed Unity.

Create `progress.md` NOW using the mandatory schema, plus `README.md`, `docs/ARCHITECTURE.md`, `docs/ACCEPTANCE.md`, `docs/DEMO_SCRIPT.md` and `docs/ASSET_LICENSES.md`. Add `.gitignore`. Make the first playable prototype RUN in the actual Unity Editor if possible, verify basic input, run a build/compile check, record outputs and failures. No cinematic polish yet unless cheap. Do not jump ahead to Phase 1. Deliver current exact commands, verified status and remaining blockers in progress.md. If Unity is not installed, scaffold documentation and safe code only and report installation steps without claiming completion.

**Exit criteria:** valid Unity project + loads 3D scene + selectable building + camera controls + deterministic simulation shell + progress.md + real compile/build evidence or accurately recorded blocker.

---
# PROMPT 1 — 3D LIVING CAMPUS & INTERACTION
Read `docs/GAME_SPEC.md`, `progress.md`, all related code and the acceptance checklist. Implement Phase 1 only. Transform the foundation into a navigable/inspectable, premium stylized-realistic campus scene with academic blocks, laboratory, hostels, canteen, auditorium, water tanks, pump house, solar roofs, substation, waste yard, roads, paths, trees and students. Use correct meshes, PBR materials, believable proportions and optimization-friendly prefabs. Do not leave default cubes as final visible architecture. Construct using documented Unity Editor scripts and properly imported/credited licensed art where appropriate. Ensure all critical buildings are clickable with readable contextual descriptions.

Implement a robust isometric orbit/zoom/pan/focus rig using Cinemachine compatible with the installed version; use desktop input and a shared input abstraction for future mobile commands. Add basic ambient animation (solar panel movement, student walkers, service vehicle route, tank/power indicators) and subtle day/night lighting. Every named campus location must map to future simulation IDs. Distinguish visuals from authoritative simulation state. Provide scene-management and object pooling/instancing where appropriate, not uncontrolled GameObject spawning.

Test: Unity compiles without errors; camera avoids clipping and can focus every facility; each 3D facility is clickable; scene performance reasonable at 1080p; nonfunctional placeholders are documented. Build and run if possible. Update progress.md with files, tested behaviors and the next three actions. No fake visual quality claims.

**Exit criteria:** attractive *actual 3D* living campus with controllable camera, named interactive buildings and clean architecture.

---
# PROMPT 2 — FOUR OFFICIAL GAMEPLAY SYSTEMS
Read `docs/GAME_SPEC.md` and progress.md, verify Phase 1. Implement ALL FOUR explicit official challenge systems as PLAYABLE mechanics: water, electricity, waste and overcrowding, not just graphs. Create authoritative deterministic simulation model in C# with realistic educational tradeoffs, fictional documented units, budget, simulated time, sustainability and student satisfaction. Use ScriptableObjects/data definitions for buildings, upgrades, interventions, incidents and mission parameters where helpful. Each subsystem must have observable 3D physical state, 2–3 different actions with benefits/costs/risks, resource allocation and consequential feedback.

Water: leak locations, consumption, stored water, pump electricity tradeoffs, repair action and water-saving action. Electricity: energy supply and demand, excessive usage, solar option, scheduling/efficiency action, overload and outage impacts. Waste: bins fill/overflow, segregation, recycling, collection schedule/truck visible. Crowd: students move and queue, zone occupancy/limits, re-routing, staggered schedule and counter-capacity intervention. Make budget, staff capacity or action slots limited. Build the key HUD for resource indicators, satisfaction and sustainability, mission hints, and contextual interventions—not a static dashboard. Show actual changing mesh state (tank fill, lit windows, bin levels, queue density) from simulation. Test conservation of numeric state, valid clamping, cost application, time progression, repeatability with fixed random seed, and no exploit for unlimited money.

**Exit criteria:** judges can perform at least one complete interaction in each official category, see visual changes, and observe a shared resource/satisfaction/sustainability consequence. Update progress.md and evidence.

---
# PROMPT 3 — DECISIONS, CHAIN REACTIONS & STORY CAMPAIGN
Read spec/progress; test existing systems before modifying them. Build a coherent three-day campus-management campaign around Day 1 discovery, Day 2 pressure and Day 3 crisis. Implement an incident scheduler, decision history, contextual Campus Pulse advisor, in-world event indicators, limited-resource tradeoffs, success/failure conditions and at least three interrelated surprises. Define causality explicitly (example: water pump activation increases electric demand; extra canteen counter reduces crowd wait but increases energy and waste; waste truck clears bins but spends budget and can temporarily create traffic congestion). Player should be able to recover from mistakes, but cannot automatically win by spending all the budget.

Implement scoring for resource health, student satisfaction, sustainability, budget management and completion of all four challenge categories. Create meaningful multiple outcomes with persistent day summaries and final report. Add light BIS educational cards only for VERIFIED directly applicable principles, with official link/reference and appropriate disclaimer: optional BIS layer never replaces requirements or claims certification. Build tutorials, objectives, pause, restart, deterministic demo seed, mission progression and save/load where feasible. Expose event IDs/hooks for cinematic Phase 5. Tests must validate chain-reaction causality, multiple endings, order-of-operations, save/load and repeatable outcomes. Update progress.md.

**Exit criteria:** complete start-to-finish gameplay covering all official categories, real tradeoffs, surprise events, student satisfaction and sustainability outcomes.

---
# PROMPT 4 — QR MOBILE PHONE CONTROLLER (REAL DEVICE)
Read spec/progress; preserve desktop controls and working game. Build the end-to-end LAN mobile controller, not a decorative mock UI. Preferred architecture: lightweight Node.js HTTP+WebSocket relay on the Windows laptop, locally served responsive HTML/CSS/JS phone controller, validated socket client in native Windows Unity runtime, with one authoritative active controller. Document exact Node and Unity networking dependencies compatible with installed runtime.

Create an in-game 'Connect Phone' overlay displaying real LAN IPv4 + port, fresh random time-limited single-session token encoded in a real QR code, pairing status, local address and a short PIN/approval option. No localhost URL in the QR for external phones. Mobile browser must run on Android Chrome and iOS Safari without app installation; show virtual movement/pan controls, rotate/zoom or touch gesture, select/inspect, act/confirm, back/pause, controller connected/reconnecting indicators and optional haptic feedback when browser supports it. Ensure responsive landscape/portrait layouts. Commands must be rate limited, validated, bounded and edge-triggered correctly; stale/disconnected inputs automatically zero. Provide desktop keyboard/mouse fallback and do not block gameplay if networking fails. Use only private Wi-Fi/hotspot, never expose game to public internet or store tokens in Git. Explain Windows private network firewall and hotspot setup; include a manual IP override for VPN/multiple NICs.

Implement disconnect/reconnect flows, token expiry/rotation, pairing approval and helpful troubleshooting. Unit/integration test valid/invalid pairing, message schema, disconnected joystick reset and retransmission. RUN the relay and build Unity Windows player. If physical device access is available, conduct real phone pairing, tap buttons, move camera, interact with buildings, disconnect/reconnect; report which devices were actually tested. If physical test is impossible, explicitly mark it unverified and give user an exact step-by-step real-device validation checklist. Update progress.md.

**Exit criteria:** scanning the live QR opens a playable touch controller and changes the actual laptop Unity game state, OR a precisely documented remaining real-device blocker; no invented success claims.

---
# PROMPT 5 — CINEMATIC POLISH, AUDIO, FUTURE LENS
Read spec/progress and preserve all completed official mechanics and mobile controls. Turn functioning gameplay into a professional Unity visual experience rather than a simple low-poly mockup. Deliver a cohesive art-direction pass across ALL visible campus locations: refined models, PBR materials, lighting, reflection probes, soft shadows, SSAO or compatible ambient occlusion, restrained bloom, color grading, atmosphere, environmental sound, animated crowds, weather, electricity visualization, subtle water effects, collection trucks and polished UI motion. Target stable 1080p ~60 FPS on the user's RTX 4060 Laptop GPU; profile rather than asserting it.

Use Cinemachine for camera choreography and Timeline/Animator for an opening flyover, crisis cut-ins and a memorable final recovery sequence, with skip/reduced-motion options. Make environmental effects driven by actual simulation state: a water tank empties, windows go dark, bins overflow, queues swell, and campus recovers after decisions. Implement FUTURE LENS as true A/B simulation projection of the same campus under a reproducible identical seed: current decisions vs a transparently defined alternative set of decisions. Use synchronized camera/campus geometry, a split-screen or smooth draggable comparison slider, explanatory causal highlights and outcome metrics. Never hardcode a good/bad screenshot as a fake alternate simulation. Add contextual audio cues and subtitles/accessibility options.

Quality gate: assess representative gameplay scenes, eliminate camera clipping/pop-in/unclear lighting, test that animations don't change simulation values, verify Future Lens inputs and metrics, profile draw calls/frame time on real hardware when available, expose Low/Medium/High presets. Capture true screenshots only if tools allow rendering. Update progress.md with exact performance evidence or outstanding verification steps.

**Exit criteria:** cinematic, living Unity campus and genuinely state-driven Future Lens without loss of gameplay performance or correctness.

---
# PROMPT 6 — FINAL INTEGRATION, TESTS, JURY MODE & RELEASE
Read ALL docs and progress.md, reproduce current build, and systematically verify every acceptance requirement. Fix blockers before adding any optional features. Integrate Windows x64 standalone build, game launcher, local controller server launch helper, correctly resolved IP and QR pairing overlay, keyboard fallback, resource simulation, day progression, scoring, Future Lens, sound, settings and saved state. Add demo modes: full campaign; ~15-minute guided jury presentation with checkpoint skips; 5-minute emergency condensed demo; quick reset/reconnect and QR tutorial. Make game completely usable from a fresh download with no Unity Editor required to PLAY (Node runtime may be bundled or explicitly required; document clearly).

Build an automated test suite: Unity EditMode for simulation/accounting/events; PlayMode for UI/scene interactions; Node controller server validation tests; deterministic campaign outcome regression; connection-loss safety; build smoke test. Run actual Unity build/test commands and record paths, log errors, and any failures. Create a professional 15-minute jury script including: official requirements demonstrated first, live mobile-control wow, unexpected cross-system crisis, Future Lens, sustainability/satisfaction outcomes, and succinct BIS relevance without overstating official requirements. README must include install, launch, same-Wi-Fi/hotspot, private Windows firewall, how to scan QR, fallback demo, system requirements, asset licensing and troubleshooting.

IMPORTANT official event compliance: review rule that work must be developed during allotted six-hour event; request clarification from organizer about allowed advance work/AI assistance, and do not state that a prebuilt entry meets the rules without permission. Do not claim winning likelihood, certified engineering validity, actual phone interoperability or FPS metrics without evidence. Commit/tag stable release if allowed, and finish progress.md with final verified checklist, known limitations and exact launch command.

**Exit criteria:** downloadable/runnable playable Unity game + actual validated phone controller where device available + all official PS1 categories + polished cinematic presentation + scripted jury demo + clean handoff documentation.

---
# RESUME PROMPT — IF CODEX ASTRA RUNS OUT OF CONTEXT
Continue CAMPUS PULSE 2030 from the current repo. First read `docs/GAME_SPEC.md`, root `progress.md`, README, architecture and acceptance checklist. Inspect files and Git history; do NOT restart the Unity project, recreate already verified assets or skip known failures. Confirm the installed Unity version and the state of the most recent build and tests. Work ONLY on the exact current phase and the next three steps specified in progress.md. Test changed features, record actual logs and updated verification, and rewrite progress.md before ending. If a dependency or GUI operation is unavailable, preserve the work and document the exact blocker rather than claiming completion.
