using System;
using System.Collections.Generic;
using TheElevator.Generation;
using UnityEngine;

namespace TheElevator.Office
{
    public enum OfficeRoomKind { Lobby, Reception, Workroom, Conference, Breakroom, Records, Server, Security, Executive, Mailroom, Maintenance, Training, Restroom, Gallery, Lounge }
    // A zone is one physical room: one grid cell, or several neighbouring cells blended into a bigger space.
    public enum OfficeZoneKind { Lobby, OpenOffice, PrivateOffice, Lounge, CoffeeBar, Records, Server, Restroom, Mailroom, Boardroom, Vault, Gallery }
    [Serializable] public sealed class OfficeZone
    {
        public int Id;
        public OfficeZoneKind Kind;
        public bool Closed; // a small room reached through a single door
        public List<int> Rooms = new List<int>();
    }
    public enum OfficeQuality { Low, Medium, High, Ultra }
    public enum OfficeTask { Reception, Typing, Reading, Coffee, Filing, Meeting, Repair, Patrol, Present }
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
        public int MeetingRoom=-1, MeetingAnnexSide=-1;
        public bool DeskBadge;
        public string Hash;
        public List<int> ExtractionRoute;
        public List<OfficeZone> Zones = new List<OfficeZone>();
        public List<int> ZoneOf = new List<int>();
        public OfficeZone ZoneFor(int room) { return Zones[ZoneOf[room]]; }
        public bool Blended(int a, int b) { return a != b && ZoneOf[a] == ZoneOf[b]; }
        // One real door per connection, only where a door makes sense: private rooms, the locked vault and the boardroom.
        // Other connections are open passages, and blended cells have no wall between them at all.
        public bool HasDoor(int a, int b)
        {
            if (Blended(a, b)) return false;
            if (a == TargetRoom || b == TargetRoom) return true;
            for (int i = 1; i < ExtractionRoute.Count; i++)
                if ((ExtractionRoute[i - 1] == a && ExtractionRoute[i] == b) || (ExtractionRoute[i - 1] == b && ExtractionRoute[i] == a)) return false;
            return ZoneFor(a).Closed || ZoneFor(b).Closed;
        }
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
                plan.Rooms.Add(new OfficeRoomPlan { RoomId = r.Id, Department = department, Kind = OfficeRoomKind.Workroom, Clearance = r.Id == plan.TargetRoom ? 2 : 0 });
            }
            plan.DeskBadge=(unchecked((uint)map.Recipe.Seed)%2)==0;
            List<OfficeRoomPlan> eventRooms=plan.Rooms.FindAll(r=>r.RoomId>2&&r.RoomId!=plan.TargetRoom&&!map.Rooms[r.RoomId].IsStair&&map.Rooms[r.RoomId].Layer==0);
            ChooseMeetingRoom(map,plan,eventRooms,random);
            BuildZones(map,plan,random);
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

        // Port directions shared with the geometry builder: 0 +Z, 1 +X, 2 -Z, 3 -X.
        public static readonly int[] PortX = { 0, 1, 0, -1 }, PortZ = { 1, 0, -1, 0 };

        // The meeting room is a dead end you only reach by accident: one door, and a second, empty grid cell beyond it
        // so the room runs two rooms long, away from its door when possible.
        // Neighbouring cells randomly blend into zones of up to four cells: halls, L-shapes and long rooms mixed with
        // small rooms. Each zone then gets a use that fits its shape: open floors become offices, dead ends become
        // private offices, lounges, coffee bars, restrooms and back rooms.
        static void BuildZones(MapManifest map,OfficePlan plan,MapRandom random)
        {
            int count=map.Rooms.Count;int[] parent=new int[count],size=new int[count];
            for(int i=0;i<count;i++){parent[i]=i;size[i]=1;}
            int Find(int x){while(parent[x]!=x){parent[x]=parent[parent[x]];x=parent[x];}return x;}
            void Join(int a,int b){a=Find(a);b=Find(b);if(a==b)return;if(a>b){int s=a;a=b;b=s;}parent[b]=a;size[a]+=size[b];}
            // Dead ends never blend: they stay as the small single-door rooms (private offices, lounges, restrooms...).
            bool Blendable(int id){return id!=plan.TargetRoom&&id!=plan.MeetingRoom&&!map.Rooms[id].IsStair&&(id<=1||map.Neighbors(id).Count>1);}
            if(Blendable(1)&&map.Neighbors(0).Contains(1))Join(0,1); // grand lobby: entrance and reception as one hall
            List<MapLink> links=new List<MapLink>(map.Links);
            for(int i=links.Count-1;i>0;i--){int j=random.Range(i+1);MapLink swap=links[i];links[i]=links[j];links[j]=swap;}
            foreach(MapLink link in links)
            {
                int a=link.A,b=link.B;
                if(a<=1||b<=1||!Blendable(a)||!Blendable(b)||map.Rooms[a].Layer!=map.Rooms[b].Layer)continue;
                if(Find(a)==Find(b)||size[Find(a)]+size[Find(b)]>4)continue;
                if(random.Range(100)<55)Join(a,b);
            }
            Dictionary<int,OfficeZone> byRoot=new Dictionary<int,OfficeZone>();
            plan.Zones.Clear();plan.ZoneOf.Clear();
            for(int i=0;i<count;i++)
            {
                int root=Find(i);
                if(!byRoot.TryGetValue(root,out OfficeZone zone)){zone=new OfficeZone{Id=plan.Zones.Count};byRoot.Add(root,zone);plan.Zones.Add(zone);}
                zone.Rooms.Add(i);plan.ZoneOf.Add(zone.Id);
            }
            // Each use made on this floor becomes less likely again, so floors get a spread of room types.
            Dictionary<OfficeZoneKind,int> used=new Dictionary<OfficeZoneKind,int>();
            foreach(OfficeZone zone in plan.Zones)
            {
                int first=zone.Rooms[0];
                if(zone.Rooms.Contains(0))zone.Kind=OfficeZoneKind.Lobby;
                else if(first==plan.TargetRoom)zone.Kind=OfficeZoneKind.Vault;
                else if(first==plan.MeetingRoom){zone.Kind=OfficeZoneKind.Boardroom;zone.Closed=true;}
                else if(map.Rooms[first].IsStair)zone.Kind=OfficeZoneKind.Gallery;
                else if(zone.Rooms.Count>2)zone.Kind=OfficeZoneKind.OpenOffice;
                else if(zone.Rooms.Count==2)zone.Kind=Pick(random,new[]{OfficeZoneKind.OpenOffice,OfficeZoneKind.Lounge,OfficeZoneKind.CoffeeBar},new[]{55,25,20},used);
                else if(map.Neighbors(first).Count==1)
                {
                    zone.Closed=true;
                    zone.Kind=Pick(random,new[]{OfficeZoneKind.PrivateOffice,OfficeZoneKind.Lounge,OfficeZoneKind.CoffeeBar,OfficeZoneKind.Records,OfficeZoneKind.Server,OfficeZoneKind.Restroom,OfficeZoneKind.Mailroom},new[]{30,16,16,12,10,10,6},used);
                }
                else zone.Kind=Pick(random,new[]{OfficeZoneKind.OpenOffice,OfficeZoneKind.Lounge,OfficeZoneKind.Mailroom,OfficeZoneKind.Records,OfficeZoneKind.CoffeeBar},new[]{40,22,14,12,12},used);
                used[zone.Kind]=(used.TryGetValue(zone.Kind,out int n)?n:0)+1;
            }
            // Every floor needs somewhere to work and somewhere to get coffee.
            bool Ordinary(OfficeZone z){return z.Kind!=OfficeZoneKind.Lobby&&z.Kind!=OfficeZoneKind.Vault&&z.Kind!=OfficeZoneKind.Boardroom&&z.Kind!=OfficeZoneKind.Gallery&&map.Rooms[z.Rooms[0]].Layer==0;}
            if(!plan.Zones.Exists(z=>z.Kind==OfficeZoneKind.OpenOffice))
            {
                OfficeZone biggest=null;foreach(OfficeZone z in plan.Zones)if(Ordinary(z)&&(biggest==null||z.Rooms.Count>biggest.Rooms.Count))biggest=z;
                if(biggest!=null)biggest.Kind=OfficeZoneKind.OpenOffice;
            }
            if(!plan.Zones.Exists(z=>z.Kind==OfficeZoneKind.CoffeeBar&&map.Rooms[z.Rooms[0]].Layer==0))
            {
                OfficeZone coffee=plan.Zones.Find(z=>Ordinary(z)&&z.Rooms.Count==1&&z.Kind!=OfficeZoneKind.OpenOffice)??plan.Zones.Find(z=>Ordinary(z)&&z.Kind!=OfficeZoneKind.OpenOffice)??plan.Zones.FindLast(z=>Ordinary(z));
                if(coffee!=null)coffee.Kind=OfficeZoneKind.CoffeeBar;
            }
            foreach(OfficeZone zone in plan.Zones)
                foreach(int id in zone.Rooms)
                {
                    OfficeRoomKind kind;
                    switch(zone.Kind)
                    {
                        case OfficeZoneKind.Lobby:kind=id==0?OfficeRoomKind.Lobby:OfficeRoomKind.Reception;break;
                        case OfficeZoneKind.Vault:kind=plan.TargetType==2?OfficeRoomKind.Breakroom:plan.TargetType==0?OfficeRoomKind.Server:OfficeRoomKind.Records;break;
                        case OfficeZoneKind.Boardroom:kind=OfficeRoomKind.Conference;break;
                        case OfficeZoneKind.Gallery:kind=OfficeRoomKind.Gallery;break;
                        case OfficeZoneKind.PrivateOffice:kind=OfficeRoomKind.Executive;break;
                        case OfficeZoneKind.Lounge:kind=OfficeRoomKind.Lounge;break;
                        case OfficeZoneKind.CoffeeBar:kind=OfficeRoomKind.Breakroom;break;
                        case OfficeZoneKind.Records:kind=OfficeRoomKind.Records;break;
                        case OfficeZoneKind.Server:kind=OfficeRoomKind.Server;break;
                        case OfficeZoneKind.Restroom:kind=OfficeRoomKind.Restroom;break;
                        case OfficeZoneKind.Mailroom:kind=OfficeRoomKind.Mailroom;break;
                        default:kind=OfficeRoomKind.Workroom;break;
                    }
                    plan.Rooms[id].Kind=kind;
                }
        }

        static OfficeZoneKind Pick(MapRandom random,OfficeZoneKind[] items,int[] weights,Dictionary<OfficeZoneKind,int> used)
        {
            int[] adjusted=new int[weights.Length];int total=0;
            for(int i=0;i<weights.Length;i++){int n=used.TryGetValue(items[i],out int c)?c:0;adjusted[i]=Math.Max(1,weights[i]/(1+n*3));total+=adjusted[i];}
            int roll=random.Range(total);
            for(int i=0;i<items.Length;i++){roll-=adjusted[i];if(roll<0)return items[i];}
            return items[items.Length-1];
        }

        static void ChooseMeetingRoom(MapManifest map,OfficePlan plan,List<OfficeRoomPlan> candidates,MapRandom random)
        {
            List<int> order=new List<int>();foreach(OfficeRoomPlan r in candidates)order.Add(r.RoomId);
            for(int i=order.Count-1;i>0;i--){int j=random.Range(i+1);int swap=order[i];order[i]=order[j];order[j]=swap;}
            foreach(int id in order)
            {
                MapRoom room=map.Rooms[id];
                List<int> neighbors=map.Neighbors(id);
                if(neighbors.Count!=1)continue;
                MapRoom door=map.Rooms[neighbors[0]];
                int doorSide=door.X>room.X?1:door.X<room.X?3:door.Z>room.Z?0:2;
                foreach(int side in new[]{(doorSide+2)%4,(doorSide+1)%4,(doorSide+3)%4})
                {
                    int x=room.X+PortX[side],z=room.Z+PortZ[side];
                    if(z<0||map.Rooms.Exists(r=>r.X==x&&r.Z==z))continue;
                    plan.MeetingRoom=id;plan.MeetingAnnexSide=side;plan.Rooms[id].Kind=OfficeRoomKind.Conference;
                    return;
                }
            }
        }
    }
}
