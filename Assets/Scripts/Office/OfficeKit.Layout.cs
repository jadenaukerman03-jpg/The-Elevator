using System.Collections.Generic;
using UnityEngine;
using TheElevator.Generation;

namespace TheElevator.Office
{
    // Zone-aware furnishing. Every cell keeps a clear cross through its centre (the walking lanes between doorways);
    // furniture goes in the four quadrants and against solid walls, and what it is depends on what the room is for.
    public sealed partial class OfficeKit
    {
        enum Edge { Wall, Door, Open }

        // Quadrant centres and the wall-centre slot sit clear of the 3 m cross through the middle of the cell.
        const float Quadrant = 3.75f, WallSlot = 5.0f;

        Edge[] Edges(MapRoom room)
        {
            Edge[] edges = new Edge[4];
            foreach (int next in floor.Manifest.Neighbors(room.Id))
            {
                MapRoom n = floor.Manifest.Rooms[next]; if (n.Layer != room.Layer) continue;
                int port = n.X > room.X ? 1 : n.X < room.X ? 3 : n.Z > room.Z ? 0 : 2;
                edges[port] = Plan.Blended(room.Id, next) ? Edge.Open : Edge.Door;
            }
            for (int side = 0; side < 4; side++)
            {
                MapRoom n = floor.Manifest.Rooms.Find(r => r.X == room.X + OfficePlan.PortX[side] && r.Z == room.Z + OfficePlan.PortZ[side] && r.Layer == room.Layer);
                if (n != null && Plan.Blended(room.Id, n.Id)) edges[side] = Edge.Open;
            }
            if (room.Has(RoomRole.Entrance)) edges[2] = Edge.Door;
            return edges;
        }

        // Quadrants holding planned content (survey stations, batteries) stay empty.
        bool[] ReservedQuadrants(MapRoom room)
        {
            bool[] reserved = new bool[4];
            foreach (MapSocket socket in floor.Manifest.Sockets)
            {
                if (socket.RoomId != room.Id) continue;
                Vector3 local = Quaternion.Euler(0, room.QuarterTurns * 90, 0) * new[] { new Vector3(-1,0,-1), new Vector3(1,0,1), new Vector3(-1,0,1), new Vector3(1,0,-1) }[socket.Slot % 4];
                reserved[QuadrantIndex(local.x, local.z)] = true;
            }
            if (room.Id == Plan.TargetRoom) reserved[QuadrantIndex(1, 1)] = true; // the objective's dolly bay
            return reserved;
        }
        static int QuadrantIndex(float x, float z) { return (x > 0 ? 1 : 0) + (z > 0 ? 2 : 0); }
        static Vector3 QuadrantCentre(int q) { return new Vector3((q & 1) == 1 ? Quadrant : -Quadrant, 0, (q & 2) == 2 ? Quadrant : -Quadrant); }

        // A group in quadrant q whose back (+Z) faces the nearer outside wall, so seating and desks face the room.
        Transform QuadrantGroup(Transform geometry, int q, string name, Edge[] edges, float scale = 1)
        {
            Vector3 c = QuadrantCentre(q) * scale;
            int zSide = c.z > 0 ? 0 : 2, xSide = c.x > 0 ? 1 : 3;
            int back = edges[zSide] == Edge.Wall ? zSide : edges[xSide] == Edge.Wall ? xSide : zSide;
            return A.Group(geometry, name, c, back * 90);
        }

        // A group against the middle of a solid wall; local +Z points at the wall.
        Transform WallGroup(Transform geometry, int side, string name, float depth)
        {
            Transform t = A.Group(geometry, name, Vector3.zero, side * 90);
            return A.Group(t, name, new Vector3(0, 0, depth));
        }

