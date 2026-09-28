using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using TheElevator.Office;

namespace TheElevator.Generation
{
    public sealed class FloorGeometryBuilder
    {
        readonly Workshop w;
        readonly FloorContentCatalog catalog;
        readonly Dictionary<string, ModuleSpec> modules = new Dictionary<string, ModuleSpec>(StringComparer.Ordinal);
        GeneratedFloor floor;
        GenerationSettings settings;
        OfficeFloor office;
        readonly Vector3[] corners = { new Vector3(-3.8f,0,-3.8f), new Vector3(3.8f,0,3.8f), new Vector3(-3.8f,0,3.8f), new Vector3(3.8f,0,-3.8f) };

        public FloorGeometryBuilder(Workshop workshop, FloorContentCatalog theme) { w = workshop; catalog = theme; }

        public IEnumerator Build(GeneratedFloor target, Action<float> progress, Action<GenerationSocket> populate)
        {
            floor = target; settings = floor.Manifest.Recipe.Settings;
            if (OfficePlan.Enabled(floor.Manifest.Recipe)) { office = floor.gameObject.AddComponent<OfficeFloor>(); office.Prepare(w,floor); }
            foreach (ModuleSpec module in floor.Manifest.Recipe.Modules) modules.Add(module.Id, module);
            Stopwatch budget = Stopwatch.StartNew();
            int total = floor.Manifest.Rooms.Count + floor.Manifest.Links.Count + floor.Manifest.Sockets.Count, done = 0;
            foreach (MapRoom room in floor.Manifest.Rooms)
            {
                BuildRoom(room); done++; progress?.Invoke((float)done / total);
                if (budget.ElapsedMilliseconds >= settings.GeometryBudgetMilliseconds) { yield return null; budget.Restart(); }
            }
            foreach (MapLink link in floor.Manifest.Links)
            {
                if (link.Kind != LinkKind.Stairs) BuildCorridor(link);
                done++; progress?.Invoke((float)done / total);
                if (budget.ElapsedMilliseconds >= settings.GeometryBudgetMilliseconds) { yield return null; budget.Restart(); }
            }
            foreach (MapSocket plan in floor.Manifest.Sockets)
            {
                MapRoom room = floor.Manifest.Rooms[plan.RoomId];
                Transform root = floor.RoomRoots[room.Id];
                Vector3 local = Quaternion.Euler(0, room.QuarterTurns * 90, 0) * corners[plan.Slot % 4];
                Transform marker = w.Group(plan.Id, root, local);
                GenerationSocket socket = marker.gameObject.AddComponent<GenerationSocket>();
                socket.StableId = plan.Id; socket.Kind = plan.Kind; socket.RoomId = plan.RoomId; socket.ContentSeed = plan.ContentSeed;
                floor.ContentSockets.Add(socket);
                if (plan.Kind == SocketKind.Objective) BuildTerminal(marker, socket);
                populate?.Invoke(socket);
                done++; progress?.Invoke((float)done / total);
                if (budget.ElapsedMilliseconds >= settings.GeometryBudgetMilliseconds) { yield return null; budget.Restart(); }
            }
            floor.PrepareVisibility(); floor.Ready = true; progress?.Invoke(1);
        }

        Color Accent(MapRoom room)
        {
            if (room.District == "UTILITIES") return catalog ? catalog.UtilitiesAccent : new Color(0.21f,0.67f,0.78f);
            if (room.District == "DISPATCH") return catalog ? catalog.DispatchAccent : Workshop.Red;
            return catalog ? catalog.RecordsAccent : Workshop.Yellow;
        }
        void Box(Transform root, string name, Vector3 p, Vector3 size, Color color, bool collision = true)
        { w.Shape(name, root, p, size, color, PrimitiveType.Cube, collision); }

