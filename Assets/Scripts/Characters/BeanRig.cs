using System.Collections.Generic;
using UnityEngine;

namespace TheElevator
{
    // Plain is the undressed avatar base that the wardrobe layers clothing onto; the rest are NPC job outfits.
    public enum BeanOutfit { Plain, Office, Reception, Technician, Clerk, Security }

    // Flat colors only: the style reads through silhouette and hue, never texture or small hardware.
    // Primary is the main garment, Accent the tie/cap/headset. Face features index the styles in BuildFace.
    public struct BeanLook
    {
        public BeanOutfit Outfit;
        public Color Skin, Primary, Accent;
        public int Eyes, Mouth, Brows, Glasses;
    }

    public struct BeanPose
    {
        public float Speed;
        public bool Seated, Carrying, Talking, Typing, Reaching;
        public float ArmPitch, ElbowBend; // degrees, negative lifts forward
        public Vector3 ReachTarget, LookTarget;
    }

    // Big round head sunk into a bean torso, long noodle arms, short legs, painted-on face.
    // Built from a few smooth procedural meshes shared by every character; clothing lives in BeanRig.Clothing
    // (NPC jobs) and AvatarWardrobe (player cosmetics), both layered through the public builder API below.
    public sealed partial class BeanRig : MonoBehaviour
    {
        public static readonly Color[] SkinColors = {
            new Color(.97f,.45f,.40f), new Color(1f,.62f,.26f), new Color(1f,.83f,.30f),
            new Color(.66f,.86f,.31f), new Color(.31f,.76f,.47f), new Color(.26f,.78f,.78f),
            new Color(.36f,.61f,.96f), new Color(.63f,.49f,.93f), new Color(.96f,.56f,.79f)
        };
        public static readonly Color Cream = new Color(.97f,.95f,.88f);
        public static readonly Color FaceInk = new Color(.07f,.07f,.09f);
        public static readonly Color Trousers = new Color(.24f,.26f,.32f);
        public static readonly Color Shoes = new Color(.17f,.15f,.15f);
        public static readonly Vector3 HeadCenter = new Vector3(0,.27f,0), HeadRadii = new Vector3(.34f,.31f,.31f);

        const float Scale = 1.1f, HipHeight = .52f, SeatedHip = .50f;
        const float UpperArm = .24f, Forearm = .22f, Thigh = .20f, Shin = .19f;
        static readonly Vector2[] TorsoProfile = {
            new Vector2(0,.44f), new Vector2(.10f,.43f), new Vector2(.16f,.39f), new Vector2(.19f,.30f),
            new Vector2(.20f,.16f), new Vector2(.20f,.02f), new Vector2(.18f,-.07f), new Vector2(.12f,-.12f), new Vector2(0,-.13f) };

        static readonly Dictionary<Color,Material> paints = new Dictionary<Color,Material>();
        static readonly Dictionary<string,Mesh> meshes = new Dictionary<string,Mesh>();

        Transform pelvis, head, shoulderL, shoulderR, elbowL, elbowR, hipL, hipR, kneeL, kneeR, ankleL, ankleR;
        readonly Dictionary<Renderer,bool> castsShadow = new Dictionary<Renderer,bool>();
        readonly List<Transform> blinkers = new List<Transform>();
        readonly List<Vector3> blinkScales = new List<Vector3>();
        readonly List<GameObject> shines = new List<GameObject>();
        GameObject mouthClosed, mouthOpen;
        MeshFilter mittenL, mittenR;
        Vector3 handPose = RelaxedHand;
        int identity;
        float phase, seat, pace, clock;
        Quaternion gaze = Quaternion.identity;
        Vector2 bob, bobVelocity;
        Vector3 lastPosition, lastVelocity;
        bool tracking;

        public BeanLook Look { get; private set; }
        public Transform Pelvis { get { return pelvis; } }
        public Transform Head { get { return head; } }
        public Transform RightHand { get; private set; }
        public Transform Shoulder(int side) { return side < 0 ? shoulderL : shoulderR; }
        public Transform Elbow(int side) { return side < 0 ? elbowL : elbowR; }
        public Transform Hand(int side) { return Elbow(side).Find("Hand"); }
        public Transform Hip(int side) { return side < 0 ? hipL : hipR; }
        public Transform Knee(int side) { return side < 0 ? kneeL : kneeR; }
        public Transform Ankle(int side) { return side < 0 ? ankleL : ankleR; }

