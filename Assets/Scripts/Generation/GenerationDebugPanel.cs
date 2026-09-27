using UnityEngine;

namespace TheElevator.Generation
{
    public sealed class GenerationDebugPanel : MonoBehaviour
    {
        DescentGame game;
        bool visible, wasPaused;
        string seedText;
        int layer;
        MapSize size = MapSize.Standard;
        public void Initialize(DescentGame owner) { game = owner; }
        void Update()
        {
            if (!game || !game.UseProceduralFloors || !(Application.isEditor || Debug.isDebugBuild)) return;
            if (Input.GetKeyDown(KeyCode.F3))
            {
                visible = !visible;
                if (visible)
                {
                    wasPaused = game.Paused;
                    seedText = game.ActiveSeed.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    size = game.MapScale;
                    if (game.ControlsActive) game.SetPaused(true);
                }
                else if (!wasPaused && game.Paused) game.SetPaused(false);
            }
        }
        void OnGUI()
        {
            if (!visible || !game || !game.CurrentMap || !game.CurrentMap.Ready) return;
            GUI.depth = -20;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            Rect bounds = new Rect(20,20,1240,680);
            Color old = GUI.color; GUI.color = new Color(0.03f,0.05f,0.055f,0.98f); GUI.DrawTexture(bounds, Texture2D.whiteTexture); GUI.color = old;
            MapManifest map = game.CurrentMap.Manifest;
            GUI.Label(new Rect(42,38,400,30), "GENERATION LAB / F3 TO CLOSE");
            GUI.Label(new Rect(42,76,370,125), "SEED " + map.Recipe.Seed + "   /   VERSION " + map.Recipe.GenerationVersion +
                "\nPROFILE " + map.Recipe.Settings.ProfileId + "   /   " + map.Rooms.Count + " ROOMS\n" + map.CycleCount +
                " LOOPS  /  " + map.Recipe.Settings.VerticalLayers + " LEVELS\nAPPROX. " + map.EstimatedWalkableSquareMeters + " M2 BEFORE PROPS\nSTRUCTURE " + map.StructureHash.Substring(0,16));
            GUI.Label(new Rect(42,205,350,24), "Signed integer seed");
            seedText = GUI.TextField(new Rect(42,235,338,32), seedText ?? "0");
            if (GUI.Button(new Rect(42,281,338,32), "PROFILE: " + size)) size = (MapSize)(((int)size + 1) % 4);
            GUI.Label(new Rect(42,321,338,44), game.ProfileOverride ? "Inspector profile override takes precedence." : "Generation is allowed only from inside the lift.");
            if (GUI.Button(new Rect(42,372,338,40), "GENERATE THIS SEED"))
            {
                int seed;
                if (int.TryParse(seedText, out seed)) Regenerate(seed, false);
                else game.Notify("Enter a signed 32-bit integer seed.");
            }
            if (GUI.Button(new Rect(42,425,338,40), "NEW RANDOM SEED")) Regenerate(System.Environment.TickCount, false);
            if (GUI.Button(new Rect(42,478,338,40), "REPLAY LAST SAVED FLOOR")) Regenerate(0, true);
            if (GUI.Button(new Rect(42,540,338,32), "VIEW LEVEL: " + layer)) layer = (layer + 1) % map.Recipe.Settings.VerticalLayers;
            GUI.Label(new Rect(42,590,342,90), "Yellow: lift / landmark\nMint: objective   Red: optional risk\nPurple: stairs   Gray: other rooms\nGraph is a developer tool, not a player minimap.");
            MapGraphGUI.Draw(map, new Rect(415,45,820,625), Mathf.Min(layer,map.Recipe.Settings.VerticalLayers - 1));
        }
        void Regenerate(int seed, bool replay)
        {
            if (!game.Player.InCabin) { game.Notify("Return to the elevator before regenerating."); return; }
            visible = false;
            game.RegenerateMap(seed, size, replay);
        }
    }

    public static class MapGraphGUI
    {
        public static void Draw(MapManifest map, Rect rect, int layer)
        {
            int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
            foreach (MapRoom room in map.Rooms) if (room.Layer == layer)
            { minX = Mathf.Min(minX,room.X); maxX = Mathf.Max(maxX,room.X); minZ = Mathf.Min(minZ,room.Z); maxZ = Mathf.Max(maxZ,room.Z); }
            if (minX == int.MaxValue) return;
            float cell = Mathf.Min(rect.width / (maxX - minX + 2), rect.height / (maxZ - minZ + 2));
            System.Func<MapRoom, Vector2> point = room => new Vector2(rect.x + (room.X - minX + 1) * cell, rect.yMax - (room.Z - minZ + 1) * cell);
            foreach (MapLink link in map.Links)
            {
                MapRoom a = map.Rooms[link.A], b = map.Rooms[link.B];
                if (a.Layer != layer || b.Layer != layer) continue;
                Line(point(a), point(b), link.Kind == LinkKind.Shortcut ? Workshop.Mint : new Color(0.4f,0.5f,0.5f), 2);
            }
            foreach (MapRoom room in map.Rooms)
            {
                if (room.Layer != layer) continue;
                Vector2 p = point(room); float box = Mathf.Min(27,cell * 0.65f);
                Color c = room.IsStair ? new Color(0.7f,0.5f,0.9f) : room.Has(RoomRole.Entrance | RoomRole.Landmark) ? Workshop.Yellow
                    : room.Has(RoomRole.Objective) ? Workshop.Mint : room.Has(RoomRole.HighRisk) ? Workshop.Red : Workshop.Steel;
                Color old = GUI.color; GUI.color = c; GUI.DrawTexture(new Rect(p.x-box/2,p.y-box/2,box,box),Texture2D.whiteTexture); GUI.color = old;
                GUI.Label(new Rect(p.x+box/2,p.y-10,60,25),room.Id.ToString());
            }
        }
        static void Line(Vector2 a, Vector2 b, Color color, float width)
        {
            Matrix4x4 previous = GUI.matrix; Color old = GUI.color;
            GUI.color = color; GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x) * Mathf.Rad2Deg,a);
            GUI.DrawTexture(new Rect(a.x,a.y-width/2,(b-a).magnitude,width),Texture2D.whiteTexture);
            GUI.matrix = previous; GUI.color = old;
        }
    }
}
