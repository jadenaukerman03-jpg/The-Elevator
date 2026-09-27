# Morrow Systems office — implementation and review

**Status: working preproduction office slice. The final visual-quality acceptance criteria are not yet met.** The new materials, architectural kit and theft loop are implemented and tested. Character art, animation, room variety and social simulation still need substantial work; this is not presented as a finished showcase.

> **Latest update:** See [the detail-pass report](DETAIL-PASS.md) for the added room, furniture, vending-machine and robot detail, plus current validation and performance. The figures below describe the earlier office slice.

## Visual diagnosis and approach

The old generator's exposed slab walls, blank ceilings, one material response, sparse corner props, identical lighting and rounded coverall workers made scale feel empty. Merely increasing the random prop count would not fix that.

The office introduces Morrow Systems: warm public reception, graphite carpet, walnut veneer, etched privacy glazing, champagne trim and cooler working departments. Corporate branding is configurable in the MorrowOffice catalog's ThemePayload. Coordinated workstation clusters include furniture, terminals, keyboards, cables, paperwork, badges and personal effects. Finished wall bays, ceiling tiles, vents, lights, skirting, frames and connector treatments provide architectural rhythm. Reusable materials distinguish cloth, carpet, plaster, metal, wood, glass and emissive screens. Shared beveled meshes and per-room material batching limit render overhead.

After the first render, empty space, oversized signs, coarse surface grain and missing contact shadows were corrected. A second pass doubled workstation clusters, added interior glass partitions, reduced signage, refined ceiling scale and budgeted two nearby shadow-casting lights.

## Test the slice

Open **The Elevator > Office > Open Showcase**, then Play and Clock In. The scene is Assets/Scenes/OfficeShowcase.unity. It retains the original bootstrap and generator; Prototype.unity remains the Civic Works regression scene.

Use **seed 104729**, **Small**, **High** quality for the shortest complete test: 18 rooms, 36 employees, a vending-machine target, a receptionist and a reachable clearance-holding supervisor. Standard produces 72 rooms/144 employees; Extreme caps at 320 employees across 192 rooms. Population is constrained by real task stations and the configured budget.

1. Explore reception and work areas. The mandatory asset's room number appears in the contract HUD.
2. Approach the supervisor from behind and **hold G** to lift the department badge. Ordinary employee and higher-risk security badges can also be stolen. Front approaches and blocked sightlines fail the pickpocket envelope; witnesses can react.
3. Aim at the secured door and press **E** to swipe. A matching department badge or security badge is required.
4. Aim at the vending machine and press **E** to disconnect power and engage the dolly. Walk slowly; walk backward to pull. **Q** releases the handles.
5. Return the complete object inside the elevator. Press **R** to request departure. Optional cargo alone cannot satisfy the contract. Battery/power/overload rules still apply; the vending machine weighs 125 kg, before adding the worker.
6. Steal optional data units, awards and portable terminals with the existing E/Q carrying system. Their value contributes to the existing cargo total; damaging them reduces value.

**Hold T** at a free task station to look busy. Stations are reserved so employees cannot claim the same workspace. **B** gives a limited bluff response during a badge check. **L** toggles the flashlight. **F3** retains seed/size/replay tools, usable from inside the cabin.

Use **The Elevator > Office > Build Windows Showcase** for a standalone build, or launch Builds/Office/TheElevatorOffice.exe when the provided build is present. The entire Office build folder is required.

## What changed

