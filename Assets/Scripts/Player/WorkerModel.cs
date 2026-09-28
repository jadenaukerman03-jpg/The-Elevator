using UnityEngine;

namespace TheElevator
{
    public sealed class WorkerModel : MonoBehaviour
    {
        BeanRig rig;
        bool firstPerson, crouched, applied;

        public AvatarLoadout Loadout { get; private set; }
        // Bumped on every rebuild so the first-person hands can re-read their colors.
        public int Version { get; private set; }
        public Color HandColor { get { return rig.ColorOf("Mitten"); } }
        public Color SleeveColor { get { return rig.ColorOf("Forearm"); } }

        public void Build(Workshop w, int colorIndex) { Apply(AvatarLoadout.Load()); }

        // Rebuilds the plain avatar and layers the loadout's cosmetics on top.
        public void Apply(AvatarLoadout loadout)
        {
            Loadout = loadout.Clone();
            if (rig)
            {
                rig.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(rig.gameObject); else DestroyImmediate(rig.gameObject);
            }
            GameObject root = new GameObject("Avatar");
            root.transform.SetParent(transform, false);
            rig = root.AddComponent<BeanRig>();
            rig.Build(AvatarWardrobe.Look(Loadout), 4);
            AvatarWardrobe.Dress(rig, Loadout);
            Version++;
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
