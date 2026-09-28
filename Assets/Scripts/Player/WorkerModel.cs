using UnityEngine;

namespace TheElevator
{
    public sealed class WorkerModel : MonoBehaviour
    {
        public static readonly Color[] SuitColors = {
            Workshop.Yellow, Workshop.Mint, new Color(0.73f, 0.53f, 0.91f), Workshop.Red
        };
        BeanRig rig;
        int suit;
        bool firstPerson, crouched, applied;

        // First-person hands copy the body's gloves and sleeves.
        public Color HandColor { get { return BeanRig.Glove; } }
        public Color SleeveColor { get { return BeanRig.CrewLook(suit).Primary; } }

        public void UseOfficeRig(TheElevator.Office.OfficeArt art) { }

        public void Build(Workshop w, int colorIndex) { SetColor(colorIndex); }

        // Each uniform is a whole crew look (suit, skin, face and headwear), one per future co-op player.
        public void SetColor(int index)
        {
            suit = index;
            if (rig) DestroyImmediate(rig.gameObject);
            GameObject root = new GameObject("Crew body");
            root.transform.SetParent(transform, false);
            rig = root.AddComponent<BeanRig>();
            rig.Build(BeanRig.CrewLook(index), 4 + index);
            applied = false;
            SetView(firstPerson, crouched);
        }

        // The full body stays in the world in first person: head and arms only cast its shadow.
        public void SetView(bool firstPersonView, bool crouching)
        {
            gameObject.SetActive(true);
            if (applied && firstPerson == firstPersonView && crouched == crouching) return;
            applied = true; firstPerson = firstPersonView; crouched = crouching;
            transform.localPosition = firstPerson ? new Vector3(0, 0, -.14f) : Vector3.zero;
            float squash = crouched ? .62f : 1;
            transform.localScale = new Vector3(1, squash, 1);
            rig.Head.localScale = new Vector3(1, 1 / squash, 1);
            rig.SetFirstPerson(firstPerson);
        }

        public void Animate(float speed, bool carrying, float dt)
        {
            rig.Animate(dt, new BeanPose { Speed = speed, Carrying = carrying, ElbowBend = -12 });
        }
    }
}
