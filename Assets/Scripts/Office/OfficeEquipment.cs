using UnityEngine;
namespace TheElevator.Office
{
    public sealed class OfficeEquipment : MonoBehaviour { public OfficeTaskPoint Station; public bool Recoverable; }
    public sealed class OfficeKeycard : MonoBehaviour
    {
        public OfficeFloor Office;
        public bool Taken;
        public void Take()
        {
            if(Taken||!Office.Game)return;
            Taken=true;Office.GiveBadge(2,Office.Plan.Rooms[Office.Plan.TargetRoom].Department);
            Office.ReportAction(Office.Game.Player.transform.position,24,null);
            Office.Game.Notify("SUPERVISOR KEYCARD / clearance 02 acquired.");gameObject.SetActive(false);
        }
    }
}

