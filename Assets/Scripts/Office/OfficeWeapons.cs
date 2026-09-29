using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.Office
{
    public enum Arms { None, Rifle, Bazooka }

    // Two employees on every floor keep an assault rifle in their desk and one keeps a bazooka. In an employee's
    // hands the rifle does 10 a round and misses a lot, so the player can get away; a rocket's 10 m blast does up
    // to 90 and hurts anyone caught in it, employees included. Knock an armed employee out and they drop it: in a
    // player's hands every rifle round and every rocket blast is lethal.
    public static class OfficeWeapons
    {
        public const float RifleDamage = 10, PunchDamage = 5, BlastRadius = 10, BlastDamage = 90, RocketSpeed = 15;
        // Weapons in a player's hands: one shot, one knockout.
        public const float PlayerRifleDamage = 1000, PlayerBlastDamage = 1000;
        public const int RifleMagazine = 30, BazookaRockets = 3;
        const int Mask = ~((1 << 2) | (1 << 8));
        // Counters for checks and tuning.
        public static int Shots, Explosions, Swings;
        static readonly Dictionary<Color, Material> glows = new Dictionary<Color, Material>();
        static AudioClip rifleShot, rocketLaunch, explosion, punch;

        // ---- Models: local +Z is the muzzle direction, the grip sits at the origin ----
        public static Transform Build(Arms arms, OfficeArt a, Transform parent)
        {
            Transform root = a.Group(parent, arms == Arms.Rifle ? "Assault rifle" : "Bazooka", Vector3.zero);
            if (arms == Arms.Rifle)
            {
                a.Box(root, "Rifle stock", new Vector3(0, -.01f, -.26f), new Vector3(.045f, .09f, .22f), a.Wood);
                a.Box(root, "Rifle receiver", new Vector3(0, .02f, .06f), new Vector3(.055f, .08f, .36f), a.Dark);
                a.Box(root, "Rifle handguard", new Vector3(0, .015f, .32f), new Vector3(.05f, .06f, .18f), a.Wood);
                GameObject barrel = a.Round(root, "Rifle barrel", new Vector3(0, .03f, .5f), new Vector3(.022f, .12f, .022f), a.Dark);
                barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
                GameObject magazine = a.Box(root, "Curved magazine", new Vector3(0, -.09f, .12f), new Vector3(.035f, .16f, .06f), a.Dark);
                magazine.transform.localRotation = Quaternion.Euler(-18, 0, 0);
                a.Box(root, "Pistol grip", new Vector3(0, -.06f, -.04f), new Vector3(.035f, .09f, .045f), a.Wood).transform.localRotation = Quaternion.Euler(-15, 0, 0);
                a.Box(root, "Front sight", new Vector3(0, .07f, .56f), new Vector3(.01f, .04f, .01f), a.Dark);
                a.Group(root, "Muzzle", new Vector3(0, .03f, .63f));
            }
            else
            {
                GameObject tube = a.Round(root, "Launcher tube", new Vector3(0, .08f, .05f), new Vector3(.12f, .55f, .12f), a.W.Soft.Leaf);
                tube.transform.localRotation = Quaternion.Euler(90, 0, 0);
                GameObject bell = a.Round(root, "Rear bell", new Vector3(0, .08f, -.52f), new Vector3(.16f, .04f, .16f), a.Dark);
                bell.transform.localRotation = Quaternion.Euler(90, 0, 0);
                a.Box(root, "Launcher grip", new Vector3(0, -.03f, -.02f), new Vector3(.035f, .12f, .05f), a.Dark);
                a.Box(root, "Launcher sight", new Vector3(-.07f, .15f, .12f), new Vector3(.02f, .06f, .08f), a.Dark);
                a.Round(root, "Rocket nose", new Vector3(0, .08f, .62f), new Vector3(.1f, .1f, .14f), a.Red, PrimitiveType.Sphere);
                a.Group(root, "Muzzle", new Vector3(0, .08f, .66f));
            }
            return root;
        }

        public static Transform Muzzle(Transform weapon) { return weapon ? weapon.Find("Muzzle") : null; }

        // ---- Rifle: one round, with a wide random spread that grows with distance ----
        public static bool FireRifle(Vector3 muzzle, Vector3 target, WorkerController player, System.Random random)
        {
            bool struck = RifleRound(muzzle, target, player.transform.position, player.MotionSpeed, random, out Vector3 direction, out float along) && !player.Down;
            Shots++;
            Flash(muzzle, .14f, .06f, new Color(1f, .85f, .4f));
            Tracer(muzzle, muzzle + direction * along, new Color(1f, .9f, .5f));
            Play(Clip(ref rifleShot, 0), muzzle, .9f);
            if (struck) player.Damage(RifleDamage, muzzle, 3);
            return struck;
        }

        // Where one round goes: a random direction inside a cone that widens with distance (and when the target
        // is moving). Returns whether it hits the body standing at feet before any wall.
        public static bool RifleRound(Vector3 muzzle, Vector3 target, Vector3 feet, float targetSpeed, System.Random random, out Vector3 direction, out float along)
        {
            float distance = Vector3.Distance(muzzle, target);
            float spread = 9 + distance * .55f + (targetSpeed > 1 ? 4 : 0);
            direction = Scatter((target - muzzle).normalized, spread, random);
            float wall = Physics.Raycast(muzzle, direction, out RaycastHit hit, 60, Mask, QueryTriggerInteraction.Ignore) ? hit.distance : 60;
            along = wall;
            if (RayHitsBody(muzzle, direction, feet, out float t) && t < wall) { along = t; return true; }
            return false;
        }

        // ---- A player's rifle: along the crosshair with a hair of spread; anything it hits is out ----
        public static void FirePlayerRifle(OfficeFloor office, WorkerController shooter, Vector3 muzzle, System.Random random)
        {
            Transform view = shooter.View.transform;
            Vector3 direction = Scatter(view.forward, .6f, random);
            float along = 80;
            Object struck = null;
            // Rounds pass through chair frames; anything else solid stops them.
            if (Physics.Raycast(view.position, direction, out RaycastHit hit, 80, ~((1 << 2) | (1 << 9)), QueryTriggerInteraction.Ignore))
            {
                along = hit.distance;
                OfficeEmployee employee = hit.collider.GetComponentInParent<OfficeEmployee>();
                if (employee && !employee.Dead) struck = employee;
            }
            // Teammates have no collider in the way of rays (their own layer), so test them as bodies.
            if (office && office.Game)
                foreach (WorkerController member in office.Game.Crew)
                    if (member != shooter && !member.Down && RayHitsBody(view.position, direction, member.transform.position, out float t) && t < along) { along = t; struck = member; }
            Shots++;
            Flash(muzzle, .16f, .06f, new Color(1f, .85f, .4f));
            Tracer(muzzle, view.position + direction * along, new Color(1f, .9f, .5f));
            Play(Clip(ref rifleShot, 0), muzzle, 1);
            if (struck is OfficeEmployee e) e.Damage(PlayerRifleDamage, view.position, 3);
            else if (struck is WorkerController w) w.Damage(PlayerRifleDamage, view.position, 3);
        }

        // ---- Bazooka ----
        public static void FireRocket(OfficeEmployee shooter, Vector3 muzzle, Vector3 target, System.Random random)
        { FireRocket(shooter.Office, shooter, null, muzzle, target, 3, BlastDamage, random); }

        public static void FireRocket(OfficeFloor office, OfficeEmployee npc, WorkerController player, Vector3 muzzle, Vector3 target, float spread, float damage, System.Random random)
        {
            Vector3 direction = Scatter((target - muzzle).normalized, spread, random);
            GameObject rocket = new GameObject("Rocket");
            rocket.transform.SetPositionAndRotation(muzzle, Quaternion.LookRotation(direction));
            OfficeArt a = office.Kit.A;
            GameObject body = a.Round(rocket.transform, "Rocket body", Vector3.zero, new Vector3(.09f, .18f, .09f), a.W.Soft.Leaf);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            a.Round(rocket.transform, "Rocket nose", new Vector3(0, 0, .2f), new Vector3(.09f, .09f, .12f), a.Red, PrimitiveType.Sphere);
            Glow(PrimitiveType.Sphere, rocket.transform, new Vector3(0, 0, -.24f), Vector3.one * .12f, new Color(1f, .6f, .2f));
            rocket.AddComponent<OfficeRocket>().Launch(office, npc, player, direction * RocketSpeed, damage);
            Flash(muzzle, .3f, .1f, new Color(1f, .7f, .3f));
            Play(Clip(ref rocketLaunch, 1), muzzle, 1);
        }

        // Everyone within the blast radius with nothing solid in between is hurt, most at the centre.
        public static void Explode(OfficeFloor office, Vector3 centre) { Explode(office, centre, BlastDamage); }
        public static void Explode(OfficeFloor office, Vector3 centre, float damage)
        {
            Explosions++;
            Flash(centre, BlastRadius * .4f, .55f, new Color(1f, .55f, .2f));
            Flash(centre + Vector3.up * .8f, BlastRadius * .28f, 1.1f, new Color(.55f, .55f, .55f));
            GameObject light = new GameObject("Blast light");
            light.transform.position = centre + Vector3.up;
            Light lamp = light.AddComponent<Light>(); lamp.type = LightType.Point; lamp.range = BlastRadius * 1.4f; lamp.intensity = 6; lamp.color = new Color(1f, .7f, .4f);
            Object.Destroy(light, .25f);
            Play(Clip(ref explosion, 2), centre, 1);
            if (!office || !office.Game) return;
            foreach (WorkerController member in office.Game.Crew)
                if (!member.Down && Exposed(centre, member.transform.position + Vector3.up, out float d))
                    member.Damage(damage * (1 - d / BlastRadius), centre, 14 * (1 - d / BlastRadius));
            foreach (OfficeEmployee employee in office.Employees)
                if (employee && !employee.Dead && Exposed(centre, employee.transform.position + Vector3.up, out float e))
                    employee.Blasted(damage * (1 - e / BlastRadius), centre, 10 * (1 - e / BlastRadius));
        }

        static bool Exposed(Vector3 centre, Vector3 body, out float distance)
        {
            distance = Vector3.Distance(centre, body);
            return distance < BlastRadius && !Physics.Linecast(centre + Vector3.up * .3f, body, Mask, QueryTriggerInteraction.Ignore);
        }

        public static void Punch(Vector3 at) { Swings++; Play(Clip(ref punch, 3), at, .8f); }

        // ---- Hit testing: a ray against a standing body (a capsule from the knees to the top of the head) ----
        public static bool RayHitsBody(Vector3 origin, Vector3 direction, Vector3 feet, out float along)
        {
            Vector3 a = feet + Vector3.up * .35f, b = feet + Vector3.up * 1.55f;
            ClosestPoints(origin, origin + direction * 60, a, b, out float s, out float t);
            Vector3 onRay = origin + direction * 60 * s, onBody = a + (b - a) * t;
            along = 60 * s;
            return (onRay - onBody).sqrMagnitude < .38f * .38f;
        }

        public static bool InBody(Vector3 point, Vector3 feet, float padding)
        {
            Vector3 a = feet + Vector3.up * .35f, b = feet + Vector3.up * 1.55f;
            float t = Mathf.Clamp01(Vector3.Dot(point - a, b - a) / (b - a).sqrMagnitude);
            return (point - (a + (b - a) * t)).sqrMagnitude < (.34f + padding) * (.34f + padding);
        }

        static void ClosestPoints(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out float s, out float t)
        {
            Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
            float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r), c = Vector3.Dot(d1, r), b = Vector3.Dot(d1, d2);
            float denominator = a * e - b * b;
            s = denominator > 1e-6f ? Mathf.Clamp01((b * f - c * e) / denominator) : 0;
            t = (b * s + f) / e;
            if (t < 0) { t = 0; s = Mathf.Clamp01(-c / a); }
            else if (t > 1) { t = 1; s = Mathf.Clamp01((b - c) / a); }
        }

        static Vector3 Scatter(Vector3 direction, float degrees, System.Random random)
        {
            float angle = (float)System.Math.Sqrt(random.NextDouble()) * degrees, around = (float)random.NextDouble() * 360;
            Vector3 side = Vector3.Cross(direction, Mathf.Abs(direction.y) > .95f ? Vector3.right : Vector3.up).normalized;
            return Quaternion.AngleAxis(around, direction) * Quaternion.AngleAxis(angle, side) * direction;
        }

        // ---- Effects ----
        public static void Flash(Vector3 at, float size, float life, Color color)
        {
            Transform fx = Glow(PrimitiveType.Sphere, null, at, Vector3.zero, color);
            fx.gameObject.AddComponent<OfficeFx>().Play(Vector3.one * size, life);
        }

        public static void Tracer(Vector3 from, Vector3 to, Color color)
        {
            Vector3 middle = (from + to) * .5f;
            Transform fx = Glow(PrimitiveType.Cube, null, middle, new Vector3(.015f, .015f, Vector3.Distance(from, to)), color);
            if (to != from) fx.rotation = Quaternion.LookRotation(to - from);
            fx.gameObject.AddComponent<OfficeFx>().Fade(.06f);
        }

        static Transform Glow(PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(shape);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Weapon effect";
            if (parent) { go.transform.SetParent(parent, false); go.transform.localPosition = position; }
            else go.transform.position = position;
            go.transform.localScale = scale;
            if (!glows.TryGetValue(color, out Material material) || !material)
            {
                material = new Material(Shader.Find("Elevator/Foam")) { name = "Weapon glow", color = color };
                material.SetFloat("_Glow", .9f);
                glows[color] = material;
            }
            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        // ---- Sounds: synthesized once ----
        static void Play(AudioClip clip, Vector3 at, float volume) { if (Application.isPlaying) AudioSource.PlayClipAtPoint(clip, at, volume); }

        static AudioClip Clip(ref AudioClip cache, int kind)
        {
            if (cache) return cache;
            const int rate = 22050;
            float seconds = kind == 0 ? .22f : kind == 1 ? .7f : kind == 2 ? 1.6f : .12f;
            float[] data = new float[Mathf.CeilToInt(seconds * rate)];
            System.Random random = new System.Random(911 + kind);
            float low = 0, body = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, n = (float)random.NextDouble() * 2 - 1;
                low += (n - low) * (kind == 2 ? .04f : kind == 1 ? .2f : .35f);
                if (kind == 0) data[i] = (n * .5f + low * .8f) * Mathf.Exp(-t * 28) + Mathf.Sin(t * 2 * Mathf.PI * 90) * Mathf.Exp(-t * 20) * .5f;
                else if (kind == 1) data[i] = low * Mathf.Min(1, t * 12) * Mathf.Exp(-t * 3.2f) * .9f;
                else if (kind == 2) { body += (low - body) * .1f; data[i] = (body * 3 + low * .8f) * Mathf.Exp(-t * 2.4f) + Mathf.Sin(t * 2 * Mathf.PI * 45) * Mathf.Exp(-t * 4) * .6f; }
                else data[i] = (Mathf.Sin(t * 2 * Mathf.PI * 120) * .7f + low * .5f) * Mathf.Exp(-t * 40);
                data[i] = Mathf.Clamp(data[i], -1, 1);
            }
            cache = AudioClip.Create(kind == 0 ? "Rifle shot" : kind == 1 ? "Rocket launch" : kind == 2 ? "Explosion" : "Punch", data.Length, 1, rate, false);
            cache.SetData(data, 0);
            return cache;
        }
    }

    // A short-lived glowing shape: grows to full size, then shrinks away (fireballs, muzzle flashes), or just vanishes (tracers).
    public sealed class OfficeFx : MonoBehaviour
    {
        Vector3 full;
        float life, age;
        bool grow;
        public void Play(Vector3 size, float seconds) { full = size; life = seconds; grow = true; }
        public void Fade(float seconds) { life = seconds; }
        void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { Destroy(gameObject); return; }
            if (!grow) return;
            float t = age / life;
            transform.localScale = full * (t < .25f ? Mathf.SmoothStep(0, 1, t / .25f) : Mathf.SmoothStep(1, 0, (t - .25f) / .75f));
        }
    }

    // A rocket in flight: explodes on the first wall, floor, person or after a few seconds. The one who fired it
    // is safe from a direct hit for the first moment (not from the blast).
    public sealed class OfficeRocket : MonoBehaviour
    {
        OfficeFloor office;
        OfficeEmployee npc;
        WorkerController player;
        Vector3 velocity;
        float age, damage;
        const int Mask = ~((1 << 2) | (1 << 8));
        public void Launch(OfficeFloor floor, OfficeEmployee fromEmployee, WorkerController fromPlayer, Vector3 v, float blast)
        { office = floor; npc = fromEmployee; player = fromPlayer; velocity = v; damage = blast; }
        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            Vector3 step = velocity * dt, next = transform.position + step;
            bool boom = age > 4;
            Vector3 at = next;
            if (!boom && Physics.Raycast(transform.position, velocity.normalized, out RaycastHit hit, step.magnitude + .1f, Mask, QueryTriggerInteraction.Ignore)) { boom = true; at = hit.point; }
            if (!boom && office && office.Game)
                foreach (WorkerController member in office.Game.Crew)
                    if (!member.Down && (member != player || age > .4f) && OfficeWeapons.InBody(next, member.transform.position, .15f)) { boom = true; break; }
            if (!boom && office)
                foreach (OfficeEmployee employee in office.Employees)
                    if (employee && !employee.Dead && (employee != npc || age > .4f) && OfficeWeapons.InBody(next, employee.transform.position, .1f)) { boom = true; break; }
            if (boom) { OfficeWeapons.Explode(office, at, damage); Destroy(gameObject); return; }
            transform.position = next;
        }
    }
}