| Files / assets | Purpose |
|---|---|
| Assets/Scripts/Office/OfficePlan.cs | Deterministic department grouping, compatible objective selection, reachable credential holder, conservative extraction contract |
| OfficeArt.cs, OfficeKit.cs | Shared materials/meshes and contextual architecture/furniture templates |
| BusinessRobot.cs | Original articulated robot meshes, volumetric jacket/lapels, separate sleeves/trousers, fingers, pose interpolation and head tracking |
| OfficeTaskPoint.cs, OfficeEmployee.cs | Reserved workstations, home departments, roles, work/break schedules, route following, individual suspicion and security investigation |
| OfficeDoor.cs | Department clearance, reader interaction and sliding leaves |
| OfficeCargo.cs, OfficeFloor.cs | Mandatory objective, dolly physics, optional valuables, observation/reporting, cover interaction, sound pool and contract HUD |
| OfficeBenchmark.cs | Opt-in standalone rendering/profile harness; inert during normal play |
| Assets/Resources/OfficeSurface.shader, OfficeGlass.shader | Surface variation, per-material response, emission and etched glass approximation |
| Assets/Resources/Generation/MorrowOffice.asset | Separate configurable office theme |
| Assets/Scenes/OfficeShowcase.unity | Seeded office entry scene; original prototype retained |
| Assets/Editor/OfficeTools.cs, OfficeValidation.cs | Showcase rendering, player build and repeatable tests |
| Tools/VerifyOffice.ps1 | Isolated validation without closing the user's main editor |

Small integrations in MapRecipe, FloorContentCatalog, FloorGeometryBuilder, GenerationLabWindow and DescentGame add a theme payload and an office-specific layer. Empty payloads preserve existing Civic Works configuration hashes. The base graph algorithm and generation version remain unchanged. WorkerController/WorkerModel add office interactions/disguise presentation; SalvageItem adds condition-based valuation; Workshop owns the extra materials. The HUD displays office room types. Static mesh batching now removes empty render-only source objects after combination.

## Implemented behavior and validation boundaries

Every office has one mandatory object selected from a vending machine, server or archive safe. Placement stays on the ground level for this first heavy-hauling implementation. The required supervisor is reachable without passing its own locked door. All entrances to the target room receive controlled doors. Startup validates a conservative rotated horizontal envelope against physical geometry, using the actual cargo collider bounds in play. Actual collider dimensions are measured from the modeled mesh bounds, excluding text. Open-door clearance is tested separately from locked gameplay state.

One credential layer is implemented. Department-specific levels and a security override exist, with configurable per-floor expiration and alarm invalidation. Multi-stage investigation/key chains, temporary access work orders, badge swapping and charging-station theft are not implemented.

Employees reserve real task positions and alternate compatible destinations. Nearby staff run full movement/perception; distant staff retain their job/reservation with coarser checks. Suspicion belongs to individual witnesses, followed by delayed department/security reporting. Security uses the last reported position and direct sight when available. Questioning precedes an alarm; high alerts can lead to investigation and workplace-incident damage using the existing health system. This is a prototype response model, not a complete workplace/social simulation.

Large cargo uses force/torque-driven dolly handling instead of the small-item kinematic hold. Velocity/depenetration limits reduce instability. Rough vending-machine handling can spill a bounded number of products and create witnessed noise. Automatic recovery is limited to an out-of-world technical fault; there is no player teleport button. Ordinary jams still need physical resolution. Multiplayer grabbing, straps, recovery from every possible jam and network authority remain unimplemented.

The existing survey terminals, electrical hazards, cargo accounting/persistence, three-stop clock/power loop, four map profiles, graph roles, stairs, loops and replay remain. Office landmarks receive suspended corporate sculptures without blocking haul routes. The original custodian is preserved in Civic Works; office employees/security replace its office enemy sockets.

## Verification and evidence

- Original engine-independent checks: 15 run-rule checks and 42,449 generation assertions over 800 primary map builds passed.
- Original elevator Play-mode regression passed after office integration.
- Office planning: 200 recipes across all four sizes, each planned twice with matching fingerprints.
- Office geometry: Small, Standard and Extreme at seeds 104729 and -17; all graph routes and planned objective envelopes passed.
- Office Play-mode checks cover objective gating, credential presence, angle restrictions, witness-local suspicion, reader access, actual cargo bounds, value accounting, persistence through descent, power and per-floor badge reset. All three objective types are exercised.
- Windows Development build succeeded. Offscreen benchmark forces Camera.Render and synchronous GPU readback; it is not a claim about displayed-window FPS.

Evidence and exact metrics live in TestResults/Office: before-arrival.png, after-lobby.png, after-workroom.png, after-vending.png, after-robot.png, showcase-metrics.txt, geometry-validation.txt and play-validation.txt. Benchmark evidence is under TestResults/Office/Benchmark, with machine details and a CSV. Counts refer to authored mesh pieces **before batching**, not unique props or draw calls. NPC rigs contribute additional mesh renderers.

