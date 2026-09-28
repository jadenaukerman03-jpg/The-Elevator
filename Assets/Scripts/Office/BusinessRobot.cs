using UnityEngine;

namespace TheElevator.Office
{
    // Office staff wear the shared bean rig in jacket and tie; this adapter keeps the employee-facing API.
    public sealed class BusinessRobot : MonoBehaviour
    {
        static readonly Color[] Jackets = {
            new Color(.20f,.30f,.52f), new Color(.14f,.47f,.50f), new Color(.47f,.25f,.45f), new Color(.82f,.60f,.22f)
        };
        static readonly Color SupervisorJacket = new Color(.22f,.22f,.27f);
        static readonly Color SupervisorTie = new Color(.90f,.22f,.20f);
        static readonly Color Tie = new Color(1f,.80f,.28f);
        BeanRig rig;
        public Transform RightHand { get { return rig ? rig.RightHand : null; } }
        public Transform Head { get { return rig ? rig.Head : null; } }
        public Color SkinColor { get; private set; }
        public Color JacketColor { get; private set; }
        public Vector3 ReachTarget;
        public bool Reaching;
        public bool Talking;
        public OfficeTask Activity;
        public bool Seated;
        public float Speed;
        public Vector3 LookTarget;
        public void Build(OfficeArt a,int variant,bool supervisor,Color? skin=null)
        {
            SkinColor=skin??BeanRig.SkinColors[(variant*5+2)%BeanRig.SkinColors.Length];
            JacketColor=supervisor?SupervisorJacket:Jackets[variant%Jackets.Length];
            rig=gameObject.AddComponent<BeanRig>();
            rig.Build(new BeanLook{Outfit=BeanOutfit.Office,Skin=SkinColor,Primary=JacketColor,Accent=supervisor?SupervisorTie:Tie,Eyes=variant%3,Mouth=variant%4},variant);
            float height=1+(variant%5-2)*.025f; transform.localScale=new Vector3(1+(variant%3-1)*.055f,height,1);
        }
        public void Animate(float dt)
        {
            if(!rig) return;
            bool work=Speed<.15f;
            bool desk=work&&(Activity==OfficeTask.Typing||Activity==OfficeTask.Reception),coffee=work&&Activity==OfficeTask.Coffee;
            rig.Animate(dt,new BeanPose{
                Speed=Speed,Seated=Seated,Talking=Talking,Typing=work&&Activity==OfficeTask.Typing,
                Reaching=Reaching,ReachTarget=ReachTarget,LookTarget=LookTarget,
                ArmPitch=desk?-40:coffee?-60:0,ElbowBend=desk?-60:coffee?-95:-12});
        }
    }
}
