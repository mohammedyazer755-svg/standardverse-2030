# Campus Pulse 2030

Unity 6.3 URP campus environment. Current verified status is in [progress.md](progress.md).

[GitHub repository](https://github.com/mohammedyazer755-svg/standardverse-2030) · [Download verified Windows foundation](https://github.com/mohammedyazer755-svg/standardverse-2030/releases/tag/foundation-v0.1.0). The release contains the foundation; `main` also preserves unfinished Phase 1 source. See [distribution details and checksum](docs/DISTRIBUTION.md).

Open this repository root in Unity **6000.3.26f1**. Packages resolve from `Packages/manifest.json` and the lock file. Open `Assets/_Game/Scenes/Campus.unity` after scene generation; press Play and **Enter Campus**.

Controls: WASD/arrows pan; Q/E or right-drag orbit; scroll zoom; middle-drag pan; click facility to inspect; F focuses selection; Home returns to overview; Esc pauses. HUD buttons provide focus, overview, pause/resume and restart.

The scene currently displays explicitly labeled illustrative baseline resources. Phase 2 interventions, three-day campaign, phone control and Future Lens are not part of this phase.

## Editor automation

Save any dirty scene first. Use **Campus Pulse → Generate Foundation** only for the foundation scene, and **Campus Pulse → Build Windows** to build. Generation replaces only the generated Campus scene, not SampleScene. Do not regenerate over hand-edited Campus content without preserving it.

For the existing open editor, put a supported command in `Library/CampusAutomation/request.txt`: `foundation`, `benchmark`, `edit-tests`, `play-tests`, or `build`. The editor polls once per second when ready. If external edits are not imported, focus the editor and choose Assets → Refresh before submitting a command. Results go to `Logs/Campus/`. `campus` is reserved for the full expansion and is not ready while the benchmark gate remains pending.

The latest saved scene/build is the verified foundation. The benchmark source is in progress; check `progress.md` before generation. A Windows firewall prompt from the development player can be cancelled for this offline phase; network access is not needed.

When this project is closed in the editor, the Windows build equivalent is:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\GameDev\CampusPulse2030' -executeMethod CampusPulse.Editor.CampusBuilder.Build -logFile 'C:\GameDev\CampusPulse2030\Logs\Campus\batch-build.log'
```

Run `builds/Windows/CampusPulse2030.exe` after a successful build. Unity Editor is not required to play; distribute the entire Windows build folder, not only the executable.

See [architecture](docs/ARCHITECTURE.md), [acceptance](docs/ACCEPTANCE.md), [asset provenance](docs/ASSET_LICENSES.md), and [implementation plan](docs/IMPLEMENTATION_PLAN.md).
