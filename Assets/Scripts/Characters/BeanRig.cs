using System.Collections.Generic;
using UnityEngine;

namespace TheElevator
{
    public enum BeanOutfit { Crew, Office, Reception, Technician, Clerk, Security }

    // Flat colors only: the style reads through silhouette and hue, never texture or small hardware.
    // Primary is the main garment, Accent the tie/hat/headset, Hat picks the crew headwear (0-3).
    public struct BeanLook
    {
        public BeanOutfit Outfit;
        public Color Skin, Primary, Accent;
        public int Eyes, Mouth, Hat;
    }

    public struct BeanPose
    {
        public float Speed;
        public bool Seated, Carrying, Talking, Typing, Reaching;
        public float ArmPitch, ElbowBend; // degrees, negative lifts forward
        public Vector3 ReachTarget, LookTarget;
    }

    // Big round head sunk into a bean torso, long noodle arms, short legs, painted-on face.
    // Built from a few smooth procedural meshes shared by every character.
    public sealed class BeanRig : MonoBehaviour
    {
        public static readonly Color[] SkinColors = {
            new Color(.97f,.45f,.40f), new Color(1f,.62f,.26f), new Color(1f,.83f,.30f),
            new Color(.66f,.86f,.31f), new Color(.31f,.76f,.47f), new Color(.26f,.78f,.78f),
            new Color(.36f,.61f,.96f), new Color(.63f,.49f,.93f), new Color(.96f,.56f,.79f)
        };
        public static readonly Color Cream = new Color(.97f,.95f,.88f);
        static readonly Color FaceInk = new Color(.07f,.07f,.09f);
        public static readonly Color Glove =new Color(.25f,.27f,.31f);
        static readonly Color Trousers = new Color(.21f,.23f,.29f);
        static readonly Color WorkTrousers = new Color(.52f,.45f,.33f);
        static readonly Color Boots = new Color(.17f,.15f,.15f);
        static readonly Color Leather = new Color(.36f,.22f,.14f);
        static readonly Color Gold = new Color(1f,.78f,.25f);
        static readonly Color Lens = new Color(.30f,.82f,.86f);

        // Four distinct maintenance-crew looks, one per future co-op player.
        public static BeanLook CrewLook(int teammate)
        {
            switch (teammate % 4)
            {
                case 1: return new BeanLook { Outfit = BeanOutfit.Crew, Skin = SkinColors[0], Primary = Workshop.Mint, Accent = new Color(.90f,.27f,.27f), Hat = 1, Eyes = 1, Mouth = 3 };
                case 2: return new BeanLook { Outfit = BeanOutfit.Crew, Skin = SkinColors[3], Primary = new Color(.73f,.53f,.91f), Accent = new Color(.14f,.50f,.56f), Hat = 2, Eyes = 2, Mouth = 0 };
                case 3: return new BeanLook { Outfit = BeanOutfit.Crew, Skin = SkinColors[2], Primary = Workshop.Red, Accent = new Color(.20f,.21f,.25f), Hat = 3, Eyes = 0, Mouth = 2 };
                default: return new BeanLook { Outfit = BeanOutfit.Crew, Skin = SkinColors[6], Primary = Workshop.Yellow, Accent = new Color(.97f,.95f,.90f), Hat = 0, Eyes = 0, Mouth = 0 };
            }
        }

        const float Scale = 1.1f, HipHeight = .52f, SeatedHip = .50f;
        const float UpperArm = .24f, Forearm = .22f, Thigh = .20f, Shin = .19f;
        static readonly Vector3 HeadCenter = new Vector3(0,.27f,0), HeadRadii = new Vector3(.34f,.31f,.31f);

        static readonly Dictionary<Color,Material> paints = new Dictionary<Color,Material>();
        static readonly Dictionary<string,Mesh> meshes = new Dictionary<string,Mesh>();

