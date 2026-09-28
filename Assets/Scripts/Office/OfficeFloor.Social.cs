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
                int capacity=room.RoomId==Plan.MeetingRoom?6:room.Kind==OfficeRoomKind.Workroom?Math.Min(2,Plan.Config.EmployeesPerRoom):1;
                foreach(OfficeTaskPoint task in Stations)
                {
                    if(capacity<=0||Employees.Count>=Plan.Config.PopulationCap)break;
                    if(task.RoomId!=room.RoomId||task.Occupant||task.Activity==OfficeTask.Coffee)continue;
                    if(room.RoomId==Plan.MeetingRoom&&task.Activity!=OfficeTask.Meeting)continue;
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
        public bool IsMeetingSpeaker(OfficeEmployee employee)
        {
            if(Plan.MeetingRoom<0||!employee.AtStation||employee.HomeRoom!=Plan.MeetingRoom)return false;
            var attendees=Employees.FindAll(e=>e.AtStation&&e.HomeRoom==Plan.MeetingRoom);
            return attendees.Count>0&&attendees[(int)(Time.time/5)%attendees.Count]==employee;
        }
        public string MeetingLine
        {
            get
            {
                string[] lines={"The productivity target is now one hundred and twelve percent.","Should we schedule a meeting to discuss fewer meetings?","That request requires a separate approval meeting.","Please remember: all personal time is company property."};
                return lines[(int)(Time.time/5)%lines.Length];
            }
        }
        public void InterruptMeeting(int roomA,int roomB)
        {
            int room=Plan.MeetingRoom;if(room<0||(roomA!=room&&roomB!=room))return;
            if(meetingCooldown.TryGetValue(room,out float until)&&Time.time<until)return;
            int count=0;foreach(OfficeEmployee employee in Employees)
                if(employee.Station&&employee.Station.RoomId==room&&!employee.Travelling){employee.React(14,"This meeting is private. Can we help you?");count++;}
            if(count>0)meetingCooldown[room]=Time.time+20;
        }
        void AddRoomDoors()
        {
            foreach(var link in Map.Manifest.Links)
            {
                if(link.A==Plan.TargetRoom||link.B==Plan.TargetRoom||Map.Manifest.Rooms[link.A].Layer!=Map.Manifest.Rooms[link.B].Layer)continue;
                CreateDoor(link.A,link.B,false);
            }
        }
        void CreateDoor(int aId,int bId,bool locked)
        {
            Vector3 a=Map.Center(Map.Manifest.Rooms[aId]),b=Map.Center(Map.Manifest.Rooms[bId]);
            Transform root=Kit.A.Group(transform,locked?"Badge-controlled door":"Operable office door",transform.InverseTransformPoint((a+b)*.5f));root.rotation=Quaternion.LookRotation(b-a);
            OfficeDoor door=root.gameObject.AddComponent<OfficeDoor>();door.Office=this;door.RoomA=aId;door.RoomB=bId;door.RequiredClearance=locked?2:0;door.Department=Plan.Rooms[aId].Department;door.Unlocked=!locked;
            door.Left=Kit.A.Box(root,"Door left",new Vector3(-.69f,1.4f,0),new Vector3(1.36f,2.8f,.12f),locked?Kit.A.Metal:Kit.A.Wood,true).transform;
            door.Right=Kit.A.Box(root,"Door right",new Vector3(.69f,1.4f,0),new Vector3(1.36f,2.8f,.12f),locked?Kit.A.Metal:Kit.A.Wood,true).transform;
            for(int side=-1;side<=1;side+=2)
            {
                Transform reader=Kit.A.Group(root,"Door control",new Vector3(1.49f,1.3f,side*.17f),side<0?0:180);
                Kit.A.Box(reader,"Operable door button",Vector3.zero,new Vector3(.23f,.31f,.12f),Kit.A.Dark,true);
                Kit.A.Box(reader,"Door control lamp",new Vector3(0,.065f,-.07f),new Vector3(.16f,.035f,.01f),locked?Kit.A.WarmLight:Kit.A.Screen);
                Kit.A.Label(reader,locked?"BADGE / 02":"OPEN / CLOSE",new Vector3(0,-.03f,-.075f),.013f);
            }
            bool meeting=aId==Plan.MeetingRoom||bId==Plan.MeetingRoom;
            Kit.A.Label(root,meeting?"MEETING IN PROGRESS":locked?"CONTROLLED ASSET":"M / OFFICE",new Vector3(0,2.25f,-.075f),.032f);
            Doors.Add(door);
        }
    }
}