The first hidden-window benchmark failed to produce screenshots, so its tiny timings were discarded as rendering evidence. The corrected test forces rendered frames and GPU readback. The test machine has a Ryzen 9 5900X and RTX 5080; results should not be extrapolated to lower-end hardware. Extreme's roughly 1 GB allocation is a remaining optimization concern. Low/Medium currently reduce keyboard detail and shadow lights; they are not fully tuned quality tiers yet.

Final standalone sample, seed 104729, High, 1280×720, Windows Development build:

| Size | Rooms | NPCs | Modeled pieces before batching | Generation | Mean render + readback | 95th percentile | Unity allocated memory |
|---|---:|---:|---:|---:|---:|---:|---:|
| Small | 18 | 36 | 12,609 | 921 ms | 9.53 ms | 10.64 ms | 175 MiB |
| Standard | 72 | 144 | 51,457 | 1,172 ms | 17.38 ms | 18.93 ms | 478 MiB |
| Extreme | 192 | 320 | 135,423 | 3,319 ms | 19.17 ms | 21.52 ms | 1,090 MiB |

These measurements include synchronous GPU readback, exclude displayed-window presentation, and sample one stationary workroom after warm-up. They do not establish minimum-spec frame rate, traversal performance or four-player performance. The reviewed static showcase contains 12,540 pieces and took 536 ms to construct in the batch editor; runtime cargo adds further pieces. No unique-prop count is inferred from those assembly-piece counts.

Unity's known batch SearchDatabase startup exception remains in raw logs and is excluded only when the stack contains no game code. Game errors still fail the Play validator. Tests use a separate project copy.

## Remaining quality gap

**No character, animation or environment art is claimed production-ready.** There are no imported paid/restrictively licensed assets and no finished humanoid asset library in this project. The new characters are original segmented mesh rigs, not skinned production models. Their elbows/knees, suit volume and hands improve the old worker, but gait, foot planting, clothing deformation, face direction and hand/object contact still need professional-quality work. Procedural pose interpolation is not a replacement for authored animation clips and IK.

Room layouts still inherit the 12 m square module vocabulary. The added finish and furnishing make them more legible, but architectural silhouettes, department-specific stories, custom equipment, bathroom internals and larger atria are incomplete. Glass uses tint/banding/Fresnel, not true rough transmission or refraction. Material grain is procedural; there is no complete normal/trim/decal atlas or wear pipeline. Lighting lacks a full reflection/GI and occlusion solution. Sound is synthesized ventilation, keys, steps/readers and impacts; there are no voice performances or announcements.

NPC recovery can abandon a blocked station, but crowd navigation, turning, seating transitions and object alignment still require human playtesting. The prototype does not include all requested workplace behaviors, locks/lockdowns, identity checks, cover-task consequences, damage interactions, optional loot classes or security behaviors. Networking and proximity voice remain undecided; cooperative handling and multiplayer fun are not verified.

The most important next improvement is **a properly skinned business-robot asset with a fitted suit and authored walk/sit/type/reach/badge animations, supported by hand and foot IK**, evaluated in this functioning office scene. In parallel, replace the repeated square-room dressing with a small number of strongly authored reception, cubicle, breakroom and secured-records modules. That is the gap between this testable slice and the finished-quality showcase requested.


## First-floor startup correction

The office is the first playable floor, not an optional demo. Default build settings now start OfficeShowcase; the legacy Prototype scene also explicitly uses the office catalog, Small size and seed 104729. New/unassigned DescentGame bootstraps default to the same office catalog. The briefing and initial notification explain badge theft and mandatory extraction. The office Play validator opens Prototype, verifies its catalog/build entry, then clears the catalog to exercise the runtime fallback. The Civic Works regression fixture explicitly selects its catalog.

Unity scenes generate geometry when Play starts; an empty edit-mode scene is expected. Existing runs must be stopped and restarted to use changed startup settings. Later floors still use the current office prototype; distinct later-floor themes are not implemented by this correction.