        Transform pelvis, head, shoulderL, shoulderR, elbowL, elbowR, hipL, hipR, kneeL, kneeR, ankleL, ankleR;
        readonly List<Renderer> primaryParts = new List<Renderer>();
        readonly Dictionary<Renderer,bool> castsShadow = new Dictionary<Renderer,bool>();
        Transform[] eyes; Vector3 eyeSize; GameObject[] highlights;
        GameObject mouthClosed, mouthOpen;
        int identity;
        float phase, seat;
        Quaternion gaze = Quaternion.identity;
        Vector2 bob, bobVelocity;
        Vector3 lastPosition, lastVelocity;
        bool tracking;

        public Transform Head { get { return head; } }
        public Transform RightHand { get; private set; }

        public void Build(BeanLook look, int id)
        {
            identity = id;
            BeanOutfit outfit = look.Outfit;
            Transform body = Group("Bean body", transform, Vector3.zero);
            body.localScale = Vector3.one * Scale;
            pelvis = Group("Pelvis", body, new Vector3(0, HipHeight, 0));
            primaryParts.Add(Part(outfit == BeanOutfit.Crew ? "Coverall body" : "Outfit body", pelvis, Torso(), Vector3.zero, new Vector3(1,1,.82f), look.Primary));

            Transform neck = Group("Neck", pelvis, new Vector3(0,.40f,0));
            head = Group("Head", neck, Vector3.zero);
            Part("Head", head, Sphere(), HeadCenter, HeadRadii * 2, look.Skin);
            BuildFace(look);

            Color forearm = look.Primary, hand = look.Skin, legs = Trousers;
            switch (outfit)
            {
                case BeanOutfit.Crew:
                    hand = Glove; legs = look.Primary;
                    Part("Belt", pelvis, Band(), Vector3.zero, new Vector3(1,1,.82f), Cream);
                    Part("ID badge", pelvis, Sphere(), new Vector3(-.09f,.24f,.158f), new Vector3(.06f,.075f,.018f), Cream);
                    CrewHat(look.Hat, look.Accent);
                    break;
                case BeanOutfit.Office:
                    Tie(look.Accent);
                    Part("Badge", pelvis, Sphere(), new Vector3(-.105f,.21f,.158f), new Vector3(.05f,.065f,.015f), Cream);
                    break;
                case BeanOutfit.Reception:
                    for (int side = -1; side <= 1; side += 2)
                        Part("Blouse collar", pelvis, Sphere(), new Vector3(side * .055f,.37f,.12f), new Vector3(.10f,.055f,.04f), Cream)
                            .transform.localRotation = Quaternion.Euler(0, 0, side * 30);
                    Part("Name pin", pelvis, Sphere(), new Vector3(.10f,.24f,.158f), new Vector3(.07f,.035f,.015f), Gold);
                    Headset(look.Accent);
                    break;
                case BeanOutfit.Technician:
                    forearm = look.Skin; legs = WorkTrousers; // short-sleeved polo
                    for (int side = -1; side <= 1; side += 2)
                        Part("Polo collar", pelvis, Sphere(), new Vector3(side * .05f,.37f,.12f), new Vector3(.09f,.05f,.04f), Cream)
                            .transform.localRotation = Quaternion.Euler(0, 0, side * 30);
                    Part("Tool belt", pelvis, Band(), new Vector3(0,-.02f,0), new Vector3(1.04f,1.1f,.86f), Leather);
                    Part("Tool pouch", pelvis, Sphere(), new Vector3(.15f,-.07f,.10f), new Vector3(.09f,.11f,.07f), Leather);
                    Part("Tool pouch", pelvis, Sphere(), new Vector3(-.16f,-.07f,.08f), new Vector3(.08f,.10f,.07f), Leather);
                    Part("Wrench handle", pelvis, Capsule(.10f,.012f,.012f), new Vector3(-.13f,.02f,.125f), Vector3.one, look.Accent);
                    Part("Pocket pen", pelvis, Capsule(.06f,.008f,.008f), new Vector3(-.08f,.30f,.16f), Vector3.one, look.Accent);
                    break;
                case BeanOutfit.Clerk:
                    Tie(look.Accent);
                    for (int i = 0; i < 3; i++)
                        Part("Cardigan button", pelvis, Sphere(), new Vector3(.055f,.20f - i * .08f,.162f), new Vector3(.025f,.025f,.012f), Cream);
                    Glasses(Leather);
                    break;
                case BeanOutfit.Security:
                    Tie(Boots);
                    Part("Shield badge", pelvis, Sphere(), new Vector3(-.10f,.26f,.158f), new Vector3(.055f,.065f,.016f), Gold);
                    Part("Shoulder radio", pelvis, Sphere(), new Vector3(.17f,.34f,.07f), new Vector3(.06f,.08f,.05f), Boots);
                    Part("Radio antenna", pelvis, Capsule(.06f,.008f,.008f), new Vector3(.19f,.44f,.07f), Vector3.one, Boots)
                        .transform.localRotation = Quaternion.Euler(180, 0, 0);
                    Transform cap = Cap(look.Accent);
                    Part("Cap badge", cap, Sphere(), new Vector3(0,.12f,.31f), new Vector3(.06f,.06f,.02f), Gold);
                    break;
            }

            RightHand = Arm(1, look.Primary, forearm, hand, out shoulderR, out elbowR);
            Arm(-1, look.Primary, forearm, hand, out shoulderL, out elbowL);
            Leg(-1, legs, out hipL, out kneeL, out ankleL);
            Leg(1, legs, out hipR, out kneeR, out ankleR);
        }

