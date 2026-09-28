# Elevator and interaction revision

Player launch scenes now choose a new random seed each run. Manual seeds remain available in the Inspector for debugging and replay. Tests explicitly opt into fixed seeds.

The reception counter always has a bright access card, including seeds that also allow supervisor pickpocketing. It grants the secured room credential.

The elevator has steel panels, mounting brackets, rails, ceiling diffusers, door tracks, and two framed privacy doors. Passing light bands appear during travel without exposing the shaft. The right panel has 51 separate colliders for numbers 0–50. Selection uses the same precise center-screen ray as the cursor. Current floor is amber, unlocked floors green, locked floors grey. Floor 0 is reserved. Each run starts at 1; securing the required asset unlocks the next floor. Backtracking to unlocked floors is supported. Later floors currently reuse the procedural office theme; 50 distinct themes are not implemented.

Interactable targets show a pointing-hand cursor; empty space shows a dot. E or left click interacts. Hover labels retain item names and omit prices. A bound notebook sits on the elevator table. Pick it up, turn pages with its page buttons, and close with E.

Hands have inward-facing thumbs, continuous tapered fingers, and movement-dependent arm sway. Infinite stamina remains enabled. These are original procedural hands, not copied assets or photorealistic hands.

Conference whiteboards have stable stands. Kitchen upper cabinetry connects to floor-supported backing panels. Archive shelves have full-height uprights. World lettering receives physical sign backing; survey labels are mounted to the console.

CabinValidation covers randomized starts and different layouts, all 51 button rays and inter-button gaps, locks, credential pickup, notebook pickup/return, transit, second-floor loading and stamina. VerifyOffice runs these alongside geometry, social, extraction and interaction regressions and builds both Windows targets.
