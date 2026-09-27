using UnityEngine;
using System.Collections.Generic;
using TheElevator.Generation;

namespace TheElevator
{
    public sealed class Custodian : MonoBehaviour
    {
        DescentGame game;
        CharacterController motor;
        Transform head;
        Light eye;
        Vector3 destination;
        float alert, patrolTime, stuckTime;
        bool sensitive;
        readonly List<Vector3> route = new List<Vector3>();
        float nextRoute;

        public void Initialize(DescentGame owner, Workshop w, bool soundSensitive)
        {
            game = owner;
            sensitive = soundSensitive;
            motor = gameObject.AddComponent<CharacterController>();
            motor.height = 1.9f; motor.radius = 0.5f; motor.center = Vector3.up * 0.95f;
            w.Shape("Custodian body", transform, Vector3.up * 0.85f, new Vector3(1.1f, 1.5f, 0.9f),
                Workshop.Ink, PrimitiveType.Cube, false);
            head = w.Group("Custodian head", transform, Vector3.up * 1.75f);
            w.Shape("Camera head", head, Vector3.zero, new Vector3(1.0f, 0.65f, 0.7f), Workshop.Yellow, PrimitiveType.Cube, false);
            w.Shape("Optical sensor", head, new Vector3(0, 0, 0.38f), new Vector3(0.47f, 0.3f, 0.09f), Workshop.Red, PrimitiveType.Sphere, false);
            w.Shape("Wheel", transform, Vector3.up * 0.25f, new Vector3(0.8f, 0.45f, 0.7f), Workshop.Steel, PrimitiveType.Sphere, false);
            eye = w.Lamp(head, Vector3.forward * 0.6f, Workshop.Red, 1.5f, 4);
            destination = new Vector3(0, 0, 8);
        }

        public void Hear(Vector3 position, float strength)
        {
            if (!game || position.z < 0 || Vector3.Distance(position, transform.position) > (sensitive ? 15 : 9) * strength) return;
            destination = position;
            alert = 5;
        }

        void Update()
        {
            if (!game || !game.ControlsActive || game.Phase == DescentGame.RunPhase.Transit) return;
            if (game.CurrentMap && game.CurrentMap.Ready) { UpdateGeneratedFloor(); return; }
            Vector3 player = game.Player.transform.position;
            Vector3 toPlayer = player - transform.position;
            toPlayer.y = 0;
            if (player.z > 0 && toPlayer.magnitude < (sensitive ? 4.2f : 6))
            {
                Vector3 origin = transform.position + Vector3.up * 1.1f;
                if (!Physics.Raycast(origin, toPlayer.normalized, toPlayer.magnitude - 0.5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                { destination = player; alert = 4; }
            }
            alert = Mathf.Max(0, alert - Time.deltaTime);
            patrolTime -= Time.deltaTime;
            Vector3 delta = destination - transform.position;
            delta.y = 0;
            if (alert <= 0 && (delta.magnitude < 0.7f || patrolTime <= 0))
            {
                destination = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(6f, 19f));
                patrolTime = 6;
            }
            destination.z = Mathf.Max(1, destination.z); // Lift is the safe zone.
            float speed = alert > 0 ? 3.1f : 1.3f;
            Vector3 before = transform.position;
            motor.Move((delta.normalized * speed + Vector3.down * 5) * Time.deltaTime);
            if (delta.sqrMagnitude > 0.1f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 5);
            stuckTime = (transform.position - before).sqrMagnitude < 0.00005f ? stuckTime + Time.deltaTime : 0;
            if (stuckTime > 0.6f) { destination += transform.right * 2.5f; stuckTime = 0; }
            head.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 2) * 8, Mathf.Sin(Time.time * 5) * 4);
            eye.intensity = alert > 0 ? 3 : 1;
            if (toPlayer.magnitude < 1.25f && player.z > 0) game.Player.Hurt(transform.position);
        }

        void UpdateGeneratedFloor()
        {
            GeneratedFloor map = game.CurrentMap;
            Vector3 player = game.Player.transform.position;
            Vector3 toward = player + Vector3.up - (transform.position + Vector3.up);
            if (!game.Player.InCabin && Mathf.Abs(toward.y) < 2 && toward.magnitude < 6 &&
                !Physics.Raycast(transform.position + Vector3.up, toward.normalized, Mathf.Max(0.1f,toward.magnitude - 0.5f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            { destination = player; alert = 4; }
            alert = Mathf.Max(0, alert - Time.deltaTime);
            if (Time.time >= nextRoute || route.Count == 0)
            {
                MapRoom from = map.NearestRoom(transform.position);
                MapRoom to;
                if (alert > 0) to = map.NearestRoom(destination);
                else
                {
                    List<int> neighbors = map.Manifest.Neighbors(from.Id);
                    to = map.Manifest.Rooms[neighbors[Random.Range(0, neighbors.Count)]];
                    destination = map.Anchor(to);
                }
                if (to.Id == 0) { to = from; destination = map.Anchor(from); } // Arrival room and lift stay safe.
                // Refresh chase routes when targets change rooms; avoid walking back to the anchor every frame.
                if (route.Count == 0 || (route[route.Count - 1] - destination).sqrMagnitude > 4)
                {
                    route.Clear();
                    if (from.Id != to.Id) route.AddRange(map.Route(from.Id, to.Id));
                    route.Add(destination);
                }
                nextRoute = Time.time + (alert > 0 ? 1f : 4f);
            }
            while (route.Count > 0 && Vector3.Distance(transform.position, route[0]) < 0.6f) route.RemoveAt(0);
            if (route.Count == 0) return;
            Vector3 delta = route[0] - transform.position; delta.y = 0;
            motor.Move((delta.normalized * (alert > 0 ? 3.1f : 1.3f) + Vector3.down * 5) * Time.deltaTime);
            if (delta.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 5);
            head.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 2) * 8, Mathf.Sin(Time.time * 5) * 4);
            eye.intensity = alert > 0 ? 3 : 1;
            if (!game.Player.InCabin && toward.magnitude < 1.3f) game.Player.Hurt(transform.position);
        }
    }

    public sealed class ElectricalHazard : MonoBehaviour
    {
        DescentGame game;
        Renderer surface;
        Material safe, danger;
        float offset;
        public void Initialize(DescentGame owner, Workshop workshop, float timingOffset)
        {
            game = owner;
            surface = GetComponent<Renderer>();
            safe = workshop.Mat(Workshop.Mint);
            danger = workshop.Mat(Workshop.Red);
            offset = timingOffset;
        }
        void Update()
        {
            if (!game || !game.ControlsActive) return;
            bool live = (Time.time + offset) % 5f > 2.5f;
            surface.sharedMaterial = live ? danger : safe;
            Vector3 p = game.Player.transform.position - transform.position;
            if (live && Mathf.Abs(p.x) < transform.localScale.x / 2 + 0.25f && Mathf.Abs(p.z) < transform.localScale.z / 2 + 0.25f && p.y > -0.4f && p.y < 0.45f)
                game.Player.Hurt(transform.position);
        }
    }
}
