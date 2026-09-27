using System;
using System.Collections.Generic;

namespace TheElevator.Generation
{
    public static class MapValidator
    {
        public static void MeasureDistances(MapManifest map)
        {
            foreach (MapRoom room in map.Rooms) room.Distance = -1;
            map.Rooms[0].Distance = 0; map.FurthestDistance = 0;
            Queue<int> queue = new Queue<int>(); queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int id = queue.Dequeue();
                foreach (int next in map.Neighbors(id))
                {
                    if (map.Rooms[next].Distance >= 0) continue;
                    map.Rooms[next].Distance = map.Rooms[id].Distance + 1;
                    map.FurthestDistance = Math.Max(map.FurthestDistance, map.Rooms[next].Distance);
                    queue.Enqueue(next);
                }
            }
        }

        public static List<string> Validate(MapManifest map)
        {
            List<string> errors = new List<string>();
            GenerationSettings p = map.Recipe.Settings;
            if (map.Rooms.Count != p.TargetRooms) errors.Add("Target room count not reached");
            if (map.Rooms.Count == 0) return errors;
            HashSet<string> cells = new HashSet<string>(), edges = new HashSet<string>(), ids = new HashSet<string>();
            HashSet<string> modules = new HashSet<string>();
            foreach (ModuleSpec module in map.Recipe.Modules) modules.Add(module.Id);
            int objectives = 0, landmarks = 0, optional = 0, stairs = 0, deadEnds = 0, main = 0;
            foreach (MapRoom room in map.Rooms)
            {
                if (room.Id < 0 || room.Id >= map.Rooms.Count || map.Rooms[room.Id] != room) errors.Add("Unstable room ID");
                if (!cells.Add(room.X + ":" + room.Z + ":" + room.Layer)) errors.Add("Overlapping module cells");
                if (room.Z < 0 || room.Layer < 0 || room.Layer >= p.VerticalLayers) errors.Add("Room outside valid region");
                if (room.BranchDepth > p.MaxBranchDepth) errors.Add("Branch depth exceeded");
                if (!modules.Contains(room.ModuleId)) errors.Add("Missing room module");
                if (room.Has(RoomRole.Objective)) { objectives++; if (room.Distance < p.ObjectiveMinDistance) errors.Add("Objective too close to lift"); }
                if (room.Has(RoomRole.Landmark)) landmarks++;
                if (room.Has(RoomRole.Optional)) optional++;
                if (room.Has(RoomRole.MainRoute)) main++;
                if (map.Neighbors(room.Id).Count == 1 && room.Id != 0) deadEnds++;
            }
            foreach (MapLink edge in map.Links)
            {
                if (edge.A < 0 || edge.B >= map.Rooms.Count || edge.A >= edge.B) { errors.Add("Invalid edge endpoints"); continue; }
                if (!edges.Add(edge.A + ":" + edge.B)) errors.Add("Duplicate graph link");
                MapRoom a = map.Rooms[edge.A], b = map.Rooms[edge.B];
                int horizontal = Math.Abs(a.X - b.X) + Math.Abs(a.Z - b.Z);
                if (edge.Kind == LinkKind.Stairs)
                {
                    stairs++;
                    if (horizontal != 0 || b.Layer != a.Layer + 1 || !a.Has(RoomRole.StairUp) || !b.Has(RoomRole.StairDown)) errors.Add("Misaligned stairwell");
                }
                else if (horizontal != 1 || a.Layer != b.Layer) errors.Add("Nonmatching doorway sockets");
            }
            if (!map.Rooms[0].Has(RoomRole.Entrance) || map.Rooms[0].X != 0 || map.Rooms[0].Z != 0 || map.Rooms[0].Layer != 0) errors.Add("Lift entrance misplaced");
            if (main != p.MainRouteRooms) errors.Add("Main route target not met");
            if (objectives != p.ObjectiveCount || landmarks != p.LandmarkCount || optional != p.OptionalZoneCount) errors.Add("Semantic zone targets not met");
            if (stairs != p.VerticalLayers - 1) errors.Add("Missing vertical connections");
            if (map.Links.Count - map.Rooms.Count + 1 < p.LoopCount) errors.Add("Missing required loops");
            if (deadEnds < 2) errors.Add("Not enough meaningful dead ends");
            MeasureDistances(map);
            foreach (MapRoom room in map.Rooms) if (room.Distance < 0) errors.Add("Disconnected room");
            int enemyCount = 0;
            foreach (MapSocket socket in map.Sockets)
            {
                if (!ids.Add(socket.Id)) errors.Add("Duplicate content socket ID");
                if (socket.RoomId < 0 || socket.RoomId >= map.Rooms.Count) { errors.Add("Socket outside map"); continue; }
                if (socket.Kind == SocketKind.Enemy) { enemyCount++; if (map.Rooms[socket.RoomId].Distance < 5) errors.Add("Enemy inside safe arrival region"); }
            }
            if (enemyCount > p.EnemySpawnCapacity) errors.Add("Enemy budget exceeded");
            return errors;
        }
    }
}
