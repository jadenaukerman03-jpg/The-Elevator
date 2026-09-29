using UnityEngine;
using TheElevator.Office;
namespace TheElevator
{
    // First-person hands are the same one-piece mitten the bodies wear, without arms. On pickup they reach from the
    // relaxed pose out to the object, wrap around its near edge, and ride along with it as it comes in.
    public sealed class FirstPersonHands : MonoBehaviour
    {
        sealed class Hand
        {
            public Transform Root;
            public Mesh Mesh;
            public Vector3 Bend = BeanRig.RelaxedHand;
        }
        const float PalmHalfThickness = .042f, FingerHinge = .055f;
        // Palm flat on the side face, fingers straight to the near edge, then folded over it.
        static readonly Vector3 WrapHand = new Vector3(6, 88, 45);
        Hand left,right;
        Material skin,sleeve;
        WorkerController player;
        float reach=1,releaseMotion;
        int outfitVersion=-1;
        SalvageItem previous;
        public string GripName { get; private set; }
        public float MaxContactError { get; private set; }
        public void Initialize(WorkerController owner)
        {
            player=owner;
            skin=new Material(Shader.Find("Elevator/ViewmodelSkin")){name="First-person hand"};
            sleeve=new Material(skin){name="First-person cuff"};
            left=Build(-1);right=Build(1);
        }
        Hand Build(int side)
        {
            Hand hand=new Hand();
            hand.Root=new GameObject(side<0?"Left hand":"Right hand").transform;hand.Root.SetParent(transform,false);
            hand.Mesh=new Mesh{name="First-person mitten"};hand.Mesh.MarkDynamic();BeanRig.Mitten(hand.Mesh,hand.Bend,side);
            Part(hand.Root,"First-person mitten",hand.Mesh,Vector3.zero,Vector3.one,skin);
            Part(hand.Root,"Sleeve cuff",BeanRig.Torus(.05f,.018f),new Vector3(0,-.082f,0),new Vector3(1.15f,1,.8f),sleeve);
            return hand;
        }
        static Renderer Part(Transform parent,string name,Mesh mesh,Vector3 position,Vector3 scale,Material material)
        {
            GameObject go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;MeshRenderer renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return renderer;
        }
        public const string ExtinguisherGrip="CANISTER AND NOZZLE";
        public static string GripFor(SalvageItem item)
        {
            if(item.IsBattery)return "BATTERY CRADLE";
            if(item.GetComponent<FireExtinguisher>())return ExtinguisherGrip;
            string name=item.Title.ToLowerInvariant();
            if(name.Contains("computer")||name.Contains("terminal"))return "MONITOR SIDE GRIP";
            if(name.Contains("award")||name.Contains("artifact"))return "PEDESTAL SUPPORT";
            if(name.Contains("data"))return "SMALL DEVICE PINCH";
            if(name.Contains("case"))return "CASE EDGE GRIP";
            return "TWO HAND SUPPORT";
        }
        public void Present(SalvageItem held,bool visible,float dt)
        {
            gameObject.SetActive(visible);if(!visible)return;
            if(outfitVersion!=player.Model.Version){outfitVersion=player.Model.Version;skin.color=player.Model.HandColor;sleeve.color=player.Model.SleeveColor;}
            if(held!=previous){if(previous&&!held)releaseMotion=1;reach=held?0:1;previous=held;}
            reach=Mathf.MoveTowards(reach,1,dt*4.5f);releaseMotion=Mathf.MoveTowards(releaseMotion,0,dt*4);
            BoxCollider shape=held?held.GetComponent<BoxCollider>():null;
            GripName=held?GripFor(held):"RELAXED";MaxContactError=0;
            Pose(left,-1,held,shape);Pose(right,1,held,shape);
        }
        void Pose(Hand hand,int side,SalvageItem held,BoxCollider shape)
        {
            Idle(side,out Vector3 position,out Quaternion rotation);
            Vector3 bend=BeanRig.RelaxedHand;
            if(held&&shape)
            {
                Vector3 gripPosition;Quaternion gripRotation;
                FireExtinguisher extinguisher=GripName==ExtinguisherGrip&&side>0?held.GetComponent<FireExtinguisher>():null;
                if(extinguisher)NozzleGrip(extinguisher.Nozzle,out gripPosition,out gripRotation);
                else Grip(side,held,shape,out gripPosition,out gripRotation);
                float t=Mathf.SmoothStep(0,1,reach);
                position=Vector3.Lerp(position,gripPosition,t);rotation=Quaternion.Slerp(rotation,gripRotation,t);
                bend=Vector3.Lerp(BeanRig.RelaxedHand,extinguisher?BeanRig.GripHand:WrapHand,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,reach)));
            }
            hand.Root.SetPositionAndRotation(position,rotation);
            if((bend-hand.Bend).sqrMagnitude>.25f){hand.Bend=bend;BeanRig.Mitten(hand.Mesh,bend,side);}
        }
        void Idle(int side,out Vector3 position,out Quaternion rotation)
        {
            float speed=player.MotionSpeed;
            float stride=Time.time*(player.MovingFast?9f:6.5f)+side*1.57f;
            float motion=Mathf.Clamp01(speed/4f);
            float sway=Mathf.Sin(Time.time*1.7f)*.005f+Mathf.Sin(stride)*motion*(player.MovingFast?.04f:.018f);
            position=player.View.transform.TransformPoint(new Vector3(side*.22f,-.27f+sway+releaseMotion*.07f,.46f+releaseMotion*.10f+Mathf.Cos(stride)*motion*.02f));
            rotation=player.View.transform.rotation*Quaternion.Euler(35+Mathf.Cos(stride)*motion*10,side*-12,side*-10);
        }
        // The palm presses the collider's side face; the finger block folds at its middle hinge over the near edge.
        void Grip(int side,SalvageItem item,BoxCollider box,out Vector3 position,out Quaternion rotation)
        {
            Vector3 extent=box.size*.5f,center=box.center;
            // The monitor's physics box includes its deep stand. Grip the thin display shell itself.
            if(GripName=="MONITOR SIDE GRIP" && item.GetComponent<OfficeEquipment>())
            {extent=new Vector3(.325f,.201f,.021f);center=new Vector3(0,.37f,0);}
            float y=GripName==ExtinguisherGrip?extent.y*.2f:GripName=="BATTERY CRADLE"?-extent.y*.35f:GripName=="PEDESTAL SUPPORT"?-extent.y*.65f:GripName=="SMALL DEVICE PINCH"?0:extent.y*.05f;
            Transform t=item.transform;
            rotation=Quaternion.LookRotation(t.TransformDirection(new Vector3(-side,0,0)),t.TransformDirection(Vector3.back));
            position=t.TransformPoint(center+new Vector3(side*(extent.x+PalmHalfThickness),y,-extent.z+FingerHinge));
            // Contact check: the fold line must sit exactly on the collider's near vertical edge.
            Vector3 hinge=t.InverseTransformPoint(position+rotation*new Vector3(0,FingerHinge,PalmHalfThickness))-center;
            MaxContactError=Mathf.Max(MaxContactError,Mathf.Abs(hinge.z+extent.z)+Mathf.Abs(Mathf.Abs(hinge.x)-extent.x));
        }
        // Right hand on the nozzle like holding a torch: the wrist comes from behind, the palm lies along the
        // nozzle's right side and the fingers curl over it toward the horn.
        static void NozzleGrip(Transform nozzle,out Vector3 position,out Quaternion rotation)
        {
            rotation=Quaternion.LookRotation(nozzle.TransformDirection(Vector3.left),nozzle.TransformDirection(new Vector3(0,.3f,1)));
            position=nozzle.TransformPoint(new Vector3(.016f+PalmHalfThickness,-.01f,-.075f));
        }
        void OnDestroy()
        {
            foreach(Hand hand in new[]{left,right})if(hand!=null&&hand.Mesh)Destroy(hand.Mesh);
            if(skin)Destroy(skin);if(sleeve)Destroy(sleeve);
        }
    }
}
