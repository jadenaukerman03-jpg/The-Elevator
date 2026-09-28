using System.Collections.Generic;
using UnityEngine;

namespace TheElevator.Office
{
    // Original articulated mesh prototype. This is not a purchased/skinned production character.
    public sealed class BusinessRobot : MonoBehaviour
    {
        static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        Transform pelvis,head,leftArm,rightArm,leftForearm,rightForearm,leftThigh,rightThigh,leftShin,rightShin;
        float phase,poseBlend;
        Transform leftAnkle,rightAnkle;
        Transform leftOptic,rightOptic;
        public Transform RightHand { get; private set; }
        public Transform Head { get { return head; } }
        public Vector3 ReachTarget;
        public bool Reaching;
        public bool Talking;
        int identity;
        public OfficeTask Activity;
        public bool Seated;
        public float Speed;
        public Vector3 LookTarget;
        public void Build(OfficeArt a,int variant,bool supervisor)
        {
            identity=variant;Material cloth=a.DepartmentAccents[variant%a.DepartmentAccents.Length];
            pelvis=a.Group(transform,"Padded corporate jacket",new Vector3(0,.94f,0));
            a.Box(pelvis,"Rounded jacket",new Vector3(0,.22f,0),new Vector3(.66f,.70f,.42f),cloth);
            a.Box(pelvis,"Broad shirt bib",new Vector3(0,.30f,.215f),new Vector3(.26f,.32f,.04f),a.Paper);
            a.Box(pelvis,"Soft tie",new Vector3(.02f,.25f,.25f),new Vector3(.08f,.26f,.06f),a.Brass).transform.localRotation=Quaternion.Euler(0,0,-8);
            a.Box(pelvis,"Employee badge",new Vector3(-.23f,.3f,.245f),new Vector3(.17f,.22f,.055f),supervisor?a.Red:a.Paper);
            head=a.Group(pelvis,"Head",new Vector3(0,.70f,0));
            a.Round(head,"Spherical porcelain shell",Vector3.zero,Vector3.one*.60f,a.Skin,PrimitiveType.Sphere);
            a.Box(head,"Friendly faceplate",new Vector3(0,0,.30f),new Vector3(.45f,.26f,.055f),a.Dark);
            leftOptic=a.Box(head,"Left optic",new Vector3(-.10f,.005f,.333f),new Vector3(.065f,.095f,.025f),a.Brass).transform;
            rightOptic=a.Box(head,"Right optic",new Vector3(.10f,.025f,.333f),new Vector3(.065f,.095f,.025f),a.Brass).transform;
            a.Box(head,"Service tab",new Vector3(.30f,0,0),new Vector3(.075f,.15f,.18f),cloth);
            leftArm=Arm(a,pelvis,-1,cloth,out leftForearm);rightArm=Arm(a,pelvis,1,cloth,out rightForearm);
            leftThigh=Leg(a,pelvis,-1,cloth,out leftShin);rightThigh=Leg(a,pelvis,1,cloth,out rightShin);
        }
        Transform Arm(OfficeArt a,Transform root,int side,Material cloth,out Transform forearm)
        {
            var upper=a.Group(root,"Shoulder",new Vector3(side*.35f,.44f,0));
            a.Box(upper,"Puffy upper sleeve",new Vector3(0,-.145f,0),new Vector3(.22f,.32f,.25f),cloth);
            forearm=a.Group(upper,"Elbow",new Vector3(0,-.29f,0));
            a.Box(forearm,"Puffy forearm",new Vector3(0,-.135f,0),new Vector3(.20f,.30f,.23f),cloth);
            var hand=a.Group(forearm,"Hand",new Vector3(0,-.31f,0));if(side>0)RightHand=hand;
            a.Box(hand,"Soft palm",new Vector3(0,-.02f,0),new Vector3(.16f,.15f,.13f),a.Skin);
            for(int finger=0;finger<4;finger++)a.Box(hand,"Rounded finger",new Vector3(-.057f+finger*.038f,-.10f,.025f),new Vector3(.036f,.105f,.055f),a.Skin);
            a.Box(hand,"Thumb",new Vector3(-side*.083f,-.035f,.035f),new Vector3(.06f,.11f,.07f),a.Skin);
            return upper;
        }
        Transform Leg(OfficeArt a,Transform root,int side,Material cloth,out Transform shin)
        {
            var thigh=a.Group(root,"Hip",new Vector3(side*.19f,0,0));
            a.Box(thigh,"Trouser thigh",new Vector3(0,-.205f,0),new Vector3(.27f,.43f,.29f),cloth);
            shin=a.Group(thigh,"Knee",new Vector3(0,-.43f,0));
            a.Box(shin,"Trouser calf",new Vector3(0,-.19f,0),new Vector3(.24f,.40f,.27f),cloth);
            var ankle=a.Group(shin,"Ankle",new Vector3(0,-.40f,0));if(side<0)leftAnkle=ankle;else rightAnkle=ankle;
            a.Box(ankle,"Soft shoe",new Vector3(0,-.025f,.06f),new Vector3(.29f,.16f,.42f),a.Dark);return thigh;
        }        void ReachRightHand(Vector3 target)
        {
            Vector3 start=rightArm.position,delta=target-start;
            float upper=.29f*transform.lossyScale.y,lower=.34f*transform.lossyScale.y;
            float distance=Mathf.Clamp(delta.magnitude,.04f,upper+lower-.005f);
            Vector3 direction=delta.normalized;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            Vector3 pole=Vector3.ProjectOnPlane(transform.right*.8f-transform.up,direction).normalized;
            Vector3 elbow=start+direction*along+pole*height;
            rightArm.rotation=Quaternion.FromToRotation(Vector3.down,elbow-start);
            rightForearm.rotation=Quaternion.FromToRotation(Vector3.down,target-elbow);
        }        public void Animate(float dt)
        {
            if(!pelvis) return;
            float blink=Application.isPlaying && (Time.time+identity*.71f)%4.7f<.12f?.15f:1;
            if(leftOptic)leftOptic.localScale=new Vector3(1,blink,1);
            if(rightOptic)rightOptic.localScale=new Vector3(1,blink,1);
            phase+=dt*Mathf.Max(.8f,Speed*4.2f); float walk=Mathf.Clamp01(Speed/1.6f),swing=Mathf.Sin(phase)*walk;
            poseBlend=Mathf.MoveTowards(poseBlend,Seated?1:0,dt*2.6f);
            pelvis.localPosition=new Vector3(0,.94f-poseBlend*.38f+Mathf.Abs(swing)*.018f,0);
            leftThigh.localRotation=Quaternion.Euler(Mathf.Lerp(swing*27,-85,poseBlend),0,0);
            rightThigh.localRotation=Quaternion.Euler(Mathf.Lerp(-swing*27,-85,poseBlend),0,0);
            leftShin.localRotation=Quaternion.Euler(Mathf.Lerp(Mathf.Max(0,-swing)*35,85,poseBlend),0,0);
            rightShin.localRotation=Quaternion.Euler(Mathf.Lerp(Mathf.Max(0,swing)*35,85,poseBlend),0,0);
            if(leftAnkle&&rightAnkle)
            {
                Quaternion flat=Quaternion.Euler(0,transform.eulerAngles.y,0);leftAnkle.rotation=flat;rightAnkle.rotation=flat;
                if(poseBlend<.05f&&walk>.05f)
                {
                    float sole=Mathf.Min(leftAnkle.position.y,rightAnkle.position.y)-.105f*transform.lossyScale.y;
                    Vector3 position=pelvis.position;position.y-=Mathf.Clamp(sole-transform.position.y,-.04f,.10f);pelvis.position=position;
                }
            }
            bool work=Speed<.15f; float arms=work&&(Activity==OfficeTask.Typing||Activity==OfficeTask.Reception)?-35:work&&Activity==OfficeTask.Coffee?-60:0;
            leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(arms-swing*21,0,4),dt*7);
            rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(arms+swing*21,0,-4),dt*7);
            float elbow=work?(Activity==OfficeTask.Typing||Activity==OfficeTask.Reception)?-65:Activity==OfficeTask.Coffee?-100:-20:-12;
            leftForearm.localRotation=Quaternion.Slerp(leftForearm.localRotation,Quaternion.Euler(elbow+Mathf.Sin(phase*6)*3,0,0),dt*7);
            rightForearm.localRotation=Quaternion.Slerp(rightForearm.localRotation,Quaternion.Euler(elbow+Mathf.Cos(phase*6)*3,0,0),dt*7);
                        if(Talking&&work&&!Reaching)rightForearm.localRotation=Quaternion.Euler(-65+Mathf.Sin(phase*2)*12,0,-18);
            if(Reaching)ReachRightHand(ReachTarget);
            Vector3 toward=LookTarget==Vector3.zero?Vector3.forward:transform.InverseTransformDirection(LookTarget-head.position);
            float yaw=Mathf.Clamp(Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg,-55,55);
            head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(work&&Activity==OfficeTask.Typing?12:0,yaw,0),dt*3);
        }
    }
}
