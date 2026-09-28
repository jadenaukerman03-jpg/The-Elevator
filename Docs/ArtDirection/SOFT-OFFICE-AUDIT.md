# Art-direction diagnosis — soft office target

The realism direction is retired. The target is a playful robot office built from rounded, weighty silhouettes and a restrained palette. This is a replacement visual language, not a texture pass.

| Area | Current source of the problem | Treatment |
|---|---|---|
| First-person hands | FirstPersonHands constructs slender fingers, a narrow wrist, skin shading and separate contact geometry. A realistic hand reads poorly without a complete authored anatomy/animation asset. | Replace the silhouette with compact rubber palms, four short rounded fingers, inward thumbs and chunky cuffs. Preserve interaction semantics, replace presentation. |
| Player body | WorkerModel initially creates broad coveralls, then UseOfficeRig hides it and installs BusinessRobot. | Keep the avatar/color API; replace the office rig. |
| NPC proportions and suits | BusinessRobot has a small anatomical head, long narrow limb segments, tapered realistic jacket profiles, tiny lapels, buttons, barcodes and shoe laces. | Replace visible body parts with broad jacket shells, compact legs, soft shoes, a readable faceplate and one oversized badge/tie. Keep employee identity and task state. |
| Walls, halls and ceilings | FloorGeometryBuilder uses 12 m grid cells and raw slabs. OfficeKit repeats thin wall bands, narrow mullions and small ceiling detail over those slabs. | Keep navigation dimensions, ports and collision; add a distinct rounded visual kit, thick jambs, coved corners and fewer ceiling forms. Do not change generation rules just to soften visuals. |
| Bevels | OfficeArt scales a shared unit rounded box. Nonuniform scaling compresses bevels on thin axes and still reads as stretched rectangular slabs. | New mesh builder uses bevel radius in world units, with deliberate thick silhouettes. |
| Furniture and equipment | Thin desks, many square panels, tiny vents/keys, rectangular chair parts and dense display text repeat the same outline. | Replace asset recipes. Use curved counters, inflated chair cushions, bulky monitors, oversized keys and simple screen symbols. |
| Plants and props | Small flattened leaves, narrow trunks, realistic tiny hardware and repeated micro-clutter are hard to read at play distance. | Broad clustered leaves, substantial pots, large folders/mugs/cards and intentional groups. |
| Materials | OfficeSurface adds grain, carpet patterns, veneer and metallic response to a muted green-grey palette. | Keep material ownership/batching; replace material library with low-gloss cream, teal, coral, golden yellow and dark ink. No photographic wear. |
| Lighting | Dark ambient light, fog and an intense nearby flashlight produce grey rooms and clipped bright hands. | Warm broad key light, cool ambient fill, soft contact shadows, quiet screens; omit the flashlight in the target. |
| Camera | Fixed 68-degree FOV and realistic-ish eye height make the narrow limbs and large rooms feel remote. | Target uses 74 degrees, slightly lower eye height and eased sprint FOV. No forced camera shake. |
| Movement | WorkerController switches speed immediately. Articulation largely follows sinusoids or analytical reach targets with little torso/head follow-through. | Prototype eased acceleration, stop settling, alternating body roll, secondary head lag and low-amplitude breathing. Preserve infinite stamina. |
| Object handling | SalvageItem disables collision while held and drives a kinematic body to a camera-relative pose. It feels like rigid UI placement. | Target compares a capped spring/damper pickup with collisions retained, light tumbling and modest bounce. This is a feel study; retain the original mission rules until the new feel is integrated. |
| UI | DescentHUD exposes timer, money, power, load, incidents, stamina, survey/seed status and notices simultaneously. OfficeFloor adds contract, badge, cover, suspicion and prompts. | Target has a dot/hand cursor and one item name. Put testing instructions on a physical card. Keep mission information available for a later compact HUD redesign. |

## Reuse and replace

Reuse macro layout generation, collision dimensions, deterministic replay, clearance logic, suspicion, NPC scheduling, objective extraction, item identity and floor unlocking. Restyle their adapters after the target is reviewed. Replace character/hand meshes and office furniture recipes. Re-author materials, lighting, signs and animation curves. The legacy art stays available for comparison; no new rooms are being generated in that style.

## Target-scene boundary

One office corner: reception/work desk, employee silhouettes, a chunky workstation, chair, plant, scanner/door, vending machine, oversized card and folders, a physical cargo object and bubbly first-person hands. Original shapes and palette; no borrowed character designs, assets, shaders or UI. This is a playable visual/handling study, not a new multiplayer implementation or a replacement for the existing heist systems.

## Acceptance criteria

At normal play distance, recognize the robot, clearance card, workstation and cargo by silhouette. Rounded edges retain visible thickness. Five main palette families carry the scene; no procedural noise textures or micro-hardware. Hands read as soft robot gloves. Movement settles gently and held props retain physical response. Maintain broad routes, low UI density and readable lighting. Validate in both overview and first person before expanding to more rooms.
