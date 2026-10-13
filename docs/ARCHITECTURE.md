# Architecture

- `Campus.Simulation`: plain C# authoritative facility records and deterministic clock shell. No UnityEngine reference. Phase 1 resource values are illustrative and static; future flow models belong here.
- `Campus.Runtime`: composition root `CampusSession`, state-bound facility visuals, Cinemachine strategic camera, uGUI HUD. The simulation owns occupancy; any visual crowd is derived and capped.
- `Campus.Editor`: scene/material creation through Unity APIs, Windows BuildPipeline helper, explicit local command-file bridge, Unity TestRunner integration.
- EditMode and PlayMode test assemblies are excluded from normal player code by Unity's test-assembly flags.

Input uses Unity Input System keyboard/mouse devices and a `CameraCommand` struct accepted by the camera rig. Future remote input can feed the same command API. Selection uses world raycasts, gated by EventSystem UI pointer blocking. Cinemachine owns the output camera; the rig smoothly positions its virtual camera.

The Canvas HUD uses top/bottom navigation and a compact right-hand inspector. A menu overlay enters the same scene rather than loading duplicate campus state. Reset reconstructs the simulation and clears selection. Time progression is one fictional in-game minute per real second; pause halts authoritative ticks.

Generation creates real scene and material assets, with metadata assigned by Unity. It refuses to replace a dirty scene. Generated Campus content can be regenerated; hand-authored SampleScene and template content remain untouched. No external 3D models or raster reference imagery are embedded.

Dependency evidence: [Unity 6000.3 Cinemachine package documentation](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-cinemachine) lists 3.1.7 for this editor series. [Unity command-line reference](https://docs.unity.com/en-us/engine/6000.0/manual/unity-editor/command-line-arguments/editor) documents batch execution. Resolved local package code is the API authority for implementation.
