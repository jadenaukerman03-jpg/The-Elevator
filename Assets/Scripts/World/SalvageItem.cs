using UnityEngine;

namespace TheElevator
{
    public sealed class SalvageItem : MonoBehaviour
    {
        public string Title { get; private set; }
        public float Mass { get; private set; }
        public int Value { get; private set; }
        int undamagedValue;
        public void ApplyCondition(float condition) { Value = Mathf.RoundToInt(undamagedValue * Mathf.Clamp(condition,0.25f,1)); }
        public bool IsBattery { get; private set; }
        public bool IsHeld { get; private set; }
        public Rigidbody Body { get; private set; }
        Collider shell;
        WorkerController carrier;
        float lastImpact,pickupTime;
        Vector3 pickupPosition;
        Quaternion pickupRotation;
        DescentGame game;

        public void Configure(DescentGame owner, string title, float mass, int value, bool battery, Vector3 size)
        {
            game = owner;
            Title = title;
            Mass = mass;
            Value = value;
            undamagedValue = value;
            IsBattery = battery;
            shell = gameObject.AddComponent<BoxCollider>();
            ((BoxCollider)shell).size = size;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = mass;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearDamping = 0.35f;
            Body.angularDamping = 0.65f;
            game.Items.Add(this);
        }

        public void PickUp(WorkerController worker)
        {
            var officeCargo=GetComponent<TheElevator.Office.OfficeCargo>();
            if(!IsHeld&&officeCargo&&officeCargo.Office)officeCargo.Office.WitnessTheft(officeCargo);
            pickupTime=Time.time;pickupPosition=transform.position;pickupRotation=transform.rotation;
            carrier = worker;
            IsHeld = true;
            if(!Body.isKinematic)Body.linearVelocity = Vector3.zero;
            if(!Body.isKinematic)Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;
            shell.enabled = false;
        }

        // Thrown items hurt the first employee they hit (10 damage) and make them furious.
        float thrownUntil;
        bool thrown;
        void FixedUpdate()
        {
            if (!IsHeld && thrown)
            {
                if (Time.time > thrownUntil || Body.linearVelocity.magnitude < 2.5f || !game || !game.CurrentOffice) thrown = false;
                else
                {
                    BoxCollider bounds = shell as BoxCollider;
                    float reach = bounds ? Mathf.Min(.25f, bounds.size.magnitude * .3f) : .15f;
                    TheElevator.Office.OfficeEmployee struck = game.CurrentOffice.EmployeeAt(Body.worldCenterOfMass, reach);
                    if (struck) { thrown = false; struck.HitByThrown(Body.worldCenterOfMass); }
                }
            }
            if (!IsHeld || !carrier) return;
            Vector3 chest=carrier.View.transform.position;
            Vector3 forward=carrier.View.transform.forward;
            BoxCollider box=shell as BoxCollider;
            float halfDepth=box?box.size.z*.5f:.25f;
            float distance=Mathf.Max(.72f,halfDepth+.55f)-carrier.ThrowCharge*.13f;
            Vector3 origin=chest-carrier.View.transform.up*.26f;
            if(Physics.SphereCast(origin,.16f,forward,out RaycastHit hit,distance+halfDepth,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))distance=Mathf.Max(.28f,hit.distance-halfDepth-.08f);
            Vector3 heldCenter=box?box.center:Vector3.zero;
            Quaternion rotation=carrier.View.transform.rotation;
            string grip=FirstPersonHands.GripFor(this);
            if(grip=="BATTERY CRADLE")rotation*=Quaternion.Euler(0,0,18);
            else if(grip=="PEDESTAL SUPPORT")rotation*=Quaternion.Euler(-8,0,0);
            else if(grip=="SMALL DEVICE PINCH")rotation*=Quaternion.Euler(12,0,-8);
            float duration=grip=="SMALL DEVICE PINCH"?.22f:grip=="BATTERY CRADLE"?.44f:.34f;
            float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-pickupTime)/duration));
            Vector3 target=origin+forward*distance;
            if(grip==FirstPersonHands.ExtinguisherGrip)
            {
                // Carried low on the left by its body; the right hand is free for the nozzle.
                Transform view=carrier.View.transform;
                target=view.TransformPoint(new Vector3(-.25f,-.37f,Mathf.Min(.52f,distance)));
                rotation=view.rotation*Quaternion.Euler(-6,12,0);
            }
            Quaternion pose=Quaternion.Slerp(pickupRotation,rotation,blend);
            Body.MovePosition(Vector3.Lerp(pickupPosition,target-pose*heldCenter,blend));
            Body.MoveRotation(pose);
        }
        public static float ThrowSpeed(float mass,float charge)
        {return Mathf.Lerp(2.2f,10f,Mathf.Clamp01(charge))/Mathf.Max(1,Mathf.Sqrt(Mathf.Max(0,mass)/6));}
        public void Release(bool toss){Release(toss,toss?.5f:0);}
        public void Release(bool toss,float charge)
        {
            if(!IsHeld)return;
            Vector3 direction=carrier.View?carrier.View.transform.forward:carrier.transform.forward;
            Vector3 velocity=direction*(toss?ThrowSpeed(Mass,charge):.6f)+Vector3.up*(toss?Mathf.Lerp(.3f,1.3f,Mathf.Clamp01(charge)):.1f);
            IsHeld=false;carrier=null;shell.enabled=true;Body.isKinematic=false;Body.linearVelocity=velocity;
            if(toss){thrown=true;thrownUntil=Time.time+3;}
        }
        void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < 2.8f || Time.time - lastImpact < 0.5f || !game) return;
            lastImpact = Time.time;
            game.NoiseAt(transform.position, 0.65f);
            game.Sound.Play(110, 0.065f, 0.1f);
        }

        void OnDestroy() { if (game) game.Items.Remove(this); }
    }
}





