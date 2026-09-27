# Procedural generation foundation

## Audit and decisions

Before modification, the project contained a linked bootstrap scene, DescentGame run states, a CharacterController, funny primitive worker models, freight elevator, cargo persistence, power/overload rules, three compact layouts, custodian AI, electrical hazards, IMGUI screens and synthesized sound. These systems were retained. FacilityBuilder still builds the elevator and salvage; the original floors remain available with UseProceduralFloors disabled.

Engine: Unity 6000.3.25f1, built-in render pipeline, legacy input. No networking package was installed. The user chose **four players** as the design target and **networking undecided**. Only Unity's built-in JSON serialization module was added.

Direction: **Civic Works**, an oversized municipal building whose records, utilities and dispatch departments extend underground. Department stripes, infrastructure monuments, office equipment, service conduits and staff galleries support descriptions such as “blue pipes, room 037, upstairs.” Generation happens behind closed elevator doors. The chunky worker remains; gameplay now defaults to first person.

## Pipeline and ownership

| Stage | Implementation | Responsibility |
|---|---|---|
| Recipe | Generation/Core/MapModel.cs | Seed, version, full settings, modules and departments |
| Macro layout | Generation/Core/MacroLayoutGenerator.cs | Integer-cell graph, roles, room IDs and connections |
| Validation | Generation/Core/MapValidator.cs | Connectivity, counts, loops, objectives, stairs and sockets |
| Content planning | Separate seeded streams in macro generator | Stable socket IDs, kinds, slots and content seeds |
| Geometry | Generation/FloorGeometryBuilder.cs | Shells, matching openings, connectors, stairs and interiors |
| Runtime map | Generation/GeneratedFloor.cs | World positions, graph routes, surveys, visibility, mesh cleanup |
| Gameplay | DescentGame.PopulateSocket | Existing salvage, batteries, custodians and electrical plates |
| Replay | Generation/GenerationRecordStore.cs | Complete recipes, fingerprints and failed-seed logs |
| Tools | Editor/GenerationLabWindow.cs and GenerationDebugPanel.cs | Graphs, replay, preview and export |

The pure C# core has no Unity dependency. Topology finishes before geometry spawns. Shells own connectivity; interior content does not decide how rooms connect. The geometry builder accepts a population callback so future gameplay systems can consume sockets without changing topology.

## Macro structure

Each layer receives a budgeted main route with forward progression and turns. The entrance starts along a readable axis away from the lift. Reconnecting detours are reserved before bounded department wings fill remaining space. Paired stair rooms connect main-route endpoints between levels.

Graph distances place objectives far from extraction and from one another. Optional leaves receive risk/reward roles; other optional zones fill the requested quota. Landmarks spread by graph distance. Periodic supply opportunities support long return trips. Enemy and hazard sockets exclude the first few steps from safety.

Departments occupy spatial bands and select among weighted eligible module IDs. Layout, module selection, content and interior dressing use separate streams. Changing prop density does not rearrange the building.

Loops offer local route alternatives. Inter-level stairs remain bottlenecks; two edge-disjoint extraction paths from every room are not guaranteed. Shortcut marks an open reconnecting route with a mint threshold. RestrictedZone and Event sockets are metadata only; no locked-door or event mechanic is claimed.

## Profiles and dimensions

| Profile | Rooms | Main route | Branch depth | Levels | Loops | Landmarks | Optional | Objectives / minimum distance | Enemy cap |
|---|---:|---:|---:|---:|---:|---:|---:|---|---:|
| Small | 18 | 8 | 4 | 1 | 2 | 2 | 3 | 1 / 5 | 1 |
| Standard | 72 | 26 | 7 | 2 | 8 | 6 | 8 | 3 / 12 | 6 |
| Large | 120 | 38 | 9 | 2 | 14 | 9 | 12 | 4 / 16 | 10 |
| Extreme | 192 | 54 | 11 | 3 | 20 | 12 | 18 | 5 / 20 | 14 |

The kit uses 12 × 12 m rooms, default 14 m cell spacing, and 6 m storeys. Supported cell spacing is 14–18 m. Default doors are 2.8 m wide/high, connectors 3.4 m wide, ordinary ceilings 3.4–4.9 m. Worker dimensions are 1.9 m height, 0.38 m controller radius, 1.7 m viewpoint. Stair flights are 2.4 m wide with 18 visible treads per 3 m rise, smooth ramp collision, mid-landings and 1.1 m gallery rails.

These clearances allow two worker-sized capsules to pass ordinary doorways; actual four-player crowding and voice have not been tested. World signage and landmarks target first-person eye height. The debug graph is not gameplay presentation.

