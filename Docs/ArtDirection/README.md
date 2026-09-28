# Integrated soft office redesign

The rounded art direction now runs in the original game, through `DescentGame`, `WorkerController`, `OfficeFloor`, and the existing procedural generator. This is a redesign of The Elevator, not a replacement mini-game.

Start `PLAY THE ELEVATOR.cmd` or `Builds/Windows/TheElevator.exe`. Unity menu **The Elevator > Play Latest Target** now opens the original office game. **Play Updated Office** also opens it. The older `PLAY SOFT OFFICE.cmd` redirects to the original game. The small art study remains available only under **Art Direction > Open Soft Office Target** for reference.

The shared palette and rounded geometry apply to generated offices, furniture and elevator parts. Broad ceiling islands replace dense ceiling grids; padded dividers, chairs, chunky monitors and oversized clocks add the softer identity. Characters share one procedural rig (`Assets/Scripts/Characters/BeanRig.cs`): a big round head sunk into a bean torso, painted-on eyes and mouth, noodle arms, mitten hands, short legs, flat colors under the `Elevator/SoftCharacter` two-tone shader. The player starts as the plain avatar and layers wardrobe items (`AvatarWardrobe.cs`) chosen in Settings. NPCs dress by job and keep their existing navigation, task animations, coffee interactions and suspicion behavior. The player has a full animated body with a visible torso/feet and a complete first-person shadow; first-person mittens retain item-specific surface contacts.

Existing gameplay remains in place: random layouts, reception keycard and pickpocketing, security doors, meetings, suspicion, mandatory cargo extraction, numbered elevator controls, unlock progression, crouching, charged throws and unlimited stamina.

Validation uses the original office interaction, social, cabin and objective tests. The separate small study is no longer the delivery build.

Integration verified: original interaction, social, cabin and objective Play-mode suites passed; both Windows executable paths were rebuilt. The original TheElevator.exe launched visibly and generated a fresh 18-room office without runtime exceptions in its startup log.
