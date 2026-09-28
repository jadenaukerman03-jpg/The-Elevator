using UnityEngine;
namespace TheElevator.Office
{
    public sealed class OfficeTaskPoint : MonoBehaviour
    {
        public int RoomId;
        public OfficeTask Activity;
        public bool Seated;
        public OfficeEmployee Occupant;
        public bool ReservedByPlayer;
        public bool EquipmentMissing;
        public OfficeEmployee HomeOwner;
        public OfficeEquipment Equipment;
        public OfficeCoffeeStation Coffee;
        public bool Available { get { return !Occupant && !HomeOwner && !ReservedByPlayer; } }
    }
}