        void Furnish(Transform geometry, MapRoom room, OfficeRoomPlan info, float height)
        {
            Edge[] edges = Edges(room);
            bool[] reserved = ReservedQuadrants(room);
            List<int> walls = new List<int>(), doors = new List<int>();
            for (int side = 0; side < 4; side++) { if (edges[side] == Edge.Wall) walls.Add(side); else if (edges[side] == Edge.Door) doors.Add(side); }
            // The wall opposite the only door is the room's focal wall.
            int focal = doors.Count == 1 && edges[(doors[0] + 2) % 4] == Edge.Wall ? (doors[0] + 2) % 4 : walls.Count > 0 ? walls[0] : -1;
            OfficeZone zone = Plan.ZoneFor(room.Id);
            HashSet<int> usedWalls = new HashSet<int>();
            for (int q = 0; q < 4; q++)
            {
                if (reserved[q]) continue;
                // Public rooms keep a walkway behind the reception counter.
                bool publicRoom = info.Kind == OfficeRoomKind.Lobby || info.Kind == OfficeRoomKind.Reception;
                Transform t = QuadrantGroup(geometry, q, "Furniture / " + info.Kind + " / " + q, edges, publicRoom ? 3.5f / Quadrant : 1);
                switch (info.Kind)
                {
                    case OfficeRoomKind.Lobby: if (q == 2) Reception(t, room.Id); else Lounge(t); break;
                    case OfficeRoomKind.Reception: if (q == 2) Reception(t, room.Id); else if (q == 3) Desk(t, room.Id, true); else Lounge(t); break;
                    case OfficeRoomKind.Workroom: DeskPair(t, room.Id); break;
                    case OfficeRoomKind.Executive:
                        if (q % 2 == 0) Lounge(t); else { Plant(t, new Vector3(1.2f,0,1.1f)); FilingCabinet(t, new Vector3(-.6f,0,1.2f)); }
                        break;
                    case OfficeRoomKind.Lounge: if (q < 2 || !zone.Closed) LoungeSeating(t, room.Id); else ArmchairPair(t); break;
                    case OfficeRoomKind.Breakroom: if (q == 3) Vending(A.Group(t, "Snack machine", new Vector3(0,0,.6f))); else if (q == 0 && zone.Closed) Lounge(t); else CafeTable(t); break;
                    case OfficeRoomKind.Records: Shelves(A.Group(t, "Stack row", new Vector3(0,0,.5f))); Shelves(A.Group(t, "Stack row", new Vector3(0,0,-.9f), 180)); Task(t, room.Id, OfficeTask.Filing, new Vector3(1.4f,0,-.2f), false, -90); break;
                    case OfficeRoomKind.Mailroom: if (q % 2 == 0) { Shelves(t); Task(t, room.Id, OfficeTask.Filing, new Vector3(0,0,-1.3f), false); } else SortingTable(t); break;
                    case OfficeRoomKind.Server:
                        for (int r = -1; r <= 1; r += 2) { Server(A.Group(t, "Rack", new Vector3(r * .6f,0,.6f))); Server(A.Group(t, "Rack", new Vector3(r * .6f,0,-1.3f), 180)); }
                        Task(t, room.Id, OfficeTask.Repair, new Vector3(0,0,-.35f), false); break;
                    case OfficeRoomKind.Restroom: if (q >= 2) Stalls(t); else Plant(t, new Vector3(0,0,1.2f)); break;
                    default: Shelves(t); break;
                }
            }
            // Focal-wall pieces: the thing the room is about.
            if (focal >= 0)
            {
                switch (info.Kind)
                {
                    case OfficeRoomKind.Executive:
                        Transform desk = WallGroup(geometry, focal, "Executive desk", 3.4f); desk.localRotation = Quaternion.Euler(0, 180, 0);
                        Desk(desk, room.Id, false);
                        for (int side = -1; side <= 1; side += 2) { Transform guest = A.Group(desk, "Guest chair", new Vector3(side * .6f, 0, 1.25f), 180); Chair(guest, Vector3.zero); }
                        Shelves(WallGroup(geometry, focal, "Bookcase", 5.1f));
                        usedWalls.Add(focal); break;
                    case OfficeRoomKind.Lounge: TvWall(WallGroup(geometry, focal, "Media wall", 5.6f)); usedWalls.Add(focal); break;
                    case OfficeRoomKind.Breakroom: Kitchen(WallGroup(geometry, focal, "Coffee counter", 5.0f), room.Id); usedWalls.Add(focal); break;
                    case OfficeRoomKind.Restroom: Washroom(WallGroup(geometry, focal, "Washbasins", 5.0f)); usedWalls.Add(focal); break;
                    case OfficeRoomKind.Workroom: case OfficeRoomKind.Records: case OfficeRoomKind.Mailroom:
                        PrinterStation(WallGroup(geometry, focal, "Printer station", 5.45f)); usedWalls.Add(focal); break;
                }
            }
            // Open offices get a kitchenette on a second wall when they span several cells.
            if (info.Kind == OfficeRoomKind.Workroom && zone.Rooms.Count > 1 && zone.Rooms[zone.Rooms.Count - 1] == room.Id)
                foreach (int side in walls) if (!usedWalls.Contains(side)) { Kitchen(WallGroup(geometry, side, "Kitchenette", 5.0f), room.Id); usedWalls.Add(side); break; }
            // Remaining solid walls get something that belongs in the room; tall pieces take the wall, low ones leave room for a display above.
            foreach (int side in walls)
            {
                if (usedWalls.Contains(side)) continue;
                switch (info.Kind)
                {
                    case OfficeRoomKind.Workroom: case OfficeRoomKind.Mailroom: case OfficeRoomKind.Records: case OfficeRoomKind.Reception:
                        Transform files = WallGroup(geometry, side, "Filing row", 5.5f);
                        for (int i = -1; i <= 1; i++) FilingCabinet(files, new Vector3(i * .55f,0,0));
                        break;
                    case OfficeRoomKind.Server:
                        Transform racks = WallGroup(geometry, side, "Wall racks", 5.2f);
                        for (int r = -1; r <= 1; r += 2) Server(A.Group(racks, "Rack", new Vector3(r * .6f,0,0)));
                        usedWalls.Add(side); break;
                    case OfficeRoomKind.Lounge: Lounge(WallGroup(geometry, side, "Wall sofa", 5.2f)); break;
                    case OfficeRoomKind.Breakroom: Fridge(WallGroup(geometry, side, "Fridge", 5.45f)); usedWalls.Add(side); break;
                    case OfficeRoomKind.Executive: Credenza(WallGroup(geometry, side, "Credenza", 5.55f)); break;
                    case OfficeRoomKind.Restroom: Washroom(WallGroup(geometry, side, "Washbasins", 5.0f)); usedWalls.Add(side); break;
                }
            }
            WallDetails(geometry, room, info, walls, usedWalls, doors);
        }

