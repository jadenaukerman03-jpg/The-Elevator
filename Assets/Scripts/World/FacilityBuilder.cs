using UnityEngine;

namespace TheElevator
{
    public sealed class FacilityBuilder
    {
        readonly DescentGame game;
        readonly Workshop w;
        public Transform LeftDoor { get; private set; }
        public Transform RightDoor { get; private set; }
        public TextMesh Display { get; private set; }
        public static readonly string[] Names = { "MISPLACED PROPERTY", "HYDROLOGY / DO NOT DRINK", "HUMAN RESOURCES" };

        public FacilityBuilder(DescentGame owner, Workshop workshop) { game = owner; w = workshop; }

        void Box(string name, Transform root, float x, float y, float z, float sx, float sy, float sz, Color color)
        {
            w.Shape(name, root, new Vector3(x, y, z), new Vector3(sx, sy, sz), color);
        }

        public void Cabin(Transform root)
        {
            Box("Freight lift deck", root, 0, -0.20f, -6, 9, 0.4f, 8, Workshop.Steel);
            Box("Rear wall", root, 0, 1.8f, -10, 9, 3.6f, 0.25f, Workshop.Ink);
            Box("Left wall", root, -4.5f, 1.8f, -6, 0.25f, 3.6f, 8, Workshop.Steel);
            Box("Right wall", root, 4.5f, 1.8f, -6, 0.25f, 3.6f, 8, Workshop.Steel);
            Box("Roof", root, 0, 3.7f, -6, 9, 0.2f, 8, Workshop.Ink);
            Box("Door frame left", root, -3.5f, 1.8f, -2, 2, 3.6f, 0.4f, Workshop.Ink);
            Box("Door frame right", root, 3.5f, 1.8f, -2, 2, 3.6f, 0.4f, Workshop.Ink);
            Box("Door frame header", root, 0, 3.25f, -2, 5, 0.9f, 0.4f, Workshop.Ink);
            LeftDoor = w.Shape("Left door", root, new Vector3(-1.25f, 1.4f, -2),
                new Vector3(2.5f, 2.8f, 0.25f), Workshop.Steel).transform;
            RightDoor = w.Shape("Right door", root, new Vector3(1.25f, 1.4f, -2),
                new Vector3(2.5f, 2.8f, 0.25f), Workshop.Steel).transform;
            for (int i = 0; i < 12; i++)
            {
                Box("Hazard stripe", root, -4.1f + i * 0.75f, 0.015f, -2.35f, 0.36f, 0.02f, 0.38f,
                    i % 2 == 0 ? Workshop.Yellow : Workshop.Ink);
                Box("Deck seam", root, -4.1f + i * 0.75f, 0.011f, -6.2f, 0.015f, 0.012f, 7,
                    new Color(0.16f, 0.22f, 0.23f));
            }
            Box("Floor indicator backing",root,0,3.24f,-2.23f,2.8f,.45f,.08f,Workshop.Ink);
            Display=w.Label("01",root,new Vector3(0,3.24f,-2.278f),.065f,Workshop.Mint);
            Transform cabin=w.Group("Passenger freight elevator fittings",root,Vector3.zero);
            cabin.gameObject.AddComponent<ElevatorCabin>().Build(game,w,LeftDoor,RightDoor);
            for (int i = 0; i < 2; i++)
            {
                Box("Ceiling lamp", root, -2.4f + i * 4.8f, 3.53f, -6, 0.22f, 0.05f, 3, Workshop.Cream);
                w.Lamp(root, new Vector3(-2.3f + i * 4.6f, 2.9f, -6), new Color(1, 0.84f, 0.58f), 2.8f, 9);
            }
        }

        public void SetDoors(float closed)
        {
            LeftDoor.localPosition = new Vector3(-1.25f - (1 - closed) * 2.5f, 1.4f, -2);
            RightDoor.localPosition = new Vector3(1.25f + (1 - closed) * 2.5f, 1.4f, -2);
        }

