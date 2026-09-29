using UnityEngine;

namespace TheElevator
{
    public sealed class DescentHUD : MonoBehaviour
    {
        DescentGame game;
        GUIStyle title, heading, body, small, button, choice;
        bool settings;
        static readonly WardrobeSlot[] slots = (WardrobeSlot[])System.Enum.GetValues(typeof(WardrobeSlot));
        static readonly string[] slotLabels = { "SKIN COLOR", "HEAD", "EYES", "MOUTH", "EYEBROWS", "GLASSES", "SHIRT", "JACKET", "PANTS", "SHOES", "GEAR" };
        public void Initialize(DescentGame owner) { game = owner; }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 66, fontStyle = FontStyle.Bold, wordWrap = true };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            choice = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            choice.normal.textColor = Workshop.Cream;
            title.normal.textColor = Workshop.Cream;
            heading.normal.textColor = Workshop.Cream;
            body.normal.textColor = Workshop.Cream;
            small.normal.textColor = new Color(0.68f, 0.76f, 0.74f);
            button.normal.textColor = Workshop.Ink;
        }

        void Panel(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
        bool Button(Rect rect, string label, Color color)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool pressed = GUI.Button(rect, label, button);
            GUI.backgroundColor = old;
            return pressed;
        }
        void Text(Rect rect, string text, GUIStyle style) { GUI.Label(rect, text, style); }
        void Bar(Rect rect, float fraction, Color color)
        {
            Panel(rect, new Color(0.15f, 0.22f, 0.23f));
            Panel(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color);
        }

        void OnGUI()
        {
            if (!game || !game.Player || game.Player.ReadingNotebook) return;
            Styles();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            if (settings && !game.Paused && game.Phase != DescentGame.RunPhase.Briefing) CloseSettings();
            if (settings) { Settings(height); return; }
            if (game.Phase == DescentGame.RunPhase.Generating)
            {
                Panel(new Rect(32,32,590,height - 64),new Color(0.025f,0.055f,0.065f,0.97f));
                Text(new Rect(66,100,510,100),"LOCATING\nYOUR FLOOR",heading);
                Text(new Rect(66,230,510,65),"Preparing rooms, routes, landmarks, and service access.",body);
                Bar(new Rect(66,340,490,8),game.GenerationProgress,Workshop.Yellow);
                Text(new Rect(66,375,500,70),"SEED " + game.ActiveSeed + "\n" + Mathf.RoundToInt(game.GenerationProgress * 100) + "%",body);
                return;
            }
            if (game.Phase == DescentGame.RunPhase.Briefing)
            {
                Panel(new Rect(32, 32, 590, height - 64), new Color(0.025f, 0.055f, 0.065f, 0.97f));
                Panel(new Rect(32, 32, 590, 6), Workshop.Yellow);
                Text(new Rect(66, 61, 520, 24), "FACILITY SERVICES / EMPLOYEE INDUCTION 004", small);
                Text(new Rect(62, 100, 535, 160), "THE\nELEVATOR", title);
                Text(new Rect(66, 277, 505, 35), game.CurrentOffice ? "FLOOR 1 / MORROW SYSTEMS" : "Going down. Mostly.", heading);
                Text(new Rect(66, 332, 500, 80), game.CurrentOffice ? "You are here to steal office equipment. Find the one employee carrying the access card, use it on the locked door, and bring the asset behind it back to the elevator." : game.UseProceduralFloors ? "A sprawling facility. One freight lift. Find remote survey stations, recover valuables, and remember your way back." : "Three floors. One freight lift. Grab whatever looks expensive and get back before the doors close.", body);
                Text(new Rect(66, 422, 500, 58), game.CurrentOffice ? "Your field notebook is on the table inside the elevator. Read it for controls, access and floor selection." : "Batteries keep you moving. Heavy cargo slows the doors and burns extra power. You count toward the weight limit.", body);
                if (Button(new Rect(66, 502, 500, 42), "SETTINGS  /  CUSTOMIZE AVATAR", Workshop.Cream)) settings = true;
                if (Button(new Rect(66, 558, 500, 56), "CLOCK IN  /  START DESCENT", Workshop.Yellow)) game.Begin();
                Text(new Rect(66, 631, 500, 22), "EARLY PROTOTYPE  /  SINGLE PLAYER  /  WINDOWS", small);
                Text(new Rect(width - 530, height - 60, 490, 32), "WASD MOVE  /  E INTERACT  /  L FLASHLIGHT  /  F3 DEBUG", small);
                return;
            }

            Panel(new Rect(24, 24, 374, 105), new Color(0.025f, 0.055f, 0.065f, 0.92f));
            Text(new Rect(42, 37, 335, 35), "B" + (game.FloorIndex + 1).ToString("00") + "  /  " +
                (game.Phase == DescentGame.RunPhase.Transit ? "DESCENDING" : "SALVAGE SHIFT"), heading);
            string location = game.CurrentMap && game.CurrentMap.Ready ? game.Player.InCabin ? "FREIGHT 04 / SAFE ARRIVAL" :
                game.CurrentMap.NearestRoom(game.Player.transform.position).District + " / ROOM " + game.CurrentMap.NearestRoom(game.Player.transform.position).Id.ToString("000") : "ELEVATOR";
            if(game.CurrentOffice&&!game.Player.InCabin)
            {
                int room=game.CurrentMap.NearestRoom(game.Player.transform.position).Id;
                location=game.CurrentOffice.Plan.Rooms[room].Kind.ToString().ToUpper()+" / ROOM "+room.ToString("000");
            }
            Text(new Rect(42, 78, 335, 38), location, small);
            if (game.CurrentMap && game.CurrentMap.Ready)
                Text(new Rect(42,132,600,25),"SEED " + game.ActiveSeed + "   /   SURVEYS " + game.CurrentMap.CompletedObjectives + "/" + game.CurrentMap.Manifest.Recipe.Settings.ObjectiveCount,small);
            Panel(new Rect(width - 280, 24, 256, 105), new Color(0.025f, 0.055f, 0.065f, 0.92f));
            int secondsLeft = Mathf.CeilToInt(game.Clock);
            string clock = game.Phase == DescentGame.RunPhase.Closing ? "DOORS CLOSING" :
                game.Phase == DescentGame.RunPhase.Transit ? "PLEASE STAND BY" :
                "DEPARTS " + (secondsLeft / 60).ToString("00") + ":" + (secondsLeft % 60).ToString("00");
            Text(new Rect(width - 263, 38, 225, 35), clock, heading);
            Text(new Rect(width - 263, 82, 230, 22), "RECOVERED   $" + game.CargoValue, body);
            if (game.CurrentOffice)
            {
                // Building-wide anger: fills with every offence, never goes down; full means everyone is after you.
                TheElevator.Office.OfficeFloor office = game.CurrentOffice;
                Panel(new Rect(width - 280, 136, 256, 52), new Color(0.025f, 0.055f, 0.065f, 0.92f));
                Text(new Rect(width - 263, 143, 230, 20), office.Riot ? "OFFICE ANGER  /  EVERYONE IS AFTER YOU" : "OFFICE ANGER  " + Mathf.FloorToInt(office.BuildingAnger) + " / " + Mathf.RoundToInt(TheElevator.Office.OfficeFloor.RiotPoint), small);
                Bar(new Rect(width - 263, 170, 220, 6), office.BuildingAnger / TheElevator.Office.OfficeFloor.RiotPoint, office.BuildingAnger > 35 ? Workshop.Red : Workshop.Yellow);
            }
            if (game.Phase == DescentGame.RunPhase.Closing)
                Bar(new Rect(width - 263, 118, 220, 4), 1 - game.ClosingProgress, Workshop.Red);

            float bottom = height - 150;
            Panel(new Rect(24, bottom, 300, 126), new Color(0.025f, 0.055f, 0.065f, 0.92f));
            Text(new Rect(42, bottom + 13, 265, 24), "LIFT POWER  " + Mathf.CeilToInt(game.Power) + "%", body);
            Bar(new Rect(42, bottom + 44, 264, 6), game.Power / 100, Workshop.Mint);
            Text(new Rect(42, bottom + 63, 265, 24), "LOAD  " + Mathf.CeilToInt(game.Load) + " / 180 KG", body);
            Bar(new Rect(42, bottom + 96, 264, 6), game.Load / RunRules.Capacity,
                game.Load > RunRules.Capacity ? Workshop.Red : Workshop.Yellow);
            Text(new Rect(width - 267, height - 112, 245, 23), "HEALTH  " + Mathf.CeilToInt(game.Player.Health), small);
            Bar(new Rect(width - 267, height - 84, 220, 6), game.Player.Health / WorkerController.MaxHealth, game.Player.Health < 35 ? Workshop.Red : Workshop.Mint);
            Text(new Rect(342, height - 75, 520, 23), "STAMINA", small);
            Bar(new Rect(342, height - 47, 180, 5), game.Player.Stamina, Workshop.Cream);

            Text(new Rect(width - 267, height - 51, 245, 24), "L  LIGHT     ESC  PAUSE", small);
            if (game.Load > RunRules.Capacity)
                Text(new Rect(342, bottom, 600, 27), "OVERLOADED  /  EXTRA POWER + SLOWER DOORS", body);

            if(game.Player.CanInteract && !game.Player.ReadingNotebook) DrawHandCursor(width/2,height/2);
            else if(!game.Player.ReadingNotebook) Panel(new Rect(width / 2 - 2, height / 2 - 2, 4, 4), Workshop.Cream);
            if (!string.IsNullOrEmpty(game.Player.Prompt) && game.ControlsActive)
            {
                Panel(new Rect(width / 2 - 330, height / 2 + 65, 660, 39), new Color(0.025f, 0.055f, 0.065f, 0.92f));
                Text(new Rect(width / 2 - 314, height / 2 + 75, 640, 24), game.Player.Prompt, small);
            }
            if (Time.unscaledTime < game.NoticeUntil)
            {
                Panel(new Rect(width / 2 - 365, 147, 730, 66), new Color(0.025f, 0.055f, 0.065f, 0.92f));
                Text(new Rect(width / 2 - 348, 158, 698, 53), game.Notice, body);
            }
            if (game.Player.Held)
                Text(new Rect(342, height - 116, 640, 28), "CARRYING  " + game.Player.Held.Title.ToUpper() + "  /  " + game.Player.Held.Mass + " KG", body);

            if (game.Paused || game.Phase == DescentGame.RunPhase.Won || game.Phase == DescentGame.RunPhase.Lost)
                Overlay(width, height);
        }

        void DrawHandCursor(float x,float y)
        {
            // A compact pointing hand silhouette, independent of font glyph support.
            Color c=Workshop.Cream;
            Panel(new Rect(x-4,y-10,5,19),c);
            Panel(new Rect(x+2,y-1,5,12),c);
            Panel(new Rect(x+8,y+2,5,10),c);
            Panel(new Rect(x+14,y+5,4,9),c);
            Panel(new Rect(x-4,y+8,22,10),c);
            Panel(new Rect(x-10,y+4,7,8),c);
            Panel(new Rect(x-1,y+18,15,4),c);
        }
        void Overlay(float width, float height)
        {
            Panel(new Rect(0, 0, width, height), new Color(0.015f, 0.035f, 0.045f, 0.92f));
            float x = width / 2 - 280;
            bool ended = !game.Paused;
            Text(new Rect(x, 90, 560, 90), ended ? game.Phase == DescentGame.RunPhase.Won ? "SHIFT COMPLETE" : "SHIFT TERMINATED" : "ON BREAK", heading);
            if (ended)
            {
                Text(new Rect(x, 165, 560, 85), game.Outcome, body);
                Text(new Rect(x, 250, 560, 50), "$" + game.CargoValue + " IN CABIN", heading);
                Text(new Rect(x, 307, 560, 40), game.Phase == DescentGame.RunPhase.Won ? RunRules.Grade(game.CargoValue) : "Try lighter cargo. Leave earlier. Bring a battery.", body);
            }
            else
            {
                Text(new Rect(x, 165, 560, 235), "WASD   Move       MOUSE   Look\nSHIFT   Sprint       SPACE   Jump\nE   Interact / carry / sit       HOLD Q   Throw\nHOLD MOUSE   Use item (spray)\nF   Connect held battery inside lift\nR   Depart early       L   Flashlight\nF3   Generation debug       ESC   Resume\nExplore, record remote surveys, and return to the lift.", body);
            }
            if (Button(new Rect(x, 410, 560, 52), ended ? "CLOCK IN AGAIN" : "BACK TO WORK", Workshop.Yellow))
            { if (ended) game.Restart(); else game.SetPaused(false); }
            if (!ended && Button(new Rect(x, 474, 560, 45), "SETTINGS  /  AVATAR", Workshop.Cream)) { settings = true; game.Player.PreviewAvatar = true; }
            if (!ended && Button(new Rect(x, 530, 560, 45), "RESTART SHIFT", Workshop.Cream)) game.Restart();
            if (Button(new Rect(x, ended ? 480 : 586, 560, 45), "QUIT", Workshop.Cream)) game.Quit();
        }

        void CloseSettings() { settings = false; game.Player.PreviewAvatar = false; }

        // Avatar customizer. Lives in settings for now; the lobby shop will reuse the wardrobe and its prices.
        void Settings(float height)
        {
            Panel(new Rect(32, 32, 590, height - 64), new Color(0.025f, 0.055f, 0.065f, 0.97f));
            Panel(new Rect(32, 32, 590, 6), Workshop.Yellow);
            Text(new Rect(66, 56, 520, 30), "SETTINGS  /  AVATAR", heading);
            Text(new Rect(66, 90, 520, 22), "Pick a look. Changes save automatically.", small);
            AvatarLoadout loadout = game.Player.Model.Loadout.Clone();
            bool changed = false;
            for (int i = 0; i < slots.Length; i++)
            {
                float y = 124 + i * 42;
                WardrobeSlot slot = slots[i];
                int index = loadout.Get(slot);
                Text(new Rect(66, y + 6, 160, 24), slotLabels[i], small);
                if (Button(new Rect(226, y, 44, 34), "<", Workshop.Cream)) { loadout.Set(slot, index - 1); changed = true; }
                Panel(new Rect(276, y, 240, 34), new Color(0.08f, 0.13f, 0.15f));
                if (slot == WardrobeSlot.Skin) Panel(new Rect(282, y + 6, 22, 22), BeanRig.SkinColors[index]);
                GUI.Label(new Rect(276, y, 240, 34), AvatarWardrobe.Name(slot, index) + "  (" + (index + 1) + "/" + AvatarWardrobe.Count(slot) + ")", choice);
                if (Button(new Rect(522, y, 44, 34), ">", Workshop.Cream)) { loadout.Set(slot, index + 1); changed = true; }
            }
            float buttons = 124 + slots.Length * 42 + 8;
            if (Button(new Rect(66, buttons, 160, 44), "RANDOM", Workshop.Mint))
            { foreach (WardrobeSlot slot in slots) loadout.Set(slot, Random.Range(0, AvatarWardrobe.Count(slot))); changed = true; }
            if (Button(new Rect(236, buttons, 160, 44), "PLAIN", Workshop.Cream)) { loadout = new AvatarLoadout(); changed = true; }
            if (Button(new Rect(406, buttons, 160, 44), "DONE", Workshop.Yellow)) CloseSettings();
            if (changed) { game.Player.Model.Apply(loadout); loadout.Save(); }
        }
    }
}