        // Wall-mounted details only ever go on solid walls: signs, clock, fire point, notices and a display.
        void WallDetails(Transform geometry, MapRoom room, OfficeRoomPlan info, List<int> walls, HashSet<int> usedWalls, List<int> doors)
        {
            if (walls.Count == 0) return;
            Transform first = A.Group(geometry, "Wall details", Vector3.zero, walls[0] * 90);
            Clock(first);
            Safety(A.Group(first, "Safety station", new Vector3(2.55f,0,5.72f)));
            DepartmentPlaque(first, info, room.Id);
            foreach (int side in walls)
            {
                if (usedWalls.Contains(side)) continue;
                Transform t = A.Group(geometry, "Wall details", Vector3.zero, side * 90);
                if (side == walls[0]) CorporateDisplay(t, room.Id); else Noticeboard(t, room.Id, info);
                usedWalls.Add(side);
                break;
            }
            // Plants flank each doorway on the inside.
            foreach (int side in doors)
            {
                if (room.Has(RoomRole.Entrance) && side == 2) continue;
                Transform t = A.Group(geometry, "Doorway planting", Vector3.zero, side * 90);
                Plant(t, new Vector3(2.15f, 0, 5.35f));
            }
        }

        // ---- Furniture ---------------------------------------------------------------------------------

        // Two desks face each other across a low screen: the open-office workstation unit.
        void DeskPair(Transform t, int room)
        {
            // Monitors face the shared screen in the middle; chairs sit on the outside.
            Desk(A.Group(t, "Workstation front", new Vector3(0,0,-.95f)), room, false);
            Desk(A.Group(t, "Workstation back", new Vector3(0,0,.95f), 180), room, false);
            A.Box(t, "Desk screen", new Vector3(0,1.05f,0), new Vector3(2.1f,.5f,.06f), A.Upholstery);
        }

