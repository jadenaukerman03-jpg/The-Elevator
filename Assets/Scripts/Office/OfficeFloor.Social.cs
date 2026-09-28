using System;
using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.Office
{
    public sealed partial class OfficeFloor
    {
        public OfficeKeycard DeskKeycard { get; private set; }
        readonly Dictionary<int,float> meetingCooldown=new Dictionary<int,float>();
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
        void BuildDeskKeycard(OfficeTaskPoint supervisor)
        {
            // A reception spare always provides a discoverable route to the first contract.
            if(Plan.DeskBadge)Employees[0].HasBadge=false;
            Vector3 location=supervisor.transform.parent.TransformPoint(new Vector3(.56f,supervisor.Activity==OfficeTask.Reception?1.214f:.823f,.30f));
            Transform card=Kit.A.Group(transform,"VISIBLE SUPERVISOR KEYCARD",transform.InverseTransformPoint(location));
            card.rotation=supervisor.transform.rotation;
            Kit.A.Box(card,"Bright clearance card",Vector3.zero,new Vector3(.29f,.025f,.19f),Kit.A.WarmLight);
            Kit.A.Box(card,"Supervisor stripe",new Vector3(0,.018f,.048f),new Vector3(.25f,.005f,.045f),Kit.A.Red);
            Kit.A.Box(card,"Badge clip",new Vector3(0,.02f,.11f),new Vector3(.08f,.015f,.04f),Kit.A.Brass);
            BoxCollider collider=card.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(.31f,.08f,.23f);
            DeskKeycard=card.gameObject.AddComponent<OfficeKeycard>();DeskKeycard.Office=this;
            TextMesh print=Kit.A.W.Label("ACCESS / 02",card,new Vector3(0,.016f,-.018f),.006f,Color.black);print.transform.localRotation=Quaternion.Euler(90,0,0);
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
            OfficeEmployee owner=cargo.Workstation?cargo.Workstation.Occupant:null;
            bool disturbed=owner&&!owner.Travelling&&Vector3.Distance(owner.transform.position,cargo.Workstation.transform.position)<.8f;
            ReportAction(Game.Player.transform.position,22,disturbed?owner:null);
            if(disturbed){owner.React(62,"I was using that computer. Put it back.");cargo.Workstation.Equipment=null;cargo.Workstation.EquipmentMissing=true;}
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
        // Walking into the meeting turns every head, and each attendee gets angrier the longer you stay.
        public void CheckMeetingEntry(float dt)
        {
            if(Plan.MeetingRoom<0||!Game)return;
            bool inside=Map.NearestRoom(Game.Player.transform.position).Id==Plan.MeetingRoom;
            if(inside&&!playerInMeeting)InterruptMeeting();
            playerInMeeting=inside;
            if(!inside)return;
            foreach(OfficeEmployee employee in Employees)
                if(employee.HomeRoom==Plan.MeetingRoom&&!employee.Travelling)employee.Glare(dt*2.5f);
        }
        public string MeetingLine
        {
            get
            {
                string[] lines={"The productivity target is now one hundred and twelve percent.","Should we schedule a meeting to discuss fewer meetings?","That request requires a separate approval meeting.","Please remember: all personal time is company property."};
                return lines[(int)(Time.time/5)%lines.Length];
            }
        }
        void InterruptMeeting()
        {
            int room=Plan.MeetingRoom;
            if(meetingCooldown.TryGetValue(room,out float until)&&Time.time<until)return;
            int count=0;foreach(OfficeEmployee employee in Employees)
                if(employee.Station&&employee.Station.RoomId==room&&!employee.Travelling){employee.React(22,"This meeting is private. Can we help you?");count++;}
            if(count>0)meetingCooldown[room]=Time.time+20;
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
                Kit.A.Label(sign,meeting?"MEETING IN PROGRESS":locked?"CONTROLLED ASSET":DoorName(face<0?bId:aId),Vector3.zero,.03f);
            }
            door.SetOpenForValidation(false);
            Doors.Add(door);
        }
    }
}