        public Transform Floor(int index, int seed)
        {
            Transform root = w.Group("Floor " + (index + 1) + " - " + Names[index], game.transform, Vector3.zero);
            Color wall = index == 1 ? new Color(0.20f, 0.34f, 0.34f) : new Color(0.29f, 0.33f, 0.31f);
            Box("Concrete slab", root, 0, -0.25f, 10, 24, 0.5f, 24, new Color(0.28f, 0.31f, 0.29f));
            Box("West wall", root, -12, 2.2f, 10, 0.3f, 4.4f, 24, wall);
            Box("East wall", root, 12, 2.2f, 10, 0.3f, 4.4f, 24, wall);
            Box("Back wall", root, 0, 2.2f, 22, 24, 4.4f, 0.3f, wall);
            Box("Front left wall", root, -8.2f, 2.2f, -2, 7.6f, 4.4f, 0.3f, wall);
            Box("Front right wall", root, 8.2f, 2.2f, -2, 7.6f, 4.4f, 0.3f, wall);
            w.Label("B" + (index + 1).ToString("00") + " / " + Names[index], root,
                new Vector3(0, 3.1f, 21.75f), 0.12f, Workshop.Cream);
            w.Label("LIFT / RETURN THIS WAY", root, new Vector3(0, 3.85f, -1.7f), 0.08f, Workshop.Yellow, 180);
            for (int z = 1; z < 22; z += 3)
                Box("Return guide stripe", root, 0, 0.013f, z, 0.12f, 0.02f, 1.1f, Workshop.Yellow);
            for (int z = 3; z <= 19; z += 8)
            {
                Box("Ceiling crossbeam", root, 0, 4.3f, z, 24, 0.35f, 0.35f, Workshop.Ink);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("Column", root, side * 10, 2.1f, z, 0.55f, 4.2f, 0.55f, Workshop.Steel);
                    Box("Lamp strip", root, side * 6, 4.08f, z, 3.2f, 0.08f, 0.22f, Workshop.Cream);
                    w.Lamp(root, new Vector3(side * 6, 3.5f, z), index == 1 ? Workshop.Mint : Workshop.Cream,
                        index == 2 ? 1.6f : 2.5f, 12);
                }
            }
            if (index == 0) Storage(root);
            else if (index == 1) Laboratory(root);
            else Office(root);

            System.Random random = new System.Random(seed + index * 997);
            Vector3[] locations = {
                new Vector3(-3, 1, 2), new Vector3(4, 1, 5), new Vector3(-7, 1, 9),
                new Vector3(7, 1, 14), new Vector3(-4, 1, 18), new Vector3(4, 1, 20),
                new Vector3(-8, 1, 17), new Vector3(8, 1, 3)
            };
            for (int i = 0; i < locations.Length; i++)
            {
                Vector3 p = locations[i] + new Vector3((float)random.NextDouble() * 0.7f - 0.35f, 0, 0);
                string kind = i == 0 || i == 6 ? "battery" : i == 4 ? "safe" : i == 5 ? "artifact" : i % 2 == 0 ? "monitor" : "case";
                Item(root, p, kind, index);
            }
            if (index >= 1)
            {
                Transform sentry = w.Group("Security custodian", root, new Vector3(0, 0.1f, 16));
                Custodian enemy = sentry.gameObject.AddComponent<Custodian>();
                enemy.Initialize(game, w, index == 2);
                game.Guard = enemy;
            }
            return root;
        }