        void LoungeSeating(Transform t, int room)
        {
            Lounge(t);
            Task(t, room, OfficeTask.Reading, new Vector3(-.62f,0,.05f), true, 180);
        }

        void ArmchairPair(Transform t)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Transform chair = A.Group(t, "Armchair", new Vector3(side * .75f,0,.3f), side * -15);
                A.Box(chair, "Armchair seat", new Vector3(0,.38f,0), new Vector3(.85f,.36f,.8f), A.Upholstery, true);
                A.Box(chair, "Armchair back", new Vector3(0,.72f,.34f), new Vector3(.85f,.55f,.16f), A.Upholstery);
                for (int arm = -1; arm <= 1; arm += 2) A.Box(chair, "Armchair arm", new Vector3(arm * .4f,.58f,0), new Vector3(.12f,.3f,.78f), A.Wood);
            }
            A.Round(t, "Side table", new Vector3(0,.28f,.6f), new Vector3(.45f,.28f,.45f), A.Wood);
            Mug(t, new Vector3(0,.56f,.6f));
        }

        void CafeTable(Transform t)
        {
            A.Round(t, "Cafe table top", new Vector3(0,.74f,0), new Vector3(1.05f,.035f,1.05f), A.Wood);
            A.Round(t, "Cafe table stem", new Vector3(0,.37f,0), new Vector3(.12f,.37f,.12f), A.Metal);
            A.Round(t, "Cafe table foot", new Vector3(0,.02f,0), new Vector3(.6f,.02f,.6f), A.Dark);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120 + 30;
                Transform seat = A.Group(t, "Cafe chair", Quaternion.Euler(0, angle, 0) * new Vector3(0,0,-.95f), angle);
                Chair(seat, Vector3.zero);
            }
            Mug(t, new Vector3(.2f,.76f,.1f));
        }

        void SortingTable(Transform t)
        {
            A.Box(t, "Sorting table", new Vector3(0,.8f,.2f), new Vector3(2.0f,.07f,.9f), A.Wood, true);
            for (int side = -1; side <= 1; side += 2) A.Box(t, "Table leg frame", new Vector3(side * .9f,.4f,.2f), new Vector3(.08f,.8f,.8f), A.Metal);
            for (int i = 0; i < 4; i++) A.Box(t, "Parcel", new Vector3(-.7f + i * .45f,.95f + (i % 2) * .03f,.2f), new Vector3(.36f,.24f + (i % 2) * .08f,.4f), i % 3 == 0 ? A.Paper : A.Wood);
        }

        void Stalls(Transform t)
        {
            for (int i = 0; i < 2; i++)
            {
                Transform stall = A.Group(t, "Toilet stall", new Vector3(-.55f + i * 1.1f,0,.2f));
                for (int side = -1; side <= 1; side += 2) A.Box(stall, "Stall partition", new Vector3(side * .54f,1.0f,0), new Vector3(.05f,1.8f,1.5f), A.Plastic, true);
                A.Box(stall, "Stall door", new Vector3(0,1.0f,-.74f), new Vector3(1.0f,1.75f,.04f), A.Plastic, true);
                A.Round(stall, "Toilet", new Vector3(0,.22f,.35f), new Vector3(.4f,.22f,.55f), A.Paper, PrimitiveType.Sphere);
            }
        }

        void FilingCabinet(Transform t, Vector3 p)
        {
            Transform c = A.Group(t, "Filing cabinet", p);
            A.Box(c, "Cabinet body", new Vector3(0,.66f,0), new Vector3(.5f,1.32f,.6f), A.Metal, true);
            for (int d = 0; d < 4; d++) A.Box(c, "Drawer pull", new Vector3(0,.25f + d * .32f,-.31f), new Vector3(.18f,.03f,.03f), A.Dark);
        }

        void Fridge(Transform t)
        {
            A.Box(t, "Staff fridge", new Vector3(-.45f,.95f,0), new Vector3(.8f,1.9f,.7f), A.Metal, true);
            A.Box(t, "Fridge handle", new Vector3(-.12f,1.2f,-.37f), new Vector3(.04f,.5f,.04f), A.Dark);
            A.Box(t, "Fridge notice", new Vector3(-.5f,1.45f,-.355f), new Vector3(.3f,.36f,.01f), A.Paper);
            A.Label(t, "LABEL YOUR\nFOOD OR\nLOSE IT", new Vector3(-.5f,1.45f,-.362f), .014f, Color.black);
            A.Box(t, "Recycling bin", new Vector3(.45f,.4f,0), new Vector3(.5f,.8f,.5f), A.Upholstery, true);
        }

        void Credenza(Transform t)
        {
            A.Box(t, "Credenza", new Vector3(0,.42f,0), new Vector3(2.0f,.84f,.5f), A.Wood, true);
            for (int i = -1; i <= 1; i += 2) A.Box(t, "Credenza door", new Vector3(i * .5f,.42f,-.255f), new Vector3(.9f,.7f,.01f), A.Dark);
            Mug(t, new Vector3(-.6f,.84f,0));
            A.Box(t, "Framed award", new Vector3(.5f,1.05f,.05f), new Vector3(.4f,.42f,.04f), A.Brass);
        }

        void TvWall(Transform t)
        {
            A.Box(t, "Media console", new Vector3(0,.3f,0), new Vector3(2.2f,.6f,.5f), A.Wood, true);
            A.Box(t, "Wall screen frame", new Vector3(0,1.65f,.23f), new Vector3(2.3f,1.3f,.06f), A.Dark);
            A.Box(t, "Wall screen", new Vector3(0,1.65f,.19f), new Vector3(2.15f,1.15f,.02f), A.Screen);
            A.Label(t, "MORROW NEWS / 24H", new Vector3(0,1.95f,.175f), .04f);
            A.Label(t, "SHARE PRICE UP. MORALE UNDER REVIEW.", new Vector3(0,1.55f,.175f), .024f);
            Plant(t, new Vector3(1.4f,0,.1f));
        }

        void PrinterStation(Transform t)
        {
            A.Box(t, "Printer cabinet", new Vector3(0,.4f,0), new Vector3(1.2f,.8f,.6f), A.Plastic, true);
            A.Box(t, "Printer", new Vector3(0,1.02f,0), new Vector3(.9f,.44f,.55f), A.Metal);
            A.Box(t, "Paper tray", new Vector3(0,.98f,-.3f), new Vector3(.5f,.04f,.12f), A.Paper);
            A.Box(t, "Printer panel", new Vector3(.3f,1.2f,-.27f), new Vector3(.2f,.08f,.02f), A.Screen);
            A.Round(t, "Water cooler bottle", new Vector3(1.05f,1.25f,0), new Vector3(.3f,.3f,.3f), A.Glass);
            A.Box(t, "Water cooler stand", new Vector3(1.05f,.5f,0), new Vector3(.36f,1.0f,.36f), A.Plastic, true);
        }

        void Clock(Transform wall)
        {
            Transform clock = A.Group(wall, "Department clock", new Vector3(-2.55f,3.25f,5.8f));
            A.Round(clock, "Clock face", Vector3.zero, new Vector3(.9f,.9f,.06f), A.Paper, PrimitiveType.Sphere);
            A.W.Soft.Ring(clock, "Plum clock rim", Vector3.zero, .45f, .045f, A.W.Soft.Plum);
            A.Box(clock, "Minute hand", new Vector3(.08f,.09f,-.05f), new Vector3(.055f,.34f,.02f), A.Red).transform.localRotation = Quaternion.Euler(0,0,-32);
            A.Box(clock, "Hour hand", new Vector3(-.08f,0,-.06f), new Vector3(.23f,.065f,.02f), A.Dark);
        }

        // Extinguisher on its backing board; the sign sits on the board.
        void Safety(Transform safety)
        {
            A.Box(safety,"Fire equipment backing",new Vector3(0,1.2f,0),new Vector3(.5f,1.2f,.055f),A.Dark);
            A.Round(safety,"Extinguisher tank",new Vector3(0,.95f,-.15f),new Vector3(.24f,.26f,.24f),A.Red);
            A.Box(safety,"Extinguisher grip",new Vector3(0,1.28f,-.15f),new Vector3(.2f,.05f,.07f),A.Metal);
            A.Box(safety,"Extinguisher hose",new Vector3(.14f,1.06f,-.15f),new Vector3(.035f,.39f,.035f),A.Dark);
            A.Label(safety,"FIRE\nAND OTHER FEELINGS",new Vector3(0,1.64f,-.035f),.021f);
        }

        void DepartmentPlaque(Transform wall, OfficeRoomPlan info, int room)
        {
            A.Box(wall, "Department plaque", new Vector3(-2.55f,2.35f,5.83f), new Vector3(1.55f,.42f,.05f), A.Dark);
            A.Label(wall, "M / " + OfficePlan.Departments[info.Department] + "\n" + KindName(info.Kind) + "  " + room.ToString("000"), new Vector3(-2.55f,2.35f,5.8f), .031f);
        }

        void CorporateDisplay(Transform wall, int room)
        {
            Transform display = A.Group(wall, "Corporate communications", new Vector3(0,0,5.8f));
            A.Box(display, "Corporate display frame", new Vector3(0,2.2f,0), new Vector3(2.25f,1.05f,.08f), A.Dark);
            A.Box(display, "Corporate display", new Vector3(0,2.2f,-.045f), new Vector3(2.1f,.91f,.018f), A.Screen);
            A.Label(display, room == 0 ? Plan.Config.Corporation : "PRODUCTIVITY / " + (91 + room % 9) + "%", new Vector3(0,2.43f,-.06f), .055f);
            A.Label(display, room % 3 == 0 ? "YOUR BREAK HAS BEEN OPTIMIZED." : "PLEASE ENJOY YOUR ASSIGNED PURPOSE.", new Vector3(0,2.1f,-.06f), .025f);
        }

        void Noticeboard(Transform wall, int room, OfficeRoomPlan info)
        {
            Transform board = A.Group(wall, "Noticeboard", new Vector3(0,0,5.8f));
            A.Box(board, "Noticeboard surround", new Vector3(0,1.95f,0), new Vector3(2.35f,1.3f,.06f), A.Wood);
            A.Box(board, "Noticeboard felt", new Vector3(0,1.95f,-.04f), new Vector3(2.23f,1.18f,.02f), A.Upholstery);
            string[] notices = { "MANDATORY JOY\nTHURSDAY / 09:00", "LOST: ONE HAND\nRETURN TO HR", "SAFETY RECORD\n003 DAYS", "COFFEE IS A\nREVOCABLE PRIVILEGE", "PROMOTION LIST\nPENDING FOREVER", "REMEMBER TO\nRECHARGE" };
            for (int i = 0; i < 3; i++)
            {
                Transform sheet = A.Group(board, "Pinned notice", new Vector3(-.72f + i * .72f,1.96f,-.06f));
                sheet.localRotation = Quaternion.Euler(0,0,(i - 1) * 4);
                A.Box(sheet, "Notice paper", Vector3.zero, new Vector3(.6f,.79f,.012f), A.Paper);
                A.Box(sheet, "Notice header", new Vector3(0,.27f,-.01f), new Vector3(.5f,.09f,.006f), A.DepartmentAccents[info.Department]);
                A.Label(sheet, notices[(room + i) % notices.Length], new Vector3(0,.07f,-.012f), .019f, Color.black);
                A.Round(sheet, "Notice pin", new Vector3(0,.36f,-.016f), new Vector3(.028f,.028f,.017f), A.Red, PrimitiveType.Sphere);
            }
        }

        static string KindName(OfficeRoomKind kind)
        {
            switch (kind)
            {
                case OfficeRoomKind.Workroom: return "OPEN OFFICE";
                case OfficeRoomKind.Executive: return "PRIVATE OFFICE";
                case OfficeRoomKind.Lounge: return "STAFF LOUNGE";
                case OfficeRoomKind.Breakroom: return "COFFEE BAR";
                case OfficeRoomKind.Conference: return "BOARDROOM";
                default: return kind.ToString().ToUpper();
            }
        }
    }
}