        void BuildRoom(MapRoom room)
        {
            Transform root = w.Group("R" + room.Id.ToString("000") + " / " + room.District + " / " + room.ModuleId,
                floor.transform, floor.transform.InverseTransformPoint(floor.Center(room)));
            floor.RoomRoots.Add(root);
            Transform geometry = w.Group("Architecture", root, Vector3.zero);
            ModuleSpec module = modules[room.ModuleId];
            float height = room.Has(RoomRole.StairUp) ? 6 : module.CeilingMillimeters / 1000f;
            Color wall = catalog ? catalog.WallColor : new Color(0.27f,0.34f,0.33f);
            Color deck = catalog ? catalog.FloorColor : new Color(0.20f,0.24f,0.23f);
            Color accent = Accent(room);
            if (room.Has(RoomRole.StairDown)) UpperDeck(geometry, deck);
            else Box(geometry, "Floor slab", new Vector3(0,-0.15f,0), new Vector3(12,0.3f,12), deck);
            if (!room.Has(RoomRole.StairUp)) Box(geometry, "Ceiling", new Vector3(0,height + 0.10f,0), new Vector3(12.2f,0.2f,12.2f), Workshop.Ink);

            HashSet<int> ports = new HashSet<int>();
            foreach (int next in floor.Manifest.Neighbors(room.Id))
            {
                MapRoom other = floor.Manifest.Rooms[next];
                if (other.Layer == room.Layer) ports.Add(other.X > room.X ? 1 : other.X < room.X ? 3 : other.Z > room.Z ? 0 : 2);
            }
            if (room.Has(RoomRole.Entrance)) ports.Add(2);
            // Blended neighbours share one room: no wall, and the gap between the cells becomes floor.
            HashSet<int> blended = new HashSet<int>();
            if (office)
                for (int side = 0; side < 4; side++)
                {
                    MapRoom n = CellAt(room.X + OfficePlan.PortX[side], room.Z + OfficePlan.PortZ[side], room.Layer);
                    if (n != null && office.Plan.Blended(room.Id, n.Id)) { blended.Add(side); ports.Remove(side); }
                }
            foreach (int side in blended) if (side <= 1) BlendSpan(geometry, room, side, height, wall, deck);
            if (office && SameZone(room, 1, 0) && SameZone(room, 0, 1) && SameZone(room, 1, 1))
            {
                float gap = settings.CellSizeMillimeters / 1000f - 12, mid = 6 + gap / 2;
                Box(geometry, "Floor slab", new Vector3(mid,-0.15f,mid), new Vector3(gap,0.3f,gap), deck);
                Box(geometry, "Ceiling", new Vector3(mid,height + 0.10f,mid), new Vector3(gap,0.2f,gap), Workshop.Ink);
            }
            int annex = office && office.Plan.MeetingRoom == room.Id ? office.Plan.MeetingAnnexSide : -1;
            if (annex >= 0)
            {
                Annex(geometry, annex, height, wall, accent, deck);
                float cell = settings.CellSizeMillimeters / 1000f;
                floor.Extensions.Add(new KeyValuePair<int, Vector3>(room.Id, floor.Center(room) + floor.transform.TransformDirection(new Vector3(OfficePlan.PortX[annex], 0, OfficePlan.PortZ[annex])) * cell));
            }
            for (int side = 0; side < 4; side++)
            {
                if (side == annex || blended.Contains(side)) continue;
                Transform segment = w.Group("Wall " + side, geometry, Vector3.zero);
                segment.localRotation = Quaternion.Euler(0, side * 90, 0);
                float opening = room.Has(RoomRole.Entrance) && side == 2 ? 5f : settings.DoorWidthMillimeters / 1000f;
                Wall(segment, ports.Contains(side), opening, height, wall, accent);
                if (ports.Contains(side) && !office)
                {
                    string sign = room.Has(RoomRole.Entrance) && side == 2 ? "FREIGHT 04 / EXIT" : room.District + " / " + room.Id.ToString("000");
                    w.Label(sign, segment, new Vector3(0, settings.DoorHeightMillimeters / 1000f + 0.24f, 5.82f), 0.037f, accent);
                }
            }
            Transform interior = w.Group("Authored interior", geometry, Vector3.zero);
            interior.localRotation = Quaternion.Euler(0, room.QuarterTurns * 90, 0);
            if (room.Has(RoomRole.StairUp)) Staircase(geometry, root, accent);
            if (!room.IsStair && !office)
            {
                GameObject authored = catalog ? catalog.FindPrefab(room.ModuleId) : null;
                if (authored) UnityEngine.Object.Instantiate(authored, interior, false);
                else Decorate(interior, room, module.Style, accent);
                if (room.WetFloor && !room.Has(RoomRole.Entrance))
                    Box(geometry, "Shallow condensation", new Vector3(0,0.025f,0), new Vector3(11.6f,0.035f,11.6f), new Color(0.11f,0.29f,0.31f), false);
            }
            if (room.Has(RoomRole.Entrance) && !office)
            {
                w.Label("CIVIC WORKS / ARRIVALS", root, new Vector3(0,3.42f,5.78f), 0.065f, Workshop.Cream);
                w.Label("FOLLOW YOUR TEAM.\nTHE BUILDING WILL NOT HELP.", root, new Vector3(-4,2.55f,5.78f), 0.03f, Workshop.Cream);
            }
            if (room.Has(RoomRole.Landmark) && !office)
                w.Label(room.LandmarkName, root, new Vector3(0,3.35f,5.78f), 0.062f, accent);
            if (office) office.Kit.Dress(geometry,room,height);
            for (int side = -1; side <= 1 && !office; side += 2)
            {
                Box(geometry, "Strip-light housing", new Vector3(side * 3.8f,height - 0.15f,0), new Vector3(0.18f,0.12f,3.2f), Workshop.Cream, false);
                w.Lamp(root, new Vector3(side * 3.8f,Mathf.Min(height - 0.4f,4),0), side < 0 ? Workshop.Cream : accent, 2.2f, 9);
                // Overhead service lines reinforce district identity without blocking eye-level routes.
                Box(geometry, "Service conduit", new Vector3(side * 4.5f,height - 0.45f,0), new Vector3(0.16f,0.16f,11.6f), accent, false);
            }
            CombineStaticMeshes(geometry);
        }

