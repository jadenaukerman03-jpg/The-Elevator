using UnityEngine;
namespace TheElevator.Office
{
    public sealed class OfficeDoor : MonoBehaviour
    {
        public OfficeFloor Office;
        public int RequiredClearance=2,Department;
        public Transform Left,Right;
        public bool Unlocked;
        public bool OpenForEmployees;
        float opening;
        public void Use()
        {
            if(!Office.Game) return;
            if(Office.HasAccess(RequiredClearance,Department)) { Unlocked=true; Office.Game.Sound.Play(760,.12f,.09f); Office.Game.Notify("ACCESS GRANTED / badge retained for this floor."); }
            else { Office.Game.Sound.Play(120,.16f,.13f); Office.Game.Notify("ACCESS DENIED / department supervisor credential required."); Office.RaiseLocal(Department,4); }
        }
        void Update()
        {
            if(!Office.Game||Office.Game.Paused) return;
            bool occupied=Physics.CheckBox(transform.TransformPoint(new Vector3(0,1.1f,0)),new Vector3(1.1f,1,1.1f),transform.rotation,1<<2,QueryTriggerInteraction.Ignore);
            opening=Mathf.MoveTowards(opening,Unlocked||OpenForEmployees||occupied&&opening>.2f?1:0,Time.deltaTime*1.2f);
            Left.localPosition=new Vector3(-.69f-opening*1.35f,1.4f,0);Right.localPosition=new Vector3(.69f+opening*1.35f,1.4f,0);
        }
        public void SetOpenForValidation(bool open)
        { opening=open?1:0;Left.localPosition=new Vector3(-.69f-opening*1.35f,1.4f,0);Right.localPosition=new Vector3(.69f+opening*1.35f,1.4f,0); }
    }
}
