using UnityEngine;
namespace TheElevator.Office
{
    // A weapon dropped by a knocked-out employee, now a pickup. In a player's hands it is lethal: hold the use
    // button to fire: the pistol shoots once per click (12 rounds, every hit a knockout), the bazooka fires a rocket per click (3 rockets, a blast
    // that knocks out everyone in 10 m, the shooter included if they are too close). The ring around the cursor
    // shows how much ammunition has been used.
    public sealed class PlayerWeapon : MonoBehaviour
    {
        public Arms Kind { get; private set; }
        public int Ammo { get; private set; }
        public int Capacity { get; private set; }
        public float Used { get { return Capacity > 0 ? 1 - Ammo / (float)Capacity : 1; } }
        public bool Empty { get { return Ammo <= 0; } }
        OfficeFloor office;
        float nextShot;
        readonly System.Random random = new System.Random();

        // Turns the model an employee was holding into a loose pickup at the same spot.
        public static PlayerWeapon Drop(Transform model, Arms kind, OfficeFloor office)
        {
            model.SetParent(office.transform, true);
            PlayerWeapon weapon = model.gameObject.AddComponent<PlayerWeapon>();
            weapon.Kind = kind; weapon.office = office;
            weapon.Capacity = weapon.Ammo = kind == Arms.Pistol ? OfficeWeapons.PistolMagazine : OfficeWeapons.BazookaRockets;
            bool pistol = kind == Arms.Pistol;
            SalvageItem item = model.gameObject.AddComponent<SalvageItem>();
            item.Configure(office.Game, pistol ? "Pistol" : "Bazooka", pistol ? 1.2f : 7, pistol ? 120 : 300, false, pistol ? new Vector3(.05f, .16f, .24f) : new Vector3(.2f, .22f, 1.25f));
            model.GetComponent<BoxCollider>().center = pistol ? new Vector3(0, .01f, .05f) : new Vector3(0, .08f, .07f);
            item.Body.linearVelocity = Vector3.up * 1.2f;
            return weapon;
        }

        // held: the use button is down this frame; pressed: it went down this frame.
        public void Operate(WorkerController user, bool held, bool pressed)
        {
            if (!office || Empty || Time.time < nextShot) return;
            Transform view = user.View.transform, muzzle = OfficeWeapons.Muzzle(transform);
            Vector3 from = muzzle ? muzzle.position : view.position + view.forward * .6f;
            if (Kind == Arms.Pistol)
            {
                if (!pressed) return;
                OfficeWeapons.FirePlayerPistol(office, user, from, random);
                nextShot = Time.time + .18f;
            }
            else
            {
                if (!pressed) return;
                Vector3 aim = Physics.Raycast(view.position, view.forward, out RaycastHit hit, 60, ~(1 << 2), QueryTriggerInteraction.Ignore) ? hit.point : view.position + view.forward * 60;
                OfficeWeapons.FireRocket(office, null, user, from, aim, .5f, OfficeWeapons.PlayerBlastDamage, random);
                nextShot = Time.time + 1.2f;
            }
            Ammo--;
        }
    }
}
