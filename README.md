# The Elevator

**First floor — Morrow Systems:** choose **The Elevator > Office > Open Showcase**. Seed **104729**, Small, High demonstrates 18 rooms, 36 employees, badge theft, secured access and a mandatory vending-machine extraction. Use G behind employees, T at free workstations, E at doors/the dolly and B when questioned. See [the office implementation report](Docs/Office/SHOWCASE.md) for controls, evidence, tests, changed files and the remaining visual-quality gap. This is a working preproduction slice, not finished character/environment art. Both the normal Prototype entry and the default build now start in this office.

A standalone Unity 3D prototype about disposable maintenance workers exploring an impossible municipal facility from a freight elevator. Oversized boots, uneasy faces, bad paperwork, suspicious valuables.

**Current milestone:** playable procedural-map foundation. First-person by default; maps sized for a future four-player group. Networking remains undecided and **online co-op is not implemented**. Unity compilation, geometry checks and automated Play-mode integration have passed. Human playtesting and performance tuning remain.

## Open and play

1. In Unity Hub, open this **TheElevator** folder, containing Assets, Packages and ProjectSettings. Use Unity **6000.3.25f1** and the built-in rendering pipeline.
2. Choose **The Elevator > Open Prototype**, then press **Play**.
3. Wait for generation, choose a uniform, and click **Clock In**.
4. On the first floor, steal a supervisor badge from behind with G, open the secured room with E, and haul the required asset fully into the lift before pressing R.

The world is assembled when Play starts. For an edit-mode map, open **The Elevator > Generation Lab**, choose a preset, click **Generate graph**, then **Build 3D preview**. The preview lives in a separate temporary scene and clears before Play.

## Map sizes

| Preset | Rooms | Levels | Loops | Survey terminals | Departure timer |
|---|---:|---:|---:|---:|---:|
| Small (default) | 18 | 1 | 2 | 1 | 8 minutes |
| Standard | 72 | 2 | 8 | 3 | 25 minutes |
| Large | 120 | 2 | 14 | 4 | 30 minutes |
| Extreme | 192 | 3 | 20 | 5 | 40 minutes |

These are configurable targets, not measured session lengths. Standard is intended for 15–30 minutes of group exploration; actual four-player pacing needs testing. Profile assets live under Assets/Resources/Generation. Select the scene bootstrap to change **Map Scale**, a profile override, or a manual seed before Play.

## Your shift

Three stops share the original power, cargo, weight and health rules. Bring cargo behind the yellow threshold. The 180 kg limit includes your 70 kg worker; overload increases power consumption and door-closing time. The lift starts with 68 power and normal departure costs 24, so connect at least one cell during a complete shift.

Remote survey consoles provide distant exploration targets. Completion is tracked per floor; it currently neither gates departure nor awards an economy bonus. Records offices, wet utilities, sorting rooms, break areas, landmarks and stair galleries populate the facility. Custodians and electrical plates occupy deeper regions. The lift is safe; the timer can leave you behind.

Cargo recovered in the cabin persists between floors. Three incidents end the run. Pause and loss of focus stop simulation. Disabling **Use Procedural Floors** retains the original three compact layouts and 110-second timers.

## Controls

| Input | Action |
|---|---|
| WASD / mouse | Move / look |
| Shift / Space | Sprint / jump |
| E | Pick up, drop, or use survey terminal |
| Q | Throw held object |
| F | Connect held power cell inside lift |
| R | Depart early from inside lift |
| L | Flashlight |
| Escape | Pause |
| F3 | Developer generation panel: seed, size, replay, graph |
| V | Developer first/third-person toggle |

F3 regeneration requires returning to the cabin. An Inspector profile override takes precedence over the size selector. Seeds and surveys appear in the HUD. The graph is a developer tool, not a player minimap.

## Extend and verify

Read [the architecture and authoring guide](Docs/PROCEDURAL_GENERATION.md) for seed contracts, room dimensions, departments, prefab interiors, socket callbacks and future networking. [Verification details](VERIFICATION.md) distinguish automated checks from remaining playtests.

Run from PowerShell in this folder:

~~~powershell
.\Tools\Verify.ps1
.\Tools\VerifyGeneration.ps1
.\Tools\VerifyUnityGeneration.ps1
~~~

The last command copies source/settings into TestResults/GenerationValidation and runs a hidden Unity instance there. Your normal project can stay open. Results include logs, a manifest, a first-person render and a Play-mode result.

Use **The Elevator > Build Windows Player** to build Builds/Windows/TheElevator.exe; distribute the entire output folder. A standalone build has not been verified this milestone.

Current limits: primitive placeholder interiors, fixed 12 m room kit, graph-based enemy navigation, no transport, proximity voice, locked-door gameplay, event behaviors, progression save or destruction. Content sockets and semantic zones provide extension points. Physics and live AI are not deterministic network simulation.


