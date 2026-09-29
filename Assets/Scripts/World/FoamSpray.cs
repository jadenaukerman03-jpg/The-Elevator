using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TheElevator.Office;

namespace TheElevator
{
    // Extinguisher foam is purely visual: soft white blobs fly from the nozzle, arc down, and settle as flattened
    // puddles on the floor or table tops. Settled foam stays about twenty seconds, then shrinks away.
    // Foam that hits a person sticks to them: on the face it blinds them, and an employee hit by it is furious.
    // Everything is drawn instanced, so a floor covered in foam costs a handful of draw calls.
    public sealed class FoamSpray : MonoBehaviour
    {
        public const float Lifetime = 20;
        const float Fade = 2, Grow = .3f, Speed = 8.5f, Rate = 90, Gravity = 3.2f, Drag = .7f, FlightTime = 2.5f;
        const int MaxFlying = 260, MaxLanded = 800;
        struct Flying { public Vector3 Position, Velocity; public float Age, Size; public WorkerController Source; }
        struct Stuck { public Transform Anchor; public Vector3 Local; public Quaternion Rotation; public float Born, Size; }
        public const float StuckLifetime = 15;
        const int MaxStuck = 400;
        readonly List<Stuck> stuck = new List<Stuck>();
        readonly List<OfficeEmployee> nearby = new List<OfficeEmployee>();
        OfficeFloor office;
        Vector3 lastEmit;
        float nextScan;
        public int StuckCount { get { return stuck.Count; } }
        struct Landed { public Vector3 Position; public Quaternion Rotation; public float Born, Size; }
        readonly List<Flying> flying = new List<Flying>();
        readonly List<Landed> landed = new List<Landed>();
        readonly Matrix4x4[] batch = new Matrix4x4[1023];
        Mesh mesh;
        Material material;
        float carry;
        static FoamSpray instance;

        public int FlyingCount { get { return flying.Count; } }
        public int LandedCount { get { return landed.Count; } }

        // One foam system per floor: it lives under the floor root and disappears with it.
        public static FoamSpray Get(Transform floor)
        {
            if (instance) return instance;
            GameObject go = new GameObject("Extinguisher foam");
            if (floor) go.transform.SetParent(floor, false);
            instance = go.AddComponent<FoamSpray>();
            instance.office = floor ? floor.GetComponentInParent<OfficeFloor>() : null;
            return instance;
        }

        void Awake()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Destroy(sphere); else DestroyImmediate(sphere);
            material = new Material(Shader.Find("Elevator/Foam")) { name = "Extinguisher foam", color = new Color(.97f, .98f, 1f), enableInstancing = true };
        }

        public void Emit(Vector3 tip, Vector3 direction, Vector3 carrierVelocity, float dt, WorkerController source = null)
        {
            lastEmit = tip;
            carry += Rate * dt;
            while (carry >= 1)
            {
                carry -= 1;
                if (flying.Count >= MaxFlying) flying.RemoveAt(0);
                Vector3 spread = Random.insideUnitSphere * .07f;
                flying.Add(new Flying
                {
                    Position = tip + direction * Random.Range(0f, .15f),
                    Velocity = (direction + spread).normalized * Speed * Random.Range(.8f, 1.05f) + carrierVelocity,
                    Size = Random.Range(.08f, .15f),
                    Source = source
                });
            }
        }

        void Update() { Step(Time.deltaTime); Draw(); }

        // Advances flight, landing and expiry by dt.
        public void Step(float dt)
        {
            for (int i = flying.Count - 1; i >= 0; i--)
            {
                Flying f = flying[i];
                f.Age += dt;
                f.Velocity += Vector3.down * Gravity * dt;
                f.Velocity *= 1 - Drag * dt;
                Vector3 step = f.Velocity * dt;
                float length = step.magnitude;
                if (HitPerson(f.Position + step, f.Size, f.Source)) { flying.RemoveAt(i); continue; }
                if (length > 1e-5f && Physics.Raycast(f.Position, step / length, out RaycastHit hit, length + .02f, ~(1 << 2), QueryTriggerInteraction.Ignore))
                {
                    // On people and loose items it just splats. On floors and table tops it settles; on walls and
                    // the sides of furniture it runs down to the floor.
                    if (hit.collider.gameObject.layer == 8 || hit.rigidbody) { flying.RemoveAt(i); continue; }
                    if (hit.normal.y > .55f) { Land(hit.point, hit.normal, f.Size); flying.RemoveAt(i); continue; }
                    f.Position = hit.point + hit.normal * (f.Size * .5f + .01f);
                    f.Velocity = Vector3.down * 1.2f + hit.normal * .4f;
                    flying[i] = f;
                    continue;
                }
                f.Position += step;
                if (f.Age > FlightTime) { flying.RemoveAt(i); continue; }
                flying[i] = f;
            }
            if (Time.time >= nextScan) Scan();
            stuck.RemoveAll(s => !s.Anchor || Time.time - s.Born > StuckLifetime);
            int expired = 0;
            while (expired < landed.Count && Time.time - landed[expired].Born > Lifetime) expired++;
            if (expired > 0) landed.RemoveRange(0, expired);
        }

