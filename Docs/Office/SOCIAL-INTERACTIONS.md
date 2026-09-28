# Office interaction update

Open the actual TheElevator Unity project and choose The Elevator > Play Updated Office. If already playing, stop Play first so the new generated floor is rebuilt.

The first floor remains Morrow Systems, Small, seed 104729. Its sparse population has assigned desks, distinct schedules, limited simultaneous trips, doorway reservations and obstacle-aware routes. Coffee routines open a cupboard, retrieve a mug, visibly fill it, drink and carry it home. Meeting rooms are seed-dependent and react to interruption. Public doors can be opened and closed. Selected monitors carry movable equipment; other computers remain installed.

Witnesses react to theft and raise the displayed suspicion meter. Taking an occupied workstation computer causes a stronger reaction. Credentials alternate by seed between a visible desk card and a supervisor badge. Press E to collect a desk card; G behind the supervisor attempts badge theft. Ctrl or C crouches.

First-person hands use item-specific grip profiles and pickup transitions. Hold Q to charge a throw, release to throw, or use E to cancel/drop. The gauge caps at full charge, and heavy objects travel more slowly. A quick tap gently drops the object.

Validation covers population, distinct work assignments, real travel to coffee and back, visible filling/drinking, crouch headroom, door operation, occupied-computer suspicion, badge variants, meeting reactions, movable computer geometry, hands, grip profiles, capped throws and cancellation. Run Tools/VerifyOffice.ps1 -BuildPlayer to regenerate evidence and the Windows office build.

## Remaining art and gameplay limits

Characters and hands are procedural meshes with articulated parts, not photorealistic skinned characters or motion-captured animation. Grips fit item collider bounds; arbitrary imported shapes will need authored contact poses. Meeting discussion uses gestures and subtitles. Navigation checks cover tested office seeds, not a guarantee against every possible crowding case. Networking remains undecided and online co-op is not implemented.

Office content revision is now 2. Previous office replay hashes are intentionally incompatible with the changed population and room content.
