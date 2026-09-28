using System;
using UnityEngine;

namespace TheElevator
{
    public enum WardrobeSlot { Skin, Head, Eyes, Mouth, Brows, Glasses, Shirt, Jacket, Pants, Shoes, Gear }

    // The player's chosen cosmetics, one index per slot. Saved locally between sessions.
    [Serializable]
    public sealed class AvatarLoadout
    {
        const string Key = "TheElevator.Avatar";
        public int Skin = 6, Head, Eyes = 1, Mouth, Brows, Glasses, Shirt, Jacket, Pants, Shoes, Gear;

        public int Get(WardrobeSlot slot)
        {
            switch (slot)
            {
                case WardrobeSlot.Skin: return Skin; case WardrobeSlot.Head: return Head; case WardrobeSlot.Eyes: return Eyes;
                case WardrobeSlot.Mouth: return Mouth; case WardrobeSlot.Brows: return Brows; case WardrobeSlot.Glasses: return Glasses;
                case WardrobeSlot.Shirt: return Shirt; case WardrobeSlot.Jacket: return Jacket; case WardrobeSlot.Pants: return Pants;
                case WardrobeSlot.Shoes: return Shoes; default: return Gear;
            }
        }

        public void Set(WardrobeSlot slot, int value)
        {
            value = (value % AvatarWardrobe.Count(slot) + AvatarWardrobe.Count(slot)) % AvatarWardrobe.Count(slot);
            switch (slot)
            {
                case WardrobeSlot.Skin: Skin = value; break; case WardrobeSlot.Head: Head = value; break; case WardrobeSlot.Eyes: Eyes = value; break;
                case WardrobeSlot.Mouth: Mouth = value; break; case WardrobeSlot.Brows: Brows = value; break; case WardrobeSlot.Glasses: Glasses = value; break;
                case WardrobeSlot.Shirt: Shirt = value; break; case WardrobeSlot.Jacket: Jacket = value; break; case WardrobeSlot.Pants: Pants = value; break;
                case WardrobeSlot.Shoes: Shoes = value; break; default: Gear = value; break;
            }
        }

        public AvatarLoadout Clone() { return (AvatarLoadout)MemberwiseClone(); }

