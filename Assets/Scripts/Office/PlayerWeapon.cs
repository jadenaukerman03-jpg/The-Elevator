using UnityEngine;
namespace TheElevator.Office
{
    // A weapon dropped by a knocked-out employee, now a pickup. In a player's hands it is lethal: hold the use
    // button to fire the rifle (30 rounds, every hit a knockout), click to fire the bazooka (3 rockets, a blast
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
            weapon.Capacity = weapon.Ammo = kind == Arms.Rifle ? OfficeWeapons.RifleMagazine : OfficeWeapons.BazookaRockets;
            bool rifle = kind == Arms.Rifle;
            SalvageItem item = model.gameObject.AddComponent<SalvageItem>();
            item.Configure(office.Game, rifle ? "Assault rifle" : "Bazooka", rifle ? 4 : 7, rifle ? 150 : 300, false, rifle ? new Vector3(.08f, .22f, 1f) : new Vector3(.2f, .22f, 1.25f));
            model.GetComponent<BoxCollider>().center = rifle ? new Vector3(0, 0, .13f) : new Vector3(0, .08f, .07f);
            item.Body.linearVelocity = Vector3.up * 1.2f;
            return weapon;
        }

        // held: the use button is down this frame; pressed: it went down this frame.
        public void Operate(WorkerController user, bool held, bool pressed)
        {
            if (!office || Empty || Time.time < nextShot) return;
            Transform view = user.View.transform, muzzle = OfficeWeapons.Muzzle(transform);
            Vector3 from = muzzle ? muzzle.position : view.position + view.forward * .6f;
            if (Kind == Arms.Rifle)
            {
                if (!held) return;
                OfficeWeapons.FirePlayerRifle(office, user, from, random);
                nextShot = Time.time + .11f;
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
