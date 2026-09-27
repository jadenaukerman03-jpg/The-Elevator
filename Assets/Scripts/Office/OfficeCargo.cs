using UnityEngine;
namespace TheElevator.Office
{
    public sealed class OfficeCargo : MonoBehaviour
    {
        public OfficeFloor Office;
        public SalvageItem Item;
        public bool Mandatory;
        public bool Mounted=true;
        public float Condition=1;
        public Vector3 SpawnPosition,LastSafePosition;
        public Quaternion SpawnRotation;
        float nextNoise,nextSafe,nextRecovery;
        public void Initialize(OfficeFloor office,SalvageItem item,bool mandatory)
        {
            Office=office;Item=item;Mandatory=mandatory;SpawnPosition=LastSafePosition=transform.position;SpawnRotation=transform.rotation;
            if(mandatory){Item.Body.isKinematic=true;Item.Body.maxAngularVelocity=2.5f;Item.Body.maxDepenetrationVelocity=2;}
        }
        public void ReleaseMount()
        { Mounted=false;Item.Body.isKinematic=false;Office.ReportAction(transform.position,18,null);Office.Game.Notify("Power disconnected. Dolly engaged. Walk slowly; Q releases the handles."); }
        void FixedUpdate()
        {
            if(!Mandatory||!Office||!Office.Game||Office.Game.Paused||Mounted)return;
            Rigidbody body=Item.Body;
            if(Office.Transported==this&&Office.Game.ControlsActive)
            {
                Vector3 target=Office.Game.Player.transform.position+Office.Game.Player.transform.forward*1.5f;
                target.y=body.position.y;
                Vector3 force=Vector3.ClampMagnitude((target-body.position)*14-body.linearVelocity*7,18);
                body.AddForce(force,ForceMode.Acceleration);
                Quaternion desired=Quaternion.Euler(0,Office.Game.Player.transform.eulerAngles.y,0);
                Quaternion error=desired*Quaternion.Inverse(body.rotation);error.ToAngleAxis(out float angle,out Vector3 axis);if(angle>180)angle-=360;
                body.AddTorque(Vector3.ClampMagnitude(axis*angle*.12f-body.angularVelocity*5,12),ForceMode.Acceleration);
                if(Vector3.Distance(body.position,Office.Game.Player.transform.position)>4)Office.Transported=null;
            }
            body.linearVelocity=Vector3.ClampMagnitude(body.linearVelocity,5);
            if(Time.time>nextSafe)
            {
                nextSafe=Time.time+1;
                Vector3 floorPoint=body.position-Vector3.up*(Office.Plan.TargetBounds.y*.5f);
                if(Vector3.Dot(transform.up,Vector3.up)>.9f&&body.linearVelocity.sqrMagnitude<.3f&&Physics.Raycast(floorPoint+Vector3.up*.1f,Vector3.down,.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))LastSafePosition=body.position;
            }
            // Automatic technical recovery only: no on-demand teleport and no recovery from ordinary door jams.
            if(transform.position.y<-5&&Time.time>nextRecovery)
            {
                Office.Transported=null;body.position=LastSafePosition;body.rotation=SpawnRotation;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;nextRecovery=Time.time+10;
                Office.Game.Notify("Cargo recovery: an out-of-world physics fault was corrected.");
            }
        }
        void OnCollisionEnter(Collision collision)
        {
            if(!Office||!Office.Game||collision.relativeVelocity.magnitude<1.7f||Time.time<nextNoise)return;
            nextNoise=Time.time+.7f;Condition=Mathf.Max(.25f,Condition-.025f*collision.relativeVelocity.magnitude);
            Item.ApplyCondition(Condition);
            Office.ReportAction(transform.position,12,null);Office.PlayTone(transform.position,70,.10f);
            if(Mandatory&&Office.Plan.TargetType==2)Office.SpillProduct(transform.position+Vector3.down*.4f);
        }
    }
}
