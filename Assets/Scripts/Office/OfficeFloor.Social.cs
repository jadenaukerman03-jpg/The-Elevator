using System;
using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.Office
{
    public sealed partial class OfficeFloor
    {
        public OfficeKeycard DeskKeycard { get; private set; }
        public float SuspicionLevel
        {
            get { float level=SecurityAlert;foreach(OfficeEmployee e in Employees)level=Mathf.Max(level,e.Suspicion);return level; }
        }
        public int TravellersInRoom(int room)
        {
            int count=0;foreach(OfficeEmployee e in Employees)if(e.Travelling&&(Map.NearestRoom(e.transform.position).Id==room||(e.Station&&e.Station.RoomId==room)))count++;return count;
        }
        public bool CanTravel(OfficeEmployee employee,OfficeTaskPoint target)
        {
            bool returning=target==employee.HomeStation;
            int active=0;foreach(OfficeEmployee other in Employees)if(other!=employee&&other.Travelling&&(other.Station==other.HomeStation)==returning)active++;
            return active<Mathf.Clamp(Employees.Count/8,2,4)&&TravellersInRoom(target.RoomId)<2;
        }
        void PrepareWorkplaces()
        {
            foreach(OfficeTaskPoint task in Stations)
            {
                if(task.Activity==OfficeTask.Coffee)
                {
                    Transform coffee=Kit.A.Group(transform,"Functional coffee station",transform.InverseTransformPoint(task.transform.position));coffee.rotation=task.transform.rotation;
                    coffee.gameObject.AddComponent<OfficeCoffeeStation>().Build(Kit.A,task);
                }
                if(task.Activity==OfficeTask.Typing)
                {
                    task.Equipment=task.transform.parent.GetComponentInChildren<OfficeEquipment>();
                    if(task.Equipment)task.Equipment.Station=task;
                }
            }
        }
        // ---- Building-wide anger ----
        // Every point of anger any offence adds (a bump is 1, a thrown item 5) fills a meter for the whole
        // building. It never goes down. Full at 50: everyone in the building turns on the players, for good.
        public const float RiotPoint=50;
        public float BuildingAnger{get;private set;}
        public bool Riot{get{return BuildingAnger>=RiotPoint;}}
        public void AddAnger(float amount)
        {
            if(Riot||amount<=0)return;
            BuildingAnger=Mathf.Min(RiotPoint,BuildingAnger+amount);
            if(!Riot)return;
            if(Game)Game.Notify("The whole office has had enough of you.");
            foreach(OfficeEmployee employee in Employees)
                employee.JoinRiot(Game&&Vector3.Distance(employee.transform.position,Game.Player.transform.position)<20);
        }
        // Test hook: set the meter below the riot point (checks run many offences in a row).
        public void SetAngerForValidation(float value){if(!Riot)BuildingAnger=Mathf.Clamp(value,0,RiotPoint-.01f);}
        // With the whole building hunting at once, only a few chase routes are planned each frame.
        int planFrame,plansThisFrame;
        public bool TryPlanChase()
        {
            if(Time.frameCount!=planFrame){planFrame=Time.frameCount;plansThisFrame=0;}
            if(plansThisFrame>=6)return false;
            plansThisFrame++;return true;
        }
        // Someone pulls a weapon: everyone who can see it gasps, and any player standing close gets a look at their face.
        public int Gasps{get;private set;}
        public void WeaponDrawn(OfficeEmployee armed)
        {
            Vector3 chest=armed.transform.position+Vector3.up*1.1f;
            foreach(OfficeEmployee other in Employees)
            {
                if(other==armed||other.Dead)continue;
                Vector3 eye=other.transform.position+Vector3.up*1.34f;
                if(Vector3.Distance(eye,chest)>14||Physics.Linecast(eye,chest,~((1<<2)|(1<<8)),QueryTriggerInteraction.Ignore))continue;
                other.Gasp(armed.Weapon==Arms.Bazooka);Gasps++;
            }
            if(!Game)return;
            foreach(WorkerController member in Game.Crew)
            {
                if(member.Down||member.Remote)continue;
                Vector3 eye=member.View.transform.position;
                if(Vector3.Distance(eye,chest)<10&&!Physics.Linecast(eye,chest,~((1<<2)|(1<<8)),QueryTriggerInteraction.Ignore)&&armed.Robot.Head)
                    member.Cutscene(armed.Robot.Head,OfficeEmployee.DrawTime);
            }
        }

        // ---- The access card: always carried by one employee ----
        // The supervisor has it. Lift it from behind (hold G), or knock them out and pick it up off the floor.
        public OfficeEmployee KeycardHolder{get{return Employees.Count>0&&Employees[0].Supervisor?Employees[0]:null;}}
        public void DropKeycard(Vector3 at,Quaternion rotation)
        {
            Transform card=Kit.A.Group(transform,"DROPPED SUPERVISOR KEYCARD",transform.InverseTransformPoint(at));
            card.rotation=rotation;
            Kit.A.Box(card,"Bright clearance card",Vector3.zero,new Vector3(.29f,.025f,.19f),Kit.A.WarmLight);
            Kit.A.Box(card,"Supervisor stripe",new Vector3(0,.018f,.048f),new Vector3(.25f,.005f,.045f),Kit.A.Red);
            Kit.A.Box(card,"Badge clip",new Vector3(0,.02f,.11f),new Vector3(.08f,.015f,.04f),Kit.A.Brass);
            BoxCollider collider=card.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(.31f,.08f,.23f);
            DeskKeycard=card.gameObject.AddComponent<OfficeKeycard>();DeskKeycard.Office=this;
            TextMesh print=Kit.A.W.Label("ACCESS / 02",card,new Vector3(0,.016f,-.018f),.006f,Color.black);print.transform.localRotation=Quaternion.Euler(90,0,0);
        }

        // Every floor has exactly three armed employees: two keep a pistol in their desk, one a bazooka.
        void AssignSidearms()
        {
            System.Random dice=new System.Random(unchecked(Map.Manifest.Recipe.Seed*31+7));
            List<OfficeEmployee> pool=new List<OfficeEmployee>(Employees);
            for(int i=0;i<3&&pool.Count>0;i++){int pick=dice.Next(pool.Count);pool[pick].Sidearm=i<2?Arms.Pistol:Arms.Bazooka;pool.RemoveAt(pick);}
        }
        void PopulateEmployees(OfficeTaskPoint supervisor)
        {
            SpawnEmployee(supervisor,true);
            foreach(OfficeRoomPlan room in Plan.Rooms)
            {
                if(room.RoomId<=1||room.RoomId==Plan.TargetRoom||room.Kind==OfficeRoomKind.Breakroom||room.Kind==OfficeRoomKind.Gallery||room.Kind==OfficeRoomKind.Restroom)continue;
                if(room.RoomId==Plan.MeetingRoom){PopulateMeeting();continue;}
                // Open-plan cells are busy: most of their eight desks are taken. Other rooms hold one person.
                int capacity=room.Kind==OfficeRoomKind.Workroom?Plan.Config.EmployeesPerRoom+3:1;
                foreach(OfficeTaskPoint task in Stations)
                {
                    if(capacity<=0||Employees.Count>=Plan.Config.PopulationCap)break;
                    if(task.RoomId!=room.RoomId||task.Occupant||task.Activity==OfficeTask.Coffee)continue;
                    SpawnEmployee(task,false);capacity--;
                }
            }
        }
        void MakeComputersStealable()
        {
            
            foreach(OfficeTaskPoint task in Stations)
            {
                if(!task.Equipment||task.Equipment.GetComponent<SalvageItem>())continue;
                // One marked recoverable workstation in selected rooms; most computers stay installed.
                if(!task.Equipment.Recoverable)continue;
                OfficeEquipment equipment=task.Equipment;
                Kit.A.Box(equipment.transform,"Recoverable asset sticker",new Vector3(.265f,.20f,-.032f),new Vector3(.075f,.025f,.005f),Kit.A.WarmLight);
                Kit.A.Label(equipment.transform,"ASSET",new Vector3(.265f,.20f,-.037f),.007f,Color.black);
                SalvageItem computer=equipment.gameObject.AddComponent<SalvageItem>();
                computer.Configure(Game,"Morrow workstation computer",9,280,false,new Vector3(.67f,.59f,.25f));
                computer.GetComponent<BoxCollider>().center=new Vector3(0,.29f,0);computer.Body.isKinematic=true;
                OfficeCargo cargo=equipment.gameObject.AddComponent<OfficeCargo>();cargo.Initialize(this,computer,false);cargo.Workstation=task;
            }
        }
        public void WitnessTheft(OfficeCargo cargo)
        {
            if(!Game)return;
            // Only the owner gets upset, and only if they are at their desk or see it happen. Anyone else watching
            // just files it away (hidden suspicion that can reach security); they don't react.
            OfficeEmployee owner=cargo.Workstation?(cargo.Workstation.HomeOwner?cargo.Workstation.HomeOwner:cargo.Workstation.Occupant):null;
            if(owner&&owner.Dead)owner=null;
            bool atDesk=owner&&!owner.Travelling&&Vector3.Distance(owner.transform.position,cargo.Workstation.transform.position)<.8f;
            bool notices=owner&&(atDesk||owner.Sees(Game.Player.transform.position));
            ReportAction(Game.Player.transform.position,22,notices?owner:null);
            if(!notices||cargo.Taken)return;
            cargo.Taken=true;
            string item=cargo.Item?cargo.Item.Title.ToLowerInvariant():"";
            bool computer=item.Contains("computer")||item.Contains("laptop");
            owner.React(100);
            if(computer)owner.Offend(Grievance.Computer,owner.Anger<5?"Hey! I was using that to work with! Give that back!":null);
            else owner.Offend(item.Contains("award")?Grievance.Award:Grievance.Property);
            if(computer&&cargo.Workstation.Equipment&&cargo.Workstation.Equipment.gameObject==cargo.gameObject){cargo.Workstation.Equipment=null;cargo.Workstation.EquipmentMissing=true;}
        }
        // The presenter always stands; roughly three of every four chairs around the table are taken.
        void PopulateMeeting()
        {
            int seat=0;
            foreach(OfficeTaskPoint task in Stations)
            {
                if(task.RoomId!=Plan.MeetingRoom||task.Occupant||Employees.Count>=Plan.Config.PopulationCap)continue;
                if(task.Activity==OfficeTask.Present){SpawnEmployee(task,false);continue;}
                if(task.Activity==OfficeTask.Meeting&&seat++%4!=3)SpawnEmployee(task,false);
            }
        }
        public bool IsMeetingSpeaker(OfficeEmployee employee)
        { return employee.AtStation&&employee.Station.Activity==OfficeTask.Present; }
        // The presenter's stick moves from chart bar to chart bar, pausing on each.
        public Vector3 BoardPoint(float time)
        {
            if(!Kit.MeetingBoard)return Vector3.zero;
            float step=time/2.4f;int bar=(int)step%6,next=(bar+1)%6;float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((step-(int)step-.7f)/.3f));
            Vector3 a=new Vector3(-1.4f+bar*.56f,1.05f+.25f+bar*.2f,-.06f),b=new Vector3(-1.4f+next*.56f,1.05f+.25f+next*.2f,-.06f);
            return Kit.MeetingBoard.TransformPoint(Vector3.Lerp(a,b,blend));
        }
        bool playerInMeeting;
        // Walking into the meeting is allowed: nobody gets upset, the player just hears the presentation.
        public void CheckMeetingEntry(float dt)
        {
            if(Plan.MeetingRoom<0||!Game)return;
            playerInMeeting=Map.NearestRoom(Game.Player.transform.position).Id==Plan.MeetingRoom;
        }
        readonly HashSet<OfficeEmployee> touching=new HashSet<OfficeEmployee>();
        // ---- Hallway conversations ----
        // Two colleagues passing each other either walk on (half the time) or stop and chat, taking turns.
        sealed class Chat { public OfficeEmployee A,B; public string[] Lines; public int Next; public float At; }
        readonly List<Chat> chats=new List<Chat>();
        readonly Dictionary<long,float> passed=new Dictionary<long,float>();
        // Nobody chats again for a minute after a conversation.
        readonly Dictionary<OfficeEmployee,float> chattedUntil=new Dictionary<OfficeEmployee,float>();
        readonly System.Random chatter=new System.Random(4242);
        float nextChatScan,nextPresenterLine;
        public int ActiveConversations { get { return chats.Count; } }
        static bool Free(OfficeEmployee e){return e&&!e.Dead&&!e.Partner&&!e.Chasing&&!e.Pushed&&!e.Blinded&&e.AngerLevel<3&&!e.Robot.Seated;}
        void UpdateConversations()
        {
            if(Time.time>=nextChatScan)
            {
                nextChatScan=Time.time+.25f;
                List<OfficeEmployee> walking=Employees.FindAll(e=>e.Travelling&&Free(e));
                for(int i=0;i<walking.Count;i++)for(int j=i+1;j<walking.Count;j++)
                {
                    OfficeEmployee a=walking[i],b=walking[j];
                    if(a.Partner||b.Partner)continue;
                    // Only people walking past each other (heading in opposite directions), not walking together.
                    if(Vector3.Dot(a.transform.forward,b.transform.forward)>-.3f)continue;
                    if((chattedUntil.TryGetValue(a,out float restA)&&Time.time<restA)||(chattedUntil.TryGetValue(b,out float restB)&&Time.time<restB))continue;
                    Vector3 gap=a.transform.position-b.transform.position;if(Mathf.Abs(gap.y)>.5f)continue;gap.y=0;
                    if(gap.sqrMagnitude>1.9f*1.9f)continue;
                    long key=Mathf.Min(a.EmployeeId,b.EmployeeId)*100000L+Mathf.Max(a.EmployeeId,b.EmployeeId);
                    if(passed.TryGetValue(key,out float until)&&Time.time<until)continue;
                    passed[key]=Time.time+45;
                    // Half the time they stop and chat; otherwise they walk on, often with a quick hello.
                    if(chatter.NextDouble()<.5)StartConversation(a,b);
                    else if(chatter.NextDouble()<.6){string last=null;(chatter.NextDouble()<.5?a:b).Say(OfficeDialogue.Pick(OfficeDialogue.Greetings,chatter,ref last));}
                }
            }
            for(int i=chats.Count-1;i>=0;i--)
            {
                Chat chat=chats[i];
                bool broken=!chat.A||!chat.B||chat.A.Partner!=chat.B||chat.B.Partner!=chat.A||chat.A.AngerLevel>=3||chat.B.AngerLevel>=3||chat.A.Pushed||chat.B.Pushed;
                if(broken||(chat.Next>=chat.Lines.Length&&Time.time>=chat.At)){EndConversation(chat);chats.RemoveAt(i);continue;}
                if(Time.time<chat.At)continue;
                OfficeEmployee speaker=chat.Next%2==0?chat.A:chat.B;
                speaker.Say(chat.Lines[chat.Next++]);
                chat.At=speaker.SpeechUntil+.35f;
            }
        }
        public bool StartConversation(OfficeEmployee a,OfficeEmployee b)
        {
            if(!Free(a)||!Free(b)||a==b)return false;
            a.Partner=b;b.Partner=a;
            chats.Add(new Chat{A=a,B=b,Lines=OfficeDialogue.Conversations[chatter.Next(OfficeDialogue.Conversations.Length)],At=Time.time+.4f});
            return true;
        }
        void EndConversation(Chat chat)
        {
            if(chat.A&&chat.A.Partner==chat.B)chat.A.Partner=null;if(chat.B&&chat.B.Partner==chat.A)chat.B.Partner=null;
            if(chat.A)chattedUntil[chat.A]=Time.time+60;if(chat.B)chattedUntil[chat.B]=Time.time+60;
        }
        // The presenter keeps presenting; their words show in a bubble while you are in the room.
        void UpdatePresenter()
        {
            if(!playerInMeeting||Time.time<nextPresenterLine)return;
            OfficeEmployee presenter=Employees.Find(IsMeetingSpeaker);
            if(!presenter||presenter.AngerLevel>=3)return;
            presenter.Say(MeetingLine);nextPresenterLine=presenter.SpeechUntil+1.2f;
        }
        // Bumping: walking into an employee (seated or standing) raises that one employee's anger by one level.
        // Each contact counts once; step away and walk into them again to bump again.
        public int CheckBumps(Vector3 position,Vector3 motion)
        {
            int bumps=0;
            foreach(OfficeEmployee employee in Employees)
            {
                if(employee.Dead)continue;
                Vector3 delta=employee.transform.position-position;float height=delta.y;delta.y=0;float gap=delta.magnitude;
                // Someone in a chair is reached across the chair, so contact starts a little further out.
                float reach=employee.Robot.Seated?1.02f:.74f;
                bool contact=gap<reach&&Mathf.Abs(height)<1;
                bool into=contact&&motion.magnitude>.5f&&Vector3.Dot(motion.normalized,delta.normalized)>.3f;
                // Walking into someone pushes them out of the way for as long as you keep walking into them.
                if(into)employee.Shove(delta.normalized*motion.magnitude*.95f);
                if(contact&&!touching.Contains(employee))
                {
                    touching.Add(employee);
                    if(into){employee.Bump();bumps++;}
                }
                else if(!contact&&gap>reach+.2f)touching.Remove(employee);
            }
            return bumps;
        }
        public string MeetingLine
        {
            get
            {
                string[] lines={"The productivity target is now one hundred and twelve percent.","Should we schedule a meeting to discuss fewer meetings?","That request requires a separate approval meeting.","Please remember: all personal time is company property."};
                return lines[(int)(Time.time/5)%lines.Length];
            }
        }
        void AddRoomDoors()
        {
            foreach(var link in Map.Manifest.Links)
            {
                if(link.A==Plan.TargetRoom||link.B==Plan.TargetRoom||Map.Manifest.Rooms[link.A].Layer!=Map.Manifest.Rooms[link.B].Layer)continue;
                if(Plan.HasDoor(link.A,link.B))CreateDoor(link.A,link.B,false);
            }
        }
        // Door signs name the room behind them.
        string DoorName(int room)
        {
            switch(Plan.Rooms[room].Kind)
            {
                case OfficeRoomKind.Executive:return "PRIVATE OFFICE";
                case OfficeRoomKind.Lounge:return "STAFF LOUNGE";
                case OfficeRoomKind.Breakroom:return "COFFEE BAR";
                case OfficeRoomKind.Restroom:return "RESTROOM";
                case OfficeRoomKind.Server:return "SERVER ROOM";
                case OfficeRoomKind.Records:return "RECORDS";
                case OfficeRoomKind.Mailroom:return "MAILROOM";
                default:return OfficePlan.Departments[Plan.Rooms[room].Department];
            }
        }
        void CreateDoor(int aId,int bId,bool locked)
        {
            Vector3 a=Map.Center(Map.Manifest.Rooms[aId]),b=Map.Center(Map.Manifest.Rooms[bId]);
            Transform root=Kit.A.Group(transform,locked?"Badge-controlled door":"Operable office door",transform.InverseTransformPoint((a+b)*.5f));root.rotation=Quaternion.LookRotation(b-a);
            OfficeDoor door=root.gameObject.AddComponent<OfficeDoor>();door.Office=this;door.RoomA=aId;door.RoomB=bId;door.RequiredClearance=locked?2:0;door.Department=Plan.Rooms[aId].Department;door.Unlocked=!locked;
            // A doorframe wall fills the passage; one leaf hangs in it. The vault door is wide enough for its cargo.
            float corridor=Map.Manifest.Recipe.Settings.CorridorWidthMillimeters/1000f,width=locked?2.2f:1.2f,panel=(corridor-width)/2,top=2.5f;
            Material leaf=locked?Kit.A.Metal:Kit.A.Wood;
            // The frame is solid architecture, not part of the moving door, so navigation treats it as a wall.
            Transform frame=Kit.A.Group(transform,"Doorframe",root.localPosition);frame.localRotation=root.localRotation;
            for(int side=-1;side<=1;side+=2)
            {
                Kit.A.Box(frame,"Doorframe wall",new Vector3(side*(width/2+panel/2),1.6f,0),new Vector3(panel,3.2f,.14f),Kit.A.Plaster,true,false);
                Kit.A.Box(frame,"Door jamb",new Vector3(side*(width/2+.04f),top/2,0),new Vector3(.08f,top,.18f),Kit.A.Dark);
            }
            Kit.A.Box(frame,"Door header",new Vector3(0,(top+3.2f)/2,0),new Vector3(width,3.2f-top,.14f),Kit.A.Plaster,true,false);
            door.Hinge=Kit.A.Group(root,"Door hinge",new Vector3(-width/2,0,0));
            Kit.A.Box(door.Hinge,"Door leaf",new Vector3(width/2,top/2,0),new Vector3(width-.04f,top-.03f,.06f),leaf,true);
            for(int face=-1;face<=1;face+=2)Kit.A.Box(door.Hinge,"Door handle",new Vector3(width-.14f,1.02f,face*.06f),new Vector3(.12f,.035f,.04f),Kit.A.Brass);
            if(locked)Kit.A.Box(door.Hinge,"Vault stripe",new Vector3(width/2,1.25f,-.035f),new Vector3(width-.3f,.14f,.01f),Kit.A.Red);
            bool meeting=aId==Plan.MeetingRoom||bId==Plan.MeetingRoom;
            for(int face=-1;face<=1;face+=2)
            {
                Transform reader=Kit.A.Group(root,"Door control",new Vector3(width/2+.3f,1.3f,face*.1f),face<0?0:180);
                Kit.A.Box(reader,"Operable door button",Vector3.zero,new Vector3(.2f,.28f,.06f),Kit.A.Dark,true);
                Kit.A.Box(reader,"Door control lamp",new Vector3(0,.065f,-.035f),new Vector3(.14f,.03f,.01f),locked?Kit.A.WarmLight:Kit.A.Screen);
                Transform sign=Kit.A.Group(frame,"Door sign",new Vector3(0,(top+3.2f)/2,face*.08f),face<0?0:180);
                Kit.A.Label(sign,meeting?"MEETING IN PROGRESS":locked?"AUTHORIZED PERSONNEL":DoorName(face<0?bId:aId),Vector3.zero,.03f);
            }
            door.SetOpenForValidation(false);
            Doors.Add(door);
        }
    }
}