        public void Build(BeanLook look, int id)
        {
            identity = id; Look = look;
            Transform body = Group("Bean body", transform, Vector3.zero);
            body.localScale = Vector3.one * Scale;
            pelvis = Group("Pelvis", body, new Vector3(0, HipHeight, 0));
            Part("Outfit body", pelvis, Torso(), Vector3.zero, new Vector3(1,1,.82f), look.Primary);

            Transform neck = Group("Neck", pelvis, new Vector3(0,.40f,0));
            head = Group("Head", neck, Vector3.zero);
            Part("Head", head, Sphere(), HeadCenter, HeadRadii * 2, look.Skin);
            BuildFace(look);

            // The plain avatar wears a short-sleeved tee: bare forearms.
            Color forearm = look.Outfit == BeanOutfit.Plain ? look.Skin : look.Primary, legs = Trousers;
            DressJob(look, ref forearm, ref legs);
            RightHand = Arm(1, look.Primary, forearm, look.Skin, out shoulderR, out elbowR);
            Arm(-1, look.Primary, forearm, look.Skin, out shoulderL, out elbowL);
            Leg(-1, legs, out hipL, out kneeL, out ankleL);
            Leg(1, legs, out hipR, out kneeR, out ankleR);
        }

        // ---- Builder API used by clothing -------------------------------------------------------------

        public Renderer Add(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Color color,
            Quaternion? rotation = null, bool shadows = true)
        { return Part(name, parent, mesh, position, scale, color, rotation, shadows); }

        public void Repaint(string partName, Color color)
        {
            foreach (Renderer part in GetComponentsInChildren<Renderer>(true)) if (part.name == partName) part.sharedMaterial = Paint(color);
        }

        public Color ColorOf(string partName)
        {
            foreach (Renderer part in GetComponentsInChildren<Renderer>(true)) if (part.name == partName) return part.sharedMaterial.color;
            return Look.Skin;
        }

        public Vector3 HeadPoint(float yaw, float pitch, out Vector3 normal) { return Surface(Direction(yaw, pitch), out normal); }
        public Vector3 HeadPoint(Vector3 direction, out Vector3 normal) { return Surface(direction.normalized, out normal); }

        public static float TorsoRadius(float y)
        {
            for (int i = 0; i < TorsoProfile.Length - 1; i++)
                if (y <= TorsoProfile[i].y && y >= TorsoProfile[i + 1].y)
                    return Mathf.Lerp(TorsoProfile[i].x, TorsoProfile[i + 1].x, Mathf.InverseLerp(TorsoProfile[i].y, TorsoProfile[i + 1].y, y));
            return 0;
        }

        // A vertical line down the chest, `lift` in front of the torso surface (pelvis space).
        public static Vector3[] FrontLine(float top, float bottom, float lift)
        {
            Vector3[] path = new Vector3[12];
            for (int i = 0; i < path.Length; i++) { float y = Mathf.Lerp(top, bottom, i / 11f); path[i] = new Vector3(0, y, TorsoRadius(y) * .82f + lift); }
            return path;
        }

        // First person: head and arms only cast the body's shadow; unshadowed face details are hidden outright.
        public void SetFirstPerson(bool on)
        {
            foreach (Renderer part in GetComponentsInChildren<Renderer>(true))
            {
                Transform t = part.transform;
                if (!t.IsChildOf(head) && !t.IsChildOf(shoulderL) && !t.IsChildOf(shoulderR)) continue;
                if (!castsShadow.TryGetValue(part, out bool casts)) castsShadow[part] = casts = part.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
                part.shadowCastingMode = !casts ? UnityEngine.Rendering.ShadowCastingMode.Off
                    : on ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
                part.enabled = !on || casts;
            }
        }

        // ---- Body --------------------------------------------------------------------------------------

        Transform Arm(int side, Color sleeve, Color forearm, Color hand, out Transform shoulder, out Transform elbow)
        {
            shoulder = Group(side < 0 ? "Left shoulder" : "Right shoulder", pelvis, new Vector3(side * .20f, .33f, 0));
            Part("Upper sleeve", shoulder, Capsule(UpperArm,.058f,.054f), Vector3.zero, Vector3.one, sleeve);
            elbow = Group("Elbow", shoulder, new Vector3(0, -UpperArm, 0));
            Part("Forearm", elbow, Capsule(Forearm,.054f,.05f), Vector3.zero, Vector3.one, forearm);
            Transform wrist = Group("Hand", elbow, new Vector3(0, -Forearm, 0));
            // Mitten space is fingers +Y, palm +Z: turn it to hang with the palm toward the body, thumb forward.
            Renderer mitten = Part("Mitten", wrist, Mitten(RelaxedHand, side), new Vector3(0,-.07f,0), Vector3.one, hand,
                Quaternion.LookRotation(new Vector3(-side, 0, 0), Vector3.down));
            if (side < 0) mittenL = mitten.GetComponent<MeshFilter>(); else mittenR = mitten.GetComponent<MeshFilter>();
            return wrist;
        }

        void Leg(int side, Color trouser, out Transform hip, out Transform knee, out Transform ankle)
        {
            hip = Group(side < 0 ? "Left hip" : "Right hip", pelvis, new Vector3(side * .105f, -.04f, 0));
            Part("Thigh", hip, Capsule(Thigh,.07f,.066f), Vector3.zero, Vector3.one, trouser);
            knee = Group("Knee", hip, new Vector3(0, -Thigh, 0));
            Part("Shin", knee, Capsule(Shin,.066f,.062f), Vector3.zero, Vector3.one, trouser);
            ankle = Group("Ankle", knee, new Vector3(0, -Shin, 0));
            Part("Boot", ankle, Sphere(), new Vector3(0,-.03f,.04f), new Vector3(.17f,.14f,.26f), Shoes);
        }

