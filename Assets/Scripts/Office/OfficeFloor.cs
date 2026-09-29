using System;
using System.Collections.Generic;
using UnityEngine;
using TheElevator.Generation;

namespace TheElevator.Office
{
    public sealed partial class OfficeFloor : MonoBehaviour
    {
        public OfficeKit Kit { get; private set; }
        public OfficePlan Plan { get { return Kit.Plan; } }
        public GeneratedFloor Map { get; private set; }
        public DescentGame Game { get; private set; }
        public OfficeCargo Objective,Transported;
        public OfficeEmployee Questioner;
        public readonly List<OfficeTaskPoint> Stations=new List<OfficeTaskPoint>();
        public readonly List<OfficeEmployee> Employees=new List<OfficeEmployee>();
        public readonly List<OfficeDoor> Doors=new List<OfficeDoor>();
        public readonly Dictionary<int,float> DepartmentAwareness=new Dictionary<int,float>();
        public int SecurityAlert { get; private set; }
        public bool Alarm { get { return SecurityAlert>=80; } }
        public bool Blending { get { return blendStation&&Time.time<blendUntil; } }
        public string Prompt { get; private set; }
        public int BadgeLevel { get; private set; }=1;
        public int BadgeDepartment { get; private set; }=-1;
        public float GenerationMilliseconds;
        public string FitResult;
        public Vector3 LastReportedPosition;
        Light[] officeLights;
        float nextLighting;
        OfficeTaskPoint blendStation;
        float blendUntil,nextDecay,nextBluff;
        readonly List<GameObject> spilled=new List<GameObject>();
        readonly List<AudioSource> audioPool=new List<AudioSource>();
        AudioClip officeHum,tone;
        int audioCursor;
        public bool RequiredRecovered { get { if(!Objective||!Objective.Item)return false;Bounds b=Objective.GetComponent<BoxCollider>().bounds;return DescentGame.InCabin(b.min)&&DescentGame.InCabin(b.max); } }
        public void Prepare(Workshop workshop,GeneratedFloor map) { Map=map;Kit=new OfficeKit(workshop,map); }
        public void Assemble(DescentGame game)
        {
            Game=game;Stations.AddRange(GetComponentsInChildren<OfficeTaskPoint>(true));
            OfficeTaskPoint supervisor=Stations.Find(s=>s.RoomId==Plan.SupervisorRoom);
            if(!supervisor)throw new InvalidOperationException("Credential holder has no usable workstation.");
            PrepareWorkplaces();
            PopulateEmployees(supervisor);
            BuildDeskKeycard(supervisor);
            BuildDoors();
            if(Game)
            {
                BuildCargo(); MakeComputersStealable(); BuildExtinguishers();
                SetupSound();
                officeLights=GetComponentsInChildren<Light>(true);
                // Appearance switches only for the office theme; Civic Works keeps its worker model.
                // Bright, even office light: the building reads clearly without relying on the flashlight.
                RenderSettings.ambientLight=new Color(.40f,.42f,.43f);
                RenderSettings.fogDensity=.008f;
            }
            Physics.SyncTransforms();
            FitResult=ValidateObjectiveRoute();
            Map.PrepareVisibility();
        }
        void SpawnEmployee(OfficeTaskPoint point,bool supervisor)
        {
            Transform root=Kit.A.Group(transform,supervisor?"Department supervisor / clearance holder":"Office employee",Vector3.zero);
            OfficeEmployee employee=root.gameObject.AddComponent<OfficeEmployee>();employee.Initialize(this,point,Employees.Count,supervisor);Employees.Add(employee);
        }
        void BuildDoors()
        {
            foreach(int other in Map.Manifest.Neighbors(Plan.TargetRoom))CreateDoor(Plan.TargetRoom,other,true);
            AddRoomDoors();
        }        void BuildCargo()
        {
            Vector3 size=Plan.TargetBounds;Vector3 position=Map.Center(Map.Manifest.Rooms[Plan.TargetRoom])+new Vector3(3.5f,size.y*.5f+.13f,3.5f);
            Transform root=Kit.A.Group(transform,"MANDATORY / "+Plan.TargetName,transform.InverseTransformPoint(position));
            Transform art=Kit.A.Group(root,"Objective assembly",new Vector3(0,-size.y*.5f,0));
            if(Plan.TargetType==2)Kit.Vending(art);
            else if(Plan.TargetType==0) { Transform scaled=Kit.A.Group(art,"Prototype rack",Vector3.zero);scaled.localScale=new Vector3(.82f,.83f,.9f);Kit.Server(scaled);foreach(Collider col in scaled.GetComponentsInChildren<Collider>())col.enabled=false; }
            else
            {
                Kit.A.Box(art,"Vault housing",new Vector3(0,.7f,0),size,Kit.A.Dark);
                Kit.A.Box(art,"Vault door",new Vector3(0,.74f,-.47f),new Vector3(.97f,1.2f,.09f),Kit.A.Metal);
                Kit.A.Round(art,"Combination dial",new Vector3(.25f,.85f,-.54f),new Vector3(.15f,.15f,.07f),Kit.A.Brass,PrimitiveType.Sphere);
                Kit.A.Label(art,"M / INTERNAL ONLY",new Vector3(0,1.13f,-.53f),.03f);
            }
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Kit.A.Round(art,"Transport dolly wheel",new Vector3(x*.5f,-.035f,z*.32f),new Vector3(.15f,.14f,.15f),Kit.A.Dark,PrimitiveType.Sphere);
            Kit.A.Box(art,"Industrial dolly deck",new Vector3(0,.015f,0),new Vector3(size.x+.08f,.08f,size.z+.06f),Kit.A.Brass);
            SalvageItem item=root.gameObject.AddComponent<SalvageItem>();item.Configure(Game,Plan.TargetName,Plan.TargetType==2?125:80,1000+Plan.TargetType*350,false,size+new Vector3(.08f,.12f,.08f));
            Bounds actual=new Bounds();bool first=true;
            foreach(MeshRenderer renderer in art.GetComponentsInChildren<MeshRenderer>())if(renderer.GetComponent<MeshFilter>())
            {if(first){actual=renderer.bounds;first=false;}else actual.Encapsulate(renderer.bounds);}
            BoxCollider shell=root.GetComponent<BoxCollider>();shell.center=root.InverseTransformPoint(actual.center);shell.size=actual.size+Vector3.one*.015f;
            Objective=root.gameObject.AddComponent<OfficeCargo>();Objective.Initialize(this,item,true);
            // Optional assets share SalvageItem registration, value accounting and transition persistence.
            int loot=0;
            foreach(OfficeTaskPoint task in Stations)
            {
                if(task.RoomId<2||loot>=Math.Max(6,Map.Manifest.Rooms.Count/3)||task.Activity!=OfficeTask.Typing)continue;
                // Loot rests on the desk top, in the clear gap between the paperwork, keyboard and monitor.
                Transform desk=task.transform.parent;
                float height=loot%3==0?.14f:loot%3==1?.42f:.05f;
                Vector3 p=desk.TransformPoint(new Vector3(-.3f,.82f+height*.5f+.005f,.1f));
                Transform asset=Kit.A.Group(transform,"Optional office property",transform.InverseTransformPoint(p));asset.rotation=desk.rotation;
                string title;Vector3 lootSize;float mass;int price;
                if(loot%3==0)
                {
                    title="Encrypted data unit";lootSize=new Vector3(.22f,.14f,.3f);mass=3;price=95+task.RoomId%7*30;
                    Kit.A.Box(asset,title,Vector3.zero,lootSize,Kit.A.Metal);
                    Kit.A.Box(asset,"Data-unit display",new Vector3(0,.04f,-.16f),new Vector3(.13f,.045f,.01f),Kit.A.Screen);
                }
                else if(loot%3==1)
                {
                    title="Employee obedience award";lootSize=new Vector3(.25f,.42f,.25f);mass=5;price=210;
                    Kit.A.Box(asset,"Award plinth",new Vector3(0,-.15f,0),new Vector3(.25f,.12f,.25f),Kit.A.Wood);
                    Kit.A.Round(asset,"Corporate award",new Vector3(0,.06f,0),new Vector3(.15f,.3f,.15f),Kit.A.Brass,PrimitiveType.Sphere);
                }
                else
                {
                    title="Executive laptop";lootSize=new Vector3(.4f,.05f,.28f);mass=3;price=340;
                    Kit.A.Box(asset,"Laptop shell",Vector3.zero,lootSize,Kit.A.Dark);
                    Kit.A.Box(asset,"Laptop lid logo",new Vector3(0,.026f,0),new Vector3(.08f,.004f,.08f),Kit.A.Brass);
                }
                SalvageItem value=asset.gameObject.AddComponent<SalvageItem>();value.Configure(Game,title,mass,price,false,lootSize);
                OfficeCargo cargo=asset.gameObject.AddComponent<OfficeCargo>();cargo.Initialize(this,value,false);loot++;
            }
        }
        // Wall-mounted extinguishers: cheap pickups the player can carry, throw or spray.
        void BuildExtinguishers()
        {
            foreach(Transform mount in Kit.ExtinguisherMounts)
                if(mount)FireExtinguisher.Create(Game,Kit.A,transform,mount.position+Vector3.up*FireExtinguisher.HalfHeight,mount.rotation);
        }
        public bool HasAccess(int clearance,int department)
        { return BadgeLevel>=clearance&&(clearance<2||BadgeDepartment==department||BadgeLevel>=3)&&!(Alarm&&Plan.Config.InvalidateBadgesOnAlarm); }
        public void GiveBadge(int level,int department){BadgeLevel=Math.Max(BadgeLevel,level);BadgeDepartment=department;}
        public void RaiseLocal(int department,float amount)
        {
            if(!DepartmentAwareness.ContainsKey(department))DepartmentAwareness.Add(department,0);
            DepartmentAwareness[department]=Mathf.Clamp(DepartmentAwareness[department]+amount,0,100);
            SecurityAlert=Mathf.Clamp(SecurityAlert+Mathf.RoundToInt(amount*.35f),0,100);
        }
        public void ReportToSecurity(int department,float amount,Vector3 lastSeen){LastReportedPosition=lastSeen;RaiseLocal(department,amount);}
        public void ReportAction(Vector3 point,float amount,OfficeEmployee excluded)
        { foreach(OfficeEmployee employee in Employees)if(employee!=excluded&&employee.Sees(point))employee.React(amount,"What are you doing with company property?"); }
        public void HearNoise(Vector3 point,float strength)
        { foreach(OfficeEmployee employee in Employees)if(Vector3.Distance(employee.transform.position,point)<strength*12)employee.Robot.LookTarget=point; }
        public bool InteractPressed()
        {
            if(!Game||!Game.ControlsActive)return false;
            Ray ray=Game.Player.View.ViewportPointToRay(new Vector3(.5f,.5f,0));
            if(Physics.Raycast(ray,out RaycastHit hit,2.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                OfficeKeycard card=hit.collider.GetComponentInParent<OfficeKeycard>();if(card){card.Take();return true;}
                OfficeDoor door=hit.collider.GetComponentInParent<OfficeDoor>();if(door){door.Use();return true;}
                OfficeCargo cargo=hit.collider.GetComponentInParent<OfficeCargo>();
                if(cargo&&cargo.Mandatory)
                {
                    if(Game.Player.Held){Game.Notify("Free your hands before taking the dolly.");return true;}
                    if(cargo.Mounted)cargo.ReleaseMount();Transported=Transported==cargo?null:cargo;return true;
                }
            }
            return false;
        }
        void Update()
        {
            if(!Game||!Game.ControlsActive)return;
            if(Time.time>nextLighting&&officeLights!=null)
            {
                nextLighting=Time.time+.6f;Vector3 eye=Game.Player.View.transform.position;
                Array.Sort(officeLights,(a,b)=>(a.transform.position-eye).sqrMagnitude.CompareTo((b.transform.position-eye).sqrMagnitude));int budget=Plan.Config.Quality>=OfficeQuality.High?2:0;
                foreach(Light light in officeLights)if(light)light.shadows=budget-->0?LightShadows.Soft:LightShadows.None;
            }
            Prompt="";
            if(blendStation&&Time.time>blendUntil){blendStation.ReservedByPlayer=false;blendStation=null;}
            if(Input.GetKeyDown(KeyCode.Q))Transported=null;
            OfficeEmployee near=Employees.Find(e=>e.HasBadge&&Vector3.Distance(e.transform.position,Game.Player.transform.position)<2.2f);
            if(near&&near.HasBadge)
            {
                Prompt=near.CanPickpocket(Game.Player.transform)?"HOLD G / LIFT BADGE  "+Mathf.RoundToInt(near.PickpocketProgress*100)+"%":near.Job.ToUpper()+" / APPROACH FROM BEHIND";
                if(Input.GetKeyDown(KeyCode.G)&&!near.CanPickpocket(Game.Player.transform)){near.Suspicion=Mathf.Min(100,near.Suspicion+12);near.Robot.LookTarget=Game.Player.transform.position+Vector3.up*1.5f;}
                if(Input.GetKey(KeyCode.G))near.Pickpocket(Time.deltaTime);else near.PickpocketProgress=0;
            }
            if(!near)
            {
                OfficeTaskPoint point=Stations.Find(s=>(s.Available||s==blendStation)&&Vector3.Distance(s.transform.position,Game.Player.transform.position)<1.4f);
                if(point){Prompt="HOLD T / PRETEND TO "+point.Activity.ToString().ToUpper();if(Input.GetKey(KeyCode.T)){blendStation=point;point.ReservedByPlayer=true;blendUntil=Time.time+.3f;}}
            }
            if(Questioner&&Questioner.Suspicion>40&&Vector3.Distance(Questioner.transform.position,Game.Player.transform.position)<10)
            {
                Prompt="B / BLUFF: 'FACILITIES AUTHORIZED THIS.'";
                if(Input.GetKeyDown(KeyCode.B)&&Time.time>nextBluff)
                { Questioner.Suspicion=Mathf.Max(0,Questioner.Suspicion-(BadgeLevel>=2?25:10));nextBluff=Time.time+20;Game.Notify("Identity check deferred. Try looking busy."); }
            }
            CheckMeetingEntry(Time.deltaTime);
            if(!Game.Player.Seat)CheckBumps(Game.Player.transform.position,Game.Player.Motion);
            if(Transported)Prompt="DOLLY ATTACHED / WALK BACKWARD TO PULL / Q RELEASE";
            if(Time.time>nextDecay){nextDecay=Time.time+8;SecurityAlert=Mathf.Max(0,SecurityAlert-1);}
        }
        public string ValidateObjectiveRoute()
        {
            foreach(OfficeDoor door in Doors)door.SetOpenForValidation(true);
            List<Collider> disabled=new List<Collider>();
            if(Game)foreach(Collider col in Game.GetComponentsInChildren<Collider>(true))if(col.enabled&&(col.name=="Left door"||col.name=="Right door")){col.enabled=false;disabled.Add(col);}
            foreach(OfficeEmployee employee in Employees)foreach(Collider col in employee.GetComponentsInChildren<Collider>())if(col.enabled){col.enabled=false;disabled.Add(col);}
            if(Objective)foreach(Collider col in Objective.GetComponentsInChildren<Collider>())if(col.enabled){col.enabled=false;disabled.Add(col);}
            // Persistent cargo is a movable gameplay obstacle, not a defect in the newly generated architecture.
            if(Game)foreach(SalvageItem cargo in Game.Items)
                if(cargo&&(!Objective||cargo!=Objective.Item)&&DescentGame.InCabin(cargo.transform.position))
                    foreach(Collider col in cargo.GetComponentsInChildren<Collider>())if(col.enabled){col.enabled=false;disabled.Add(col);}
            Physics.SyncTransforms();
            try
            {
                Vector3 bounds=(Objective?Objective.GetComponent<BoxCollider>().size:Plan.TargetBounds)+new Vector3(.12f,.12f,.12f);
                Vector3 extents=new Vector3(new Vector2(bounds.x,bounds.z).magnitude*.5f,bounds.y*.5f, new Vector2(bounds.x,bounds.z).magnitude*.5f);
                List<Vector3> route=Map.Route(Plan.TargetRoom,0);route.Insert(0,Map.Center(Map.Manifest.Rooms[Plan.TargetRoom])+new Vector3(3.5f,0,3.5f));route.Add(new Vector3(0,0,-6));
                int segments=0;
                for(int i=1;i<route.Count;i++)
                {
                    Vector3 direction=route[i]-route[i-1];if(direction.sqrMagnitude<.001f)continue;
                    Vector3 origin=route[i-1]+Vector3.up*(extents.y+.22f);
                    if(Physics.BoxCast(origin,extents*.98f,direction.normalized,out RaycastHit hit,Quaternion.identity,direction.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                        throw new InvalidOperationException("Objective route obstructed by "+hit.collider.name+" at segment "+i);
                    segments++;
                }
                return "PASS / "+segments+" conservative objective-bounds sweeps; ground-level route, credential outside locked zone.";
            }
            finally{foreach(Collider col in disabled)if(col)col.enabled=true;foreach(OfficeDoor door in Doors)door.SetOpenForValidation(false);Physics.SyncTransforms();}
        }
        void SetupSound()
        {
            officeHum=AudioClip.Create("Office ventilation",22050*2,1,22050,false);float[] samples=new float[22050*2];
            for(int i=0;i<samples.Length;i++)samples[i]=(.35f*Mathf.Sin(i*2*Mathf.PI*60/22050)+.13f*Mathf.Sin(i*2*Mathf.PI*119/22050))*.05f;
            officeHum.SetData(samples,0);
            AudioSource hum=gameObject.AddComponent<AudioSource>();hum.clip=officeHum;hum.loop=true;hum.volume=.22f;hum.Play();
            tone=AudioClip.Create("Office equipment click",2205,1,22050,false);float[] click=new float[2205];for(int i=0;i<click.Length;i++)click[i]=Mathf.Sin(i*.08f)*(1-i/2205f)*.14f;tone.SetData(click,0);
            for(int i=0;i<8;i++){GameObject go=new GameObject("Pooled workplace sound");go.transform.SetParent(transform);AudioSource source=go.AddComponent<AudioSource>();source.spatialBlend=1;source.maxDistance=18;source.rolloffMode=AudioRolloffMode.Linear;source.clip=tone;audioPool.Add(source);}
        }
        public void PlayTone(Vector3 position,float pitch,float volume)
        {if(audioPool.Count==0)return;AudioSource source=audioPool[audioCursor++%audioPool.Count];source.transform.position=position;source.pitch=pitch/110;source.volume=volume;source.Play();}
        public void SpillProduct(Vector3 position)
        {
            if(spilled.Count>=8)return;
            GameObject can=Kit.A.Round(transform,"Spilled nutrient can",transform.InverseTransformPoint(position),new Vector3(.115f,.17f,.115f),Kit.A.Brass);
            can.AddComponent<SphereCollider>().radius=.5f;Rigidbody body=can.AddComponent<Rigidbody>();body.mass=.1f;body.linearVelocity=Vector3.up*.7f;spilled.Add(can);
        }
        void OnGUI()
        {
            if(!Game||!Game.ControlsActive)return;
            // The presenter's words, shown as a subtitle while you are in the room with them.
            if(playerInMeeting)
                GUI.Box(new Rect(Screen.width*.5f-300,Screen.height-205,600,35),"PRESENTER: "+MeetingLine);
            // No anger or suspicion counters: read each employee's face instead.
            GUI.Box(new Rect(20,Screen.height-154,560,114),"");
            GUI.Label(new Rect(34,Screen.height-146,530,100),"CONTRACT / "+Plan.TargetName.ToUpper()+"\n"+(RequiredRecovered?"REQUIRED ASSET SECURED":"MANDATORY / ROOM "+Plan.TargetRoom.ToString("000"))+"   |   COVER: FACILITIES ASSISTANT\nBADGE "+BadgeLevel+"   "+(Blending?"LOOKING PRODUCTIVE":""));
        }
        void OnDestroy(){if(officeHum)Destroy(officeHum);if(tone)Destroy(tone);}
    }
}