        MapRoom CellAt(int x, int z, int layer) { return floor.Manifest.Rooms.Find(r => r.X == x && r.Z == z && r.Layer == layer); }
        bool SameZone(MapRoom room, int dx, int dz)
        {
            MapRoom other = CellAt(room.X + dx, room.Z + dz, room.Layer);
            return other != null && office.Plan.ZoneOf[other.Id] == office.Plan.ZoneOf[room.Id];
        }

        // Floor, ceiling and outer walls across the gap between two blended cells (built once, from the lower cell).
        void BlendSpan(Transform geometry, MapRoom room, int side, float height, Color wall, Color deck)
        {
            float gap = settings.CellSizeMillimeters / 1000f - 12, mid = 6 + gap / 2;
            Transform span = w.Group("Blended span " + side, geometry, Vector3.zero);
            span.localRotation = Quaternion.Euler(0, side * 90, 0);
            Box(span, "Floor slab", new Vector3(0,-0.15f,mid), new Vector3(12,0.3f,gap), deck);
            Box(span, "Ceiling", new Vector3(0,height + 0.10f,mid), new Vector3(12,0.2f,gap), Workshop.Ink);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                int lateral = sign > 0 ? (side + 1) % 4 : (side + 3) % 4;
                int lx = OfficePlan.PortX[lateral], lz = OfficePlan.PortZ[lateral];
                bool interior = SameZone(room, lx, lz) && SameZone(room, OfficePlan.PortX[side] + lx, OfficePlan.PortZ[side] + lz);
                if (!interior) Box(span, "Wall", new Vector3(sign * 6,height / 2,mid), new Vector3(0.24f,height,gap + 0.24f), wall);
            }
        }