        void Storage(Transform root)
        {
            for (int z = 5; z <= 17; z += 6)
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 9;
                    Box("Pallet", root, x, 0.13f, z, 3, 0.25f, 2, new Color(0.43f, 0.32f, 0.20f));
                    Box("Shipping crate", root, x, 1, z, 2.5f, 1.5f, 1.7f, Workshop.Steel);
                    Box("Cargo strap", root, x, 1.78f, z, 0.15f, 0.035f, 1.75f, Workshop.Yellow);
                }
            w.Label("EVERYTHING HERE IS\nSOMEBODY ELSE'S PROBLEM", root, new Vector3(-6, 2.8f, 21.7f),
                0.07f, Workshop.Yellow);
        }

        void Laboratory(Transform root)
        {
            w.Shape("Shallow floodwater", root, new Vector3(0, 0.085f, 12), new Vector3(23.7f, 0.08f, 19.6f),
                new Color(0.13f, 0.38f, 0.39f), PrimitiveType.Cube, false);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int z = 7; z <= 19; z += 6)
                {
                    w.Shape("Specimen tank", root, new Vector3(side * 10, 1.35f, z), new Vector3(1.7f, 1.3f, 1.7f),
                        Workshop.Steel, PrimitiveType.Cylinder);
                    w.Shape("Tank lid", root, new Vector3(side * 10, 2.72f, z), new Vector3(1.9f, 0.08f, 1.9f),
                        Workshop.Mint, PrimitiveType.Cylinder);
                }
                Transform plate = w.Shape("Exposed electrical plate", root, new Vector3(side * 6, 0.15f, 11),
                    new Vector3(3.8f, 0.12f, 3.2f), Workshop.Yellow, PrimitiveType.Cube, false).transform;
                ElectricalHazard hazard = plate.gameObject.AddComponent<ElectricalHazard>();
                hazard.Initialize(game, w, side > 0 ? 2.5f : 0);
            }
            w.Label("LIVE CIRCUITS\nWAIT FOR GREEN", root, new Vector3(0, 2.3f, 21.7f), 0.095f, Workshop.Yellow);
        }

        void Office(Transform root)
        {
            for (int side = -1; side <= 1; side += 2)
                for (int z = 6; z <= 18; z += 6)
                {
                    float x = side * 6.5f;
                    Box("Cubicle divider", root, x, 1, z + 1.8f, 5, 2, 0.16f, Workshop.Steel);
                    Box("Desk", root, x, 0.9f, z, 3.8f, 0.16f, 1.5f, Workshop.Cream);
                    Box("Desk support", root, x - 1.5f, 0.43f, z, 0.2f, 0.85f, 1.2f, Workshop.Ink);
                    Box("Desk support", root, x + 1.5f, 0.43f, z, 0.2f, 0.85f, 1.2f, Workshop.Ink);
                }
            w.Label("QUIET PLEASE.\nIT IS STILL ON THE PAYROLL.", root, new Vector3(0, 2.3f, 21.7f), 0.09f, Workshop.Yellow);
        }

        public void Item(Transform root, Vector3 position, string kind, int depth)
        {
            Transform item = w.Group(kind, root, position);
            string title;
            float mass;
            int value;
            Vector3 size;
            if (kind == "battery")
            {
                title = "Power cell"; mass = 12; value = 35; size = new Vector3(0.45f, 0.75f, 0.45f);
                w.Shape(title, item, Vector3.zero, size, Workshop.Ink, PrimitiveType.Cube, false);
                w.Shape("Charge strip", item, new Vector3(0, 0, -0.235f), new Vector3(0.27f, 0.5f, 0.025f), Workshop.Mint, PrimitiveType.Cube, false);
                w.Shape("Terminal", item, new Vector3(0, 0.4f, 0), new Vector3(0.22f, 0.12f, 0.22f), Workshop.Cream, PrimitiveType.Cube, false);
            }
            else if (kind == "safe")
            {
                title = "Management's safe"; mass = 85; value = 290 + depth * 40; size = new Vector3(1.2f, 1.3f, 1.0f);
                w.Shape(title, item, Vector3.zero, size, Workshop.Steel, PrimitiveType.Cube, false);
                w.Shape("Door", item, new Vector3(0, 0, -0.52f), new Vector3(1, 1.1f, 0.09f), Workshop.Ink, PrimitiveType.Cube, false);
                w.Shape("Dial", item, new Vector3(0.1f, 0.1f, -0.6f), new Vector3(0.22f, 0.22f, 0.10f), Workshop.Cream, PrimitiveType.Sphere, false);
            }
            else if (kind == "artifact")
            {
                title = "Unapproved specimen"; mass = 38; value = 230 + depth * 60; size = new Vector3(0.8f, 1.1f, 0.8f);
                w.Shape(title, item, Vector3.zero, size, new Color(0.65f, 0.46f, 0.83f), PrimitiveType.Sphere, false);
                w.Shape("Definitely an eye", item, new Vector3(0, 0.15f, -0.37f), new Vector3(0.37f, 0.38f, 0.18f), Workshop.Cream, PrimitiveType.Sphere, false);
                w.Shape("Eye pupil", item, new Vector3(0, 0.15f, -0.46f), new Vector3(0.09f, 0.23f, 0.03f), Workshop.Ink, PrimitiveType.Sphere, false);
            }
            else if (kind == "monitor")
            {
                title = "Government computer"; mass = 24; value = 115 + depth * 20; size = new Vector3(0.9f, 0.75f, 0.7f);
                w.Shape(title, item, Vector3.zero, size, Workshop.Cream, PrimitiveType.Cube, false);
                w.Shape("Screen", item, new Vector3(0, 0, -0.36f), new Vector3(0.72f, 0.52f, 0.035f), Workshop.Ink, PrimitiveType.Cube, false);
                w.Shape("Green terminal", item, new Vector3(-0.20f, 0.12f, -0.39f), new Vector3(0.17f, 0.035f, 0.025f), Workshop.Mint, PrimitiveType.Cube, false);
            }
            else
            {
                title = "Unclaimed briefcase"; mass = 18; value = 85 + depth * 20; size = new Vector3(0.85f, 0.5f, 0.35f);
                w.Shape(title, item, Vector3.zero, size, Workshop.Yellow, PrimitiveType.Cube, false);
                w.Shape("Handle", item, new Vector3(0, 0.3f, 0), new Vector3(0.3f, 0.13f, 0.12f), Workshop.Ink, PrimitiveType.Cube, false);
            }
            item.gameObject.AddComponent<SalvageItem>().Configure(game, title, mass, value, kind == "battery", size);
        }
    }
}

