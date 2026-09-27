using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TheElevator.Generation
{
    public sealed class MacroLayoutGenerator
    {
        readonly int[] dx = { 0, 1, 0, -1 }, dz = { 1, 0, -1, 0 };
        MapManifest map;
        MapRandom random;
        GenerationSettings settings;
        readonly Dictionary<string, int> occupied = new Dictionary<string, int>();
        int operations;

        public MapManifest Generate(MapRecipe recipe)
        {
            ValidateRecipe(recipe);
            settings = recipe.Settings;
            List<string> failures = new List<string>();
            for (int attempt = 0; attempt < settings.MaxGenerationAttempts; attempt++)
            {
                map = new MapManifest { Recipe = recipe, Attempt = attempt, ConfigurationHash = recipe.ConfigurationHash() };
                occupied.Clear(); operations = 0;
                random = new MapRandom(recipe.Seed, unchecked(0x53545255u + (uint)attempt * 7919u));
                try
                {
                    BuildLayout();
                    AssignMeaning();
                    PlanContent();
                    ComputeHashes(map);
                    List<string> errors = MapValidator.Validate(map);
                    if (errors.Count != 0) throw new InvalidOperationException(string.Join("; ", errors.ToArray()));
                    map.FailedAttempts.AddRange(failures);
                    return map;
                }
                catch (InvalidOperationException error)
                {
                    failures.Add("attempt=" + attempt + " seed=" + recipe.Seed + " " + error.Message);
                }
            }
            throw new InvalidOperationException("Generation failed for seed " + recipe.Seed + " config " + recipe.ConfigurationHash()
                + " after " + settings.MaxGenerationAttempts + " bounded attempts. " + string.Join(" | ", failures.ToArray()));
        }

        public static void ValidateRecipe(MapRecipe recipe)
        {
            if (recipe == null || recipe.Settings == null || recipe.Modules == null) throw new ArgumentException("Incomplete map recipe.");
            if (recipe.GenerationVersion != MapRecipe.SupportedVersion) throw new ArgumentException("Unsupported generation version; do not silently replay using a newer algorithm.");
            recipe.Settings.Validate();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<ModuleStyle> styles = new HashSet<ModuleStyle>();
            foreach (ModuleSpec module in recipe.Modules)
            {
                if (module == null || string.IsNullOrEmpty(module.Id) || module.Id.IndexOfAny(new[] { '|', ':', '\n' }) >= 0 || !ids.Add(module.Id))
                    throw new ArgumentException("Module IDs must be unique, nonempty, and cannot contain separators.");
                if (module.Weight < 1 || module.Weight > 100 || module.CeilingMillimeters < 3200 || module.CeilingMillimeters > 5800 || !Enum.IsDefined(typeof(ModuleStyle), module.Style))
                    throw new ArgumentException("Invalid module weight, clearance, or style: " + module.Id);
                styles.Add(module.Style);
            }
            foreach (ModuleStyle required in new[] { ModuleStyle.Arrival, ModuleStyle.Landmark, ModuleStyle.Stairwell })
                if (!styles.Contains(required)) throw new ArgumentException("The module catalog lacks the required " + required + " family.");
            if (recipe.Districts == null || recipe.Districts.Length == 0) throw new ArgumentException("At least one department is required.");
            HashSet<string> districtIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DistrictSpec district in recipe.Districts)
            {
                if (district == null || string.IsNullOrEmpty(district.Id) || !districtIds.Add(district.Id) ||
                    string.IsNullOrEmpty(district.DisplayName) || district.Id.IndexOfAny(new[] { '|', ':', '\n' }) >= 0 ||
                    district.DisplayName.IndexOfAny(new[] { '|', ':', '\n' }) >= 0 || district.ModuleIds == null || district.ModuleIds.Length == 0)
                    throw new ArgumentException("Departments need unique IDs, names, and a nonempty module selection without separators.");
                HashSet<string> choices = new HashSet<string>(StringComparer.Ordinal);
                foreach (string id in district.ModuleIds)
                {
                    if (id == null || !ids.Contains(id) || !choices.Add(id)) throw new ArgumentException("Unknown or duplicate department module: " + id);
                    ModuleSpec selected = Array.Find(recipe.Modules, m => m.Id == id);
                    if (selected.Style == ModuleStyle.Stairwell || selected.Style == ModuleStyle.Arrival || selected.Style == ModuleStyle.Landmark)
                        throw new ArgumentException("Department selections must use ordinary interior modules.");
                }
            }
        }

        string Key(int x, int z, int layer) { return x + ":" + z + ":" + layer; }
        void Budget() { if (++operations > settings.OperationBudget) throw new InvalidOperationException("Deterministic operation budget exceeded."); }
        bool Free(int x, int z, int layer) { Budget(); return z >= 0 && !occupied.ContainsKey(Key(x, z, layer)); }
        MapRoom Add(int x, int z, int layer, RoomRole role, int branchDepth)
        {
            if (!Free(x, z, layer)) throw new InvalidOperationException("Placement collision.");
            MapRoom room = new MapRoom { Id = map.Rooms.Count, X = x, Z = z, Layer = layer, Roles = role, BranchDepth = branchDepth };
            occupied.Add(Key(x, z, layer), room.Id); map.Rooms.Add(room); return room;
        }
        void Link(MapRoom a, MapRoom b, LinkKind kind) { map.Links.Add(new MapLink(a.Id, b.Id, kind)); }

        void BuildLayout()
        {
            MapRoom previousExit = null;
            for (int layer = 0; layer < settings.VerticalLayers; layer++)
            {
                int roomGoal = settings.TargetRooms / settings.VerticalLayers + (layer < settings.TargetRooms % settings.VerticalLayers ? 1 : 0);
                int mainGoal = settings.MainRouteRooms / settings.VerticalLayers + (layer < settings.MainRouteRooms % settings.VerticalLayers ? 1 : 0);
                int loopGoal = settings.LoopCount / settings.VerticalLayers + (layer < settings.LoopCount % settings.VerticalLayers ? 1 : 0);
                int first = map.Rooms.Count;
                MapRoom start = Add(previousExit == null ? 0 : previousExit.X, previousExit == null ? 0 : previousExit.Z, layer,
                    RoomRole.MainRoute | (layer == 0 ? RoomRole.Entrance : RoomRole.StairDown), 0);
                if (previousExit != null) { previousExit.Roles |= RoomRole.StairUp; Link(previousExit, start, LinkKind.Stairs); }
                List<MapRoom> spine = new List<MapRoom> { start };
                int lastDirection = 0, straight = 0;
                for (int step = 1; step < mainGoal; step++)
                {
                    MapRoom previous = spine[spine.Count - 1];
                    List<int> directions = new List<int> { 0, 1, 3 }; // Progress into the facility, with deliberate turns.
                    random.Shuffle(directions);
                    if (straight >= 2) { directions.Remove(lastDirection); directions.Add(lastDirection); }
                    MapRoom next = null;
                    foreach (int direction in directions)
                    {
                        if (!Free(previous.X + dx[direction], previous.Z + dz[direction], layer)) continue;
                        if (step == 1 && layer == 0 && direction != 0) continue; // Recognizable arrival axis.
                        next = Add(previous.X + dx[direction], previous.Z + dz[direction], layer, RoomRole.MainRoute, 0);
                        Link(previous, next, LinkKind.Passage);
                        straight = direction == lastDirection ? straight + 1 : 1; lastDirection = direction;
                        break;
                    }
                    if (next == null) throw new InvalidOperationException("Main route could not reach its target.");
                    spine.Add(next);
                }
                previousExit = spine[spine.Count - 1];
                if (layer < settings.VerticalLayers - 1) previousExit.Roles |= RoomRole.StairUp;

                // Budget intentional reconnecting detours before optional wings consume their space.
                for (int loop = 0; loop < loopGoal; loop++)
                {
                    List<int> edges = new List<int>();
                    for (int i = 1; i < spine.Count - 2; i++) edges.Add(i);
                    random.Shuffle(edges);
                    bool made = false;
                    foreach (int i in edges)
                    {
                        MapRoom a = spine[i], b = spine[i + 1];
                        int px = -(b.Z - a.Z), pz = b.X - a.X;
                        int sign = random.Range(2) == 0 ? -1 : 1;
                        for (int side = 0; side < 2 && !made; side++, sign = -sign)
                        {
                            int depth = random.Range(2) + 1;
                            if (map.Rooms.Count - first + depth * 2 + (loopGoal - loop - 1) * 2 > roomGoal - 3) depth = 1;
                            bool fits = true;
                            for (int offset = 1; offset <= depth; offset++)
                                fits &= Free(a.X + px * sign * offset, a.Z + pz * sign * offset, layer)
                                    && Free(b.X + px * sign * offset, b.Z + pz * sign * offset, layer);
                            if (!fits) continue;
                            MapRoom cursor = a;
                            for (int offset = 1; offset <= depth; offset++)
                            {
                                MapRoom room = Add(a.X + px * sign * offset, a.Z + pz * sign * offset, layer, RoomRole.Ordinary, offset);
                                Link(cursor, room, LinkKind.Loop); cursor = room;
                            }
                            for (int offset = depth; offset >= 1; offset--)
                            {
                                MapRoom room = Add(b.X + px * sign * offset, b.Z + pz * sign * offset, layer, RoomRole.Ordinary, offset);
                                Link(cursor, room, LinkKind.Loop); cursor = room;
                            }
                            Link(cursor, b, loop % 2 == 0 ? LinkKind.Shortcut : LinkKind.Loop);
                            made = true;
                        }
                        if (made) break;
                    }
                    if (!made) throw new InvalidOperationException("Not enough space for required reconnecting routes.");
                }

                // Grow bounded department wings from distributed anchors, not one unconstrained random walk.
                while (map.Rooms.Count - first < roomGoal)
                {
                    List<MapRoom> anchors = new List<MapRoom>();
                    for (int i = first + 1; i < map.Rooms.Count; i++)
                        if (!map.Rooms[i].IsStair && map.Rooms[i].BranchDepth < settings.MaxBranchDepth && map.Neighbors(i).Count < 4)
                            anchors.Add(map.Rooms[i]);
                    random.Shuffle(anchors);
                    bool made = false;
                    foreach (MapRoom anchor in anchors)
                    {
                        List<int> directions = new List<int> { 0, 1, 2, 3 }; random.Shuffle(directions);
                        foreach (int direction in directions)
                        {
                            if (!Free(anchor.X + dx[direction], anchor.Z + dz[direction], layer)) continue;
                            MapRoom next = Add(anchor.X + dx[direction], anchor.Z + dz[direction], layer, RoomRole.Ordinary, anchor.BranchDepth + 1);
                            Link(anchor, next, LinkKind.Passage); made = true; break;
                        }
                        if (made) break;
                    }
                    if (!made) throw new InvalidOperationException("Department wings exhausted their branch-depth budget.");
                }
            }
            map.CycleCount = map.Links.Count - map.Rooms.Count + 1;
        }

        void AssignMeaning()
        {
            MapValidator.MeasureDistances(map);
            int[][] distances = map.DistanceMatrix();
            List<MapRoom> candidates = new List<MapRoom>();
            foreach (MapRoom room in map.Rooms) if (!room.IsStair && !room.Has(RoomRole.Entrance)) candidates.Add(room);
            candidates.Sort((a, b) => { int c = b.Distance.CompareTo(a.Distance); return c != 0 ? c : a.Id.CompareTo(b.Id); });
            int objectives = 0;
            List<MapRoom> placed = new List<MapRoom>();
            foreach (MapRoom room in candidates)
            {
                if (room.Distance < settings.ObjectiveMinDistance) break;
                bool spaced = true;
                foreach (MapRoom other in placed) if (distances[room.Id][other.Id] < 3) spaced = false;
                if (!spaced) continue;
                room.Roles |= RoomRole.Objective | RoomRole.HighRisk;
                placed.Add(room);
                if (++objectives == settings.ObjectiveCount) break;
            }
            if (objectives != settings.ObjectiveCount) throw new InvalidOperationException("Objective zones too close to the elevator or one another.");
            int optional = 0;
            foreach (MapRoom room in candidates)
            {
                if (room.Has(RoomRole.Objective | RoomRole.MainRoute) || map.Neighbors(room.Id).Count != 1) continue;
                room.Roles |= RoomRole.Optional | RoomRole.HighRisk;
                if (++optional == settings.OptionalZoneCount) break;
            }
            foreach (MapRoom room in candidates)
            {
                if (optional >= settings.OptionalZoneCount) break;
                if (room.Has(RoomRole.Objective | RoomRole.Optional)) continue;
                room.Roles |= RoomRole.Optional; optional++;
            }
            List<MapRoom> landmarks = new List<MapRoom>();
            for (int i = 0; i < settings.LandmarkCount; i++)
            {
                MapRoom best = null; int bestScore = -1;
                foreach (MapRoom room in candidates)
                {
                    if (room.Has(RoomRole.Objective | RoomRole.Landmark)) continue;
                    int score = room.Distance;
                    foreach (MapRoom landmark in landmarks) score = Math.Min(score, distances[room.Id][landmark.Id]);
                    if (score > bestScore) { best = room; bestScore = score; }
                }
                if (best == null) throw new InvalidOperationException("Insufficient landmark spaces.");
                best.Roles |= RoomRole.Landmark;
                string[] names = { "THE BLUE TURBINE", "THE QUEUE CLOCK", "THE HANGING CABINET", "THE RED SWITCHBOARD", "THE DRY FOUNTAIN", "THE LOST PROPERTY TREE" };
                best.LandmarkName = names[i % names.Length] + " " + (i + 1).ToString("00");
                if (i % 3 == 0) best.Roles |= RoomRole.Event;
                landmarks.Add(best);
            }

            List<ModuleSpec> catalog = new List<ModuleSpec>(map.Recipe.Modules);
            catalog.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            foreach (MapRoom room in map.Rooms)
            {
                MapRandom moduleRandom = new MapRandom(map.Recipe.Seed, unchecked(0x4d4f4455u + (uint)room.Id * 313u));
                // Coherent departments occupy spatial bands; module variants within each department differ.
                int count = map.Recipe.Districts.Length;
                int district = ((room.X / 3 + room.Z / 4 + room.Layer) % count + count) % count;
                DistrictSpec department = map.Recipe.Districts[district];
                room.District = department.DisplayName; room.DistrictId = department.Id; room.WetFloor = department.WetFloor;
                ModuleStyle style = room.IsStair ? ModuleStyle.Stairwell : room.Has(RoomRole.Entrance) ? ModuleStyle.Arrival
                    : ModuleStyle.Landmark;
                bool special = room.IsStair || room.Has(RoomRole.Entrance | RoomRole.Landmark);
                List<ModuleSpec> choices = catalog.FindAll(m => special ? m.Style == style : Array.IndexOf(department.ModuleIds,m.Id) >= 0);
                int sum = 0; foreach (ModuleSpec m in choices) sum += m.Weight;
                int roll = moduleRandom.Range(sum);
                foreach (ModuleSpec m in choices) { roll -= m.Weight; if (roll < 0) { room.ModuleId = m.Id; break; } }
                room.QuarterTurns = room.IsStair || room.Has(RoomRole.Entrance) ? 0 : moduleRandom.Range(4);
            }
            map.EstimatedWalkableSquareMeters = settings.TargetRooms * 144 + (int)((long)(map.Links.Count - settings.VerticalLayers + 1) *
                (settings.CellSizeMillimeters - settings.RoomSizeMillimeters) * settings.CorridorWidthMillimeters / 1000000);
        }

        void Socket(MapRoom room, SocketKind kind, int slot)
        {
            MapRandom stream = new MapRandom(map.Recipe.Seed, unchecked(0x534f434bu + (uint)room.Id * 317u + (uint)kind * 7001u));
            map.Sockets.Add(new MapSocket { Id = "r" + room.Id.ToString("000") + "-" + kind.ToString().ToLowerInvariant() + "-" + slot,
                RoomId = room.Id, Kind = kind, Slot = slot, ContentSeed = unchecked((int)stream.Next()) });
        }
        void PlanContent()
        {
            int enemies = 0;
            foreach (MapRoom room in map.Rooms)
            {
                if (room.IsStair) continue;
                MapRandom decor = new MapRandom(map.Recipe.Seed, unchecked(0x4445434fu + (uint)room.Id * 1013u));
                if (room.Id == 0 || (room.Distance > 0 && room.Distance % 7 == 0)) Socket(room, SocketKind.Supply, 0);
                if (room.Has(RoomRole.Objective)) Socket(room, SocketKind.Objective, 1);
                else if (room.Has(RoomRole.Optional) || decor.Range(100) < 30) Socket(room, SocketKind.Reward, 1);
                if (room.Has(RoomRole.Event)) Socket(room, SocketKind.Event, 2);
                if (room.Has(RoomRole.Optional | RoomRole.HighRisk)) Socket(room, SocketKind.RestrictedZone, 3);
                if (room.Distance >= 4 && decor.Range(100) < settings.HazardDensityPercent) Socket(room, SocketKind.Hazard, 2);
                if (room.Distance >= 5 && enemies < settings.EnemySpawnCapacity && decor.Range(100) < 30)
                { Socket(room, SocketKind.Enemy, 3); enemies++; }
            }
        }

        public static void ComputeHashes(MapManifest manifest)
        {
            StringBuilder structure = new StringBuilder();
            foreach (MapRoom r in manifest.Rooms)
                structure.AppendFormat(CultureInfo.InvariantCulture, "{0}:{1},{2},{3}:{4}:{5}:{6}:{7}:{8};", r.Id, r.X, r.Z, r.Layer, (int)r.Roles, r.ModuleId, r.QuarterTurns, r.DistrictId, r.WetFloor);
            List<MapLink> links = new List<MapLink>(manifest.Links);
            links.Sort((a, b) => a.A != b.A ? a.A.CompareTo(b.A) : a.B.CompareTo(b.B));
            foreach (MapLink e in links) structure.AppendFormat(CultureInfo.InvariantCulture, "{0}-{1}:{2};", e.A, e.B, (int)e.Kind);
            manifest.StructureHash = StableHash.Of(structure.ToString());
            StringBuilder content = new StringBuilder();
            foreach (MapSocket s in manifest.Sockets)
                content.AppendFormat(CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3};", s.Id, s.RoomId, s.Slot, s.ContentSeed);
            manifest.ContentHash = StableHash.Of(content.ToString());
        }
    }
}