        // ---- Face: 6 eyes, 6 mouths, 6 brows (0 = none), 6 glasses (0 = none) -------------------------

        void BuildFace(BeanLook look)
        {
            BuildEyes(look.Eyes % 6);
            BuildMouth(look.Mouth % 6);
            BuildBrows(look.Brows % 6);
            BuildGlasses(look.Glasses % 6);
            mouthOpen = Feature("Talking mouth", -16, new Vector3(.06f,.048f,.02f));
            mouthOpen.SetActive(false);
        }

        void BuildEyes(int style)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 point = Surface(Direction(side * 16, 6), out Vector3 normal);
                Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                if (style == 3)
                {
                    Transform arc = Part("Happy eye", head, Arc("Happy eye", .09f, .045f, .013f), point + normal * .01f, Vector3.one, FaceInk, facing, false).transform;
                    blinkers.Add(arc); blinkScales.Add(Vector3.one);
                    continue;
                }
                Vector3 size = style == 1 ? new Vector3(.09f,.09f,.03f) : style == 2 ? new Vector3(.095f,.052f,.03f)
                    : style == 4 ? new Vector3(.095f,.14f,.03f) : style == 5 ? new Vector3(.05f,.05f,.03f) : new Vector3(.078f,.125f,.03f);
                blinkers.Add(Part("Eye", head, Sphere(), point + normal * .006f, size, FaceInk, facing, false).transform);
                blinkScales.Add(size);
                if (style == 2 || style == 5) continue;
                shines.Add(Part("Eye shine", head, Sphere(), point + normal * .02f + facing * new Vector3(size.x * .22f, size.y * .24f, 0),
                    Vector3.one * (style == 4 ? .036f : .026f), Color.white, facing, false).gameObject);
                if (style == 4)
                    shines.Add(Part("Eye shine", head, Sphere(), point + normal * .02f + facing * new Vector3(-size.x * .2f, -size.y * .22f, 0),
                        Vector3.one * .016f, Color.white, facing, false).gameObject);
            }
        }

        void BuildMouth(int style)
        {
            switch (style)
            {
                case 1: mouthClosed = Feature("Small round mouth", -15, new Vector3(.042f,.034f,.02f)); break;
                case 2: mouthClosed = SmilePart(8, .5f, 0); break;
                case 3: mouthClosed = SmilePart(14, 7, 0); break;
                case 4:
                    mouthClosed = Group("Laughing mouth", head, Vector3.zero).gameObject;
                    Vector3 point = Surface(Direction(0, -16), out Vector3 normal);
                    Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                    Part("Open mouth", mouthClosed.transform, Sphere(), point + normal * .004f, new Vector3(.10f,.065f,.02f), FaceInk, facing, false);
                    Part("Tongue", mouthClosed.transform, Sphere(), point + normal * .012f + facing * new Vector3(0,-.014f,0), new Vector3(.05f,.026f,.012f), new Color(.95f,.45f,.5f), facing, false);
                    break;
                case 5: mouthClosed = SmilePart(10, 3, 4); break;
                default: mouthClosed = SmilePart(11, 4.5f, 0); break;
            }
        }

        GameObject SmilePart(float half, float curve, float tilt)
        { return Part("Smile", head, Smile(half, curve, tilt), Vector3.zero, Vector3.one, FaceInk, Quaternion.identity, false).gameObject; }

        void BuildBrows(int style)
        {
            if (style == 0) return;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 point = Surface(Direction(side * 16, 25), out Vector3 normal);
                Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                if (style == 2) { Part("Eyebrow", head, Arc("Arched brow", .085f, .018f, .01f), point + normal * .008f, Vector3.one, FaceInk, facing, false); continue; }
                float roll = style == 3 ? -side * 20 : style == 4 ? side * 22 : side * -6;
                Vector3 size = style == 5 ? new Vector3(.105f,.038f,.022f) : new Vector3(.075f,.02f,.02f);
                Part("Eyebrow", head, Sphere(), point + normal * .01f, size, FaceInk, facing * Quaternion.Euler(0, 0, roll), false);
            }
        }

        void BuildGlasses(int style)
        {
            if (style == 0) return;
            Color frame = style == 1 ? new Color(.36f,.22f,.14f) : style == 5 ? new Color(1f,.78f,.25f) : FaceInk;
            float ring = style == 4 ? .092f : .074f, thickness = style == 4 ? .016f : .01f;
            Vector3[] centers = new Vector3[2];
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                Vector3 point = Surface(Direction(side * 16, 6), out Vector3 normal);
                Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                centers[i] = point + normal * .035f;
                if (style == 5 && side < 0) continue;
                if (style == 2) Part("Glasses frame", head, SquareFrame(), centers[i], Vector3.one, frame, facing, false);
                else Part("Glasses frame", head, Torus(ring, thickness), centers[i], Vector3.one, frame, facing * Quaternion.Euler(90,0,0), false);
                if (style == 3)
                {
                    Part("Dark lens", head, Sphere(), centers[i] - normal * .004f, new Vector3(.145f,.145f,.012f), new Color(.1f,.12f,.2f), facing, false);
                    Part("Lens glint", head, Sphere(), centers[i] + normal * .004f + facing * new Vector3(.03f,.03f,0), new Vector3(.03f,.018f,.006f), Color.white, facing * Quaternion.Euler(0,0,35), false);
                }
                if (style == 5)
                {
                    Vector3 cheek = Surface(Direction(side * 40, -25), out Vector3 cheekNormal) + cheekNormal * .01f;
                    Part("Monocle chain", head, Tube("Monocle chain", new[] { centers[i] + facing * new Vector3(ring, 0, 0), Vector3.Lerp(centers[i], cheek, .5f) + Vector3.down * .04f, cheek }, .005f), Vector3.zero, Vector3.one, frame, null, false);
                }
            }
            if (style == 5) return;
            Vector3 from = centers[0] + Vector3.right * ring, to = centers[1] - Vector3.right * ring;
            Part("Glasses bridge", head, Capsule(.05f,.008f,.008f), from, new Vector3(1, (to - from).magnitude / .05f, 1), frame, Quaternion.FromToRotation(Vector3.down, to - from), false);
        }

        GameObject Feature(string name, float pitch, Vector3 size)
        {
            Vector3 point = Surface(Direction(0, pitch), out Vector3 normal);
            return Part(name, head, Sphere(), point + normal * .004f, size, FaceInk, Quaternion.LookRotation(normal, Vector3.up), false).gameObject;
        }

        // ---- Animation ---------------------------------------------------------------------------------

        public void Animate(float dt, BeanPose pose)
        {
            if (!pelvis) return;
            // Own clock: idle motion runs from the frame times it is given, so it also plays in paused previews and edit-mode renders.
            clock += Mathf.Min(dt, .1f);
            // Smoothed ground speed; cadence follows stride length so fast running lengthens strides instead of flailing.
            pace = Mathf.MoveTowards(pace, pose.Speed, dt * 12);
            float walk = Mathf.Clamp01(pace / 1.2f), run = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2.4f, 4.8f, pace)), idle = 1 - walk;
            float cycles = pace < .05f ? 0 : Mathf.Clamp(pace / (.9f + pace * .18f), .6f, 2.4f);
            phase = (phase + dt * cycles * Mathf.PI * 2) % (Mathf.PI * 200);
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase), breathe = Mathf.Sin(clock * 1.7f + identity);
            float shift = Mathf.Sin(clock * .55f + identity * 1.3f) * idle * (1 - seat);
            seat = Mathf.MoveTowards(seat, pose.Seated ? 1 : 0, dt * 2.6f);

            float bounce = walk * Mathf.Abs(c) * Mathf.Lerp(.018f, .045f, run);
            float lean = walk * Mathf.Lerp(4, 15, run) * (1 - seat) + (pose.Carrying ? 4 : 0);
            float twist = s * walk * Mathf.Lerp(5, 9, run);
            pelvis.localPosition = new Vector3(shift * .012f, Mathf.Lerp(HipHeight - run * .03f + bounce, SeatedHip, seat) + breathe * .004f, 0);
            pelvis.localRotation = Quaternion.Euler(lean, twist, s * walk * 3 + shift * 1.6f);
            PoseLeg(hipL, kneeL, ankleL, s, c, walk, run, Mathf.Max(0, shift));
            PoseLeg(hipR, kneeR, ankleR, -s, -c, walk, run, Mathf.Max(0, -shift));

            bool talking = pose.Talking && !pose.Reaching && pace < .15f;
            float follow = 1 - Mathf.Exp(-(10 + walk * 10) * dt), drag = 1 - Mathf.Exp(-(7 + walk * 8) * dt);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform shoulder = side < 0 ? shoulderL : shoulderR, elbow = side < 0 ? elbowL : elbowR;
                float armSwing = side * s * walk; // arms oppose the same-side leg
                float sway = Mathf.Sin(clock * .9f + side * 1.7f + identity) * idle;
                Quaternion arm = pose.Carrying ? Quaternion.Euler(-78, 0, side * 3)
                    : Quaternion.Euler(pose.ArmPitch + armSwing * Mathf.Lerp(22, 50, run) + sway * 2, 0, side * (Mathf.Lerp(6, 12, run * walk) + sway * 1.5f));
                // Walking arms hang loose; running arms pump bent at the elbow, bending further on the forward swing.
                float bend = pose.Carrying ? -35 : pose.ElbowBend + Mathf.Lerp(-6, -78, run) * walk - Mathf.Max(0, -armSwing) * Mathf.Lerp(10, 18, run) + sway * 3;
                if (pose.Typing) bend += Mathf.Sin(clock * 14 + side) * 3;
                if (talking && side > 0) { arm = Quaternion.Euler(-25, 0, 12); bend = -70 + Mathf.Sin(clock * 4) * 14; }
                shoulder.localRotation = Quaternion.Slerp(shoulder.localRotation, arm, follow);
                elbow.localRotation = Quaternion.Slerp(elbow.localRotation, Quaternion.Euler(bend, 0, 0), drag);
            }
            if (pose.Reaching) Reach(pose.ReachTarget);
            SetHands(pose.Carrying || pose.Reaching ? GripHand : pose.Typing ? new Vector3(8, 10, 5) : RelaxedHand);

            Vector3 toward = pose.LookTarget == Vector3.zero ? Vector3.forward : Quaternion.Inverse(transform.rotation) * (pose.LookTarget - head.position);
            float yaw = Mathf.Clamp(Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg, -60, 60);
            float tilt = pose.LookTarget == Vector3.zero ? 0 : Mathf.Clamp(-Mathf.Atan2(toward.y, new Vector2(toward.x, toward.z).magnitude) * Mathf.Rad2Deg, -25, 30);
            if (pose.LookTarget == Vector3.zero) { yaw += Mathf.Sin(clock * .23f + identity) * 9 * idle; tilt += Mathf.Sin(clock * .31f + identity * .7f) * 3 * idle; }
            if (pose.Typing) tilt = 14;
            gaze = Quaternion.Slerp(gaze, Quaternion.Euler(tilt, yaw, 0), 1 - Mathf.Exp(-3.5f * dt));
            Bobble(dt);
            // The head steadies against the hips' twist so the face stays pointed where the body is going.
            head.localRotation = Quaternion.Euler(-lean * .5f, -twist * .8f, 0) * gaze * Quaternion.Euler(bob.x + Mathf.Sin(phase * 2) * walk * 1.5f, 0, bob.y + breathe * .6f);

            float blink = (clock + identity * .71f) % 4.3f < .11f ? .12f : 1;
            for (int i = 0; i < blinkers.Count; i++) { Vector3 scale = blinkScales[i]; blinkers[i].localScale = new Vector3(scale.x, scale.y * blink, scale.z); }
            foreach (GameObject shine in shines) shine.SetActive(blink == 1);
            bool open = talking && Mathf.Sin(clock * 17 + identity) > 0;
            mouthClosed.SetActive(!open); mouthOpen.SetActive(open);
        }

        // swing: +1 leg back, -1 leg forward. The knee folds while the leg travels forward; the toe rolls off behind.
        void PoseLeg(Transform hip, Transform knee, Transform ankle, float swing, float travel, float walk, float run, float rest)
        {
            float thigh = swing * walk * Mathf.Lerp(26, 48, run);
            float fold = walk * (Mathf.Max(0, -travel) * Mathf.Lerp(35, 95, run) + Mathf.Lerp(4, 12, run)) + rest * 6;
            hip.localRotation = Quaternion.Euler(Mathf.Lerp(thigh - fold * .25f, -86, seat), 0, 0);
            knee.localRotation = Quaternion.Euler(Mathf.Lerp(fold, 86, seat), 0, 0);
            float toe = Mathf.Max(0, swing) * walk * Mathf.Lerp(10, 25, run) * (1 - seat);
            ankle.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0) * Quaternion.Euler(toe, 0, 0);
        }

        // ---- One-piece mitten: knuckle, finger and thumb bends of a single continuous surface ----------

        public static readonly Vector3 RelaxedHand = new Vector3(12, 18, 10), GripHand = new Vector3(38, 68, 45);

        void SetHands(Vector3 bend)
        {
            if (bend == handPose || !mittenL) return;
            handPose = bend;
            mittenL.sharedMesh = Mitten(bend, -1);
            mittenR.sharedMesh = Mitten(bend, 1);
        }

        public static Mesh Mitten(Vector3 bend, int side)
        {
            string key = "Mitten " + bend + "/" + side;
            if (meshes.TryGetValue(key, out Mesh cached) && cached) return cached;
            Mesh mesh = new Mesh { name = key, hideFlags = HideFlags.DontSave };
            Mitten(mesh, bend, side);
            meshes[key] = mesh;
            return mesh;
        }

        // Mitten space: palm centre at the origin, wrist at -Y, fingers toward +Y, palm facing +Z, thumb on the -side X edge.
        // bend = (knuckle, finger middle, thumb) curl in degrees toward the palm.
        public static void Mitten(Mesh mesh, Vector3 bend, int side)
        {
            const int rings = 18, segments = 24;
            Vector3 thumbBase = new Vector3(-side * .045f, -.03f, .01f), thumbOut = new Vector3(-side, .75f, .3f).normalized;
            Vector3 knuckle = new Vector3(0, .02f, .042f), middle = new Vector3(0, .055f, .042f), thumbPivot = new Vector3(-side * .035f, -.045f, .02f);
            Quaternion knuckleTurn = Quaternion.Euler(bend.x, 0, 0), middleTurn = Quaternion.Euler(bend.y, 0, 0);
            var vertices = new Vector3[(rings - 1) * segments + 2];
            vertices[0] = Deform(new Vector3(0, .09f, 0));
            vertices[vertices.Length - 1] = Deform(new Vector3(0, -.075f, 0));
            for (int r = 1; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    float lat = Mathf.PI * r / rings, lon = Mathf.PI * 2 * s / segments;
                    // Squarer than an ellipse across the palm, rounder along the thickness.
                    float x = Mathf.Sign(Mathf.Cos(lon)) * Mathf.Pow(Mathf.Abs(Mathf.Cos(lon)), .75f), z = Mathf.Sin(lon);
                    float y = Mathf.Cos(lat), ring = Mathf.Pow(Mathf.Sin(lat), .8f);
                    vertices[1 + (r - 1) * segments + s] = Deform(new Vector3(x * ring * .06f, y > 0 ? y * .085f + .005f : y * .08f + .005f, z * ring * .042f));
                }
            var triangles = new List<int>();
            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;
                triangles.Add(0); triangles.Add(1 + next); triangles.Add(1 + s);
                for (int r = 1; r < rings - 1; r++)
                {
                    int a = 1 + (r - 1) * segments + s, b = 1 + (r - 1) * segments + next, cc = a + segments, d = b + segments;
                    triangles.Add(a); triangles.Add(b); triangles.Add(cc);
                    triangles.Add(b); triangles.Add(d); triangles.Add(cc);
                }
                int last = 1 + (rings - 2) * segments;
                triangles.Add(last + s); triangles.Add(last + next); triangles.Add(vertices.Length - 1);
            }
            mesh.Clear();
            mesh.vertices = vertices; mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();

            Vector3 Deform(Vector3 p)
            {
                // Thumb: a smooth lobe grown out of the palm edge, then swung toward the palm about its root.
                float thumb = Mathf.Exp(-(p - thumbBase).sqrMagnitude / (.05f * .05f));
                p += thumbOut * .05f * thumb * thumb * (3 - 2 * thumb);
                if (thumb > .05f) p = thumbPivot + Quaternion.Euler(0, side * bend.z * thumb, 0) * (p - thumbPivot);
                // Fingers: two soft hinges across the width; the finger block is everything past the knuckle line.
                float finger = (1 - thumb) * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0f, .045f, p.y));
                float tip = (1 - thumb) * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.035f, .085f, p.y));
                Vector3 original = p;
                p = middle + Quaternion.Slerp(Quaternion.identity, middleTurn, tip) * (p - middle);
                p = knuckle + Quaternion.Slerp(Quaternion.identity, knuckleTurn, finger) * (p - knuckle);
                return finger > 0 || tip > 0 ? p : original;
            }
        }

        // Head lags behind body acceleration on an underdamped spring: the wobbly bobblehead feel.
        void Bobble(float dt)
        {
            if (!Application.isPlaying || dt <= 0) return;
            Vector3 velocity = (transform.position - lastPosition) / dt;
            lastPosition = transform.position;
            if (!tracking || velocity.magnitude > 20) { tracking = true; lastVelocity = velocity.magnitude > 20 ? Vector3.zero : velocity; return; }
            Vector3 accel = Quaternion.Inverse(transform.rotation) * Vector3.ClampMagnitude((velocity - lastVelocity) / dt, 25);
            lastVelocity = velocity;
            float step = Mathf.Min(dt, .05f);
            Vector2 drive = new Vector2(-accel.z, accel.x) * 180;
            bobVelocity += (drive - bob * 90 - bobVelocity * 7) * step;
            bob = Vector2.ClampMagnitude(bob + bobVelocity * step, 20);
        }

        void Reach(Vector3 target)
        {
            float scale = shoulderR.lossyScale.y;
            float upper = UpperArm * scale, lower = (Forearm + .06f) * scale;
            Vector3 start = shoulderR.position, delta = target - start;
            float distance = Mathf.Clamp(delta.magnitude, .04f, upper + lower - .005f);
            Vector3 direction = delta.normalized;
            float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
            float height = Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
            Vector3 pole = Vector3.ProjectOnPlane(transform.right * .8f - transform.up, direction).normalized;
            Vector3 elbow = start + direction * along + pole * height;
            shoulderR.rotation = Quaternion.FromToRotation(Vector3.down, elbow - start);
            elbowR.rotation = Quaternion.FromToRotation(Vector3.down, target - elbow);
        }

        // ---- Helpers -----------------------------------------------------------------------------------

        Vector3 Surface(Vector3 direction, out Vector3 normal)
        {
            Vector3 r = HeadRadii;
            float t = 1 / Mathf.Sqrt(Sq(direction.x / r.x) + Sq(direction.y / r.y) + Sq(direction.z / r.z));
            Vector3 p = direction * t;
            normal = new Vector3(p.x / (r.x * r.x), p.y / (r.y * r.y), p.z / (r.z * r.z)).normalized;
            return HeadCenter + p;
        }

        static Vector3 Direction(float yaw, float pitch)
        {
            float y = yaw * Mathf.Deg2Rad, p = pitch * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(y) * Mathf.Cos(p), Mathf.Sin(p), Mathf.Cos(y) * Mathf.Cos(p));
        }

        static float Sq(float v) { return v * v; }

        public static Transform Group(string name, Transform parent, Vector3 position)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            return t;
        }

        static Renderer Part(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale, Color color,
            Quaternion? rotation = null, bool shadows = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Paint(color);
            if (!shadows) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }

        // Shared across all characters so identical colors batch; kept for the whole session.
        public static Material Paint(Color color)
        {
            if (paints.TryGetValue(color, out Material material) && material) return material;
            Shader shader = Shader.Find("Elevator/SoftCharacter");
            if (!shader) shader = Shader.Find("Standard");
            material = new Material(shader) { color = color, name = "Soft paint " + ColorUtility.ToHtmlStringRGB(color), enableInstancing = true, hideFlags = HideFlags.DontSave };
            paints[color] = material;
            return material;
        }

        // ---- Shared meshes -----------------------------------------------------------------------------

        public static Mesh Sphere()
        {
            Vector2[] profile = new Vector2[17];
            for (int i = 0; i < profile.Length; i++)
            {
                float a = Mathf.PI * i / (profile.Length - 1);
                profile[i] = new Vector2(Mathf.Sin(a) * .5f, Mathf.Cos(a) * .5f);
            }
            return Lathe("Bean sphere", profile);
        }

        public static Mesh Torso() { return Lathe("Bean torso", TorsoProfile); }

        public static Mesh Band()
        {
            return Lathe("Bean belt", new[] {
                new Vector2(.19f,.035f), new Vector2(.212f,.03f), new Vector2(.216f,0), new Vector2(.212f,-.03f), new Vector2(.19f,-.035f) });
        }

        // Pivot at the top joint so neighbouring segments overlap into one smooth noodle.
        public static Mesh Capsule(float length, float top, float bottom)
        {
            const int cap = 6;
            Vector2[] profile = new Vector2[cap * 2];
            for (int i = 0; i < cap; i++)
            {
                float a = Mathf.PI * .5f * i / (cap - 1);
                profile[i] = new Vector2(Mathf.Sin(a) * top, Mathf.Cos(a) * top);
                float b = Mathf.PI * .5f + Mathf.PI * .5f * i / (cap - 1);
                profile[cap + i] = new Vector2(Mathf.Sin(b) * bottom, -length + Mathf.Cos(b) * bottom);
            }
            return Lathe("Noodle " + length + "/" + top + "/" + bottom, profile);
        }

        public static Mesh Box(Vector3 size, float radius)
        {
            string key = "Box " + size + "/" + radius;
            if (meshes.TryGetValue(key, out Mesh cached) && cached) return cached;
            Mesh mesh = TheElevator.SoftOffice.SoftArt.Rounded(size, radius);
            mesh.hideFlags = HideFlags.DontSave; meshes[key] = mesh;
            return mesh;
        }

        static Mesh Smile(float halfYaw, float curve, float tilt)
        {
            Vector3[] path = new Vector3[17];
            for (int r = 0; r < path.Length; r++)
            {
                float u = r / (path.Length - 1f) * 2 - 1;
                Vector3 d = Direction(u * halfYaw, -13 - curve * (1 - u * u) + tilt * u);
                float t = 1 / Mathf.Sqrt(Sq(d.x / HeadRadii.x) + Sq(d.y / HeadRadii.y) + Sq(d.z / HeadRadii.z));
                path[r] = HeadCenter + d * (t + .004f);
            }
            return Tube("Smile " + halfYaw + "/" + curve + "/" + tilt, path, .011f);
        }

        // An upside-down U in a feature's local plane (x right, y up): happy eyes, arched brows.
        static Mesh Arc(string key, float width, float height, float radius)
        {
            Vector3[] path = new Vector3[13];
            for (int i = 0; i < path.Length; i++) { float u = i / 12f * 2 - 1; path[i] = new Vector3(u * width * .5f, (1 - u * u) * height - height * .5f, 0); }
            return Tube(key, path, radius);
        }

        static Mesh SquareFrame()
        {
            const float w = .075f, h = .06f, corner = .025f;
            var path = new List<Vector3>();
            Vector2[] centers = { new Vector2(w - corner, h - corner), new Vector2(-w + corner, h - corner), new Vector2(-w + corner, -h + corner), new Vector2(w - corner, -h + corner) };
            for (int c = 0; c < 4; c++)
                for (int i = 0; i < 5; i++)
                {
                    float a = (c * 90 + i * 22.5f) * Mathf.Deg2Rad;
                    path.Add(new Vector3(centers[c].x + Mathf.Cos(a) * corner, centers[c].y + Mathf.Sin(a) * corner, 0));
                }
            return Tube("Square glasses", path.ToArray(), .01f, true);
        }

        // A round tube along a path; open paths pinch closed at both ends, closed loops wrap around.
        public static Mesh Tube(string key, Vector3[] path, float radius, bool closed = false)
        {
            if (meshes.TryGetValue(key, out Mesh cached) && cached) return cached;
            const int sides = 8;
            int rings = path.Length, segments = closed ? rings : rings - 1;
            var vertices = new Vector3[rings * sides];
            var triangles = new int[segments * sides * 6];
            for (int r = 0; r < rings; r++)
            {
                Vector3 next = closed ? path[(r + 1) % rings] : path[Mathf.Min(r + 1, rings - 1)];
                Vector3 previous = closed ? path[(r + rings - 1) % rings] : path[Mathf.Max(r - 1, 0)];
                Vector3 tangent = (next - previous).normalized;
                Vector3 across = Vector3.Cross(tangent, Vector3.forward).normalized;
                if (across.sqrMagnitude < .1f) across = Vector3.Cross(tangent, Vector3.up).normalized;
                Vector3 normal = Vector3.Cross(across, tangent);
                float round = closed ? radius : radius * Mathf.Pow(Mathf.Max(0, Mathf.Sin(Mathf.PI * r / (rings - 1f))), .35f);
                for (int k = 0; k < sides; k++)
                {
                    float angle = k * Mathf.PI * 2 / sides;
                    vertices[r * sides + k] = path[r] + (across * Mathf.Cos(angle) + normal * Mathf.Sin(angle)) * round;
                }
            }
            int n = 0;
            for (int r = 0; r < segments; r++)
                for (int k = 0; k < sides; k++)
                {
                    int a = r * sides + k, b = r * sides + (k + 1) % sides, c = (r + 1) % rings * sides + k, d = (r + 1) % rings * sides + (k + 1) % sides;
                    triangles[n++] = a; triangles[n++] = c; triangles[n++] = b;
                    triangles[n++] = b; triangles[n++] = c; triangles[n++] = d;
                }
            Mesh mesh = new Mesh { name = key, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            meshes[key] = mesh;
            return mesh;
        }

        // Open-bottomed cap of a sphere, from the crown down to `degrees` from the top; sits over the head.
        public static Vector2[] Dome(float radius, float height, float degrees)
        {
            Vector2[] profile = new Vector2[8];
            for (int i = 0; i < profile.Length; i++)
            {
                float a = degrees * Mathf.Deg2Rad * i / (profile.Length - 1);
                profile[i] = new Vector2(Mathf.Sin(a) * radius, Mathf.Cos(a) * height);
            }
            return profile;
        }

        public static Mesh Torus(float ring, float thickness)
        {
            Vector2[] profile = new Vector2[13];
            for (int i = 0; i < profile.Length; i++)
            {
                float a = Mathf.PI * 2 * i / (profile.Length - 1);
                profile[i] = new Vector2(ring + Mathf.Sin(a) * thickness, Mathf.Cos(a) * thickness);
            }
            return Lathe("Torus " + ring + "/" + thickness, profile);
        }

        // Revolves a (radius, height) profile, listed top to bottom, around Y with analytic smooth normals.
        // A repeated profile point makes a crease; a zero radius closes a pole.
        public static Mesh Lathe(string key, Vector2[] profile)
        {
            if (meshes.TryGetValue(key, out Mesh cached) && cached) return cached;
            const int sides = 24;
            int rows = profile.Length;
            var vertices = new Vector3[rows * (sides + 1)];
            var normals = new Vector3[vertices.Length];
            var triangles = new List<int>();
            for (int i = 0; i < rows; i++)
            {
                Vector2 before = i > 0 && profile[i - 1] != profile[i] ? profile[i - 1] : profile[i];
                Vector2 after = i < rows - 1 && profile[i + 1] != profile[i] ? profile[i + 1] : profile[i];
                Vector2 tangent = after - before;
                Vector2 outward = tangent.sqrMagnitude > 0 ? new Vector2(-tangent.y, tangent.x).normalized : Vector2.up;
                if (profile[i].x < .0001f) outward = new Vector2(0, i == 0 ? 1 : -1);
                for (int s = 0; s <= sides; s++)
                {
                    float angle = s * Mathf.PI * 2 / sides, cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                    vertices[i * (sides + 1) + s] = new Vector3(profile[i].x * cos, profile[i].y, profile[i].x * sin);
                    normals[i * (sides + 1) + s] = new Vector3(outward.x * cos, outward.y, outward.x * sin);
                }
            }
            for (int i = 0; i < rows - 1; i++)
                for (int s = 0; s < sides; s++)
                {
                    int a = i * (sides + 1) + s, b = a + 1, c = a + sides + 1, d = c + 1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(b); triangles.Add(d); triangles.Add(c);
                }
            Mesh mesh = new Mesh { name = key, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices; mesh.normals = normals; mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            meshes[key] = mesh;
            return mesh;
        }
    }
}