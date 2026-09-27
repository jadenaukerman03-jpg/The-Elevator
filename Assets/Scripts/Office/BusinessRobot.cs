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
        Transform leftOptic,rightOptic;
        int identity;
        public OfficeTask Activity;
        public bool Seated;
        public float Speed;
        public Vector3 LookTarget;
        public void Build(OfficeArt a,int variant,bool supervisor)
        {
            identity=variant;
            Material cloth=variant%3==0?a.Dark:variant%3==1?a.Upholstery:a.Carpet;
            pelvis=a.Group(transform,"Pelvis",new Vector3(0,.94f,0));
            Profile(a,pelvis,"Tailored jacket",new[]{0f,.12f,.34f,.49f,.55f},new[]{.21f,.18f,.23f,.25f,.19f},new[]{.13f,.12f,.14f,.125f,.11f},cloth);
            a.Box(pelvis,"Shirt front",new Vector3(0,.34f,.135f),new Vector3(.21f,.36f,.045f),a.Paper);
            a.Box(pelvis,"Tie",new Vector3(0,.31f,.167f),new Vector3(.052f,.31f,.027f),supervisor?a.Red:a.Brass);
            a.Box(pelvis,"Tie knot",new Vector3(0,.49f,.17f),new Vector3(.065f,.063f,.04f),supervisor?a.Red:a.Brass);
            for(int side=-1;side<=1;side+=2)
            {
                GameObject lapel=a.Box(pelvis,"Structured jacket lapel",new Vector3(side*.106f,.32f,.172f),new Vector3(.072f,.37f,.038f),cloth);
                lapel.transform.localRotation=Quaternion.Euler(0,0,side*-19);
                a.Box(pelvis,"Jacket welt pocket",new Vector3(side*.15f,.13f,.142f),new Vector3(.085f,.02f,.025f),a.Metal);
            }
            a.Box(pelvis,"Employee badge",new Vector3(-.15f,.39f,.177f),new Vector3(.07f,.10f,.022f),a.Paper);
            a.Box(pelvis,"Badge clearance stripe",new Vector3(-.15f,.364f,.191f),new Vector3(.058f,.018f,.006f),supervisor?a.Red:a.Screen);
            for(int i=0;i<2;i++) a.Round(pelvis,"Jacket button",new Vector3(.027f,.1f+i*.10f,.159f),new Vector3(.018f,.018f,.012f),a.Brass,PrimitiveType.Sphere);
            a.Round(pelvis,"Segmented mechanical neck",new Vector3(0,.6f,0),new Vector3(.115f,.09f,.115f),a.Metal);
            head=a.Group(pelvis,"Head gimbal",new Vector3(0,.63f,0));
            Profile(a,head,"Humanlike cranial shell",new[]{0f,.05f,.18f,.28f,.31f},new[]{.075f,.10f,.112f,.105f,.066f},new[]{.072f,.10f,.095f,.08f,.055f},variant%2==0?a.Plastic:a.Metal);
            a.Box(head,"Recessed faceplate",new Vector3(0,.16f,.093f),new Vector3(.172f,.135f,.034f),a.Dark);
            for(int s=-1;s<=1;s+=2)
            {
                a.Box(head,"Optic aperture",new Vector3(s*.044f,.179f,.116f),new Vector3(.038f,.018f,.012f),supervisor?a.WarmLight:a.CoolLight);
                a.Round(head,"Auditory port",new Vector3(s*.114f,.17f,0),new Vector3(.025f,.06f,.065f),a.Dark,PrimitiveType.Sphere);
            }
            a.Box(head,"Nose bridge",new Vector3(0,.138f,.126f),new Vector3(.025f,.052f,.045f),a.Metal);
            a.Box(head,"Speech grille",new Vector3(0,.065f,.102f),new Vector3(.066f,.013f,.014f),a.Dark);
            if(variant%3==0) a.Box(head,"Synthetic swept crown",new Vector3(0,.29f,-.015f),new Vector3(.205f,.055f,.14f),a.Dark);
            if(variant%3==1) a.Box(head,"Optical brow attachment",new Vector3(0,.218f,.114f),new Vector3(.19f,.027f,.037f),a.Brass);
            for(int side=-1;side<=1;side+=2)
            {
                a.Box(pelvis,"Shirt collar",new Vector3(side*.055f,.50f,.15f),new Vector3(.062f,.07f,.033f),a.Paper).transform.localRotation=Quaternion.Euler(0,0,side*25);
                a.Box(head,"Cheek articulation seam",new Vector3(side*.074f,.087f,.105f),new Vector3(.033f,.006f,.012f),a.Metal);
                for(int screw=0;screw<2;screw++)a.Round(head,"Faceplate screw",new Vector3(side*.077f,.115f+screw*.083f,.115f),new Vector3(.009f,.009f,.006f),a.Brass,PrimitiveType.Sphere);
                a.Box(pelvis,"Jacket pocket flap",new Vector3(side*.15f,.105f,.146f),new Vector3(.091f,.037f,.014f),cloth);
            }
            for(int vent=0;vent<5;vent++)a.Box(head,"Vocal grille slot",new Vector3(-.026f+vent*.013f,.064f,.112f),new Vector3(.005f,.018f,.004f),a.Metal);
            a.Box(pelvis,"Pocket square",new Vector3(.15f,.38f,.15f),new Vector3(.06f,.042f,.018f),a.Paper).transform.localRotation=Quaternion.Euler(0,0,12);
            a.Box(pelvis,"Badge clip",new Vector3(-.15f,.451f,.178f),new Vector3(.022f,.038f,.015f),a.Metal);
            a.Box(pelvis,"Badge portrait",new Vector3(-.168f,.401f,.191f),new Vector3(.024f,.03f,.006f),a.Dark);
            for(int i=0;i<4;i++)a.Box(pelvis,"Badge ID barcode",new Vector3(-.17f+i*.013f,.381f,.192f),new Vector3(.004f,.009f,.004f),a.Dark);
            foreach(Transform part in head.GetComponentsInChildren<Transform>())if(part.name=="Optic aperture"){if(part.localPosition.x<0)leftOptic=part;else rightOptic=part;}
            leftArm=Arm(a,pelvis,-1,cloth,out leftForearm); rightArm=Arm(a,pelvis,1,cloth,out rightForearm);
            leftThigh=Leg(a,pelvis,-1,cloth,out leftShin); rightThigh=Leg(a,pelvis,1,cloth,out rightShin);
            float height=1+(variant%5-2)*.025f; transform.localScale=new Vector3(1+(variant%3-1)*.055f,height,1);
        }
        Transform Arm(OfficeArt a,Transform root,int side,Material cloth,out Transform forearm)
        {
            Transform upper=a.Group(root,"Shoulder",new Vector3(side*.255f,.45f,0));
            Transform sleeve=a.Group(upper,"Upper sleeve",new Vector3(0,-.29f,0));
            Profile(a,sleeve,"Jacket sleeve",new[]{0f,.14f,.29f},new[]{.060f,.074f,.085f},new[]{.06f,.075f,.075f},cloth);
            forearm=a.Group(upper,"Elbow",new Vector3(0,-.29f,0));
            a.Round(forearm,"Elbow bearing",Vector3.zero,new Vector3(.10f,.1f,.1f),a.Metal,PrimitiveType.Sphere);
            Transform lower=a.Group(forearm,"Lower sleeve",new Vector3(0,-.27f,0));
            Profile(a,lower,"Forearm sleeve",new[]{0f,.14f,.27f},new[]{.043f,.058f,.061f},new[]{.042f,.053f,.06f},cloth);
            a.Box(forearm,"Visible shirt cuff",new Vector3(0,-.26f,0),new Vector3(.091f,.053f,.09f),a.Paper);
            Transform hand=a.Group(forearm,"Hand",new Vector3(0,-.31f,0));
            a.Box(hand,"Articulated palm",new Vector3(0,-.025f,.005f),new Vector3(.08f,.09f,.043f),a.Metal);
            for(int finger=0;finger<4;finger++)
            {
                a.Box(hand,"Finger proximal",new Vector3(-.03f+finger*.02f,-.093f,.012f),new Vector3(.014f,.054f,.02f),a.Plastic);
                a.Box(hand,"Finger distal",new Vector3(-.03f+finger*.02f,-.132f,.025f),new Vector3(.013f,.034f,.018f),a.Metal);
            }
            a.Box(hand,"Opposed thumb",new Vector3(side*.049f,-.037f,.033f),new Vector3(.025f,.06f,.026f),a.Plastic);
            for(int button=0;button<3;button++)a.Round(forearm,"Sleeve cuff button",new Vector3(side*.049f,-.22f+button*.026f,.025f),new Vector3(.012f,.012f,.012f),a.Brass,PrimitiveType.Sphere);
            a.Box(forearm,"Cufflink",new Vector3(side*.05f,-.27f,0),new Vector3(.013f,.022f,.025f),a.Brass);
            return upper;
        }
        Transform Leg(OfficeArt a,Transform root,int side,Material cloth,out Transform shin)
        {
            Transform thigh=a.Group(root,"Hip",new Vector3(side*.108f,0,0));
            Transform trouser=a.Group(thigh,"Trouser thigh",new Vector3(0,-.43f,0));
            Profile(a,trouser,"Trouser upper",new[]{0f,.22f,.43f},new[]{.063f,.087f,.103f},new[]{.068f,.089f,.098f},cloth);
            shin=a.Group(thigh,"Knee",new Vector3(0,-.43f,0));
            a.Round(shin,"Knee bearing",Vector3.zero,new Vector3(.11f,.11f,.11f),a.Metal,PrimitiveType.Sphere);
            Transform lower=a.Group(shin,"Trouser calf",new Vector3(0,-.4f,0));
            Profile(a,lower,"Trouser lower",new[]{0f,.22f,.4f},new[]{.058f,.073f,.063f},new[]{.067f,.08f,.067f},cloth);
            a.Box(shin,"Dress shoe",new Vector3(0,-.45f,.062f),new Vector3(.14f,.10f,.285f),a.Dark);
            a.Box(shin,"Shoe welt",new Vector3(0,-.494f,.063f),new Vector3(.145f,.018f,.285f),a.Wood);
            for(int lace=0;lace<4;lace++)a.Box(shin,"Shoe lace",new Vector3(0,-.394f,.041f+lace*.022f),new Vector3(.072f,.008f,.009f),a.Metal);
            a.Box(shin,"Toe cap seam",new Vector3(0,-.399f,.16f),new Vector3(.12f,.006f,.008f),a.Wood);
            return thigh;
        }
        static void Profile(OfficeArt a,Transform parent,string name,float[] y,float[] x,float[] z,Material mat)
        {
            if(!meshes.TryGetValue(name,out Mesh mesh))
            {
                List<Vector3> vertices=new List<Vector3>(); List<int> triangles=new List<int>(); const int sides=16;
                for(int ring=0;ring<y.Length;ring++) for(int s=0;s<sides;s++) { float angle=s*Mathf.PI*2/sides; vertices.Add(new Vector3(Mathf.Cos(angle)*x[ring],y[ring],Mathf.Sin(angle)*z[ring])); }
                for(int ring=0;ring<y.Length-1;ring++) for(int s=0;s<sides;s++)
                { int i=ring*sides+s,j=ring*sides+(s+1)%sides; triangles.AddRange(new[]{i,i+sides,j,j,i+sides,j+sides}); }
                for(int s=1;s<sides-1;s++) { triangles.AddRange(new[]{0,s,s+1}); int top=(y.Length-1)*sides; triangles.AddRange(new[]{top,top+s+1,top+s}); }
                mesh=new Mesh{name=name}; mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(name,mesh);
            }
            GameObject go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        public void Animate(float dt)
        {
            if(!pelvis) return;
            float blink=Application.isPlaying && (Time.time+identity*.71f)%4.7f<.12f?.15f:1;
            if(leftOptic)leftOptic.localScale=new Vector3(.038f,.018f*blink,.012f);
            if(rightOptic)rightOptic.localScale=new Vector3(.038f,.018f*blink,.012f);
            phase+=dt*Mathf.Max(.8f,Speed*4.2f); float walk=Mathf.Clamp01(Speed/1.6f),swing=Mathf.Sin(phase)*walk;
            poseBlend=Mathf.MoveTowards(poseBlend,Seated?1:0,dt*2.6f);
            pelvis.localPosition=new Vector3(0,.94f-poseBlend*.38f+Mathf.Abs(swing)*.018f,0);
            leftThigh.localRotation=Quaternion.Euler(Mathf.Lerp(swing*27,-85,poseBlend),0,0);
            rightThigh.localRotation=Quaternion.Euler(Mathf.Lerp(-swing*27,-85,poseBlend),0,0);
            leftShin.localRotation=Quaternion.Euler(Mathf.Lerp(Mathf.Max(0,-swing)*35,85,poseBlend),0,0);
            rightShin.localRotation=Quaternion.Euler(Mathf.Lerp(Mathf.Max(0,swing)*35,85,poseBlend),0,0);
            bool work=Speed<.15f; float arms=work&&(Activity==OfficeTask.Typing||Activity==OfficeTask.Reception)?-35:work&&Activity==OfficeTask.Coffee?-60:0;
            leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(arms-swing*21,0,4),dt*7);
            rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(arms+swing*21,0,-4),dt*7);
            float elbow=work?(Activity==OfficeTask.Typing||Activity==OfficeTask.Reception)?-65:Activity==OfficeTask.Coffee?-100:-20:-12;
            leftForearm.localRotation=Quaternion.Slerp(leftForearm.localRotation,Quaternion.Euler(elbow+Mathf.Sin(phase*6)*3,0,0),dt*7);
            rightForearm.localRotation=Quaternion.Slerp(rightForearm.localRotation,Quaternion.Euler(elbow+Mathf.Cos(phase*6)*3,0,0),dt*7);
            Vector3 toward=LookTarget==Vector3.zero?Vector3.forward:transform.InverseTransformDirection(LookTarget-head.position);
            float yaw=Mathf.Clamp(Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg,-55,55);
            head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(work&&Activity==OfficeTask.Typing?12:0,yaw,0),dt*3);
        }
    }
}
