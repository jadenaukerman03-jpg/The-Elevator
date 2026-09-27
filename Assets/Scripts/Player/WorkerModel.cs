using UnityEngine;

namespace TheElevator
{
    public sealed class WorkerModel : MonoBehaviour
    {
        public static readonly Color[] SuitColors = {
            Workshop.Yellow, Workshop.Mint, new Color(0.73f, 0.53f, 0.91f), Workshop.Red
        };
        Transform body, head, leftArm, rightArm, leftLeg, rightLeg;
        Renderer torso;
        Workshop workshop;
        float stride;
        TheElevator.Office.BusinessRobot officeRig;
        public void UseOfficeRig(TheElevator.Office.OfficeArt art)
        {
            if(officeRig)return;
            foreach(Transform child in transform)child.gameObject.SetActive(false);
            GameObject root=new GameObject("Corporate disguise");root.transform.SetParent(transform,false);
            officeRig=root.AddComponent<TheElevator.Office.BusinessRobot>();officeRig.Build(art,4,false);
        }

        public void Build(Workshop w, int colorIndex)
        {
            workshop = w;
            Color suit = SuitColors[colorIndex];
            body = w.Group("Wobbly body", transform, new Vector3(0, 0.95f, 0));
            torso = w.Shape("Extremely round coveralls", body, Vector3.zero,
                new Vector3(0.94f, 0.52f, 0.65f), suit, PrimitiveType.Capsule, false).GetComponent<Renderer>();
            w.Shape("Reflective belt", body, new Vector3(0, -0.12f, 0.025f),
                new Vector3(0.97f, 0.13f, 0.66f), Workshop.Cream, PrimitiveType.Cube, false);
            w.Shape("Employee badge", body, new Vector3(-0.23f, 0.15f, 0.34f),
                new Vector3(0.18f, 0.24f, 0.035f), Workshop.Cream, PrimitiveType.Cube, false);
            w.Shape("Badge photo", body, new Vector3(-0.23f, 0.19f, 0.365f),
                new Vector3(0.09f, 0.09f, 0.018f), Workshop.Ink, PrimitiveType.Cube, false);
            head = w.Group("Head", body, new Vector3(0, 0.65f, 0.02f));
            w.Shape("Bean head", head, Vector3.zero, new Vector3(0.70f, 0.62f, 0.62f),
                new Color(0.80f, 0.66f, 0.49f), PrimitiveType.Sphere, false);
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.17f : 0.17f;
                float y = i == 0 ? 0.045f : 0.095f;
                w.Shape("Googly eye", head, new Vector3(x, y, 0.285f),
                    new Vector3(0.26f, 0.29f, 0.16f), Color.white, PrimitiveType.Sphere, false);
                w.Shape("Tiny worried pupil", head, new Vector3(x + 0.025f, y, 0.369f),
                    new Vector3(0.073f, 0.10f, 0.026f), Workshop.Ink, PrimitiveType.Sphere, false);
                GameObject brow = w.Shape("Concerned eyebrow", head, new Vector3(x, y + 0.19f, 0.32f),
                    new Vector3(0.25f, 0.035f, 0.04f), Workshop.Ink, PrimitiveType.Cube, false);
                brow.transform.localRotation = Quaternion.Euler(0, 0, i == 0 ? -15 : 23);
            }
            w.Shape("Nose", head, new Vector3(0.025f, -0.09f, 0.35f), new Vector3(0.15f, 0.13f, 0.20f),
                new Color(0.88f, 0.60f, 0.43f), PrimitiveType.Sphere, false);
            w.Shape("Hard hat dome", head, new Vector3(0, 0.26f, -0.01f),
                new Vector3(0.78f, 0.32f, 0.74f), Workshop.Yellow, PrimitiveType.Sphere, false);
            w.Shape("Ridiculous hard hat brim", head, new Vector3(0, 0.19f, 0.06f),
                new Vector3(0.91f, 0.055f, 0.88f), Workshop.Yellow, PrimitiveType.Cylinder, false);
            leftArm = Limb(w, body, "Left arm", new Vector3(-0.57f, 0.30f, 0), suit, false);
            rightArm = Limb(w, body, "Right arm", new Vector3(0.57f, 0.30f, 0), suit, false);
            leftLeg = Limb(w, transform, "Left leg", new Vector3(-0.26f, 0.55f, 0), suit, true);
            rightLeg = Limb(w, transform, "Right leg", new Vector3(0.26f, 0.55f, 0), suit, true);
        }

        Transform Limb(Workshop w, Transform parent, string name, Vector3 pos, Color suit, bool leg)
        {
            Transform pivot = w.Group(name, parent, pos);
            w.Shape(name, pivot, new Vector3(0, -0.18f, 0), new Vector3(0.24f, 0.28f, 0.25f),
                suit, PrimitiveType.Capsule, false);
            w.Shape(leg ? "Oversized boot" : "Safety mitten", pivot, new Vector3(0, -0.39f, leg ? 0.12f : 0),
                leg ? new Vector3(0.37f, 0.26f, 0.57f) : new Vector3(0.29f, 0.27f, 0.30f),
                Workshop.Ink, PrimitiveType.Cube, false);
            return pivot;
        }

        public void SetColor(int index) { torso.sharedMaterial = workshop.Mat(SuitColors[index]); }

        public void Animate(float speed, bool carrying, float dt)
        {
            if(officeRig){officeRig.Speed=speed;officeRig.Animate(dt);return;}
            stride += dt * (speed > 0.1f ? 10f : 2f);
            float amplitude = Mathf.Clamp01(speed / 4f);
            float wave = Mathf.Sin(stride);
            body.localRotation = Quaternion.Euler(0, 0, wave * (amplitude * 7f + 1f));
            body.localPosition = new Vector3(0, 0.95f + Mathf.Abs(wave) * amplitude * 0.07f, 0);
            head.localRotation = Quaternion.Euler(0, Mathf.Sin(stride * 0.45f) * 6f, -wave * 3f);
            leftLeg.localRotation = Quaternion.Euler(wave * amplitude * 35f, 0, -3);
            rightLeg.localRotation = Quaternion.Euler(-wave * amplitude * 35f, 0, 3);
            leftArm.localRotation = Quaternion.Euler(carrying ? -80 : -wave * amplitude * 35, 0, carrying ? -15 : 8);
            rightArm.localRotation = Quaternion.Euler(carrying ? -80 : wave * amplitude * 35, 0, carrying ? 15 : -8);
        }
    }
}
