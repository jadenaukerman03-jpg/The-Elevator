using System;
using System.Collections.Generic;
using TheElevator.Generation;
using UnityEngine;

namespace TheElevator.Office
{
    public enum OfficeRoomKind { Lobby, Reception, Workroom, Conference, Breakroom, Records, Server, Security, Executive, Mailroom, Maintenance, Training, Restroom, Gallery }
    public enum OfficeQuality { Low, Medium, High, Ultra }
    public enum OfficeTask { Reception, Typing, Reading, Coffee, Filing, Meeting, Repair, Patrol }
    [Serializable] public sealed class OfficeConfig
    {
        public int Version = 1;
        public string Corporation = "MORROW SYSTEMS";
        public string Slogan = "A BETTER TOMORROW. ON REPEAT.";
        public OfficeQuality Quality = OfficeQuality.High;
        public int EmployeesPerRoom = 2;
        public int PopulationCap = 320;
        public float ActiveDistance = 34;
        public bool BadgesExpireOnDeparture = true;
        public bool InvalidateBadgesOnAlarm;
        public int ForcedObjective = -1;
    }
    [Serializable] public sealed class OfficeRoomPlan
    {
        public int RoomId, Department, Clearance;
        public OfficeRoomKind Kind;
    }
    [Serializable] public sealed class OfficePlan
    {
        public OfficeConfig Config;
        public List<OfficeRoomPlan> Rooms = new List<OfficeRoomPlan>();
        public int TargetRoom, SupervisorRoom, TargetType;
        public int MeetingRoom=-1;
        public bool DeskBadge;
        public string Hash;
        public List<int> ExtractionRoute;
        public static readonly string[] Departments = { "CLIENT RELATIONS", "ACCOUNTS / PEOPLE", "SYSTEMS ENGINEERING", "CORPORATE RECORDS", "EXECUTIVE SERVICES", "FACILITIES" };
        public string TargetName { get { return new[] { "Prototype server", "Executive archive safe", "Nourish-9 vending machine" }[TargetType]; } }
        public Vector3 TargetBounds { get { return TargetType == 2 ? new Vector3(1.24f,2.05f,0.88f) : TargetType == 0 ? new Vector3(0.9f,1.8f,0.8f) : new Vector3(1.12f,1.4f,0.9f); } }
        public static bool Enabled(MapRecipe recipe) { return recipe.ThemeId == "morrow-office"; }
        public static OfficePlan Build(MapManifest map)
        {
            OfficeConfig config = string.IsNullOrEmpty(map.Recipe.ThemePayload) ? new OfficeConfig() : JsonUtility.FromJson<OfficeConfig>(map.Recipe.ThemePayload);
            if (config == null || config.Version != 1 || config.EmployeesPerRoom < 1 || config.EmployeesPerRoom > 4 || config.PopulationCap < 8 || config.ActiveDistance < 12 || config.ForcedObjective < -1 || config.ForcedObjective > 2)
                throw new ArgumentException("Invalid office configuration.");
            OfficePlan plan = new OfficePlan { Config = config, SupervisorRoom = 1, TargetType = config.ForcedObjective >= 0 ? config.ForcedObjective : (int)(unchecked((uint)map.Recipe.Seed) % 3) };
            List<MapRoom> choices = map.Rooms.FindAll(r => r.Layer == 0 && !r.IsStair && r.Distance >= 3 && map.Neighbors(r.Id).Count == 1 && !r.Has(RoomRole.Objective));
            if (choices.Count == 0) choices = map.Rooms.FindAll(r => r.Layer == 0 && !r.IsStair && r.Distance >= 3 && !r.Has(RoomRole.Objective));
            if (choices.Count == 0) throw new InvalidOperationException("Office requires an accessible ground-level objective room.");
            MapRandom random = new MapRandom(map.Recipe.Seed,0x4f464649u);
            plan.TargetRoom = choices[random.Range(choices.Count)].Id;
            plan.ExtractionRoute = map.FindPath(plan.TargetRoom,0);
            // Multi-source graph distance keeps related departments spatially contiguous.
            int departments = map.Rooms.Count < 30 ? 3 : 6;
            List<int> centers = new List<int> { 0 };
            int[][] distance = map.DistanceMatrix();
            while (centers.Count < departments)
            {
                int best = 0, score = -1;
                foreach (MapRoom r in map.Rooms)
                {
                    int closest = int.MaxValue;
                    foreach (int center in centers) closest = Math.Min(closest,distance[r.Id][center]);
                    if (closest > score) { best = r.Id; score = closest; }
                }
                centers.Add(best);
            }
            foreach (MapRoom r in map.Rooms)
            {
                int department = 0;
                for (int i = 1; i < centers.Count; i++) if (distance[r.Id][centers[i]] < distance[r.Id][centers[department]]) department = i;
                OfficeRoomKind[] kinds = department == 0 ? new[] { OfficeRoomKind.Workroom,OfficeRoomKind.Conference,OfficeRoomKind.Breakroom,OfficeRoomKind.Training }
                    : department == 1 ? new[] { OfficeRoomKind.Workroom,OfficeRoomKind.Records,OfficeRoomKind.Executive,OfficeRoomKind.Restroom }
                    : department == 2 ? new[] { OfficeRoomKind.Server,OfficeRoomKind.Workroom,OfficeRoomKind.Maintenance,OfficeRoomKind.Breakroom }
                    : new[] { OfficeRoomKind.Records,OfficeRoomKind.Security,OfficeRoomKind.Executive,OfficeRoomKind.Mailroom };
                OfficeRoomKind kind = kinds[(r.Id + Math.Abs(map.Recipe.Seed % 7)) % kinds.Length];
                if (r.IsStair) kind = OfficeRoomKind.Gallery;
                else if (r.Id == 0) kind = OfficeRoomKind.Lobby;
                else if (r.Id == 1) kind = OfficeRoomKind.Reception;
                else if (r.Id == 2) kind = OfficeRoomKind.Workroom;
                else if (r.Id == plan.TargetRoom) kind = plan.TargetType == 2 ? OfficeRoomKind.Breakroom : plan.TargetType == 0 ? OfficeRoomKind.Server : OfficeRoomKind.Records;
                plan.Rooms.Add(new OfficeRoomPlan { RoomId = r.Id, Department = department, Kind = kind, Clearance = r.Id == plan.TargetRoom ? 2 : 0 });
            }
            plan.DeskBadge=(unchecked((uint)map.Recipe.Seed)%2)==0;
            List<OfficeRoomPlan> eventRooms=plan.Rooms.FindAll(r=>r.RoomId>2&&r.RoomId!=plan.TargetRoom&&!map.Rooms[r.RoomId].IsStair&&map.Rooms[r.RoomId].Layer==0);
            if(eventRooms.Count>0&&random.Range(100)<55)
            {
                OfficeRoomPlan meeting=eventRooms[random.Range(eventRooms.Count)];meeting.Kind=OfficeRoomKind.Conference;plan.MeetingRoom=meeting.RoomId;
            }
            if(!plan.Rooms.Exists(r=>r.Kind==OfficeRoomKind.Breakroom&&r.RoomId!=plan.TargetRoom))
            {
                OfficeRoomPlan coffee=eventRooms.Find(r=>r.RoomId!=plan.MeetingRoom);
                if(coffee!=null)coffee.Kind=OfficeRoomKind.Breakroom;
            }
            foreach (int id in plan.ExtractionRoute) if (map.Rooms[id].Layer != 0) throw new InvalidOperationException("Heavy objective route crosses stairs.");
            float diagonal = new Vector2(plan.TargetBounds.x,plan.TargetBounds.z).magnitude + 0.3f;
            if (map.Recipe.Settings.DoorWidthMillimeters / 1000f < diagonal || map.Recipe.Settings.DoorHeightMillimeters / 1000f < plan.TargetBounds.y + 0.3f)
                throw new InvalidOperationException("Objective does not fit the door contract.");
            // Removing all target-room doors must still leave the credential holder reachable.
            HashSet<int> reachable = new HashSet<int> { 0 }; Queue<int> queue = new Queue<int>(); queue.Enqueue(0);
            while (queue.Count > 0) foreach (int n in map.Neighbors(queue.Dequeue())) if (n != plan.TargetRoom && reachable.Add(n)) queue.Enqueue(n);
            if (!reachable.Contains(plan.SupervisorRoom)) throw new InvalidOperationException("Credential is locked behind its own door.");
            plan.Hash = StableHash.Of(map.StructureHash + "|office-v2-social|" + JsonUtility.ToJson(plan));
            return plan;
        }
    }
}

