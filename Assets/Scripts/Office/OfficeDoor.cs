using UnityEngine;
namespace TheElevator.Office
{
    // One single hinged door per doorway. It swings open into the passage and closes behind people.
    public sealed class OfficeDoor : MonoBehaviour
    {
        public OfficeFloor Office;
        public int RequiredClearance=2,Department,RoomA,RoomB;
        public Transform Hinge;
        public bool Unlocked,OpenForEmployees;
        public bool RequestedOpen { get; private set; }
        public float Opening { get { return opening; } }
        public string Prompt { get { return !Unlocked&&RequiredClearance>1?"E  SWIPE SUPERVISOR BADGE":RequestedOpen?"E  CLOSE DOOR":"E  OPEN DOOR"; } }
        const float SwingDegrees=95;
        float opening,autoUntil;
        OfficeEmployee passage;
        public void Use()
        {
            if(!Office.Game)return;
            if(!Unlocked&&RequiredClearance>1&&!Office.HasAccess(RequiredClearance,Department))
            {Office.Game.Sound.Play(120,.16f,.13f);Office.Game.Notify("ACCESS DENIED / supervisor keycard required.");Office.ReportAction(Office.Game.Player.transform.position,4,null);return;}
            Unlocked=true;RequestedOpen=!RequestedOpen;
            Office.Game.Sound.Play(760,.12f,.09f);
        }
        public bool RequestPass(OfficeEmployee employee)
        {
            if(RequiredClearance>1&&!Unlocked&&employee.Clearance<RequiredClearance)return false;
            if(passage&&passage!=employee&&Time.time<autoUntil&&Vector3.Distance(passage.transform.position,transform.position)<2.8f)return false;
            passage=employee;autoUntil=Time.time+2;OpenForEmployees=true;
            return opening>.85f;
        }
        bool Occupied()
        {
            foreach(Collider col in Physics.OverlapBox(transform.TransformPoint(new Vector3(0,1.05f,0)),new Vector3(1.30f,1.05f,1.1f),transform.rotation,~0,QueryTriggerInteraction.Ignore))
                if(col.GetComponentInParent<WorkerController>()||col.GetComponentInParent<OfficeEmployee>()||col.attachedRigidbody)return true;
            return false;
        }
        void Update()
        {
            if(!Office.Game||!Office.Game.ControlsActive)return;
            OpenForEmployees=Time.time<autoUntil;
            bool open=RequestedOpen||OpenForEmployees||(opening>.15f&&Occupied());
            opening=Mathf.MoveTowards(opening,open?1:0,Time.deltaTime*1.6f);PositionLeaf();
        }
        void PositionLeaf(){Hinge.localRotation=Quaternion.Euler(0,-SwingDegrees*Mathf.SmoothStep(0,1,opening),0);}
        public void SetOpenForValidation(bool open){opening=open?1:0;PositionLeaf();}
    }
}