        public void SetPrimary(Color color)
        {
            foreach (Renderer part in primaryParts) part.sharedMaterial = Paint(color);
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

        Transform Arm(int side, Color sleeve, Color forearm, Color hand, out Transform shoulder, out Transform elbow)
        {
            shoulder = Group(side < 0 ? "Left shoulder" : "Right shoulder", pelvis, new Vector3(side * .20f, .33f, 0));
            primaryParts.Add(Part("Upper sleeve", shoulder, Capsule(UpperArm,.058f,.054f), Vector3.zero, Vector3.one, sleeve));
            elbow = Group("Elbow", shoulder, new Vector3(0, -UpperArm, 0));
            Renderer lower = Part("Forearm", elbow, Capsule(Forearm,.054f,.05f), Vector3.zero, Vector3.one, forearm);
            if (forearm == sleeve) primaryParts.Add(lower);
            Transform wrist = Group("Hand", elbow, new Vector3(0, -Forearm, 0));
            Part("Mitten", wrist, Sphere(), new Vector3(0,-.05f,.005f), new Vector3(.13f,.15f,.10f), hand);
            Part("Thumb", wrist, Capsule(.05f,.026f,.022f), new Vector3(-side * .03f,-.03f,.035f), Vector3.one, hand)
                .transform.localRotation = Quaternion.Euler(-55, 0, -side * 25);
            return wrist;
        }

        void Leg(int side, Color trouser, out Transform hip, out Transform knee, out Transform ankle)
        {
            hip = Group(side < 0 ? "Left hip" : "Right hip", pelvis, new Vector3(side * .105f, -.04f, 0));
            Renderer upper = Part("Thigh", hip, Capsule(Thigh,.07f,.066f), Vector3.zero, Vector3.one, trouser);
            knee = Group("Knee", hip, new Vector3(0, -Thigh, 0));
            Renderer lower = Part("Shin", knee, Capsule(Shin,.066f,.062f), Vector3.zero, Vector3.one, trouser);
            if (trouser != Trousers && trouser != WorkTrousers) { primaryParts.Add(upper); primaryParts.Add(lower); }
            ankle = Group("Ankle", knee, new Vector3(0, -Shin, 0));
            Part("Boot", ankle, Sphere(), new Vector3(0,-.03f,.04f), new Vector3(.17f,.14f,.26f), Boots);
        }

        void BuildFace(BeanLook look)
        {
            eyeSize = look.Eyes == 1 ? new Vector3(.09f,.09f,.03f) : look.Eyes == 2 ? new Vector3(.095f,.052f,.03f) : new Vector3(.078f,.125f,.03f);
            eyes = new Transform[2]; highlights = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                Vector3 point = Surface(Direction(side * 16, 6), out Vector3 normal);
                Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                eyes[i] = Part("Eye", head, Sphere(), point + normal * .006f, eyeSize, FaceInk, facing, false).transform;
                highlights[i] = Part("Eye shine", head, Sphere(), point + normal * .02f + facing * new Vector3(eyeSize.x * .22f, eyeSize.y * .24f, 0),
                    new Vector3(.026f,.026f,.008f), Color.white, facing, false).gameObject;
                if (look.Eyes == 2) highlights[i].SetActive(false);
            }
            int mouth = look.Mouth % 4;
            if (mouth == 1) mouthClosed = Feature("Small round mouth", -15, new Vector3(.042f,.034f,.02f));
            else
            {
                float half = mouth == 2 ? 8 : mouth == 3 ? 14 : 11, curve = mouth == 2 ? .5f : mouth == 3 ? 7 : 4.5f;
                mouthClosed = Part("Smile", head, Smile(half, curve), Vector3.zero, Vector3.one, FaceInk, Quaternion.identity, false).gameObject;
            }
            mouthOpen = Feature("Talking mouth", -16, new Vector3(.06f,.048f,.02f));
            mouthOpen.SetActive(false);
        }