        // Only people near the nozzle can be hit; the list is refreshed a few times a second.
        public void Scan()
        {
            nextScan = Time.time + .2f;
            nearby.Clear();
            if (!office) return;
            foreach (OfficeEmployee employee in office.Employees)
                if (employee && (employee.transform.position - lastEmit).sqrMagnitude < 12 * 12) nearby.Add(employee);
        }

        // Head and body are a sphere and a capsule; foam stuck to the head moves with it.
        bool HitPerson(Vector3 point, float size, WorkerController source)
        {
            foreach (OfficeEmployee employee in nearby)
            {
                if (!employee) continue;
                Transform head = employee.Robot ? employee.Robot.Head : null;
                // The head transform is the neck pivot; the round head sits above it.
                Vector3 centre = head ? head.TransformPoint(BeanRig.HeadCenter) : employee.transform.position + Vector3.up * 1.45f;
                if ((point - centre).sqrMagnitude < .38f * .38f) { Stick(head ? head : employee.transform, centre, .36f, point, size); employee.Foamed(true); return true; }
                if (InBody(employee.transform.position, point, out Vector3 axis)) { Stick(employee.transform, axis, .3f, point, size); employee.Foamed(false); return true; }
            }
            WorkerController player = office && office.Game ? office.Game.Player : null;
            if (player && player != source)
            {
                Vector3 eyes = player.transform.position + Vector3.up * WorkerController.EyeHeight;
                if ((point - eyes).sqrMagnitude < .36f * .36f) { player.Foam(.2f); return true; }
                if (InBody(player.transform.position, point, out Vector3 axis)) { Stick(player.transform, axis, .34f, point, size); return true; }
            }
            return false;
        }

        static bool InBody(Vector3 feet, Vector3 point, out Vector3 axis)
        {
            Vector3 low = feet + Vector3.up * .35f, high = feet + Vector3.up * .95f;
            float t = Mathf.Clamp01(Vector3.Dot(point - low, high - low) / (high - low).sqrMagnitude);
            axis = low + (high - low) * t;
            return (point - axis).sqrMagnitude < .3f * .3f;
        }

        void Stick(Transform anchor, Vector3 centre, float radius, Vector3 point, float size)
        {
            Vector3 normal = (point - centre).sqrMagnitude > 1e-6f ? (point - centre).normalized : Vector3.up;
            if (stuck.Count >= MaxStuck) stuck.RemoveAt(0);
            stuck.Add(new Stuck
            {
                Anchor = anchor,
                Local = anchor.InverseTransformPoint(centre + normal * radius),
                Rotation = Quaternion.Inverse(anchor.rotation) * Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, Random.Range(0f, 360f), 0),
                Born = Time.time,
                Size = size * Random.Range(1.1f, 1.6f)
            });
        }

        void Land(Vector3 point, Vector3 normal, float size)
        {
            // Foam landing on fresh foam piles up into a bigger blob instead of adding another one.
            for (int i = landed.Count - 1; i >= 0 && i >= landed.Count - 40; i--)
            {
                Landed near = landed[i];
                if (near.Size < .5f && (near.Position - point).sqrMagnitude < .12f * .12f)
                {
                    near.Size = Mathf.Min(.5f, near.Size + size * .35f);
                    landed[i] = near;
                    return;
                }
            }
            if (landed.Count >= MaxLanded) landed.RemoveAt(0);
            landed.Add(new Landed
            {
                Position = point + normal * .005f,
                Rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, Random.Range(0f, 360f), 0),
                Born = Time.time,
                Size = size * Random.Range(1.8f, 2.8f)
            });
        }

        // Queues this frame's foam for rendering.
        public void Draw()
        {
            int count = 0;
            foreach (Flying f in flying) { batch[count++] = Matrix4x4.TRS(f.Position, Quaternion.identity, Vector3.one * f.Size); if (count == batch.Length) Flush(ref count); }
            float now = Time.time;
            foreach (Landed l in landed)
            {
                float age = now - l.Born;
                float scale = l.Size * Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / Grow)) * Mathf.SmoothStep(0, 1, Mathf.Clamp01((Lifetime - age) / Fade));
                batch[count++] = Matrix4x4.TRS(l.Position, l.Rotation, new Vector3(scale, scale * .4f, scale));
                if (count == batch.Length) Flush(ref count);
            }
            foreach (Stuck s in stuck)
            {
                if (!s.Anchor) continue;
                float age = now - s.Born;
                float scale = s.Size * Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / Grow)) * Mathf.SmoothStep(0, 1, Mathf.Clamp01((StuckLifetime - age) / Fade));
                batch[count++] = Matrix4x4.TRS(s.Anchor.TransformPoint(s.Local), s.Anchor.rotation * s.Rotation, new Vector3(scale, scale * .5f, scale));
                if (count == batch.Length) Flush(ref count);
            }
            Flush(ref count);
        }

        void Flush(ref int count)
        {
            if (count > 0) Graphics.DrawMeshInstanced(mesh, 0, material, batch, count, null, ShadowCastingMode.Off, true);
            count = 0;
        }

        void OnDestroy()
        {
            if (material) Destroy(material);
            if (instance == this) instance = null;
        }
    }
}
