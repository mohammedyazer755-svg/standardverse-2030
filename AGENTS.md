# Campus Pulse 2030

Read `docs/GAME_SPEC.md`, `docs/IMPLEMENTATION_PLAN.md`, `docs/FINAL_GAME_IMPLEMENTATION_PLAN.md`, and `progress.md` before edits. The Unity project is at repository root. Preserve existing scenes and metadata. Current user scope (13 October 2026): final-game implementation planning for Phase 6, preserving previous phases. Required design: real ground-level third-person player, a few human-size animated walking students, and overhead management; first-person optional. Audit and fix critical integration defects before feature implementation. The latest user request supersedes earlier Phase 2/4 stopping instructions. This planning handoff does not claim release readiness.

Simulation owns counts and resource values; scene objects only present them. Never claim tests, screenshots, device interoperability, or performance without captured evidence. Update `progress.md` after milestones and at handoff. Keep generated artifacts in `builds/` or `Logs/`, and preserve all source assets and `.meta` files. Editor automation must not discard dirty user scenes.

## Mandatory phase-end record

At the end of EVERY phase, and whenever work stops with that phase incomplete:

1. Record what was implemented, with scope and relevant file paths.
2. Record what was actually tested: exact commands or manual steps, results, and evidence paths. Separate implementation from verification.
3. Record failures, blockers, limitations, and unfinished work. Never mark an incomplete phase complete.
4. Record which files changed, including generated assets and package/project settings. Use Git to capture the actual list.
5. State the exact next action, including its command or editor steps and prerequisites.
6. Update root `progress.md` with the phase, status, evidence, and next three steps. Maintain a phase report in `docs/phases/` using `docs/PHASE_HANDOFF_TEMPLATE.md`.
7. Create a Git checkpoint when appropriate. Verified milestones get descriptive commits; incomplete work must be labeled WIP/checkpoint, not completed. Record the commit or a command that resolves it. Explain if no checkpoint is possible.

Save project source and phase records to `https://github.com/mohammedyazer755-svg/standardverse-2030`. User authorization to save this project there persists. Inspect remote history before pushing; never force-push or overwrite unrelated work. Preserve Unity caches, secrets, and raw machine logs locally. Copy reviewed, relevant test results/screenshots to `docs/evidence/`; distribute playable builds as release assets rather than committing build binaries. Verify the remote commit after a push and report any sync failure honestly.