        GameObject Feature(string name, float pitch, Vector3 size)
        {
            Vector3 point = Surface(Direction(0, pitch), out Vector3 normal);
            return Part(name, head, Sphere(), point + normal * .004f, size, FaceInk, Quaternion.LookRotation(normal, Vector3.up), false).gameObject;
        }

        void Tie(Color color)
        {
            Part("Shirt front", pelvis, Sphere(), new Vector3(0,.30f,.135f), new Vector3(.12f,.18f,.05f), Cream);
            Part("Tie knot", pelvis, Sphere(), new Vector3(0,.365f,.166f), new Vector3(.04f,.036f,.03f), color);
            Part("Tie", pelvis, Capsule(.13f,.018f,.024f), new Vector3(0,.35f,.168f), new Vector3(1,1,.45f), color);
        }

        void CrewHat(int style, Color color)
        {
            switch (style % 4)
            {
                case 1: Beanie(color); break;
                case 2: Cap(color); break;
                case 3: Goggles(color); break;
                default: HardHat(color); break;
            }
        }

        void Beanie(Color color)
        {
            Transform hat = Group("Beanie", head, HeadCenter + new Vector3(0,.02f,0));
            Part("Knit dome", hat, Lathe("Beanie dome", Dome(.36f,.33f,68)), Vector3.zero, new Vector3(1,1,.91f), color);
            Part("Folded cuff", hat, Lathe("Beanie cuff", new[] {
                new Vector2(.33f,.05f), new Vector2(.358f,.045f), new Vector2(.364f,0), new Vector2(.358f,-.035f), new Vector2(.33f,-.04f) }),
                new Vector3(0,.10f,0), new Vector3(1,1,.91f), color);
            Part("Pompom", hat, Sphere(), new Vector3(0,.35f,0), Vector3.one * .12f, Cream);
        }

        Transform Cap(Color color)
        {
            Transform hat = Group("Cap", head, HeadCenter + new Vector3(0,.03f,0));
            Part("Cap crown", hat, Lathe("Cap crown", Dome(.36f,.33f,62)), Vector3.zero, new Vector3(1,1,.93f), color);
            Part("Cap button", hat, Sphere(), new Vector3(0,.33f,0), Vector3.one * .05f, color);
            Part("Cap visor", hat, Sphere(), new Vector3(0,.15f,.36f), new Vector3(.30f,.03f,.24f), color)
                .transform.localRotation = Quaternion.Euler(8, 0, 0);
            return hat;
        }

