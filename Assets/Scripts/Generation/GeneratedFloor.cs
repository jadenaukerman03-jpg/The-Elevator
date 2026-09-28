using System.Collections.Generic;
using UnityEngine;

namespace TheElevator.Generation
{
    public sealed class GeneratedFloor : MonoBehaviour
    {
        public MapManifest Manifest { get; private set; }
        public int CompletedObjectives { get { return completedObjectives.Count; } }
        public readonly List<GenerationSocket> ContentSockets = new List<GenerationSocket>();
        public readonly List<Transform> RoomRoots = new List<Transform>();
        public readonly List<Mesh> OwnedMeshes = new List<Mesh>();
        public bool Ready { get; set; }
        readonly HashSet<string> completedObjectives = new HashSet<string>();
        readonly List<Light> lights = new List<Light>();
        Renderer[][] roomRenderers;
        float nextVisibility;
        [SerializeField] int seed, roomCount;
        [SerializeField] string fingerprint;

        public void Initialize(MapManifest manifest)
        {
            Manifest = manifest; seed = manifest.Recipe.Seed; roomCount = manifest.Rooms.Count; fingerprint = manifest.StructureHash;
        }
        public Vector3 Center(MapRoom room)
        {
            GenerationSettings p = Manifest.Recipe.Settings;
            return transform.TransformPoint(new Vector3(room.X * p.CellSizeMillimeters / 1000f, room.Layer * p.LayerHeightMillimeters / 1000f, 4 + room.Z * p.CellSizeMillimeters / 1000f));
        }
        public Vector3 Anchor(MapRoom room) { return Center(room) + (room.IsStair ? new Vector3(0, 0, -5.1f) : Vector3.zero); }
        public MapRoom NearestRoom(Vector3 position)
        {
            MapRoom best = Manifest.Rooms[0]; float distance = float.MaxValue;
            foreach (MapRoom room in Manifest.Rooms)
            {
                Vector3 delta = position - Center(room);
                float score = delta.x * delta.x + delta.z * delta.z + delta.y * delta.y * 16;
                if (score < distance) { distance = score; best = room; }
            }
            foreach (KeyValuePair<int, Vector3> extension in Extensions)
            {
                Vector3 delta = position - extension.Value;
                float score = delta.x * delta.x + delta.z * delta.z + delta.y * delta.y * 16;
                if (score < distance) { distance = score; best = Manifest.Rooms[extension.Key]; }
            }
            return best;
        }
        // Extra cell centers that belong to an existing room (a room built two cells long).
        public readonly List<KeyValuePair<int, Vector3>> Extensions = new List<KeyValuePair<int, Vector3>>();
        public bool IsWet(Vector3 position)
        {
            MapRoom room = NearestRoom(position);
            if (room.IsStair || room.Has(RoomRole.Entrance) || !room.WetFloor) return false;
            Vector3 delta = position - Center(room);
            return Mathf.Abs(delta.x) < 5.8f && Mathf.Abs(delta.z) < 5.8f && delta.y < 0.4f && delta.y > -0.2f;
        }
        public bool CompleteObjective(string socketId) { return completedObjectives.Add(socketId); }

        // Waypoints follow actual doorway sockets. Stair routes follow landings, never the upper void.
        public List<Vector3> Route(int from, int to)
        {
            List<Vector3> points = new List<Vector3>();
            List<int> path = Manifest.FindPath(from, to);
            if (path.Count == 0) return points;
            points.Add(Anchor(Manifest.Rooms[from]));
            for (int i = 1; i < path.Count; i++)
            {
                MapRoom a = Manifest.Rooms[path[i - 1]], b = Manifest.Rooms[path[i]];
                if (a.Layer != b.Layer)
                {
                    MapRoom lower = a.Layer < b.Layer ? a : b;
                    Vector3 c = Center(lower);
                    List<Vector3> stairs = new List<Vector3> {
                        c + new Vector3(0,0,-5.1f), c + new Vector3(-2,0,-3.3f),
                        c + new Vector3(-2,3,3.1f), c + new Vector3(-2,3,3.7f), c + new Vector3(2,3,3.7f),
                        c + new Vector3(2,3,3), c + new Vector3(2,6,-3.1f), c + new Vector3(2,6,-5.1f), c + new Vector3(0,6,-5.1f)
                    };
                    if (a.Layer > b.Layer) stairs.Reverse(); points.AddRange(stairs);
                }
                else
                {
                    Vector3 direction = new Vector3(b.X - a.X, 0, b.Z - a.Z);
                    points.AddRange(ToPort(a, direction));
                    List<Vector3> incoming = ToPort(b, -direction); incoming.Reverse(); points.AddRange(incoming);
                }
            }
            return points;
        }
        public List<Vector3> ToPort(MapRoom room, Vector3 direction)
        {
            Vector3 c = Center(room);
            List<Vector3> points = new List<Vector3> { Anchor(room) };
            if (room.IsStair && direction.z > 0)
            { points.Add(c + new Vector3(4.8f,0,-5.1f)); points.Add(c + new Vector3(4.8f,0,5.1f)); points.Add(c + new Vector3(0,0,5.1f)); }
            else if (room.IsStair && Mathf.Abs(direction.x) > 0)
            { points.Add(c + new Vector3(direction.x * 4.8f,0,-5.1f)); points.Add(c + new Vector3(direction.x * 4.8f,0,0)); }
            points.Add(c + direction * 6f);
            return points;
        }

        public void PrepareVisibility()
        {
            roomRenderers = new Renderer[RoomRoots.Count][];
            for (int i = 0; i < RoomRoots.Count; i++) {
                List<Renderer> fixedRenderers=new List<Renderer>();
                foreach(Renderer renderer in RoomRoots[i].GetComponentsInChildren<Renderer>())
                    if(!renderer.GetComponentInParent<SalvageItem>(true))fixedRenderers.Add(renderer);
                roomRenderers[i]=fixedRenderers.ToArray();
            }
            lights.AddRange(GetComponentsInChildren<Light>());
        }
        void Update()
        {
            if (!Ready || roomRenderers == null || Time.unscaledTime < nextVisibility || !Camera.main) return;
            nextVisibility = Time.unscaledTime + 0.3f;
            Vector3 camera = Camera.main.transform.position;
            for (int i = 0; i < RoomRoots.Count; i++)
            {
                bool visible = (RoomRoots[i].position - camera).sqrMagnitude < 90 * 90;
                foreach (Renderer renderer in roomRenderers[i]) if (renderer) renderer.enabled = visible;
            }
            foreach (Light light in lights) if (light) light.enabled = (light.transform.position - camera).sqrMagnitude < 24 * 24;
        }
        void OnDestroy() { foreach (Mesh mesh in OwnedMeshes) if (mesh) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); } }
    }

}

