using System.Collections.Generic;
using UnityEngine;
using TheElevator.Office;

namespace TheElevator
{
    // A wall-mounted fire extinguisher: a cheap pickup that sprays white foam. Held, the left hand carries the
    // canister and the right hand aims the nozzle at the end of its hose. The stream shoves anyone standing in it
    // like a strong wind for as long as the trigger is held. The canister holds twelve seconds of spray in total;
    // stopping early keeps the rest, and an empty one no longer sprays.
    public sealed class FireExtinguisher : MonoBehaviour
    {
        public const float Capacity = 12, HalfHeight = .23f, Radius = .085f, Reach = 6.5f;
        public const string Title = "Fire extinguisher";
        public const int Price = 10;
        public float Remaining { get; private set; } = Capacity;
        public float Used { get { return 1 - Remaining / Capacity; } }
        public bool Empty { get { return Remaining <= 0; } }
        public bool Spraying { get; private set; }
        public Transform Nozzle { get; private set; }
        // Where the nozzle rests clipped to the canister when nobody is holding it.
        static readonly Vector3 RestNozzle = new Vector3(Radius + .03f, -.06f, 0);
        const float HoseRadius = .011f, Cone = 12;
        const int HoseSegments = 16, HoseSides = 8;

        public static readonly Vector3 HandleCentre = new Vector3(0, -.05f, -.035f), HandleSize = new Vector3(.03f, .085f, .034f);
        SalvageItem item;
        DescentGame game;
        Transform valve;
        Mesh hose;
        float nextHiss;
        bool emptyClick, wasHeld = true;

        public static FireExtinguisher Create(DescentGame game, OfficeArt art, Transform parent, Vector3 position, Quaternion rotation)
        {
            Transform root = art.Group(parent, Title, Vector3.zero);
            root.SetPositionAndRotation(position, rotation);
            FireExtinguisher extinguisher = root.gameObject.AddComponent<FireExtinguisher>();
            extinguisher.game = game;
            extinguisher.Build(art);
            extinguisher.item = root.gameObject.AddComponent<SalvageItem>();
            extinguisher.item.Configure(game, Title, 4, Price, false, new Vector3(Radius * 2 + .03f, HalfHeight * 2, Radius * 2 + .03f));
            // It hangs in its wall cradle until someone takes it; once dropped or thrown it falls like anything else.
            extinguisher.item.Body.isKinematic = true;
            return extinguisher;
        }

        // Canister centre at the origin; the valve outlet and hose leave on +X, the gauge faces -Z (toward whoever holds it).
        void Build(OfficeArt a)
        {
            a.Round(transform, "Extinguisher canister", new Vector3(0, -.025f, 0), new Vector3(Radius * 2, .185f, Radius * 2), a.Red);
            a.Round(transform, "Canister shoulder", new Vector3(0, .16f, 0), new Vector3(Radius * 2, .09f, Radius * 2), a.Red, PrimitiveType.Sphere);
            a.Round(transform, "Canister foot ring", new Vector3(0, -.205f, 0), new Vector3(Radius * 2 + .012f, .018f, Radius * 2 + .012f), a.Dark);
            a.Round(transform, "Instruction band", new Vector3(0, -.05f, 0), new Vector3(Radius * 2 + .004f, .055f, Radius * 2 + .004f), a.Paper);
            valve = a.Group(transform, "Valve head", new Vector3(0, .215f, 0));
            a.Box(valve, "Valve body", Vector3.zero, new Vector3(.05f, .055f, .05f), a.Dark);
            a.Box(valve, "Carry handle", new Vector3(0, .0f, .06f), new Vector3(.03f, .016f, .11f), a.Metal);
            GameObject lever = a.Box(valve, "Squeeze lever", new Vector3(0, .03f, .05f), new Vector3(.028f, .014f, .11f), a.Metal);
            lever.transform.localRotation = Quaternion.Euler(-12, 0, 0);
            a.Round(valve, "Pressure gauge", new Vector3(0, 0, -.03f), new Vector3(.034f, .034f, .012f), a.Paper, PrimitiveType.Sphere);
            a.Box(valve, "Hose outlet", new Vector3(.035f, -.005f, 0), new Vector3(.025f, .02f, .02f), a.Dark);
            a.Box(transform, "Safety pin", new Vector3(-.03f, .235f, 0), new Vector3(.03f, .006f, .006f), a.Brass);

            Nozzle = a.Group(transform, "Nozzle", RestNozzle);
            GameObject grip = a.Round(Nozzle, "Nozzle grip", new Vector3(0, 0, -.015f), new Vector3(.032f, .04f, .032f), a.Dark);
            grip.transform.localRotation = Quaternion.Euler(90, 0, 0);
            GameObject horn = a.Round(Nozzle, "Nozzle horn", new Vector3(0, 0, .06f), new Vector3(.046f, .036f, .046f), a.Dark);
            horn.transform.localRotation = Quaternion.Euler(90, 0, 0);
            // A pistol grip under the nozzle: that is what the right hand closes around.
            a.Box(Nozzle, "Nozzle pistol grip", HandleCentre, HandleSize, a.Metal);
            RestPose();

            GameObject tube = new GameObject("Hose");
            tube.transform.SetParent(transform, false);
            hose = new Mesh { name = "Extinguisher hose" };
            hose.MarkDynamic();
            tube.AddComponent<MeshFilter>().sharedMesh = hose;
            tube.AddComponent<MeshRenderer>().sharedMaterial = a.Dark;
            RebuildHose();
        }

