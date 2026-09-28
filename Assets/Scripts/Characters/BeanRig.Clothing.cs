using UnityEngine;

namespace TheElevator
{
    // Clothing pieces shared by NPC job outfits and the player wardrobe. Coordinates are pelvis or head space.
    public sealed partial class BeanRig
    {
        static readonly Color WorkTrousers = new Color(.52f,.45f,.33f);
        static readonly Color Leather = new Color(.36f,.22f,.14f);
        public static readonly Color Gold = new Color(1f,.78f,.25f);
        static readonly Color Lens = new Color(.30f,.82f,.86f);

        void DressJob(BeanLook look, ref Color forearm, ref Color legs)
        {
            switch (look.Outfit)
            {
                case BeanOutfit.Office:
                    Tie(look.Accent);
                    Part("Badge", pelvis, Sphere(), new Vector3(-.105f,.21f,.158f), new Vector3(.05f,.065f,.015f), Cream);
                    break;
                case BeanOutfit.Reception:
                    Collar(Cream);
                    Part("Name pin", pelvis, Sphere(), new Vector3(.10f,.24f,.158f), new Vector3(.07f,.035f,.015f), Gold);
                    Headset(look.Accent);
                    break;
                case BeanOutfit.Technician:
                    forearm = look.Skin; legs = WorkTrousers; // short-sleeved polo
                    Collar(Cream);
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
                    break;
                case BeanOutfit.Security:
                    Tie(Shoes);
                    Part("Shield badge", pelvis, Sphere(), new Vector3(-.10f,.26f,.158f), new Vector3(.055f,.065f,.016f), Gold);
                    ShoulderRadio();
                    SecurityCap(look.Accent);
                    break;
            }
        }

        public void Tie(Color color)
        {
            Part("Shirt front", pelvis, Sphere(), new Vector3(0,.30f,.135f), new Vector3(.12f,.18f,.05f), Cream);
            Part("Tie knot", pelvis, Sphere(), new Vector3(0,.365f,.166f), new Vector3(.04f,.036f,.03f), color);
            Part("Tie", pelvis, Capsule(.13f,.018f,.024f), new Vector3(0,.35f,.168f), new Vector3(1,1,.45f), color);
        }

        public void Collar(Color color)
        {
            for (int side = -1; side <= 1; side += 2)
                Part("Collar flap", pelvis, Sphere(), new Vector3(side * .055f,.37f,.12f), new Vector3(.10f,.055f,.04f), color)
                    .transform.localRotation = Quaternion.Euler(0, 0, side * 30);
        }

        public void ShoulderRadio()
        {
            Part("Shoulder radio", pelvis, Sphere(), new Vector3(.17f,.34f,.07f), new Vector3(.06f,.08f,.05f), Shoes);
            Part("Radio antenna", pelvis, Capsule(.06f,.008f,.008f), new Vector3(.19f,.44f,.07f), Vector3.one, Shoes)
                .transform.localRotation = Quaternion.Euler(180, 0, 0);
        }

        // ---- Headwear ----------------------------------------------------------------------------------

        public void HardHat(Color color)
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

        public void Beanie(Color color)
        {
            Transform hat = Group("Beanie", head, HeadCenter + new Vector3(0,.02f,0));
            Part("Knit dome", hat, Lathe("Beanie dome", Dome(.36f,.33f,68)), Vector3.zero, new Vector3(1,1,.91f), color);
            Part("Folded cuff", hat, Lathe("Beanie cuff", new[] {
                new Vector2(.33f,.05f), new Vector2(.358f,.045f), new Vector2(.364f,0), new Vector2(.358f,-.035f), new Vector2(.33f,-.04f) }),
                new Vector3(0,.10f,0), new Vector3(1,1,.91f), color);
            Part("Pompom", hat, Sphere(), new Vector3(0,.35f,0), Vector3.one * .12f, Cream);
        }

        public Transform Cap(Color color)
        {
            Transform hat = Group("Cap", head, HeadCenter + new Vector3(0,.03f,0));
            Part("Cap crown", hat, Lathe("Cap crown", Dome(.36f,.33f,62)), Vector3.zero, new Vector3(1,1,.93f), color);
            Part("Cap button", hat, Sphere(), new Vector3(0,.33f,0), Vector3.one * .05f, color);
            Part("Cap visor", hat, Sphere(), new Vector3(0,.15f,.36f), new Vector3(.30f,.03f,.24f), color)
                .transform.localRotation = Quaternion.Euler(8, 0, 0);
            return hat;
        }

        public void SecurityCap(Color color)
        {
            Transform cap = Cap(color);
            Part("Cap badge", cap, Sphere(), new Vector3(0,.12f,.31f), new Vector3(.06f,.06f,.02f), Gold);
        }

        Transform HeadStrap(string name, Color color)
        {
            Transform strap = Group(name, head, HeadCenter + new Vector3(0,.13f,0));
            Part(name + " strap", strap, Lathe("Head strap", new[] {
                new Vector2(.305f,.03f), new Vector2(.318f,.025f), new Vector2(.322f,0), new Vector2(.318f,-.025f), new Vector2(.305f,-.03f) }),
                Vector3.zero, new Vector3(1,1,.91f), color);
            return strap;
        }

        public void Goggles(Color strap)
        {
            HeadStrap("Goggles", strap);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 point = Surface(Direction(side * 15, 25), out Vector3 normal);
                Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                Part("Goggle rim", head, Sphere(), point + normal * .02f, new Vector3(.12f,.10f,.06f), strap, facing);
                Part("Goggle lens", head, Sphere(), point + normal * .04f, new Vector3(.09f,.075f,.03f), Lens, facing, false);
            }
        }

        public void Headlamp(Color strap)
        {
            HeadStrap("Headlamp", strap);
            Vector3 point = Surface(Direction(0, 25), out Vector3 normal);
            Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
            Part("Headlamp", head, Box(new Vector3(.09f,.06f,.05f),.018f), point + normal * .025f, Vector3.one, strap, facing);
            Part("Headlamp lens", head, Sphere(), point + normal * .052f, new Vector3(.05f,.05f,.015f), new Color(1f,.95f,.6f), facing, false);
        }

        Vector3[] OverHead(float lean, float lift)
        {
            Vector3[] band = new Vector3[17];
            for (int i = 0; i < band.Length; i++)
            {
                float angle = Mathf.Lerp(-82, 82, i / (band.Length - 1f)) * Mathf.Deg2Rad;
                band[i] = Surface(new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), lean).normalized, out Vector3 normal) + normal * lift;
            }
            return band;
        }

        public void Headset(Color color)
        {
            Part("Headset band", head, Tube("Headset band", OverHead(-.05f, .02f), .014f), Vector3.zero, Vector3.one, color);
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

        public void Earmuffs(Color band, Color muff)
        {
            Part("Earmuff band", head, Tube("Earmuff band", OverHead(.12f, .03f), .014f), Vector3.zero, Vector3.one, band);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 ear = Surface(Direction(side * 90, 0), out Vector3 normal);
                Part("Earmuff", head, Sphere(), ear + normal * .03f, new Vector3(.10f,.13f,.09f), muff, Quaternion.LookRotation(normal, Vector3.up));
            }
        }
    }
}