        void Goggles(Color strap)
        {
            Transform hat = Group("Goggles", head, HeadCenter + new Vector3(0,.13f,0));
            Part("Goggle strap", hat, Lathe("Goggle strap", new[] {
                new Vector2(.305f,.03f), new Vector2(.318f,.025f), new Vector2(.322f,0), new Vector2(.318f,-.025f), new Vector2(.305f,-.03f) }),
                Vector3.zero, new Vector3(1,1,.91f), strap);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 point = Surface(Direction(side * 15, 25), out Vector3 normal);
                Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                Part("Goggle rim", head, Sphere(), point + normal * .02f, new Vector3(.12f,.10f,.06f), strap, facing);
                Part("Goggle lens", head, Sphere(), point + normal * .04f, new Vector3(.09f,.075f,.03f), Lens, facing, false);
            }
        }

        void Headset(Color color)
        {
            Vector3[] band = new Vector3[17];
            for (int i = 0; i < band.Length; i++)
            {
                float angle = Mathf.Lerp(-82, 82, i / (band.Length - 1f)) * Mathf.Deg2Rad;
                Vector3 point = Surface(new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), -.05f).normalized, out Vector3 normal);
                band[i] = point + normal * .02f;
            }
            Part("Headset band", head, Tube("Headset band", band, .014f), Vector3.zero, Vector3.one, color);
            Vector3 cup = Vector3.zero;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 point = Surface(new Vector3(side, .02f, -.05f).normalized, out Vector3 normal);
                Part("Ear cup", head, Sphere(), point + normal * .02f, new Vector3(.09f,.11f,.06f), color, Quaternion.LookRotation(normal, Vector3.up));
                if (side < 0) cup = point + normal * .03f;
            }
            Vector3 mouth = Surface(Direction(-22, -16), out Vector3 mouthNormal) + mouthNormal * .04f;
            Vector3 start = cup + new Vector3(0,-.03f,.03f), reach = mouth - start;
            Part("Mic boom", head, Capsule(.2f,.008f,.008f), start, new Vector3(1, reach.magnitude / .2f, 1), FaceInk, Quaternion.FromToRotation(Vector3.down, reach), false);
            Part("Mic", head, Sphere(), mouth, Vector3.one * .035f, FaceInk, null, false);
        }

        void Glasses(Color frame)
        {
            Vector3[] centers = new Vector3[2]; Vector3 right = Vector3.right;
            for (int i = 0; i < 2; i++)
            {
                Vector3 point = Surface(Direction((i == 0 ? -1 : 1) * 16, 6), out Vector3 normal);
                centers[i] = point + normal * .035f;
                Part("Glasses rim", head, Torus(.072f,.01f), centers[i], Vector3.one, frame, Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(90,0,0), false);
            }
            Vector3 from = centers[0] + right * .07f, to = centers[1] - right * .07f;
            Part("Glasses bridge", head, Capsule(.05f,.008f,.008f), from, new Vector3(1, (to - from).magnitude / .05f, 1), frame, Quaternion.FromToRotation(Vector3.down, to - from), false);
        }

        void HardHat(Color color)
        {
            Transform hat = Group("Hard hat", head, HeadCenter + new Vector3(0,.13f,-.01f));
            hat.localRotation = Quaternion.Euler(-7, 0, 0);
            Part("Hat dome", hat, Lathe("Hat dome", new[] {
                new Vector2(0,.20f), new Vector2(.15f,.18f), new Vector2(.27f,.12f), new Vector2(.345f,.03f),
                new Vector2(.355f,0), new Vector2(.355f,0), new Vector2(0,0) }), Vector3.zero, Vector3.one, color);
            Part("Hat brim", hat, Lathe("Hat brim", new[] {
                new Vector2(0,.014f), new Vector2(.37f,.014f), new Vector2(.385f,0), new Vector2(.37f,-.014f), new Vector2(0,-.014f) }),
                new Vector3(0,.005f,.04f), new Vector3(.96f,1,1.1f), color);
            Part("Hat ridge", hat, Sphere(), new Vector3(0,.175f,0), new Vector3(.08f,.07f,.44f), color);
        }

        public void Animate(float dt, BeanPose pose)
        {
            if (!pelvis) return;
            float clock = Application.isPlaying ? Time.time : 0;
            float walk = Mathf.Clamp01(pose.Speed / 1.4f);
            phase += dt * (2.4f + pose.Speed * 2.3f);
            float swing = Mathf.Sin(phase) * walk, breathe = Mathf.Sin(clock * 1.7f + identity);
            seat = Mathf.MoveTowards(seat, pose.Seated ? 1 : 0, dt * 2.6f);

            float bounce = Mathf.Abs(Mathf.Cos(phase)) * walk * .03f;
            pelvis.localPosition = new Vector3(0, Mathf.Lerp(HipHeight + bounce, SeatedHip, seat) + breathe * .004f, 0);
            pelvis.localRotation = Quaternion.Euler(walk * 6 * (1 - seat) + (pose.Carrying ? 4 : 0), swing * 7, swing * 4);
            PoseLeg(hipL, kneeL, ankleL, swing);
            PoseLeg(hipR, kneeR, ankleR, -swing);

            bool talking = pose.Talking && !pose.Reaching && pose.Speed < .15f;
            float follow = 1 - Mathf.Exp(-9 * dt), drag = 1 - Mathf.Exp(-6 * dt);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform shoulder = side < 0 ? shoulderL : shoulderR, elbow = side < 0 ? elbowL : elbowR;
                Quaternion arm = pose.Carrying ? Quaternion.Euler(-78, 0, side * 3)
                    : Quaternion.Euler(pose.ArmPitch + side * swing * 30, 0, side * (7 + walk * 5));
                float bend = pose.Carrying ? -35 : pose.ElbowBend - walk * 14;
                if (pose.Typing) bend += Mathf.Sin(clock * 14 + side) * 3;
                if (talking && side > 0) { arm = Quaternion.Euler(-25, 0, 12); bend = -70 + Mathf.Sin(clock * 4) * 14; }
                shoulder.localRotation = Quaternion.Slerp(shoulder.localRotation, arm, follow);
                elbow.localRotation = Quaternion.Slerp(elbow.localRotation, Quaternion.Euler(bend, 0, 0), drag);
            }
            if (pose.Reaching) Reach(pose.ReachTarget);

            Vector3 toward = pose.LookTarget == Vector3.zero ? Vector3.forward : Quaternion.Inverse(transform.rotation) * (pose.LookTarget - head.position);
            float yaw = Mathf.Clamp(Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg, -60, 60);
            float tilt = pose.LookTarget == Vector3.zero ? 0 : Mathf.Clamp(-Mathf.Atan2(toward.y, new Vector2(toward.x, toward.z).magnitude) * Mathf.Rad2Deg, -25, 30);
            if (pose.Typing) tilt = 14;
            gaze = Quaternion.Slerp(gaze, Quaternion.Euler(tilt, yaw, 0), 1 - Mathf.Exp(-3.5f * dt));
            Bobble(dt);
            head.localRotation = gaze * Quaternion.Euler(bob.x + Mathf.Sin(phase * 2) * walk * 2.5f, 0, bob.y + breathe * .6f);

            float blink = (clock + identity * .71f) % 4.3f < .11f ? .12f : 1;
            for (int i = 0; i < 2; i++) eyes[i].localScale = new Vector3(eyeSize.x, eyeSize.y * blink, eyeSize.z);
            bool open = talking && Mathf.Sin(clock * 17 + identity) > 0;
            mouthClosed.SetActive(!open); mouthOpen.SetActive(open);
        }

        void PoseLeg(Transform hip, Transform knee, Transform ankle, float swing)
        {
            hip.localRotation = Quaternion.Euler(Mathf.Lerp(swing * 34, -86, seat), 0, 0);
            knee.localRotation = Quaternion.Euler(Mathf.Lerp(Mathf.Max(0, -swing) * 48, 86, seat), 0, 0);
            ankle.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
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

        static Transform Group(string name, Transform parent, Vector3 position)
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

        static Mesh Sphere()
        {
            Vector2[] profile = new Vector2[17];
            for (int i = 0; i < profile.Length; i++)
            {
                float a = Mathf.PI * i / (profile.Length - 1);
                profile[i] = new Vector2(Mathf.Sin(a) * .5f, Mathf.Cos(a) * .5f);
            }
            return Lathe("Bean sphere", profile);
        }

        static Mesh Torso()
        {
            return Lathe("Bean torso", new[] {
                new Vector2(0,.44f), new Vector2(.10f,.43f), new Vector2(.16f,.39f), new Vector2(.19f,.30f),
                new Vector2(.20f,.16f), new Vector2(.20f,.02f), new Vector2(.18f,-.07f), new Vector2(.12f,-.12f), new Vector2(0,-.13f) });
        }

        static Mesh Band()
        {
            return Lathe("Bean belt", new[] {
                new Vector2(.19f,.035f), new Vector2(.212f,.03f), new Vector2(.216f,0), new Vector2(.212f,-.03f), new Vector2(.19f,-.035f) });
        }

        // Pivot at the top joint so neighbouring segments overlap into one smooth noodle.
        static Mesh Capsule(float length, float top, float bottom)
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

        static Mesh Smile(float halfYaw, float curve)
        {
            Vector3[] path = new Vector3[17];
            for (int r = 0; r < path.Length; r++)
            {
                float u = r / (path.Length - 1f) * 2 - 1;
                Vector3 d = Direction(u * halfYaw, -13 - curve * (1 - u * u));
                float t = 1 / Mathf.Sqrt(Sq(d.x / HeadRadii.x) + Sq(d.y / HeadRadii.y) + Sq(d.z / HeadRadii.z));
                path[r] = HeadCenter + d * (t + .004f);
            }
            return Tube("Smile " + halfYaw + "/" + curve, path, .011f);
        }

        // A round tube along a path, pinched closed at both ends.
        static Mesh Tube(string key, Vector3[] path, float radius)
        {
            if (meshes.TryGetValue(key, out Mesh cached) && cached) return cached;
            const int sides = 8;
            int rings = path.Length;
            var vertices = new Vector3[rings * sides];
            var triangles = new int[(rings - 1) * sides * 6];
            for (int r = 0; r < rings; r++)
            {
                Vector3 tangent = (path[Mathf.Min(r + 1, rings - 1)] - path[Mathf.Max(r - 1, 0)]).normalized;
                Vector3 across = Vector3.Cross(tangent, Vector3.forward).normalized;
                Vector3 normal = Vector3.Cross(across, tangent);
                float round = radius * Mathf.Pow(Mathf.Max(0, Mathf.Sin(Mathf.PI * r / (rings - 1f))), .35f);
                for (int k = 0; k < sides; k++)
                {
                    float angle = k * Mathf.PI * 2 / sides;
                    vertices[r * sides + k] = path[r] + (across * Mathf.Cos(angle) + normal * Mathf.Sin(angle)) * round;
                }
            }
            int n = 0;
            for (int r = 0; r < rings - 1; r++)
                for (int k = 0; k < sides; k++)
                {
                    int a = r * sides + k, b = r * sides + (k + 1) % sides, c = a + sides, d = b + sides;
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
        static Vector2[] Dome(float radius, float height, float degrees)
        {
            Vector2[] profile = new Vector2[8];
            for (int i = 0; i < profile.Length; i++)
            {
                float a = degrees * Mathf.Deg2Rad * i / (profile.Length - 1);
                profile[i] = new Vector2(Mathf.Sin(a) * radius, Mathf.Cos(a) * height);
            }
            return profile;
        }

        static Mesh Torus(float ring, float thickness)
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
        static Mesh Lathe(string key, Vector2[] profile)
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