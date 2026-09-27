# Office detail pass — September 26, 2026

This pass adds authored visual detail to the existing playable office. The same controls, map topology, badge rules and extraction loop remain. This is an art/detail update, not a claim of completed production art or new multiplayer functionality.

- Workstations: desk phones with keypads and coiled leads, framed robot family photographs, drawers and recessed pulls, blotters and reminder notes.
- Rooms: department noticeboards with pinned memos and fine print, clocks, identity signs, branded reception wall panels and perimeter fire equipment.
- Kitchens: upper cupboards, individual backsplash tiles, drip trays, coffee supplies and microwave controls.
- Lounges and meeting rooms: individual cushions and seams, magazines, marker trays, dry-erase markers and speakerphones.
- Utility furniture: archive-carton lids and handles, server rack handles, labels and cooling grilles, washroom soap and towel dispensers.
- Mandatory vending machine: corner moldings, retaining screws, leveling feet, product codes and nutrition stripes, coin slot, instructions, ventilation and a rear service panel.
- Robots: separate shirt collars, pocket squares, pocket flaps, badge clips/portraits/barcodes, faceplate screws and grille slots, cuff buttons/cufflinks and shoe details. Eyes blink at staggered intervals during play.

Most new detail uses shared materials and existing static batching. High quality adds small workstation and server details. Robot additions increase renderer cost; this pass does not solve the existing rig or animation limitations. Clocks and printed notices are set dressing, not new interactive systems. Added furniture remains within existing furniture/perimeter footprints; routes are checked by the existing geometry validator.

Source: `OfficeKit.Detail.cs`, `OfficeKit.cs`, `BusinessRobot.cs`.

Before images are preserved as `TestResults/Office/detail-before-workroom.png` and `detail-before-robot.png`; updated evidence uses the existing `after-*.png` filenames. The original report's performance table describes the previous version. Current detail-pass measurements are recorded below after validation.

## Final validation

- Unity compilation and Windows Development build succeeded.
- 200 office recipes and six physical maps passed (Small, Standard, Extreme; seeds 104729 and -17).
- Play checks passed for loading, stations, extraction gating, badge theft angles, witness-local suspicion, door access, all three actual objective envelopes, valuation, descent persistence, power, new objectives and badge expiry.
- Static showcase: 18 rooms, 36 employees, 16,514 modeled pieces before batching; 702 ms editor construction. Previous showcase: 12,540 pieces. These are assembly pieces, not unique props or draw calls.
- Editor smoke sample: 16,623 modeled pieces with runtime cargo, 878 ms generation, 212 MiB allocated. This is not a player frame-rate measurement.
- Reviewed updated workroom, robot and vending screenshots. Small fittings are most visible close up. Room silhouettes and animation remain at prototype quality.

## Standalone cost of the added detail

1280 x 720, High, Development build, same opt-in forced Camera.Render and synchronous GPU-readback harness. These are stationary rendering/readback samples, **not displayed-window FPS**.

| Map | Rooms / NPCs | Generation | Mean render + readback | P95 | Allocated |
|---|---:|---:|---:|---:|---:|
| Small | 18 / 36 | 1,138 ms | 15.68 ms | 22.92 ms | 199 MiB |
| Standard | 72 / 144 | 1,837 ms | 28.07 ms | 33.85 ms | 567 MiB |
| Extreme | 192 / 320 | 5,075 ms | 26.92 ms | 33.37 ms | 1,380 MiB |

Compared with the earlier slice, this sample shows increased rendering and memory cost. It is not a controlled multi-run performance study. Additional rigid-character renderers and fine geometry need consolidation/LOD before targeting modest hardware or dense multiplayer scenes. Start with Small for reviewing the detail. Current standalone screenshots and CSV are under `TestResults/Office/DetailBenchmark`.
