using System.Collections.Generic;
using UnityEngine;
using TheElevator.Generation;
namespace TheElevator.Office
{
    public sealed class OfficeEmployee : MonoBehaviour
    {
        public OfficeFloor Office;
        public OfficeTaskPoint Station;
        public BusinessRobot Robot;
        public int EmployeeId,Department,Clearance=1,HomeRoom;
        public bool Supervisor,HasBadge=true;
        public bool IsSecurity;
        public string Job;
        public Vector3 LastObservedPosition;
        public float Suspicion,PickpocketProgress;
        public string State="Working";
        public float Threshold { get { return Supervisor?55:72; } }
        CharacterController motor;
        Renderer[] renderers;
        readonly List<Vector3> route=new List<Vector3>();
        float nextThink,nextTask,reportAt=-1,stuckTime,nextStep,nextDispatch,nextWorkSound;
        Vector3 previous;
        public void Initialize(OfficeFloor office,OfficeTaskPoint station,int id,bool supervisor)
        {
            Office=office;Station=station;EmployeeId=id;Supervisor=supervisor;Clearance=supervisor?2:1;
            HomeRoom=station.RoomId;Department=office.Plan.Rooms[HomeRoom].Department;station.Occupant=this;
            IsSecurity=!supervisor&&id%11==6;Job=supervisor?"Department supervisor":IsSecurity?"Security liaison":station.Activity==OfficeTask.Reception?"Receptionist":station.Activity==OfficeTask.Repair?"Systems technician":station.Activity==OfficeTask.Filing?"Records clerk":"Office associate";
            if(IsSecurity)Clearance=3;
            transform.position=station.transform.position;transform.rotation=station.transform.rotation;
            motor=gameObject.AddComponent<CharacterController>();motor.height=1.82f;motor.radius=.28f;motor.center=Vector3.up*.91f;motor.stepOffset=.28f;
            Transform visual=office.Kit.A.Group(transform,"Employee rig",Vector3.zero);
            Robot=visual.gameObject.AddComponent<BusinessRobot>();Robot.Build(office.Kit.A,id,supervisor||IsSecurity);Robot.Activity=station.Activity;Robot.Seated=station.Seated;
            renderers=GetComponentsInChildren<Renderer>();nextTask=25+id%17*3;previous=transform.position;
        }
        public bool CanPickpocket(Transform player)
        {
            Vector3 delta=player.position-transform.position;delta.y=0;
            return HasBadge&&delta.magnitude<1.55f&&Vector3.Dot(transform.forward,delta.normalized)<-.2f&&Suspicion<75
                &&(!Physics.Linecast(player.position+Vector3.up*1.5f,transform.position+Vector3.up*1.5f,out RaycastHit hit,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||hit.collider.GetComponentInParent<OfficeEmployee>()==this);
        }
        public void Pickpocket(float dt)
        {
            if(!Office.Game||!CanPickpocket(Office.Game.Player.transform)) { PickpocketProgress=0;return; }
            PickpocketProgress+=dt/(IsSecurity?3.5f:Supervisor?2.6f:1.8f);
            if(PickpocketProgress>=1)
            {
                HasBadge=false;PickpocketProgress=0;Office.GiveBadge(Clearance,Supervisor?Office.Plan.Rooms[Office.Plan.TargetRoom].Department:Department);
                Office.ReportAction(Office.Game.Player.transform.position,25,this);
                Office.Game.Notify("Badge lifted. Walk away like this is your job.");
            }
        }
        public bool Sees(Vector3 position)
        {
            Vector3 delta=position+Vector3.up*1.15f-(transform.position+Vector3.up*1.6f);
            if(delta.magnitude>11||Vector3.Angle(transform.forward,delta)>72)return false;
            return !Physics.Raycast(transform.position+Vector3.up*1.6f+transform.forward*.31f,delta.normalized,Mathf.Max(0,delta.magnitude-.7f),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        }
        void Update()
        {
            if(!Office.Game||!Office.Game.ControlsActive)return;
            float distance=Vector3.Distance(Office.Game.Player.transform.position,transform.position);
            bool visible=distance<65;
            if(Time.time>=nextThink)
            {
                foreach(Renderer r in renderers)if(r)r.enabled=visible;
                float interval=distance<Office.Plan.Config.ActiveDistance?.3f:2.5f;nextThink=Time.time+interval;
                if(distance<Office.Plan.Config.ActiveDistance)
                {
                    bool sees=Sees(Office.Game.Player.transform.position);
                    if(sees)
                    {
                        LastObservedPosition=Office.Game.Player.transform.position;
                        float suspicious=Office.Game.Player.MovingFast?4:0;
                        if(Office.Transported)suspicious+=10;
                        if(Office.Game.Player.Held&&!Office.Game.Player.Held.IsBattery)suspicious+=4;
                        if(Office.Map.NearestRoom(Office.Game.Player.transform.position).Id==Office.Plan.TargetRoom&&!Office.HasAccess(2,Office.Plan.Rooms[Office.Plan.TargetRoom].Department))suspicious+=12;
                        if(Office.Blending)suspicious-=8;
                        Suspicion=Mathf.Clamp(Suspicion+(suspicious-1)*interval,0,100);
                        Robot.LookTarget=Suspicion>20?Office.Game.Player.transform.position+Vector3.up*1.6f:Vector3.zero;
                    }
                    else Suspicion=Mathf.Max(0,Suspicion-interval*1.5f);
                    State=Suspicion>Threshold?"Reporting":Suspicion>40?"Badge check":Suspicion>20?"Watching":"Working";
                    if(Suspicion>40&&sees)Office.Questioner=this;
                    if(Suspicion>Threshold&&reportAt<0)reportAt=Time.time+5;
                    if(reportAt>0&&Time.time>reportAt)
                    { Office.ReportToSecurity(Department,25,LastObservedPosition);reportAt=-1;Suspicion=Mathf.Max(25,Suspicion-12); }
                }
                // Distant employees keep their reservations and coarse schedules; no perception/navigation work.
                if(distance>Office.Plan.Config.ActiveDistance)return;
                if(IsSecurity&&Office.SecurityAlert>=20&&Time.time>nextDispatch&&!Office.Game.Player.InCabin)
                {
                    Vector3 target=seesPlayer()?Office.Game.Player.transform.position:Office.LastReportedPosition;
                    int from=Office.Map.NearestRoom(transform.position).Id,to=Office.Map.NearestRoom(target).Id;
                    if(to!=0)
                    {
                        if(Station){Station.Occupant=null;Station=null;}
                        route.Clear();route.AddRange(Office.Map.Route(from,to));route.Add(target);State="Security inquiry";nextDispatch=Time.time+3;
                    }
                    if(Office.Alarm&&distance<1.25f&&Sees(Office.Game.Player.transform.position))Office.Game.Player.Hurt(transform.position);
                }
                if(!Supervisor&&Time.time>nextTask&&route.Count==0&&Suspicion<40)ChooseTask();
            }
            if(distance>Office.Plan.Config.ActiveDistance)return;
            if(route.Count>0)
            {
                Robot.Seated=false;motor.enabled=true;
                while(route.Count>0&&Vector3.Distance(transform.position,route[0])<.34f)route.RemoveAt(0);
                if(route.Count>0)
                {
                    Vector3 delta=route[0]-transform.position;delta.y=0;
                    if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),100*Time.deltaTime);
                    float alignment=Mathf.Clamp01(Vector3.Dot(transform.forward,delta.normalized));
                    Vector3 before=transform.position;motor.Move((delta.normalized*1.35f*alignment+Vector3.down*5)*Time.deltaTime);
                    stuckTime=(transform.position-before).sqrMagnitude<.000001f?stuckTime+Time.deltaTime:0;
                    if(stuckTime>4) { route.Clear();Station.Occupant=null;Station=null;nextTask=Time.time+2;stuckTime=0;State="Task obstructed"; }
                }
                else if(Station) { motor.enabled=false;transform.position=Station.transform.position;transform.rotation=Station.transform.rotation;Robot.Seated=Station.Seated;Robot.Activity=Station.Activity; }
            }
            else if(Station) { motor.enabled=false;Robot.Seated=Station.Seated; }
            Robot.Speed=Vector3.Distance(transform.position,previous)/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;
            Robot.Animate(Time.deltaTime);
            if(Robot.Speed>.3f&&Time.time>nextStep&&distance<12){Office.PlayTone(transform.position,110,.022f);nextStep=Time.time+.48f;}
            if(Robot.Speed<.1f&&Time.time>nextWorkSound&&distance<12&&Robot.Activity==OfficeTask.Typing){Office.PlayTone(transform.position,280+EmployeeId%5*17,.04f);nextWorkSound=Time.time+.3f+EmployeeId%4*.07f;}
        }
        bool seesPlayer(){return Sees(Office.Game.Player.transform.position);}
        void ChooseTask()
        {
            List<OfficeTaskPoint> options=Office.Stations.FindAll(s=>s.Available&&Office.Plan.Rooms[s.RoomId].Clearance<=1&&(Office.Plan.Rooms[s.RoomId].Department==Department||s.Activity==OfficeTask.Coffee));
            nextTask=Time.time+35+EmployeeId%9*4;if(options.Count==0)return;
            OfficeTaskPoint next=options[(EmployeeId+(int)(Time.time/40))%options.Count];
            if(Station)Station.Occupant=null;Station=next;next.Occupant=this;
            int from=Office.Map.NearestRoom(transform.position).Id;route.Clear();
            Vector3 exit=Office.Map.Anchor(Office.Map.Manifest.Rooms[from]);route.Add(new Vector3(exit.x,exit.y,transform.position.z));
            route.AddRange(Office.Map.Route(from,next.RoomId));
            Vector3 entry=Office.Map.Anchor(Office.Map.Manifest.Rooms[next.RoomId]);route.Add(new Vector3(entry.x,entry.y,next.transform.position.z));route.Add(next.transform.position);
        }
        void OnDestroy(){if(Station&&Station.Occupant==this)Station.Occupant=null;}
    }
}