        // A room that runs two cells long: the neighbouring empty cell and the gap between them become one space.
        void Annex(Transform geometry, int side, float height, Color wall, Color accent, Color deck)
        {
            float cell = settings.CellSizeMillimeters / 1000f, gap = cell - 12;
            Transform annex = w.Group("Two-cell room extension", geometry, Vector3.zero);
            annex.localRotation = Quaternion.Euler(0, side * 90, 0);
            Box(annex, "Floor slab", new Vector3(0,-0.15f,cell / 2 + 6), new Vector3(12,0.3f,cell), deck);
            Box(annex, "Ceiling", new Vector3(0,height + 0.10f,cell / 2 + 6.1f), new Vector3(12.2f,0.2f,cell), Workshop.Ink);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Box(annex, "Wall", new Vector3(sign * 6,height / 2,6 + gap / 2), new Vector3(0.24f,height,gap + 0.24f), wall);
                Transform far = w.Group("Wall " + (sign < 0 ? "extension left" : "extension right"), annex, new Vector3(0,0,cell));
                far.localRotation = Quaternion.Euler(0, sign * 90, 0);
                Wall(far, false, 0, height, wall, accent);
            }
            Transform end = w.Group("Wall extension end", annex, new Vector3(0,0,cell));
            Wall(end, false, 0, height, wall, accent);
        }

        void Wall(Transform root, bool open, float opening, float height, Color wall, Color accent)
        {
            float doorHeight = settings.DoorHeightMillimeters / 1000f;
            if (!open) Box(root, "Wall", new Vector3(0,height / 2,6), new Vector3(12.2f,height,0.24f), wall);
            else
            {
                float width = (12 - opening) / 2;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Box(root, "Door-side wall", new Vector3(sign * (opening / 2 + width / 2),height / 2,6), new Vector3(width,height,0.24f), wall);
                    Box(root, "Door frame", new Vector3(sign * (opening / 2 + 0.08f),doorHeight / 2,5.83f), new Vector3(0.12f,doorHeight,0.08f), accent, false);
                }
                Box(root, "Door lintel", new Vector3(0,(height + doorHeight) / 2,6), new Vector3(opening,height - doorHeight,0.24f), wall);
            }
            float segmentWidth = open ? (12 - opening) / 2 : 12;
            if (!open) Box(root, "Department stripe", new Vector3(0,1.2f,5.86f), new Vector3(12,0.12f,0.025f), accent, false);
            else for (int sign = -1; sign <= 1; sign += 2)
                Box(root, "Department stripe", new Vector3(sign * (opening / 2 + segmentWidth / 2),1.2f,5.86f), new Vector3(segmentWidth,0.12f,0.025f), accent, false);
        }

        void Decorate(Transform root, MapRoom room, ModuleStyle style, Color accent)
        {
            MapRandom decor = new MapRandom(floor.Manifest.Recipe.Seed, unchecked(0x50524f50u + (uint)room.Id * 997u));
            for (int slot = 0; slot < 4; slot++)
            {
                bool reserved = floor.Manifest.Sockets.Exists(s => s.RoomId == room.Id && s.Slot == slot);
                if (reserved || (style != ModuleStyle.Landmark && decor.Range(100) >= settings.PropDensityPercent)) continue;
                Transform cluster = w.Group("Corner dressing " + slot, root, corners[slot]);
                if (style == ModuleStyle.Records)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        Box(cluster, "Filing tower", new Vector3(x * 0.67f,1.15f,0), new Vector3(0.6f,2.3f,1.4f), Workshop.Steel);
                        for (int drawer = 0; drawer < 5; drawer++)
                            Box(cluster, "Drawer label", new Vector3(x * 0.67f,0.3f + drawer * 0.42f,-0.72f), new Vector3(0.34f,0.06f,0.025f), Workshop.Cream, false);
                    }
                }
                else if (style == ModuleStyle.Pumps)
                {
                    w.Shape("Pressure vessel", cluster, new Vector3(0,1.35f,0), new Vector3(1.7f,1.35f,1.7f), Workshop.Steel, PrimitiveType.Cylinder);
                    w.Shape("Pressure band", cluster, new Vector3(0,1.8f,0), new Vector3(1.76f,0.08f,1.76f), accent, PrimitiveType.Cylinder, false);
                    w.Shape("Valve", cluster, new Vector3(0,1.3f,-0.92f), new Vector3(0.6f,0.6f,0.12f), Workshop.Red, PrimitiveType.Sphere, false);
                }
                else if (style == ModuleStyle.Sorting)
                {
                    Box(cluster, "Sorting conveyor", new Vector3(0,0.85f,0), new Vector3(2.1f,0.3f,1.5f), Workshop.Steel);
                    for (int box = 0; box < 3; box++)
                        Box(cluster, "Unclaimed parcel", new Vector3(-0.6f + box * 0.6f,1.24f,0), new Vector3(0.48f,0.5f,0.8f), box == 1 ? accent : new Color(0.55f,0.45f,0.29f));
                }
                else if (style == ModuleStyle.Landmark)
                {
                    Box(cluster, "Monument plinth", new Vector3(0,0.25f,0), new Vector3(2.2f,0.5f,2.2f), Workshop.Ink);
                    w.Shape("Civic monument", cluster, new Vector3(0,1.7f,0), new Vector3(1.8f,2.4f,1.8f), accent,
                        room.Id % 2 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube);
                    w.Shape("Unnecessary dial", cluster, new Vector3(0,2.0f,-1), new Vector3(0.8f,0.8f,0.1f), Workshop.Cream, PrimitiveType.Sphere, false);
                }
                else
                {
                    Box(cluster, "Staff bench", new Vector3(0,0.45f,0), new Vector3(2.2f,0.4f,0.9f), accent);
                    Box(cluster, "Notice board", new Vector3(0,1.7f,0.65f), new Vector3(1.9f,1.1f,0.15f), Workshop.Cream);
                    for (int paper = 0; paper < 3; paper++) Box(cluster, "Old notice", new Vector3(-0.6f + paper * 0.6f,1.7f,0.55f), new Vector3(0.4f,0.65f,0.02f), Workshop.Yellow, false);
                }
            }
        }

        void BuildCorridor(MapLink link)
        {
            if (office && office.Plan.Blended(link.A, link.B)) return;
            MapRoom a = floor.Manifest.Rooms[link.A], b = floor.Manifest.Rooms[link.B];
            Vector3 center = (floor.Center(a) + floor.Center(b)) / 2;
            Transform root = w.Group("Link " + link.A + "-" + link.B + " / " + link.Kind, floor.transform, floor.transform.InverseTransformPoint(center));
            root.rotation = Quaternion.LookRotation(floor.Center(b) - floor.Center(a));
            float length = (settings.CellSizeMillimeters - settings.RoomSizeMillimeters) / 1000f + 0.3f;
            float width = settings.CorridorWidthMillimeters / 1000f;
            Box(root, "Connector deck", new Vector3(0,-0.15f,0), new Vector3(width,0.3f,length), Workshop.Steel);
            Box(root, "Connector ceiling", new Vector3(0,3.3f,0), new Vector3(width + 0.4f,0.2f,length), Workshop.Ink);
            for (int side = -1; side <= 1; side += 2)
                Box(root, "Connector wall", new Vector3(side * (width / 2 + 0.1f),1.6f,0), new Vector3(0.2f,3.2f,length), Workshop.Steel);
            if(office)office.Kit.DressConnector(root,length,width,link);
            CombineStaticMeshes(root);
        }

        void UpperDeck(Transform root, Color deck)
        {
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Box(root, "Upper side gallery", new Vector3(sign * 4.8f,-0.15f,0), new Vector3(2.4f,0.3f,12), deck);
                Box(root, "Upper landing", new Vector3(0,-0.15f,sign * 5.1f), new Vector3(7.2f,0.3f,1.8f), deck);
                Rail(root, new Vector3(sign * 3.6f,0.55f,0), new Vector3(0.1f,1.1f,8.4f));
            }
            Rail(root, new Vector3(0,0.55f,4.2f), new Vector3(7.2f,1.1f,0.1f));
            Rail(root, new Vector3(-1.45f,0.55f,-4.2f), new Vector3(4.3f,1.1f,0.1f));
            Box(root, "Stair exit landing", new Vector3(2,-0.15f,-3.8f), new Vector3(2.4f,0.3f,1.8f), deck);
        }

        void Rail(Transform root, Vector3 center, Vector3 size)
        {
            // Full box collision, open-looking visual railing. Height 1.1 m.
            GameObject bounds = new GameObject("Safety railing collision"); bounds.transform.SetParent(root, false); bounds.transform.localPosition = center;
            bounds.AddComponent<BoxCollider>().size = size;
            bool alongZ = size.z > size.x;
            Box(root, "Handrail", center + Vector3.up * 0.47f, new Vector3(size.x,0.10f,size.z), Workshop.Yellow, false);
            float length = alongZ ? size.z : size.x;
            for (float offset = -length / 2; offset <= length / 2; offset += 1.4f)
                Box(root, "Railing post", center + (alongZ ? Vector3.forward : Vector3.right) * offset, new Vector3(0.08f,1.1f,0.08f), Workshop.Steel, false);
        }

        void Staircase(Transform geometry, Transform root, Color accent)
        {
            Flight(geometry, new Vector3(-2,0,0), false, accent);
            Flight(geometry, new Vector3(2,3,0), true, accent);
            Box(geometry, "Half landing", new Vector3(0,2.85f,3.7f), new Vector3(6.4f,0.3f,1.4f), Workshop.Steel);
            Box(geometry, "Bottom landing", new Vector3(-2,-0.15f,-3.7f), new Vector3(2.4f,0.3f,1.4f), Workshop.Steel);
            Rail(geometry, new Vector3(0,3.55f,4.4f), new Vector3(6.4f,1.1f,0.10f));
            Box(geometry, "Stair sign board", new Vector3(-5.86f,1.7f,-2.9f), new Vector3(0.06f,0.8f,1.6f), Workshop.Ink, false);
            w.Label("STAFF GALLERY\nUP ONE LEVEL", root, new Vector3(-5.8f,1.7f,-2.9f), 0.055f, accent, -90);
        }
        void Flight(Transform root, Vector3 offset, bool reverse, Color accent)
        {
            Transform flight = w.Group("Stair flight", root, offset);
            if (reverse) flight.localRotation = Quaternion.Euler(0,180,0);
            const int steps = 18;
            for (int i = 0; i < steps; i++)
            {
                float rise = (i + 1) / 6f;
                Box(flight, "Tread " + i, new Vector3(0,rise / 2,-3 + (i + 0.5f) / 3), new Vector3(2.4f,rise,1f / 3), Workshop.Steel, false);
                Box(flight, "Tread edge", new Vector3(0,rise + 0.004f,-3 + i / 3f), new Vector3(2.4f,0.014f,0.035f), accent, false);
            }
            Mesh mesh = new Mesh { name = "Smooth stair collision" };
            mesh.vertices = new[] { new Vector3(-1.2f,0,-3), new Vector3(1.2f,0,-3), new Vector3(-1.2f,3,3), new Vector3(1.2f,3,3) };
            mesh.triangles = new[] { 0,2,1,1,2,3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            flight.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh; floor.OwnedMeshes.Add(mesh);
        }

        void BuildTerminal(Transform root, GenerationSocket socket)
        {
            Box(root, "Remote survey station", new Vector3(0,0.75f,0), new Vector3(0.8f,1.5f,0.6f), Workshop.Ink);
            Box(root, "Station display", new Vector3(0,1.25f,-0.32f), new Vector3(0.6f,0.35f,0.035f), Workshop.Mint, false);
            ObjectiveTerminal terminal = root.gameObject.AddComponent<ObjectiveTerminal>();
            terminal.Floor = floor; terminal.SocketId = socket.StableId;
            w.Label("SURVEY / " + socket.RoomId.ToString("000"), root, new Vector3(0,1.04f,-.327f), 0.027f, Workshop.Mint);
        }

        void CombineStaticMeshes(Transform root)
        {
            Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();
            List<MeshRenderer> sources = new List<MeshRenderer>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (!renderer || !filter.sharedMesh || filter.GetComponent<TextMesh>() || filter.GetComponentInParent<Rigidbody>(true) || (filter.GetComponentInParent<TheElevator.Office.OfficeEquipment>(true) && filter.GetComponentInParent<TheElevator.Office.OfficeEquipment>(true).Recoverable) || filter.sharedMesh.subMeshCount != 1) continue;
                Material mat = renderer.sharedMaterial;
                if (!mat) continue;
                if (!groups.ContainsKey(mat)) groups[mat] = new List<CombineInstance>();
                groups[mat].Add(new CombineInstance { mesh = filter.sharedMesh, transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                sources.Add(renderer);
            }
            foreach (var group in groups)
            {
                Mesh mesh = new Mesh { name = "Room batch", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray()); floor.OwnedMeshes.Add(mesh);
                Transform merged = w.Group("Batched geometry", root, Vector3.zero);
                merged.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                merged.gameObject.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
            }
            foreach (MeshRenderer source in sources)
            {
                MeshFilter filter = source.GetComponent<MeshFilter>();
                if (source.transform.childCount == 0 && source.GetComponents<Component>().Length == 3)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(source.gameObject); else UnityEngine.Object.DestroyImmediate(source.gameObject);
                    continue;
                }
                if (Application.isPlaying) { UnityEngine.Object.Destroy(source); UnityEngine.Object.Destroy(filter); }
                else { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(filter); }
            }
        }
    }
}



