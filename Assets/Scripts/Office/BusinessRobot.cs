using UnityEngine;

namespace TheElevator.Office
{
    // Office staff wear the shared bean rig, dressed for their job; this adapter keeps the employee-facing API.
    public sealed class BusinessRobot : MonoBehaviour
    {
        static readonly Color[] Jackets = {
            new Color(.20f,.30f,.52f), new Color(.14f,.47f,.50f), new Color(.47f,.25f,.45f), new Color(.82f,.60f,.22f)
        };
        static readonly Color[] Blouses = { new Color(.93f,.45f,.45f), new Color(.30f,.62f,.80f), new Color(.98f,.72f,.30f) };
        static readonly Color[] Cardigans = { new Color(.80f,.60f,.22f), new Color(.45f,.60f,.45f), new Color(.70f,.35f,.25f) };
        static readonly Color SupervisorJacket = new Color(.22f,.22f,.27f);
        static readonly Color SupervisorTie = new Color(.90f,.22f,.20f);
        static readonly Color Tie = new Color(1f,.80f,.28f);
        BeanRig rig;
        public Transform RightHand { get { return rig ? rig.RightHand : null; } }
        public Transform Head { get { return rig ? rig.Head : null; } }
        public Vector3 ReachTarget;
        public bool Reaching;
        public bool Talking;
        public OfficeTask Activity;
        public bool Seated;
        public float Speed;
        public Vector3 LookTarget;
        public Vector3 PointAt;
        public int Mood=1;
        public bool Crazed;
        public void HoldPointer()
        {
            if(!rig)return;
            Transform hand=rig.Hand(1);
            rig.Add(hand,"Pointer stick",BeanRig.Capsule(1.05f,.013f,.008f),new Vector3(0,-.07f,.02f),Vector3.one,new Color(.55f,.36f,.2f));
            rig.Add(hand,"Pointer tip",BeanRig.Sphere(),new Vector3(0,-1.12f,.02f),Vector3.one*.025f,BeanRig.FaceInk);
        }
        public void Build(OfficeArt a,int variant,bool supervisor,BeanOutfit outfit=BeanOutfit.Office)
        {
            BeanLook look=new BeanLook{Outfit=outfit,Skin=BeanRig.SkinColors[(variant*5+2)%BeanRig.SkinColors.Length],Eyes=variant%3,Mouth=variant%4,
                Brows=variant%4,Glasses=outfit==BeanOutfit.Clerk?1:0};
            switch(outfit)
            {
                case BeanOutfit.Reception:look.Primary=Blouses[variant%Blouses.Length];look.Accent=new Color(.20f,.21f,.25f);break;
                case BeanOutfit.Technician:look.Primary=new Color(.95f,.55f,.20f);look.Accent=new Color(.62f,.66f,.72f);break;
                case BeanOutfit.Clerk:look.Primary=Cardigans[variant%Cardigans.Length];look.Accent=new Color(.55f,.18f,.20f);break;
                case BeanOutfit.Security:look.Primary=new Color(.16f,.20f,.34f);look.Accent=new Color(.11f,.14f,.24f);break;
                default:look.Primary=supervisor?SupervisorJacket:Jackets[variant%Jackets.Length];look.Accent=supervisor?SupervisorTie:Tie;break;
            }
            rig=gameObject.AddComponent<BeanRig>();
            rig.Build(look,variant);
            float height=1+(variant%5-2)*.025f; transform.localScale=new Vector3(1+(variant%3-1)*.055f,height,1);
        }
        // Knocked out: the body goes limp and falls where physics takes it.
        public void Ragdoll(Vector3 impulse){if(rig)rig.Ragdoll(impulse);}
        public Rigidbody Core{get{return rig?rig.Core:null;}}
        public void Animate(float dt)
        {
            if(!rig) return;
            bool work=Speed<.15f;
            bool desk=work&&(Activity==OfficeTask.Typing||Activity==OfficeTask.Reception),coffee=work&&Activity==OfficeTask.Coffee;
            bool pointing=work&&Activity==OfficeTask.Present&&PointAt!=Vector3.zero;
            rig.SetMood(Mood);rig.SetCrazed(Crazed);
            rig.Animate(dt,new BeanPose{
                Speed=Speed,Seated=Seated,Talking=Talking,Typing=work&&Activity==OfficeTask.Typing,
                Reaching=Reaching||pointing,ReachTarget=pointing?PointAt:ReachTarget,LookTarget=LookTarget,
                ArmPitch=desk?-40:coffee?-60:0,ElbowBend=desk?-60:coffee?-95:-12});
        }
    }
}
