using UnityEngine;
using TheElevator.Office;

namespace TheElevator
{
    // A first aid kit hanging on a wall bracket (one room in fifty). Pick it up and use it (left mouse button) to
    // heal half your health; it is used up.
    public sealed class FirstAidKit : MonoBehaviour
    {
        public const float HalfHeight = .13f, SpawnChance = .02f, HealFraction = .5f;
        public const string Title = "First aid kit";
        SalvageItem item;
        DescentGame game;

        public static FirstAidKit Create(DescentGame game, OfficeArt art, Transform parent, Vector3 position, Quaternion rotation)
        {
            Transform root = art.Group(parent, Title, Vector3.zero);
            root.SetPositionAndRotation(position, rotation);
            FirstAidKit kit = root.gameObject.AddComponent<FirstAidKit>();
            kit.game = game;
            kit.Build(art);
            kit.item = root.gameObject.AddComponent<SalvageItem>();
            kit.item.Configure(game, Title, 1.5f, 15, false, new Vector3(.36f, HalfHeight * 2, .13f));
            // It hangs on its bracket until someone takes it.
            kit.item.Body.isKinematic = true;
            return kit;
        }

        // White case, red cross and label on the front (-Z, facing out from the wall), carry handle on top.
        void Build(OfficeArt a)
        {
            a.Box(transform, "First aid case", Vector3.zero, new Vector3(.34f, .24f, .11f), a.Paper);
            a.Box(transform, "Case lid seam", new Vector3(0, .02f, -.056f), new Vector3(.34f, .008f, .004f), a.Dark);
            a.Box(transform, "Red cross upright", new Vector3(0, -.025f, -.058f), new Vector3(.035f, .11f, .006f), a.Red);
            a.Box(transform, "Red cross bar", new Vector3(0, -.025f, -.058f), new Vector3(.11f, .035f, .006f), a.Red);
            a.Label(transform, "FIRST AID", new Vector3(0, .075f, -.06f), .012f, new Color(.75f, .1f, .1f));
            a.Box(transform, "Carry handle", new Vector3(0, .14f, 0), new Vector3(.12f, .025f, .03f), a.Dark);
            for (int side = -1; side <= 1; side += 2) a.Box(transform, "Case latch", new Vector3(side * .12f, .035f, -.058f), new Vector3(.03f, .025f, .008f), a.Metal);
        }

        public void Use(WorkerController user)
        {
            if (!user || user.Down) return;
            if (user.Health >= WorkerController.MaxHealth) { if (game) game.Notify("You're not hurt. Save the first aid kit for later."); return; }
            user.Heal(WorkerController.MaxHealth * HealFraction);
            if (game) { game.Notify("Patched up. Health " + Mathf.CeilToInt(user.Health) + "."); game.Sound.Play(520, .2f, .1f); }
            user.ConsumeHeld();
        }
    }
}
