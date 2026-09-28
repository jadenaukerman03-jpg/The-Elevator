using UnityEngine;
using TheElevator.Office;
namespace TheElevator
{
    // Viewmodel mittens match the bean characters: a soft palm, one broad finger paddle and a thumb.
    // Grip poses still wrap the paddle and thumb around the held object's collider edge.
    public sealed class FirstPersonHands : MonoBehaviour
    {
        sealed class Hand
        {
            public Transform Root;
            public readonly Mesh[] Digits=new Mesh[2];
        }
        const int Paddle=0,Thumb=1;
        Hand left,right;
        Material skin,sleeve;
        Mesh palmMesh;
        WorkerController player;
        float transition,releaseMotion;
        int outfitVersion=-1;
        SalvageItem previous;
        public string GripName { get; private set; }
        public float MaxContactError { get; private set; }
        public void Initialize(WorkerController owner,OfficeArt art)
        {
            player=owner;palmMesh=BuildPalm();
            skin=art.W.Own(new Material(Shader.Find("Elevator/ViewmodelSkin")){color=owner.Model.HandColor});
            sleeve=art.W.Own(new Material(skin){color=owner.Model.SleeveColor});
            left=Build(art,-1);right=Build(art,1);
        }
        Hand Build(OfficeArt art,int side)
        {
            Hand hand=new Hand();hand.Root=art.Group(transform,side<0?"Left mitten":"Right mitten",Vector3.zero);
            art.Box(hand.Root,"Soft palm",Vector3.zero,new Vector3(.112f,.118f,.058f),skin).GetComponent<MeshFilter>().sharedMesh=palmMesh;
            art.Round(hand.Root,"Sleeve cuff",new Vector3(0,-.085f,0),new Vector3(.094f,.045f,.08f),sleeve,PrimitiveType.Sphere);
            art.Round(hand.Root,"Sleeve",new Vector3(0,-.25f,0),new Vector3(.10f,.17f,.088f),sleeve,PrimitiveType.Capsule);
            for(int digit=0;digit<2;digit++)
            {
                GameObject part=new GameObject(digit==Paddle?"Mitten paddle":"Mitten thumb");part.transform.SetParent(hand.Root,false);
                Mesh mesh=new Mesh{name="Soft mitten digit"};mesh.MarkDynamic();hand.Digits[digit]=mesh;
                part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=skin;
            }
            foreach(Renderer renderer in hand.Root.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return hand;
        }
        public static string GripFor(SalvageItem item)
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
            if(outfitVersion!=player.Model.Version){outfitVersion=player.Model.Version;skin.color=player.Model.HandColor;sleeve.color=player.Model.SleeveColor;}
            if(held!=previous){if(previous&&!held)releaseMotion=1;transition=0;previous=held;}transition=Mathf.MoveTowards(transition,1,dt*5);releaseMotion=Mathf.MoveTowards(releaseMotion,0,dt*4);
            MaxContactError=0;
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
            Vector3[] paddle=new Vector3[4],thumb=new Vector3[4];
            for(int j=0;j<4;j++)
            {
                float t=j/3f;
                paddle[j]=hand.Root.TransformPoint(new Vector3(0,.035f,0)+new Vector3(0,1,.18f)*.07f*t+Vector3.forward*t*t*.03f);
                thumb[j]=hand.Root.TransformPoint(new Vector3(-side*.045f,-.012f,0)+new Vector3(-side*.65f,.65f,.2f)*.05f*t+Vector3.forward*t*t*.02f);
            }
            Digit(hand,Paddle,paddle,hand.Root.forward,.019f,2.3f);
            Digit(hand,Thumb,thumb,hand.Root.forward,.016f,1);
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
            float row=Mathf.Clamp(y+.006f,-extent.y+.03f,extent.y-.03f);
            Vector3[] paddle={
                center+new Vector3(side*(extent.x+.034f),row,-extent.z+.090f),
                center+new Vector3(side*(extent.x+.033f),row,-extent.z+.039f),
                center+new Vector3(side*(extent.x+.022f),row,-extent.z-.007f),
                center+new Vector3(side*Mathf.Max(0,extent.x-.026f),row,-extent.z-.011f)};
            Vector3[] thumb={
                center+new Vector3(side*(extent.x+.030f),y-.045f,-extent.z+.050f),
                center+new Vector3(side*(extent.x+.021f),y-.065f,-extent.z+.015f),
                center+new Vector3(side*(extent.x-.005f),y-.055f,-extent.z-.008f),
                center+new Vector3(side*Mathf.Max(0,extent.x-.042f),Mathf.Clamp(y-.03f,-extent.y+.016f,extent.y-.016f),-extent.z-.009f)};
            // Contact error is measured against each digit's intended rest depth on the front face.
            MaxContactError=Mathf.Max(MaxContactError,Mathf.Abs(paddle[3].z+extent.z+.011f-center.z),Mathf.Abs(thumb[3].z+extent.z+.009f-center.z));
            for(int i=0;i<4;i++){paddle[i]=item.transform.TransformPoint(paddle[i]);thumb[i]=item.transform.TransformPoint(thumb[i]);}
            Digit(hand,Paddle,paddle,item.transform.right,.019f,2.3f);
            Digit(hand,Thumb,thumb,item.transform.right,.015f,1);
        }
        void OnDestroy(){if(palmMesh)Destroy(palmMesh);foreach(Hand hand in new[]{left,right})if(hand!=null)foreach(Mesh mesh in hand.Digits)if(mesh)Destroy(mesh);}
        static Mesh BuildPalm()
        {
            const int rings=24,sides=32;
            var vertices=new Vector3[(rings+1)*(sides+1)];var triangles=new int[rings*sides*6];
            for(int r=0;r<=rings;r++)for(int s=0;s<=sides;s++)
            {
                float angle=r*Mathf.PI/rings,around=s*Mathf.PI*2/sides;
                float radius=Mathf.Pow(Mathf.Max(0,Mathf.Sin(angle)),.35f)*.5f;
                float width=Mathf.Lerp(.78f,1f,(Mathf.Cos(angle)+1)*.5f);
                vertices[r*(sides+1)+s]=new Vector3(Mathf.Cos(around)*radius*width,Mathf.Cos(angle)*.5f,Mathf.Sin(around)*radius);
            }
            int n=0;for(int r=0;r<rings;r++)for(int s=0;s<sides;s++){int a=r*(sides+1)+s,b=a+sides+1;triangles[n++]=a;triangles[n++]=a+1;triangles[n++]=b;triangles[n++]=b;triangles[n++]=a+1;triangles[n++]=b+1;}
            var mesh=new Mesh{name="Soft mitten palm"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        // A rounded tube along a cubic curve; width stretches the cross-section into a flat mitten paddle.
        static void Digit(Hand hand,int digit,Vector3[] points,Vector3 hint,float radius,float width)
        {
            const int rings=17,sides=12;
            Vector3[] vertices=new Vector3[rings*sides];int[] triangles=new int[(rings-1)*sides*6];
            for(int r=0;r<rings;r++)
            {
                float t=r/(float)(rings-1),u=1-t;
                Vector3 center=u*u*u*points[0]+3*u*u*t*points[1]+3*u*t*t*points[2]+t*t*t*points[3];
                Vector3 tangent=(3*u*u*(points[1]-points[0])+6*u*t*(points[2]-points[1])+3*t*t*(points[3]-points[2])).normalized;
                Vector3 across=Vector3.Cross(tangent,hint).normalized;if(across.sqrMagnitude<.1f)across=Vector3.Cross(tangent,Vector3.up).normalized;
                Vector3 normal=Vector3.Cross(across,tangent).normalized;
                float round=radius*Mathf.Lerp(1,.85f,t)*Mathf.Sqrt(Mathf.Clamp01((1-t)*10));
                if(r==0)round*=.85f;
                for(int k=0;k<sides;k++){float angle=k*Mathf.PI*2/sides;vertices[r*sides+k]=hand.Root.InverseTransformPoint(center+(across*Mathf.Cos(angle)*width+normal*Mathf.Sin(angle)*.8f)*round);}
            }
            int at=0;for(int r=0;r<rings-1;r++)for(int k=0;k<sides;k++){int a=r*sides+k,b=r*sides+(k+1)%sides,c=a+sides,d=b+sides;triangles[at++]=a;triangles[at++]=c;triangles[at++]=b;triangles[at++]=b;triangles[at++]=c;triangles[at++]=d;}
            Mesh mesh=hand.Digits[digit];mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
    }
}
