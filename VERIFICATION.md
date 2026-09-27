# Verification record — September 26, 2026

## Office milestone

The new office's results are documented in [Docs/Office/SHOWCASE.md](Docs/Office/SHOWCASE.md). Office planning passed 200 recipes; six Small/Standard/Extreme physical maps passed route checks. Play-mode tests cover badge acquisition, witness-local suspicion, clearance doors, objective gating, actual bounds for vending/server/safe targets, cargo valuation/persistence and floor transitions. A standalone Windows Development build and forced-render/readback benchmark succeeded. Run Tools/VerifyOffice.ps1 to reproduce office checks; add -BuildPlayer to package the executable. The original regression results below remain valid, including a repeated original Play-mode check after office integration.

## Executed

- **15 existing run-rule checks passed:** power, overload, carry speed, recharge, three-stop battery budget and grading. Package manifest parsing passed.
- **42,449 generation assertions passed:** 100 seeds per preset generated twice, totaling 800 principal builds, plus targeted variants. Tests cover replay, variation, extraction connectivity, loops, objective distances, stairs, socket IDs, stream isolation, fingerprints, catalog ordering, French locale, a custom department, area arithmetic, invalid inputs and bounded failures. The sweep recorded 142 retries and took approximately 1.1 seconds; that measures pure graphs, not Unity geometry.
- **Unity 6000.3.25f1 compilation and scene linkage passed** in an isolated copy. Both custom shaders were found.
- **Five physical geometry checks passed:** every preset at seed 104729 plus Standard at -17. Across 474 rooms, 1,608 route segments passed capsule sweeps and 10,122 samples found floor support. Capsules approximate the worker, not a carried safe or wider custodian. Tests found and fixed a 0.15 m stair-exit gap.
- **First-person render reviewed:** 1280 × 720, 1.7 m eye height. A depth-tested font shader fixed signs showing through walls. This is an offscreen geometry render, not a human gameplay recording or performance benchmark.
- **Automated Unity Play-mode integration passed:** initial loading/briefing, Standard size/timing, first-person camera/body visibility, populated content, idempotent surveys, saved-recipe comparison, replay, seed/size changes, cargo persistence through regeneration and actual descent, power deduction and next-floor generation.

The Play test calls APIs to exercise transitions. It does not synthesize keyboard/mouse input, traverse every room with a CharacterController, measure pickup reach or assess human pacing. A test cargo placement initially failed because it moved an interpolated Transform without updating the Rigidbody; the fixture now correctly updates physics position.

Unity's batch editor emits an unrelated UnityEditor.Search.SearchDatabase startup exception. It remains in the raw log. The Play validator excludes only that editor-search stack when it has no game frame; game exceptions, assertions and other errors still fail validation. Testing uses a source copy and leaves the user's open editor alone. No standalone build or network test is claimed.

## Reproduce

~~~powershell
.\Tools\Verify.ps1
.\Tools\VerifyGeneration.ps1
.\Tools\CheckSyntax.ps1
.\Tools\VerifyUnityGeneration.ps1
~~~

Set -UnityEditor on the last script if needed. It creates/reuses TestResults/GenerationValidation, copies source/settings, imports, checks geometry and enters Play. Logs remain in that copy. The reviewed image, final manifest and Play result are also copied to the main TestResults folder. The main editor may stay open.

## Human playtest checklist

1. Open Prototype, Play, Clock In. Test look, movement, sprint, jump, flashlight, pause/focus and camera clipping.
2. Start Small. Pick up the arrival cell, connect it with F, recover/throw cargo and verify threshold weight/value changes.
3. Explore Standard, record surveys and return using landmarks/signs. Time the run and assess interior variety.
4. Traverse stair galleries both ways with large cargo and under pursuit. Capsule sweeps do not replace this.
5. Test custodian hearing/wayfinding, safe lift, electrical timing and level-specific water slowdown.
6. Use F3 replay/new seeds from the cabin; check seeds, fingerprints and cargo state.
7. Test overload, insufficient power, expiry outside, three stops, victory/loss and restart.
8. Build Windows and repeat input/restart checks. Profile frame time, draw calls, memory and loading, especially Extreme.
9. After networking is selected, test four-player passing, authority, fingerprint handshake, late joins/disconnects and voice ranges.


First-floor correction verified: office Play validation passed through the legacy Prototype entry with the runtime default catalog; build settings point to OfficeShowcase. The explicit Civic Works Play regression, 42,449 generation assertions, and 15 run-rule checks also passed. Both office and normal Windows builds were rebuilt. Git tracks source, authored assets, metadata, settings, tests, tools and docs; generated Unity caches and local build outputs remain excluded by the project ignore rules.
