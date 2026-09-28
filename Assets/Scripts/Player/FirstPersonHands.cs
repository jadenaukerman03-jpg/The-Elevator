using UnityEngine;
using TheElevator.Office;
namespace TheElevator
{
    // Viewmodel hands use articulated finger segments and box-surface contacts for authored pickups.
    public sealed class FirstPersonHands : MonoBehaviour
    {
        sealed class Hand
        {
            public Transform Root;
            public TheElevator.SoftOffice.SoftGlove Glove;
            public readonly Mesh[] Fingers=new Mesh[5];
            public readonly Transform[] Nails=new Transform[5];
        }
        Hand left,right;
        Material skin;

        WorkerController player;
        float transition,releaseMotion;
        SalvageItem previous;
        public string GripName { get; private set; }
        public float MaxContactError { get; private set; }
        public void Initialize(WorkerController owner,OfficeArt art)
        {
            player=owner;skin=art.W.Soft.Cream;left=Build(art,-1);right=Build(art,1);
        }
        Hand Build(OfficeArt art,int side)
        {
            var hand=new Hand();hand.Root=art.Group(transform,side<0?"Left bubbly hand":"Right bubbly hand",Vector3.zero);
            hand.Glove=hand.Root.gameObject.AddComponent<TheElevator.SoftOffice.SoftGlove>();hand.Glove.Build(art.W.Soft,side);
            for(int i=0;i<5;i++)
            {
                var digit=new GameObject("Contact finger "+i);digit.transform.SetParent(hand.Root,false);
                var mesh=new Mesh{name="Rounded contact finger"};mesh.MarkDynamic();hand.Fingers[i]=mesh;
                digit.AddComponent<MeshFilter>().sharedMesh=mesh;digit.AddComponent<MeshRenderer>().sharedMaterial=skin;
                hand.Nails[i]=new GameObject("Contact tip "+i).transform;hand.Nails[i].SetParent(hand.Root,false);
            }
            foreach(Renderer r in hand.Root.GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return hand;
        }
        void SetGripVisuals(Hand hand,bool holding)
        {
            foreach(Transform child in hand.Root)
            {
                if(child.name.StartsWith("Finger ")||child.name=="Opposed inward thumb")child.gameObject.SetActive(!holding);
                if(child.name.StartsWith("Contact finger"))child.gameObject.SetActive(holding);
            }
            hand.Glove.Pose(holding,player.MotionSpeed/6);
        }        public static string GripFor(SalvageItem item)
        {
            if(item.IsBattery)return "BATTERY CRADLE";
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
            if(held!=previous){if(previous&&!held)releaseMotion=1;transition=0;previous=held;}transition=Mathf.MoveTowards(transition,1,dt*5);releaseMotion=Mathf.MoveTowards(releaseMotion,0,dt*4);
            MaxContactError=0;SetGripVisuals(left,held);SetGripVisuals(right,held);
            if(held)
            {
                GripName=GripFor(held);BoxCollider shape=held.GetComponent<BoxCollider>();if(!shape)return;
                PoseGrip(left,-1,held,shape);PoseGrip(right,1,held,shape);
            }
            else
            {
                GripName="RELAXED";PoseIdle(left,-1);PoseIdle(right,1);
            }
        }
        void PoseIdle(Hand hand,int side)
        {
            float speed=player.MotionSpeed;
            float stride=Time.time*(player.MovingFast?11f:7f)+side*1.57f;
            float motion=Mathf.Clamp01(speed/4f);
            float sway=Mathf.Sin(Time.time*1.7f)*.005f+Mathf.Sin(stride)*motion*(player.MovingFast?.055f:.023f);
            hand.Root.position=player.View.transform.TransformPoint(new Vector3(side*.24f,-.28f+sway+releaseMotion*.07f,.48f+releaseMotion*.10f+Mathf.Cos(stride)*motion*.025f));
            hand.Root.rotation=player.View.transform.rotation*Quaternion.Euler(35+Mathf.Cos(stride)*motion*12,side*-12,side*-10);
            hand.Root.localScale=Vector3.one;
        }
        void PoseGrip(Hand hand,int side,SalvageItem item,BoxCollider box)
        {
            Vector3 extent=box.size*.5f,center=box.center;
            // The monitor's physics box includes its deep stand. Grip the thin display shell itself.
            if(GripName=="MONITOR SIDE GRIP" && item.GetComponent<OfficeEquipment>())
            {extent=new Vector3(.325f,.201f,.021f);center=new Vector3(0,.37f,0);}
            float y=GripName=="BATTERY CRADLE"?-extent.y*.35f:GripName=="PEDESTAL SUPPORT"?-extent.y*.65f:GripName=="SMALL DEVICE PINCH"?0:extent.y*.05f;
            // Both palms follow the actual moving object's collider, including crouch, recoil and wall pullback.
            Vector3 localPalm=center+new Vector3(side*(extent.x+.032f),y,-extent.z+.085f);
            hand.Root.position=item.transform.TransformPoint(localPalm);
            hand.Root.rotation=item.transform.rotation*Quaternion.Euler(0,side*-90,0);
            hand.Root.localScale=Vector3.one;
            for(int finger=0;finger<5;finger++)
            {
                float row=Mathf.Clamp(y+.035f-finger*.027f,-extent.y+.016f,extent.y-.016f);
                Vector3[] p=new Vector3[4];
                p[0]=center+new Vector3(side*(extent.x+.032f),row,-extent.z+.090f);
                p[1]=center+new Vector3(side*(extent.x+.031f),row,-extent.z+.039f);
                p[2]=center+new Vector3(side*(extent.x+.020f),row,-extent.z-.005f);
                p[3]=center+new Vector3(side*Mathf.Max(0,extent.x-.024f),row,-extent.z-.009f);
                if(finger==4)
                {
                    p[0]=center+new Vector3(side*(extent.x+.030f),y-.040f,-extent.z+.050f);
                    p[1]=center+new Vector3(side*(extent.x+.021f),y-.06f,-extent.z+.015f);
                    p[2]=center+new Vector3(side*(extent.x-.005f),y-.05f,-extent.z-.008f);
                    p[3]=center+new Vector3(side*Mathf.Max(0,extent.x-.042f),Mathf.Clamp(y-.026f,-extent.y+.016f,extent.y-.016f),-extent.z-.009f);
                }
                for(int i=0;i<4;i++)p[i]=item.transform.TransformPoint(p[i]);
                Fingers(hand,finger,p,-item.transform.forward);
                Vector3 local=item.transform.InverseTransformPoint(p[3])-center;
                MaxContactError=Mathf.Max(MaxContactError,Mathf.Abs(local.z+extent.z+.009f));
            }
        }
        void OnDestroy(){foreach(Hand hand in new[]{left,right})if(hand!=null)foreach(Mesh mesh in hand.Fingers)if(mesh)Destroy(mesh);}
        static void Fingers(Hand hand,int finger,Vector3[] points,Vector3 nailNormal)
        {
            const int rings=17,sides=12;
            Vector3[] vertices=new Vector3[rings*sides];int[] triangles=new int[(rings-1)*sides*6];
            for(int r=0;r<rings;r++)
            {
                float t=r/(float)(rings-1),u=1-t;
                Vector3 center=u*u*u*points[0]+3*u*u*t*points[1]+3*u*t*t*points[2]+t*t*t*points[3];
                Vector3 tangent=(3*u*u*(points[1]-points[0])+6*u*t*(points[2]-points[1])+3*t*t*(points[3]-points[2])).normalized;
                Vector3 across=Vector3.Cross(tangent,nailNormal).normalized;if(across.sqrMagnitude<.1f)across=Vector3.Cross(tangent,Vector3.up).normalized;
                Vector3 normal=Vector3.Cross(across,tangent).normalized;
                float radius=(finger==4?.022f:.016f)*Mathf.Lerp(1,.76f,t)*Mathf.Sqrt(Mathf.Clamp01((1-t)*12));
                if(r==0)radius*=.8f;
                for(int k=0;k<sides;k++){float angle=k*Mathf.PI*2/sides;vertices[r*sides+k]=hand.Root.InverseTransformPoint(center+(across*Mathf.Cos(angle)+normal*Mathf.Sin(angle)*.86f)*radius);}
            }
            int at=0;for(int r=0;r<rings-1;r++)for(int k=0;k<sides;k++){int a=r*sides+k,b=r*sides+(k+1)%sides,c=a+sides,d=b+sides;triangles[at++]=a;triangles[at++]=c;triangles[at++]=b;triangles[at++]=b;triangles[at++]=c;triangles[at++]=d;}
            Mesh mesh=hand.Fingers[finger];mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            Transform nail=hand.Nails[finger];nail.position=Vector3.Lerp(points[2],points[3],.85f)+nailNormal*.008f;
            nail.rotation=Quaternion.LookRotation(nailNormal,points[3]-points[2]);
        }
    }
}