        public static AvatarLoadout Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                if (json.Length > 0)
                {
                    AvatarLoadout saved = JsonUtility.FromJson<AvatarLoadout>(json);
                    foreach (WardrobeSlot slot in Enum.GetValues(typeof(WardrobeSlot))) saved.Set(slot, saved.Get(slot));
                    return saved;
                }
            }
            catch (ArgumentException) { }
            return new AvatarLoadout();
        }

        public void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
    }

    // One cosmetic choice. Price is recorded for the future lobby shop; nothing is locked yet.
    public sealed class WardrobeItem
    {
        public readonly string Name;
        public readonly int Price;
        readonly Action<BeanRig> wear;
        public WardrobeItem(string name, int price, Action<BeanRig> wear = null) { Name = name; Price = price; this.wear = wear; }
        public void Wear(BeanRig rig) { if (wear != null) wear(rig); }
    }

    // Every player starts as the plain avatar (plain tee, trousers, shoes, bare hands) and layers these items on.
    public static class AvatarWardrobe
    {
        static readonly Color PlainTee = new Color(.60f,.66f,.74f);
        static readonly Color Reflective = new Color(.88f,.92f,.96f);
        static readonly Color Strap = new Color(.18f,.19f,.23f);
        static readonly Color GloveGrey = new Color(.25f,.27f,.31f);
        static readonly Color Leather = new Color(.55f,.34f,.2f);
        static readonly Color Sole = new Color(.10f,.09f,.09f);

        static readonly string[] SkinNames = { "Coral", "Tangerine", "Sunflower", "Lime", "Clover", "Lagoon", "Sky", "Lavender", "Bubblegum" };
        static readonly string[] EyeNames = { "Tall", "Round", "Sleepy", "Happy", "Sparkly", "Tiny" };
        static readonly string[] MouthNames = { "Smile", "Small O", "Flat", "Grin", "Laugh", "Smirk" };
        static readonly string[] BrowNames = { "None", "Neutral", "Arched", "Worried", "Angry", "Bushy" };
        static readonly string[] GlassesNames = { "None", "Round", "Square", "Sunglasses", "Big Round", "Monocle" };

        public static readonly WardrobeItem[] Heads = {
            new WardrobeItem("None", 0),
            new WardrobeItem("Hard Hat", 60, r => r.HardHat(new Color(.97f,.95f,.90f))),
            new WardrobeItem("Beanie", 40, r => r.Beanie(new Color(.90f,.27f,.27f))),
            new WardrobeItem("Ball Cap", 40, r => r.Cap(new Color(.14f,.50f,.56f))),
            new WardrobeItem("Goggles", 70, r => r.Goggles(Strap)),
            new WardrobeItem("Earmuffs", 50, r => r.Earmuffs(new Color(.85f,.22f,.28f), BeanRig.Cream)),
            new WardrobeItem("Headset", 80, r => r.Headset(Strap)),
            new WardrobeItem("Security Cap", 90, r => r.SecurityCap(new Color(.11f,.14f,.24f))),
            new WardrobeItem("Headlamp", 75, r => r.Headlamp(Strap)),
        };

        public static readonly WardrobeItem[] Shirts = {
            new WardrobeItem("Plain Tee", 0, r => Top(r, PlainTee, false)),
            new WardrobeItem("Polo", 45, r => { Top(r, new Color(.95f,.55f,.20f), false); r.Collar(BeanRig.Cream); }),
            new WardrobeItem("Shirt and Tie", 70, r => { Top(r, BeanRig.Cream, true); r.Tie(new Color(.20f,.36f,.75f)); }),
            new WardrobeItem("Hoodie", 60, r => { Color c = new Color(.40f,.52f,.80f); Top(r, c, true); Hood(r, c); Drawstrings(r, .19f); Cuffs(r, c); }),
            new WardrobeItem("Striped Tee", 35, r => { Top(r, new Color(.90f,.30f,.30f), false); foreach (float y in new[] { .06f, .17f, .28f }) r.Add(r.Pelvis, "Stripe", BeanRig.Torus(BeanRig.TorsoRadius(y) + .002f, .016f), new Vector3(0,y,0), new Vector3(1,1,.82f), BeanRig.Cream); }),
            new WardrobeItem("Work Shirt", 55, r => { Color c = Workshop.Mint, dark = Darker(c); Top(r, c, true); r.Collar(dark); Pocket(r, dark); Zipper(r, .004f); NameTag(r); }),
            new WardrobeItem("Coveralls", 65, r => { Color c = Workshop.Yellow, dark = Darker(c); Top(r, c, true); r.Collar(dark); Pocket(r, dark);
                r.Add(r.Pelvis, "Belt", BeanRig.Band(), Vector3.zero, new Vector3(1,1,.82f), BeanRig.Cream);
                r.Add(r.Pelvis, "ID badge", BeanRig.Sphere(), new Vector3(-.09f,.24f,.158f), new Vector3(.06f,.075f,.018f), BeanRig.Cream); }),
        };

        public static readonly WardrobeItem[] Jackets = {
            new WardrobeItem("None", 0),
            new WardrobeItem("Puffer Vest", 90, r => Puffer(r, new Color(1f,.55f,.2f))),
            new WardrobeItem("Hi-Vis Vest", 60, r => { Color shirt = r.ColorOf("Outfit body"); Shell(r, new Color(.85f,1f,.22f), false); FrontStrip(r, shirt);
                foreach (float y in new[] { .08f, .22f }) r.Add(r.Pelvis, "Reflective band", BeanRig.Torus(BeanRig.TorsoRadius(y) * 1.07f + .004f, .016f), new Vector3(0,y,0), new Vector3(1,1,.82f), Reflective); }),
            new WardrobeItem("Blazer", 120, r => { Color shirt = r.ColorOf("Outfit body"), c = new Color(.18f,.24f,.42f); Shell(r, c, true); FrontStrip(r, shirt);
                for (int side = -1; side <= 1; side += 2) r.Add(r.Pelvis, "Lapel", BeanRig.Sphere(), new Vector3(side * .065f,.29f,.17f), new Vector3(.06f,.20f,.03f), Darker(c), Quaternion.Euler(0, 0, side * -18));
                r.Add(r.Pelvis, "Blazer button", BeanRig.Sphere(), new Vector3(.045f,.12f,BeanRig.TorsoRadius(.12f) * .88f + .01f), Vector3.one * .025f, BeanRig.Gold); }),
            new WardrobeItem("Cardigan", 80, r => { Color shirt = r.ColorOf("Outfit body"), c = new Color(.80f,.60f,.22f); Shell(r, c, true); FrontStrip(r, shirt);
                for (int i = 0; i < 3; i++) { float y = .22f - i * .07f; r.Add(r.Pelvis, "Cardigan button", BeanRig.Sphere(), new Vector3(.05f,y,BeanRig.TorsoRadius(y) * .88f + .01f), new Vector3(.025f,.025f,.012f), BeanRig.Cream); } }),
            new WardrobeItem("Rain Jacket", 100, r => { Color c = new Color(1f,.84f,.2f); Shell(r, c, true); Hood(r, c); Zipper(r, .018f);
                for (int i = 0; i < 3; i++) { float y = .28f - i * .1f; r.Add(r.Pelvis, "Toggle", BeanRig.Capsule(.04f,.01f,.01f), new Vector3(.03f,y,BeanRig.TorsoRadius(y) * .88f + .012f), Vector3.one, Darker(c), Quaternion.Euler(0, 0, 90)); } }),
        };

        public static readonly WardrobeItem[] Pants = {
            new WardrobeItem("Plain Trousers", 0, r => Legs(r, BeanRig.Trousers, BeanRig.Trousers)),
            new WardrobeItem("Jeans", 50, r => { Color c = new Color(.25f,.38f,.62f); Legs(r, c, c);
                r.Add(r.Pelvis, "Belt", BeanRig.Band(), Vector3.zero, new Vector3(1.01f,1,.83f), new Color(.36f,.22f,.14f));
                r.Add(r.Pelvis, "Belt buckle", BeanRig.Box(new Vector3(.05f,.04f,.015f),.006f), new Vector3(0,0,.18f), Vector3.one, BeanRig.Gold); }),
            new WardrobeItem("Cargo Pants", 60, r => { Color c = new Color(.45f,.48f,.33f); Legs(r, c, c);
                for (int side = -1; side <= 1; side += 2) { r.Add(r.Hip(side), "Cargo pocket", BeanRig.Box(new Vector3(.03f,.085f,.075f),.012f), new Vector3(side * .07f,-.11f,0), Vector3.one, Darker(c));
                    r.Add(r.Hip(side), "Pocket flap", BeanRig.Box(new Vector3(.034f,.022f,.08f),.008f), new Vector3(side * .072f,-.065f,0), Vector3.one, Darker(Darker(c))); } }),
            new WardrobeItem("Shorts", 35, r => { Color c = new Color(.72f,.62f,.44f); Legs(r, c, r.Look.Skin);
                for (int side = -1; side <= 1; side += 2) r.Add(r.Knee(side), "Sock", BeanRig.Torus(.064f,.016f), new Vector3(0,-.15f,0), Vector3.one, BeanRig.Cream); }),
            new WardrobeItem("Work Pants", 55, r => { Color c = new Color(.26f,.30f,.44f); Legs(r, c, c);
                for (int side = -1; side <= 1; side += 2) { Transform knee = r.Knee(side);
                    r.Add(knee, "Reflective band", BeanRig.Torus(.068f,.011f), new Vector3(0,-.06f,0), Vector3.one, Reflective);
                    r.Add(knee, "Reflective band", BeanRig.Torus(.066f,.011f), new Vector3(0,-.11f,0), Vector3.one, Reflective);
                    r.Add(knee, "Knee pad", BeanRig.Sphere(), new Vector3(0,.005f,.05f), new Vector3(.12f,.12f,.06f), new Color(.23f,.24f,.28f)); } }),
            new WardrobeItem("Track Pants", 45, r => { Color c = new Color(.85f,.25f,.30f); Legs(r, c, c);
                for (int side = -1; side <= 1; side += 2) { r.Add(r.Hip(side), "Side stripe", BeanRig.Capsule(.2f,.013f,.013f), new Vector3(side * .066f,0,0), Vector3.one, BeanRig.Cream);
                    r.Add(r.Knee(side), "Side stripe", BeanRig.Capsule(.19f,.013f,.013f), new Vector3(side * .062f,0,0), Vector3.one, BeanRig.Cream); } }),
            new WardrobeItem("Coverall Legs", 40, r => Legs(r, Workshop.Yellow, Workshop.Yellow)),
        };

        public static readonly WardrobeItem[] Shoes = {
            new WardrobeItem("Plain Shoes", 0),
            new WardrobeItem("Work Boots", 55, r => { r.Repaint("Boot", new Color(.62f,.42f,.22f)); Soles(r, Sole); }),
            new WardrobeItem("Laced Boots", 80, r => { r.Repaint("Boot", Leather); Soles(r, Sole);
                for (int side = -1; side <= 1; side += 2) { Transform ankle = r.Ankle(side);
                    r.Add(ankle, "Boot shaft", BeanRig.Capsule(.10f,.078f,.078f), new Vector3(0,.10f,0), Vector3.one, Leather);
                    for (int l = 0; l < 3; l++) r.Add(ankle, "Lace", BeanRig.Box(new Vector3(.06f,.008f,.01f),.003f), new Vector3(0,.07f - l * .035f,.078f + l * .01f), Vector3.one, BeanRig.Cream, Quaternion.Euler(0, 0, l % 2 == 0 ? 12 : -12)); } }),
            new WardrobeItem("Sneakers", 60, r => { r.Repaint("Boot", new Color(.92f,.32f,.30f)); Soles(r, BeanRig.Cream);
                for (int side = -1; side <= 1; side += 2) { r.Add(r.Ankle(side), "Lace", BeanRig.Box(new Vector3(.05f,.01f,.012f),.004f), new Vector3(0,.034f,.09f), Vector3.one, BeanRig.Cream);
                    r.Add(r.Ankle(side), "Lace", BeanRig.Box(new Vector3(.05f,.01f,.012f),.004f), new Vector3(0,.024f,.12f), Vector3.one, BeanRig.Cream, Quaternion.Euler(-30, 0, 0)); } }),
            new WardrobeItem("Rain Boots", 45, r => { Color c = new Color(1f,.84f,.2f); r.Repaint("Boot", c); Soles(r, Darker(c));
                for (int side = -1; side <= 1; side += 2) r.Add(r.Ankle(side), "Boot shaft", BeanRig.Capsule(.14f,.08f,.08f), new Vector3(0,.14f,0), Vector3.one, c); }),
        };

        public static readonly WardrobeItem[] Gear = {
            new WardrobeItem("None", 0),
            new WardrobeItem("Tool Backpack", 110, Backpack),
            new WardrobeItem("Utility Belt", 70, UtilityBelt),
            new WardrobeItem("Flashlight and Radio", 65, r => { r.Add(r.Pelvis, "Chest flashlight", BeanRig.Capsule(.07f,.022f,.022f), new Vector3(-.12f,.30f,.18f), Vector3.one, new Color(.25f,.26f,.3f), Quaternion.Euler(-90, 0, 0));
                r.Add(r.Pelvis, "Flashlight lens", BeanRig.Sphere(), new Vector3(-.12f,.30f,.27f), new Vector3(.042f,.042f,.02f), new Color(1f,.95f,.6f)); r.ShoulderRadio(); }),
            new WardrobeItem("Scarf", 45, Scarf),
            new WardrobeItem("Work Gloves", 30, r => { r.Repaint("Mitten", GloveGrey); r.Repaint("Thumb", GloveGrey);
                for (int side = -1; side <= 1; side += 2) r.Add(r.Hand(side), "Glove cuff", BeanRig.Torus(.052f,.016f), new Vector3(0,.005f,0), Vector3.one, Color.Lerp(GloveGrey, Color.white, .15f)); }),
        };

        // Four looks built from the detail studies, plus the plain default: used for portraits and quick picks.
        public static readonly AvatarLoadout[] Presets = {
            new AvatarLoadout(),
            new AvatarLoadout { Skin = 6, Head = 1, Eyes = 0, Mouth = 0, Brows = 1, Shirt = 6, Pants = 6, Shoes = 1, Gear = 5 },
            new AvatarLoadout { Skin = 0, Head = 2, Eyes = 1, Mouth = 3, Brows = 2, Shirt = 5, Pants = 4, Shoes = 1, Gear = 5 },
            new AvatarLoadout { Skin = 3, Head = 8, Eyes = 2, Mouth = 0, Brows = 4, Glasses = 2, Shirt = 5, Pants = 2, Shoes = 3, Gear = 1 },
            new AvatarLoadout { Skin = 8, Head = 5, Eyes = 0, Mouth = 2, Brows = 3, Shirt = 3, Jacket = 1, Pants = 2, Shoes = 2, Gear = 4 },
            new AvatarLoadout { Skin = 7, Head = 0, Eyes = 4, Mouth = 5, Brows = 1, Glasses = 1, Shirt = 2, Jacket = 3, Pants = 1, Shoes = 3 },
        };

        public static int Count(WardrobeSlot slot)
        {
            switch (slot)
            {
                case WardrobeSlot.Skin: return SkinNames.Length; case WardrobeSlot.Eyes: return EyeNames.Length;
                case WardrobeSlot.Mouth: return MouthNames.Length; case WardrobeSlot.Brows: return BrowNames.Length;
                case WardrobeSlot.Glasses: return GlassesNames.Length; default: return Items(slot).Length;
            }
        }

        public static string Name(WardrobeSlot slot, int index)
        {
            switch (slot)
            {
                case WardrobeSlot.Skin: return SkinNames[index]; case WardrobeSlot.Eyes: return EyeNames[index];
                case WardrobeSlot.Mouth: return MouthNames[index]; case WardrobeSlot.Brows: return BrowNames[index];
                case WardrobeSlot.Glasses: return GlassesNames[index]; default: return Items(slot)[index].Name;
            }
        }

        static WardrobeItem[] Items(WardrobeSlot slot)
        {
            switch (slot)
            {
                case WardrobeSlot.Head: return Heads; case WardrobeSlot.Shirt: return Shirts; case WardrobeSlot.Jacket: return Jackets;
                case WardrobeSlot.Pants: return Pants; case WardrobeSlot.Shoes: return Shoes; default: return Gear;
            }
        }

        public static BeanLook Look(AvatarLoadout loadout)
        {
            return new BeanLook { Outfit = BeanOutfit.Plain, Skin = BeanRig.SkinColors[loadout.Skin], Primary = PlainTee,
                Eyes = loadout.Eyes, Mouth = loadout.Mouth, Brows = loadout.Brows, Glasses = loadout.Glasses };
        }

        // Layer order matters: jackets read the shirt color and cover it; head items go on last.
        public static void Dress(BeanRig rig, AvatarLoadout loadout)
        {
            Shirts[loadout.Shirt].Wear(rig);
            Pants[loadout.Pants].Wear(rig);
            Shoes[loadout.Shoes].Wear(rig);
            Jackets[loadout.Jacket].Wear(rig);
            Gear[loadout.Gear].Wear(rig);
            Heads[loadout.Head].Wear(rig);
        }

        // ---- Pieces ------------------------------------------------------------------------------------

        static Color Darker(Color c) { return Color.Lerp(c, Color.black, .22f); }

        static void Top(BeanRig r, Color color, bool longSleeves)
        {
            r.Repaint("Outfit body", color); r.Repaint("Upper sleeve", color);
            r.Repaint("Forearm", longSleeves ? color : r.Look.Skin);
        }

        static void Legs(BeanRig r, Color thigh, Color shin) { r.Repaint("Thigh", thigh); r.Repaint("Shin", shin); }

        static void Hood(BeanRig r, Color c) { r.Add(r.Pelvis, "Hood", BeanRig.Sphere(), new Vector3(0,.39f,-.15f), new Vector3(.32f,.17f,.19f), c); }

        static void Drawstrings(BeanRig r, float z)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                r.Add(r.Pelvis, "Drawstring", BeanRig.Capsule(.11f,.006f,.006f), new Vector3(side * .035f,.37f,z), Vector3.one, BeanRig.Cream);
                r.Add(r.Pelvis, "Drawstring tip", BeanRig.Sphere(), new Vector3(side * .035f,.255f,z), Vector3.one * .018f, BeanRig.Cream);
            }
        }

        static void Cuffs(BeanRig r, Color c)
        {
            for (int side = -1; side <= 1; side += 2) r.Add(r.Hand(side), "Ribbed cuff", BeanRig.Torus(.052f,.02f), new Vector3(0,.01f,0), Vector3.one, c);
        }

        static void Pocket(BeanRig r, Color c)
        {
            r.Add(r.Pelvis, "Chest pocket", BeanRig.Box(new Vector3(.08f,.075f,.018f),.01f), new Vector3(.09f,.21f,.158f), Vector3.one, c);
            r.Add(r.Pelvis, "Pocket flap", BeanRig.Box(new Vector3(.086f,.026f,.024f),.01f), new Vector3(.09f,.255f,.162f), Vector3.one, c);
        }

        static void Zipper(BeanRig r, float lift)
        {
            Color metal = new Color(.75f,.77f,.8f);
            r.Add(r.Pelvis, "Zipper", BeanRig.Tube("Zipper " + lift, BeanRig.FrontLine(.36f, 0f, lift), .006f), Vector3.zero, Vector3.one, metal);
            r.Add(r.Pelvis, "Zipper pull", BeanRig.Box(new Vector3(.018f,.035f,.008f),.004f), new Vector3(0,.33f,BeanRig.TorsoRadius(.33f) * .82f + lift + .008f), Vector3.one, metal);
        }

        static void NameTag(BeanRig r)
        {
            r.Add(r.Pelvis, "Name tag", BeanRig.Box(new Vector3(.10f,.034f,.01f),.006f), new Vector3(-.09f,.31f,.155f), Vector3.one, BeanRig.Cream);
            r.Add(r.Pelvis, "Name tag line", BeanRig.Box(new Vector3(.07f,.008f,.012f),.003f), new Vector3(-.09f,.31f,.157f), Vector3.one, new Color(.2f,.3f,.6f));
        }

        // A slightly larger torso over the shirt. Sleeved jackets also recolor both arm segments.
        static void Shell(BeanRig r, Color c, bool sleeves)
        {
            r.Add(r.Pelvis, "Jacket shell", BeanRig.Torso(), new Vector3(0,-.015f,0), new Vector3(1.07f,.95f,.88f), c);
            if (sleeves) { r.Repaint("Upper sleeve", c); r.Repaint("Forearm", c); }
        }

        // The open front of a jacket shows the shirt; any tie is lifted to sit on top of it.
        static void FrontStrip(BeanRig r, Color shirt)
        {
            r.Add(r.Pelvis, "Open front", BeanRig.Tube("Open front", BeanRig.FrontLine(.42f, -.11f, .006f), .03f), Vector3.zero, new Vector3(1.7f,1,1), shirt);
            foreach (Transform part in r.Pelvis) if (part.name == "Tie" || part.name == "Tie knot") part.localPosition += new Vector3(0,0,.032f);
        }

        static void Puffer(BeanRig r, Color c)
        {
            for (int i = 0; i < 5; i++)
            {
                float y = .02f + i * .085f;
                r.Add(r.Pelvis, "Puffer ring", BeanRig.Torus(BeanRig.TorsoRadius(y) + .014f, .038f), new Vector3(0,y,0), new Vector3(1,1,.84f), c);
            }
            r.Add(r.Pelvis, "Vest zipper", BeanRig.Tube("Vest zipper", BeanRig.FrontLine(.38f, 0f, .045f), .007f), Vector3.zero, Vector3.one, new Color(.3f,.2f,.15f));
        }

        static void Soles(BeanRig r, Color c)
        {
            for (int side = -1; side <= 1; side += 2) r.Add(r.Ankle(side), "Sole", BeanRig.Sphere(), new Vector3(0,-.075f,.04f), new Vector3(.18f,.05f,.27f), c);
        }

        static void Backpack(BeanRig r)
        {
            Color pack = new Color(.95f,.55f,.18f);
            r.Add(r.Pelvis, "Tool backpack", BeanRig.Box(new Vector3(.32f,.36f,.15f),.06f), new Vector3(0,.20f,-.24f), Vector3.one, pack);
            r.Add(r.Pelvis, "Backpack pocket", BeanRig.Box(new Vector3(.24f,.14f,.06f),.03f), new Vector3(0,.12f,-.33f), Vector3.one, Darker(pack));
            r.Add(r.Pelvis, "Backpack handle", BeanRig.Torus(.04f,.01f), new Vector3(0,.39f,-.24f), Vector3.one, Strap, Quaternion.Euler(0, 0, 90));
            for (int side = -1; side <= 1; side += 2)
                r.Add(r.Pelvis, "Backpack strap", BeanRig.Tube("Backpack strap " + side, new[] {
                    new Vector3(side*.11f,.10f,.162f), new Vector3(side*.12f,.26f,.165f), new Vector3(side*.12f,.38f,.125f),
                    new Vector3(side*.12f,.45f,0), new Vector3(side*.12f,.38f,-.15f), new Vector3(side*.11f,.26f,-.18f) }, .026f), Vector3.zero, Vector3.one, Strap);
        }

        static void UtilityBelt(BeanRig r)
        {
            Color brown = new Color(.36f,.22f,.14f), dark = new Color(.15f,.16f,.19f);
            r.Add(r.Pelvis, "Utility belt", BeanRig.Band(), new Vector3(0,-.02f,0), new Vector3(1.04f,1.1f,.86f), brown);
            r.Add(r.Pelvis, "Radio", BeanRig.Box(new Vector3(.06f,.10f,.035f),.012f), new Vector3(.19f,0,.08f), Vector3.one, dark, Quaternion.Euler(0, 45, 0));
            r.Add(r.Pelvis, "Radio antenna", BeanRig.Capsule(.08f,.007f,.007f), new Vector3(.2f,.13f,.08f), Vector3.one, dark, Quaternion.Euler(180, 0, 0));
            r.Add(r.Pelvis, "Tape measure", BeanRig.Box(new Vector3(.07f,.07f,.035f),.02f), new Vector3(-.19f,-.02f,.07f), Vector3.one, BeanRig.SkinColors[2], Quaternion.Euler(0, -50, 0));
            r.Add(r.Pelvis, "Key ring", BeanRig.Torus(.022f,.005f), new Vector3(-.14f,-.06f,.15f), Vector3.one, new Color(.8f,.82f,.85f), Quaternion.Euler(90, 20, 0));
            r.Add(r.Pelvis, "Key", BeanRig.Box(new Vector3(.014f,.04f,.005f),.003f), new Vector3(-.145f,-.095f,.155f), Vector3.one, new Color(.85f,.72f,.35f), Quaternion.Euler(0, 0, 15));
        }

        static void Scarf(BeanRig r)
        {
            Color scarf = new Color(.85f,.22f,.28f);
            r.Add(r.Pelvis, "Scarf", BeanRig.Torus(.155f,.045f), new Vector3(0,.40f,0), new Vector3(1,1,.9f), scarf);
            r.Add(r.Pelvis, "Scarf tail", BeanRig.Capsule(.22f,.04f,.036f), new Vector3(-.08f,.39f,.17f), new Vector3(1,1,.55f), scarf, Quaternion.Euler(-8, 0, -6));
            r.Add(r.Pelvis, "Scarf fringe", BeanRig.Box(new Vector3(.07f,.02f,.03f),.008f), new Vector3(-.105f,.16f,.19f), Vector3.one, Color.Lerp(scarf, Color.white, .2f), Quaternion.Euler(0, 0, -6));
        }
    }
}