        void RestPose() { Nozzle.localPosition = RestNozzle; Nozzle.localRotation = Quaternion.Euler(90, 0, 0); }

        public Vector3 Tip { get { return Nozzle.TransformPoint(0, 0, .1f); } }

        // Whether a world point is inside the nozzle (barrel, horn or pistol grip); used to keep the hand outside it.
        public bool InsideNozzle(Vector3 world)
        {
            Vector3 p = Nozzle.InverseTransformPoint(world);
            float radial = p.x * p.x + p.y * p.y;
            if (p.z > -.055f && p.z < .025f && radial < .016f * .016f) return true;
            if (p.z >= .025f && p.z < .095f && radial < .023f * .023f) return true;
            Vector3 h = p - HandleCentre;
            return Mathf.Abs(h.x) < HandleSize.x * .5f && Mathf.Abs(h.y) < HandleSize.y * .5f && Mathf.Abs(h.z) < HandleSize.z * .5f;
        }

        // Called by the holder after the camera has moved: the nozzle sits in the right hand, aimed at the crosshair.
        public void PoseHeld(Transform view)
        {
            Vector3 hand = view.TransformPoint(new Vector3(.15f, -.2f, .58f));
            Vector3 aim = view.position + view.forward * 8 - hand;
            Nozzle.SetPositionAndRotation(hand, Quaternion.LookRotation(aim, view.up));
            RebuildHose();
            wasHeld = true;
        }

        void LateUpdate()
        {
            // Put down or dropped: the nozzle clips back onto the canister.
            if (item && !item.IsHeld && wasHeld) { wasHeld = false; Spraying = false; RestPose(); RebuildHose(); }
        }

        // trigger: the use button is held this frame.
        public void Operate(WorkerController user, bool trigger, float dt)
        {
            Spraying = trigger && !Empty;
            if (trigger && Empty && !emptyClick) { emptyClick = true; if (game) game.Sound.Play(180, .06f, .05f); }
            if (!trigger) emptyClick = false;
            if (!Spraying) return;
            Remaining = Mathf.Max(0, Remaining - dt);
            Vector3 tip = Tip, direction = Nozzle.forward;
            FoamSpray.Get(game ? game.CurrentOffice ? game.CurrentOffice.transform : game.transform : transform.parent).Emit(tip, direction, user ? user.Motion : Vector3.zero, dt, user);
            Blow(user, tip, direction);
            if (game && Time.time > nextHiss) { nextHiss = Time.time + .08f; game.Sound.Play(1300 + Random.Range(-150, 150), .07f, .025f); }
            if (Empty && game) game.Notify("The extinguisher is empty.");
        }