Area metrics are **gross floor/connector estimates**, before subtracting clutter, stairs and openings. Standard is about 10,900 m² by that metric, not a measured accessible surface. Room count and kit dimensions determine it. Arbitrary multi-cell rooms require another placement/geometry resolver. Counts support 12–512 rooms subject to validation, with the four supplied presets seed-tested.

## Determinism and replay

Generation version is **2**. Fixed xorshift32 streams, integer cells, sorted module IDs, explicit traversal order and invariant structure serialization avoid Unity random state, dictionary enumeration and physics decisions. Same signed 32-bit seed, version, recipe and content revision reproduce the manifest. Unknown versions are rejected.

A recipe snapshots the full profile and catalog. Configuration, structure and content have separate hashes. Module catalog ordering is normalized; department ordering intentionally controls spatial assignment. When prefab content changes, bump the module content revision and catalog theme revision. Prefab bytes are not automatically hashed.

Normal play writes Generation/last-success.json under Application.persistentDataPath. GenerationRecordStore.DirectoryPath exposes the exact location. Failed attempts/recipes append to failed-seeds.jsonl. Replay validates the saved recipe, regenerates, and compares all three fingerprints. Batch tests write inside their isolated project instead of overwriting player records.

Future networking can use this boundary:

1. Host distributes a GenerationRecord recipe and expected fingerprints.
2. Clients build locally and return their fingerprints using the future transport.
3. Doors remain closed until required clients match; reject content revision differences.
4. Room/socket stable IDs anchor network entities.
5. Replicate run state, cargo physics, enemies, surveys and doors separately.

GenerationRecord.Matches provides the comparison seam. No transport, RPC, lobby, ownership, voice or disconnect handling exists. Structural determinism does not make Rigidbody simulation or live AI deterministic.

## Content authoring

Duplicate Assets/Resources/Generation/CivicWorks.asset, or use Create > The Elevator > Floor content catalog. Assign it to the bootstrap or Generation Lab. Keep IDs stable and bump revisions when content changes.

Add module definitions with ID, style, weight, ceiling and revision. Add IDs to a department's ModuleIds. Districts controls names, eligible ordinary modules and wet-floor slowdown. New departments need not use default names. Arrival, landmark and stairwell are required special families and cannot be selected as ordinary department interiors.

PrefabOverrides replace primitive interior arrangements. Prefab origin is room-floor center and rotates in 90° steps. It is **interior dressing**, not another shell. Preserve the central cross: keep static dressing inside four corner regions centered approximately at (±3.8, 0, ±3.8), no more than 2.2 m across. Keep doorway approaches clear. Reserve corners occupied by content sockets; built-in dressing does so automatically, but custom prefabs require author discipline and geometry validation. Do not place ordinary furniture in stairwell openings.

Built-in styles provide authored filing towers, pressure vessels, parcel conveyors, benches and infrastructure monuments. New visual variants can use prefabs without new C# styles. Nonstandard footprints and connection types require an explicit kit extension.

Supply, reward, objective, enemy, hazard, event and restricted-zone sockets expose IDs, rooms, slots and content seeds. Extend or replace the population callback to add gameplay content; graph placement does not depend on its component classes. Survey completion is currently informational and idempotent, without rewards or a departure gate.

Landmarks always receive monuments in available corners even at zero ordinary prop density. Landmark names/silhouettes remain placeholders; a richer theme-specific library is the next environment step. New department names use the records accent fallback until a wider palette mapping is added.

## Tools, budgets and performance boundaries

Generation Lab generates a graph before geometry, shows layers, exports manifests and builds a disposable additive preview. Closing the window or entering Play clears the preview. Do not save the temporary scene as the gameplay scene.

F3 in editor/development builds displays the seed and metrics and permits regeneration from inside the cabin, retaining recovered cargo. Inspector profile overrides take precedence over the size selector.

Topology uses deterministic operation and retry limits. Failure reports the seed, recipe and reasons. Geometry yields between work units after a configurable elapsed-time target, default 5 ms. This is a soft frame budget: one room can take longer. Scheduling does not affect layout. Construction errors terminate with diagnostics instead of leaving the lift loading forever.

Static meshes combine per room/material; collision shapes remain. Room rendering stops beyond 90 m and room lights beyond 24 m. This is a starting optimization, not portal occlusion, streaming, pooling or a frame-rate guarantee. Custodians follow graph/stair waypoints; this is not full dynamic-obstacle navigation.

## Next work

Human-playtest movement, hauling, stair pursuit and wayfinding. Measure generation time, frame time, memory and draw calls in a player build. Time real group runs after networking exists. Add richer authored room prefabs, landmark silhouettes, hazard/event resolvers and objective incentives. Production atmosphere and 15–30 minute pacing must be demonstrated by playtesting, not inferred from room count.

