using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TheElevator.Generation
{
    public enum MapSize { Small, Standard, Large, Extreme }
    [Flags] public enum RoomRole { Ordinary = 0, Entrance = 1, MainRoute = 2, Landmark = 4, Objective = 8, Optional = 16, HighRisk = 32, Event = 64, StairUp = 128, StairDown = 256 }
    public enum LinkKind { Passage, Loop, Shortcut, Stairs }
    public enum SocketKind { Supply, Reward, Objective, Enemy, Hazard, Event, RestrictedZone }
    public enum ModuleStyle { Arrival, Records, Pumps, Sorting, BreakRoom, Landmark, Stairwell }

    [Serializable]
    public sealed class GenerationSettings
    {
        public string ProfileId = "Standard";
        public int TargetRooms = 72;
        public int MainRouteRooms = 26;
        public int MaxBranchDepth = 7;
        public int VerticalLayers = 2;
        public int LoopCount = 8;
        public int LandmarkCount = 6;
        public int OptionalZoneCount = 8;
        public int ObjectiveCount = 3;
        public int ObjectiveMinDistance = 12;
        public int PropDensityPercent = 75;
        public int HazardDensityPercent = 16;
        public int EnemySpawnCapacity = 6;
        public int ExplorationSeconds = 1500;
        public int MaxGenerationAttempts = 16;
        public int OperationBudget = 250000;
        public int GeometryBudgetMilliseconds = 5;
        public int ExpectedPlayers = 4;
        // Integer dimensions avoid floating point decisions in the structural generator.
        public int CellSizeMillimeters = 14000;
        public int RoomSizeMillimeters = 12000;
        public int LayerHeightMillimeters = 6000;
        public int DoorWidthMillimeters = 2800;
        public int DoorHeightMillimeters = 2800;
        public int CorridorWidthMillimeters = 3400;

        public static GenerationSettings Preset(MapSize size)
        {
            GenerationSettings p = new GenerationSettings();
            p.ProfileId = size.ToString();
            if (size == MapSize.Small)
            {
                p.TargetRooms = 18; p.MainRouteRooms = 8; p.MaxBranchDepth = 4; p.VerticalLayers = 1;
                p.LoopCount = 2; p.LandmarkCount = 2; p.OptionalZoneCount = 3; p.ObjectiveCount = 1;
                p.ObjectiveMinDistance = 5; p.EnemySpawnCapacity = 1; p.ExplorationSeconds = 480;
            }
            else if (size == MapSize.Large)
            {
                p.TargetRooms = 120; p.MainRouteRooms = 38; p.MaxBranchDepth = 9; p.VerticalLayers = 2;
                p.LoopCount = 14; p.LandmarkCount = 9; p.OptionalZoneCount = 12; p.ObjectiveCount = 4;
                p.ObjectiveMinDistance = 16; p.EnemySpawnCapacity = 10; p.ExplorationSeconds = 1800;
            }
            else if (size == MapSize.Extreme)
            {
                p.TargetRooms = 192; p.MainRouteRooms = 54; p.MaxBranchDepth = 11; p.VerticalLayers = 3;
                p.LoopCount = 20; p.LandmarkCount = 12; p.OptionalZoneCount = 18; p.ObjectiveCount = 5;
                p.ObjectiveMinDistance = 20; p.EnemySpawnCapacity = 14; p.ExplorationSeconds = 2400;
            }
            return p;
        }

        public string Canonical()
        {
            return string.Join("|", new[] { ProfileId, TargetRooms.ToString(), MainRouteRooms.ToString(),
                MaxBranchDepth.ToString(), VerticalLayers.ToString(), LoopCount.ToString(), LandmarkCount.ToString(),
                OptionalZoneCount.ToString(), ObjectiveCount.ToString(), ObjectiveMinDistance.ToString(),
                PropDensityPercent.ToString(), HazardDensityPercent.ToString(), EnemySpawnCapacity.ToString(),
                ExplorationSeconds.ToString(), MaxGenerationAttempts.ToString(), OperationBudget.ToString(),
                GeometryBudgetMilliseconds.ToString(), ExpectedPlayers.ToString(), CellSizeMillimeters.ToString(),
                RoomSizeMillimeters.ToString(), LayerHeightMillimeters.ToString(), DoorWidthMillimeters.ToString(),
                DoorHeightMillimeters.ToString(), CorridorWidthMillimeters.ToString() });
        }

        public void Validate()
        {
            if (VerticalLayers < 1 || VerticalLayers > 8) throw new ArgumentException("VerticalLayers must be 1..8.");
            if (TargetRooms < 12 * VerticalLayers || TargetRooms > 512) throw new ArgumentException("Use 12 or more rooms per layer, up to 512 total.");
            if (MainRouteRooms < 4 * VerticalLayers || MainRouteRooms + LoopCount * 2 > TargetRooms - 2)
                throw new ArgumentException("Main route and loop detours must leave room for optional branches.");
            if (MaxBranchDepth < 2 || MaxBranchDepth > 32 || LoopCount < 1 || LoopCount > TargetRooms / 3)
                throw new ArgumentException("Invalid branch depth or loop count.");
            if (LandmarkCount < 1 || OptionalZoneCount < 1 || ObjectiveCount < 1 ||
                LandmarkCount + OptionalZoneCount + ObjectiveCount > TargetRooms - VerticalLayers * 3 || ObjectiveMinDistance < 1)
                throw new ArgumentException("Invalid landmark / optional / objective requirements.");
            if (PropDensityPercent < 0 || PropDensityPercent > 100 || HazardDensityPercent < 0 || HazardDensityPercent > 100 || EnemySpawnCapacity < 0)
                throw new ArgumentException("Densities must be 0..100 and enemy capacity nonnegative.");
            if (MaxGenerationAttempts < 1 || MaxGenerationAttempts > 64 || OperationBudget < 100 || GeometryBudgetMilliseconds < 1 || GeometryBudgetMilliseconds > 50)
                throw new ArgumentException("Invalid generation budgets.");
            if (ExplorationSeconds < 60 || ExpectedPlayers < 1 || ExpectedPlayers > 16) throw new ArgumentException("Invalid timing / group size.");
            // This content-kit contract is explicit. Multi-cell modules need a new resolver, not unsafe scaling.
            if (RoomSizeMillimeters != 12000 || CellSizeMillimeters < 14000 || CellSizeMillimeters > 18000 || LayerHeightMillimeters != 6000)
                throw new ArgumentException("Civic kit v1 requires 12 m modules, 14..18 m cells, and 6 m storeys.");
            if (DoorWidthMillimeters < 2400 || DoorWidthMillimeters > 4000 || DoorHeightMillimeters < 2600 || DoorHeightMillimeters > 3000 ||
                CorridorWidthMillimeters < DoorWidthMillimeters || CorridorWidthMillimeters > 5000)
                throw new ArgumentException("Door / corridor clearances do not fit the authored kit.");
        }
    }

    [Serializable]
    public sealed class ModuleSpec
    {
        public string Id;
        public ModuleStyle Style;
        public int Weight = 1;
        public int CeilingMillimeters = 3800;
        public int ContentRevision = 1;
        public ModuleSpec() { }
        public ModuleSpec(string id, ModuleStyle style, int weight, int ceiling)
        { Id = id; Style = style; Weight = weight; CeilingMillimeters = ceiling; }
    }

    [Serializable]
    public sealed class DistrictSpec
    {
        public string Id, DisplayName;
        public string[] ModuleIds;
        public bool WetFloor;
    }

    [Serializable]
    public sealed class MapRecipe
    {
        public const int SupportedVersion = 2;
        public int GenerationVersion = SupportedVersion;
        public int Seed;
        public string ThemeId = "civic-works";
        public int ThemeRevision = 1;
        public GenerationSettings Settings;
        public ModuleSpec[] Modules;
        public string ThemePayload;
        public DistrictSpec[] Districts = DefaultDistricts();

        public static DistrictSpec[] DefaultDistricts()
        {
            return new[] {
                new DistrictSpec { Id = "records", DisplayName = "RECORDS", ModuleIds = new[] { "records", "breakroom" } },
                new DistrictSpec { Id = "utilities", DisplayName = "UTILITIES", ModuleIds = new[] { "pumps", "breakroom" }, WetFloor = true },
                new DistrictSpec { Id = "dispatch", DisplayName = "DISPATCH", ModuleIds = new[] { "sorting", "breakroom" } }
            };
        }

        public static MapRecipe Default(int seed, MapSize size)
        {
            return new MapRecipe { Seed = seed, Settings = GenerationSettings.Preset(size), Modules = DefaultModules() };
        }
        public static ModuleSpec[] DefaultModules()
        {
            return new[] {
                new ModuleSpec("arrival", ModuleStyle.Arrival, 1, 3800),
                new ModuleSpec("records", ModuleStyle.Records, 4, 3600),
                new ModuleSpec("pumps", ModuleStyle.Pumps, 3, 4200),
                new ModuleSpec("sorting", ModuleStyle.Sorting, 3, 4000),
                new ModuleSpec("breakroom", ModuleStyle.BreakRoom, 2, 3400),
                new ModuleSpec("landmark", ModuleStyle.Landmark, 1, 4900),
                new ModuleSpec("stairs", ModuleStyle.Stairwell, 1, 5800)
            };
        }
        public string ConfigurationHash()
        {
            StringBuilder text = new StringBuilder(GenerationVersion + "|" + ThemeId + "|" + ThemeRevision + "|" + Settings.Canonical());
            List<ModuleSpec> sorted = new List<ModuleSpec>(Modules);
            sorted.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            foreach (ModuleSpec m in sorted) text.Append("|").Append(m.Id).Append(":").Append((int)m.Style).Append(":").Append(m.Weight).Append(":").Append(m.CeilingMillimeters).Append(":").Append(m.ContentRevision);
            foreach (DistrictSpec district in Districts)
            {
                text.Append("|district:").Append(district.Id).Append(":").Append(district.DisplayName).Append(":").Append(district.WetFloor);
                List<string> choices = new List<string>(district.ModuleIds); choices.Sort(StringComparer.Ordinal);
                foreach (string id in choices) text.Append(":").Append(id);
            }
            if (!string.IsNullOrEmpty(ThemePayload)) text.Append("|theme-payload:").Append(ThemePayload);
            return StableHash.Of(text.ToString());
        }
    }

    [Serializable]
    public sealed class MapRoom
    {
        public int Id, X, Z, Layer, BranchDepth, Distance, QuarterTurns;
        public RoomRole Roles;
        public string ModuleId, District, DistrictId, LandmarkName;
        public bool WetFloor;
        public bool Has(RoomRole role) { return (Roles & role) != 0; }
        public bool IsStair { get { return Has(RoomRole.StairUp | RoomRole.StairDown); } }
    }
    [Serializable]
    public sealed class MapLink
    {
        public int A, B;
        public LinkKind Kind;
        public MapLink() { }
        public MapLink(int a, int b, LinkKind kind) { A = Math.Min(a, b); B = Math.Max(a, b); Kind = kind; }
        public int Other(int id) { return id == A ? B : A; }
    }
    [Serializable]
    public sealed class MapSocket
    {
        public string Id;
        public int RoomId, Slot, ContentSeed;
        public SocketKind Kind;
    }
    [Serializable]
    public sealed class MapManifest
    {
        public MapRecipe Recipe;
        public int Attempt;
        public string ConfigurationHash, StructureHash, ContentHash;
        public int CycleCount, FurthestDistance, EstimatedWalkableSquareMeters;
        public List<MapRoom> Rooms = new List<MapRoom>();
        public List<MapLink> Links = new List<MapLink>();
        public List<MapSocket> Sockets = new List<MapSocket>();
        public List<string> FailedAttempts = new List<string>();
        [NonSerialized] List<int>[] adjacency;
        [NonSerialized] int indexedLinks = -1;

        public List<int> Neighbors(int id)
        {
            if (adjacency == null || adjacency.Length != Rooms.Count || indexedLinks != Links.Count)
            {
                adjacency = new List<int>[Rooms.Count];
                for (int i = 0; i < adjacency.Length; i++) adjacency[i] = new List<int>();
                foreach (MapLink edge in Links) { adjacency[edge.A].Add(edge.B); adjacency[edge.B].Add(edge.A); }
                foreach (List<int> neighbors in adjacency) neighbors.Sort();
                indexedLinks = Links.Count;
            }
            return adjacency[id];
        }
        public int[][] DistanceMatrix()
        {
            int[][] result = new int[Rooms.Count][];
            for (int start = 0; start < Rooms.Count; start++)
            {
                result[start] = new int[Rooms.Count];
                for (int i = 0; i < Rooms.Count; i++) result[start][i] = -1;
                Queue<int> queue = new Queue<int>(); queue.Enqueue(start); result[start][start] = 0;
                while (queue.Count > 0)
                {
                    int id = queue.Dequeue();
                    foreach (int next in Neighbors(id)) if (result[start][next] < 0)
                    { result[start][next] = result[start][id] + 1; queue.Enqueue(next); }
                }
            }
            return result;
        }
        public List<int> FindPath(int start, int end)
        {
            int[] previous = new int[Rooms.Count];
            for (int i = 0; i < previous.Length; i++) previous[i] = -1;
            Queue<int> queue = new Queue<int>(); queue.Enqueue(start); previous[start] = start;
            while (queue.Count > 0)
            {
                int id = queue.Dequeue();
                if (id == end) break;
                foreach (int next in Neighbors(id)) if (previous[next] < 0) { previous[next] = id; queue.Enqueue(next); }
            }
            List<int> path = new List<int>();
            if (previous[end] < 0) return path;
            for (int id = end; ; id = previous[id]) { path.Add(id); if (id == start) break; }
            path.Reverse(); return path;
        }
    }

    public static class StableHash
    {
        public static string Of(string input)
        {
            using (SHA256 hash = SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder text = new StringBuilder(64);
                foreach (byte b in bytes) text.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }
    }

    // xorshift32 with a fixed algorithm; never Unity.Random, string.GetHashCode, or physics queries.
    public sealed class MapRandom
    {
        uint state;
        public MapRandom(int seed, uint stream) { state = Mix(unchecked((uint)seed) ^ stream); if (state == 0) state = 0x6d2b79f5; }
        static uint Mix(uint x) { unchecked { x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15; x *= 0x846ca68b; return x ^ (x >> 16); } }
        public uint Next() { unchecked { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return state; } }
        public int Range(int count) { if (count <= 0) throw new ArgumentException("Empty random range."); return (int)(Next() % (uint)count); }
        public void Shuffle<T>(List<T> list) { for (int i = list.Count - 1; i > 0; i--) { int j = Range(i + 1); T t = list[i]; list[i] = list[j]; list[j] = t; } }
    }
}