        // Everyone standing in the stream is shoved along it, harder up close.
        void Blow(WorkerController user, Vector3 tip, Vector3 direction)
        {
            Vector3 flat = new Vector3(direction.x, 0, direction.z);
            if (flat.sqrMagnitude < .01f) return;
            flat.Normalize();
            OfficeFloor office = game ? game.CurrentOffice : null;
            if (office)
                foreach (OfficeEmployee employee in office.Employees)
                    if (InStream(tip, direction, employee.transform.position + Vector3.up * .9f, out float distance)) employee.Push(flat * Strength(distance));
            if (game && game.Player && game.Player != user && InStream(tip, direction, game.Player.transform.position + Vector3.up * .9f, out float gap))
                game.Player.Push(flat * Strength(gap));
        }

        public static float Strength(float distance) { return Mathf.Lerp(4.6f, 1.8f, Mathf.Clamp01(distance / Reach)); }

        // The stream widens with distance; a body counts when it is inside that cone with nothing solid in between.
        public static bool InStream(Vector3 tip, Vector3 direction, Vector3 body, out float distance)
        {
            Vector3 to = body - tip;
            distance = to.magnitude;
            if (distance > Reach || distance < .05f) return false;
            float along = Vector3.Dot(to, direction);
            if (along <= 0) return false;
            float off = (to - direction * along).magnitude;
            if (off > .4f + along * Mathf.Tan(Cone * Mathf.Deg2Rad)) return false;
            return !Physics.Linecast(tip, body, ~((1 << 2) | (1 << 8)), QueryTriggerInteraction.Ignore);
        }

        // A sagging tube from the valve outlet to the back of the nozzle.
        void RebuildHose()
        {
            if (!hose || !valve || !Nozzle) return;
            Vector3 start = valve.TransformPoint(new Vector3(.045f, -.005f, 0)), end = Nozzle.TransformPoint(new Vector3(0, 0, -.04f));
            float span = Vector3.Distance(start, end);
            Vector3 c1 = start + transform.right * (.05f + span * .15f) + Vector3.down * (.04f + span * .35f);
            Vector3 c2 = end - Nozzle.forward * (.06f + span * .2f) + Vector3.down * (.03f + span * .3f);
            var points = new List<Vector3>();
            for (int i = 0; i <= HoseSegments; i++)
            {
                float t = i / (float)HoseSegments, u = 1 - t;
                points.Add(transform.InverseTransformPoint(u * u * u * start + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * end));
            }
            var vertices = new Vector3[points.Count * HoseSides];
            var triangles = new int[(points.Count - 1) * HoseSides * 6];
            Vector3 normal = Vector3.up;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 forward = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                if (forward == Vector3.zero) forward = Vector3.forward;
                Vector3 side = Vector3.Cross(forward, normal);
                if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(forward, Vector3.right);
                side.Normalize();
                normal = Vector3.Cross(side, forward).normalized;
                for (int s = 0; s < HoseSides; s++)
                {
                    float angle = s * Mathf.PI * 2 / HoseSides;
                    vertices[i * HoseSides + s] = points[i] + (side * Mathf.Cos(angle) + normal * Mathf.Sin(angle)) * HoseRadius;
                }
            }
            int k = 0;
            for (int i = 0; i < points.Count - 1; i++)
                for (int s = 0; s < HoseSides; s++)
                {
                    int a = i * HoseSides + s, b = i * HoseSides + (s + 1) % HoseSides, c = a + HoseSides, d = b + HoseSides;
                    triangles[k++] = a; triangles[k++] = c; triangles[k++] = b;
                    triangles[k++] = b; triangles[k++] = c; triangles[k++] = d;
                }
            hose.Clear();
            hose.vertices = vertices;
            hose.triangles = triangles;
            hose.RecalculateNormals();
            hose.RecalculateBounds();
        }

        void OnDestroy() { if (hose) Destroy(hose); }
    }
}
