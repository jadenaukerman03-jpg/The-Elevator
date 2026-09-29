using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.Office
{
    public sealed partial class OfficeEmployee : MonoBehaviour
    {
        public OfficeFloor Office;
        public OfficeTaskPoint Station,HomeStation;
        public BusinessRobot Robot;
        public int EmployeeId,Department,Clearance=1,HomeRoom;
        public bool Supervisor,HasBadge=true,IsSecurity;
        public string Job,State="Working";
        public Vector3 LastObservedPosition;
        public float Suspicion,PickpocketProgress;
        public float Threshold { get { return Supervisor?55:72; } }
        public bool AtStation { get { return Station && Vector3.Distance(transform.position,Station.transform.position)<.35f; } }
        public bool Travelling { get { return route.Count>0; } }
        public OfficeCoffeeCup Cup { get; private set; }
        public int CompletedTrips { get; private set; }
        public string LastRouteFailure;
        public string NavigationStatus { get { return route.Count+" points / next "+(route.Count>0?route[0].ToString():"none")+" / recoveries "+recoveryAttempts; } }
        public int CoffeeStage { get; private set; }=-1;
        public float NextTaskTime { get { return nextTask; } }
        public CharacterController Motor { get { return motor; } }
        CharacterController motor;
        Renderer[] renderers;
        readonly List<Vector3> route=new List<Vector3>();
        System.Random random;
        float nextThink,nextTask,reportAt=-1,stuckTime,nextStep,nextWorkSound,coffeeTime,attentionUntil;
        Vector3 previous;
        int recoveryAttempts;
        float bestWaypointDistance=float.MaxValue;
        float nextDispatch;
        Transform stream;
        public void Initialize(OfficeFloor office,OfficeTaskPoint station,int id,bool supervisor)
        {
            Office=office;Station=HomeStation=station;station.HomeOwner=this;EmployeeId=id;Supervisor=supervisor;Clearance=supervisor?2:1;
            random=new System.Random(unchecked(office.Map.Manifest.Recipe.Seed*397+id*7919));
            HomeRoom=station.RoomId;Department=office.Plan.Rooms[HomeRoom].Department;station.Occupant=this;
            IsSecurity=!supervisor&&id%11==6;Job=supervisor?"Department supervisor":IsSecurity?"Security liaison":station.Activity==OfficeTask.Reception?"Receptionist":station.Activity==OfficeTask.Repair?"Systems technician":station.Activity==OfficeTask.Filing?"Records clerk":"Office associate";
            if(IsSecurity)Clearance=3;
            transform.position=station.transform.position;transform.rotation=station.transform.rotation;
            gameObject.layer=8;
            motor=gameObject.AddComponent<CharacterController>();motor.height=1.82f;motor.radius=.27f;motor.center=Vector3.up*.91f;motor.stepOffset=.28f;motor.skinWidth=.025f;
            foreach(OfficeEmployee other in office.Employees)if(other.Motor)Physics.IgnoreCollision(motor,other.Motor);
            Transform visual=office.Kit.A.Group(transform,"Synthetic employee",Vector3.zero);
            BeanOutfit outfit=IsSecurity?BeanOutfit.Security:supervisor?BeanOutfit.Office:station.Activity==OfficeTask.Reception?BeanOutfit.Reception:station.Activity==OfficeTask.Repair?BeanOutfit.Technician:station.Activity==OfficeTask.Filing?BeanOutfit.Clerk:BeanOutfit.Office;
            Robot=visual.gameObject.AddComponent<BusinessRobot>();Robot.Build(office.Kit.A,id,supervisor||IsSecurity,outfit);
            if(station.Activity==OfficeTask.Present){Job="Quarterly presenter";Robot.HoldPointer();}Robot.Activity=station.Activity;Robot.Seated=station.Seated;
            renderers=GetComponentsInChildren<Renderer>();nextTask=Time.time+18+id*3.17f+(float)random.NextDouble()*65;previous=transform.position;
        }
        public bool CanPickpocket(Transform player)
        {
            Vector3 delta=player.position-transform.position;delta.y=0;
            return HasBadge&&delta.magnitude<1.55f&&Vector3.Dot(transform.forward,delta.normalized)<-.2f&&Suspicion<75
                &&(!Physics.Linecast(player.position+Vector3.up*.8f,transform.position+Vector3.up*.9f,out RaycastHit hit,~((1<<2)|(1<<8)),QueryTriggerInteraction.Ignore));
        }
        public void Pickpocket(float dt)
        {
            if(!Office.Game||!CanPickpocket(Office.Game.Player.transform)){PickpocketProgress=0;return;}
            PickpocketProgress+=dt/(IsSecurity?3.5f:Supervisor?2.6f:1.8f);
            if(PickpocketProgress<1)return;
            HasBadge=false;PickpocketProgress=0;Office.GiveBadge(Clearance,Supervisor?Office.Plan.Rooms[Office.Plan.TargetRoom].Department:Department);
            Office.ReportAction(Office.Game.Player.transform.position,25,this);Office.Game.Notify("Badge lifted. Walk away like this is your job.");
        }
        public bool Sees(Vector3 position)
        {
            // Foam on the face: they can't see anything until they've wiped it off.
            if(Blinded)return false;
            bool crouched=Office.Game&&Office.Game.Player.Crouched&&(position-Office.Game.Player.transform.position).sqrMagnitude<2;
            Vector3 eye=transform.position+Vector3.up*(Robot&&Robot.Seated?WorkerController.EyeHeight-.04f:WorkerController.EyeHeight);
            Vector3 target=position+Vector3.up*(crouched?.64f:1.15f),delta=target-eye;
            if(delta.magnitude>(crouched?8:11)||Vector3.Angle(transform.forward,delta)>72)return false;
            return !Physics.Raycast(eye+transform.forward*.14f,(target-eye-transform.forward*.14f).normalized,Mathf.Max(0,delta.magnitude-.4f),~((1<<2)|(1<<8)),QueryTriggerInteraction.Ignore);
        }
        // Watching something suspicious raises this employee's hidden suspicion (enough of it alerts security) and
        // turns their head, but does not upset them: only what is done to them personally does (see Offend).
        public void React(float amount)
        {
            Suspicion=Mathf.Clamp(Suspicion+amount,0,100);attentionUntil=Time.time+3;
            if(Office.Game){LastObservedPosition=Office.Game.Player.transform.position;Office.Questioner=this;}
        }
        Vector3 push;
        float windUntil;
        public bool Pushed { get { return push.sqrMagnitude>.01f; } }
        bool shovedByPlayer;
        float glareUntil;
        // The player walking into a standing employee shoves them out of the way.
        public void Shove(Vector3 motion){if(Robot.Seated)return;Push(motion);shovedByPlayer=true;}
        // A steady shove, like a strong wind: while it keeps blowing the employee slides along with it.
        public void Push(Vector3 wind)
        {
            wind.y=0;windUntil=Time.time+.12f;
            push=Vector3.MoveTowards(push,wind,Time.deltaTime*30);
        }
        void Shoved()
        {
            Robot.Seated=false;motor.enabled=true;State="Pushed back";
            motor.Move((push+Vector3.down*5)*Time.deltaTime);
            if(Time.time>windUntil)push=Vector3.MoveTowards(push,Vector3.zero,Time.deltaTime*7);
            if(Pushed)return;
            // Shoved by the player: stop for a moment, glare at them and say so.
            if(shovedByPlayer){shovedByPlayer=false;glareUntil=Time.time+1.4f;attentionUntil=Time.time+3;Say(OfficeDialogue.Reaction(Grievance.Shove,Anger,random,ref lastLine));}
            // Once the wind stops, walk back to wherever they were going.
            if(!Travelling&&Station&&!AtStation&&!TryTravel(Station)){route.Clear();route.Add(Station.transform.position);}
        }
        void Update()
        {
            if(!Office.Game||!Office.Game.ControlsActive)return;
            if(Dead)return;
            float distance=Vector3.Distance(Office.Game.Player.transform.position,transform.position);
            if(Time.time>=nextThink)
            {
                foreach(Renderer r in renderers)if(r)r.enabled=distance<65;
                float interval=distance<Office.Plan.Config.ActiveDistance?.35f:2.5f;nextThink=Time.time+interval;
                if(distance<Office.Plan.Config.ActiveDistance)
                {
                    bool sees=Sees(Office.Game.Player.transform.position);
                    if(sees)
                    {
                        SawPlayer();
                        LastObservedPosition=Office.Game.Player.transform.position;
                        // On edge: they make a pointed remark when they spot the player again.
                        if(AngerLevel==4&&Complaint!=Grievance.None&&!Chasing&&!Speaking&&Time.time>nextSpotted){Say(OfficeDialogue.Pick(OfficeDialogue.Spotted,random,ref lastLine));nextSpotted=Time.time+45;}
                        // Walking, running or sitting nearby is not suspicious; only visible theft is.
                        float suspicious=0;
                        if(Office.Transported)suspicious+=10;
                        if(Office.Game.Player.Held&&!Office.Game.Player.Held.IsBattery)suspicious+=4;
                        if(Office.Map.NearestRoom(LastObservedPosition).Id==Office.Plan.TargetRoom&&!Office.HasAccess(2,Office.Plan.Rooms[Office.Plan.TargetRoom].Department))suspicious+=12;
                        if(Office.Blending)suspicious=0;
                        // Anger cools slowly when nothing suspicious is happening.
                        Suspicion=Mathf.Clamp(Suspicion+(suspicious>0?suspicious:-.35f)*interval,0,100);
                        if(Suspicion>20)attentionUntil=Time.time+2;
                    }
                    else Suspicion=Mathf.Max(0,Suspicion-interval*.35f);
                    if(Suspicion>40&&sees)Office.Questioner=this;
                    if(Suspicion>Threshold&&reportAt<0)reportAt=Time.time+5;
                    if(reportAt>0&&Time.time>reportAt){Office.ReportToSecurity(Department,25,LastObservedPosition);reportAt=-1;Suspicion=Mathf.Max(25,Suspicion-12);}
                }
                if(IsSecurity&&Office.SecurityAlert>=20&&Time.time>nextDispatch&&!Travelling&&!Office.Game.Player.InCabin)
                {
                    nextDispatch=Time.time+12;
                    int room=Office.Map.NearestRoom(Office.LastReportedPosition).Id;
                    OfficeTaskPoint investigate=Office.Stations.Find(s=>s.RoomId==room&&s.Available&&s.Activity!=OfficeTask.Meeting&&s.Activity!=OfficeTask.Present);
                    if(investigate)TryTravel(investigate);
                }
                if(IsSecurity&&Office.Alarm&&distance<1.25f&&Sees(Office.Game.Player.transform.position))Office.Game.Player.Knock(transform.position);
                if(!Travelling&&CoffeeStage<0&&Time.time>nextTask&&Suspicion<40&&!Supervisor&&HomeRoom!=Office.Plan.MeetingRoom&&HomeStation.Activity!=OfficeTask.Reception)ChooseTask();
            }
            Temper();UpdateSpeech();UpdateChase(distance);
            // A started trip always completes, even when the player leaves: no frozen doorway occupants.
            if(Pushed)Shoved();
            else if(Blinded||Time.time<glareUntil||Partner)Hold();
            else if(Travelling)Walk();
            else if(Station&&AtStation&&!Chasing)
            {
                // The body stays solid at work, seated or standing: nobody walks through a person.
                motor.enabled=true;Robot.Seated=Station.Seated;Robot.Activity=Station.Activity;
                if(Station.Coffee)TickCoffee(Time.deltaTime);
                else if(Station.EquipmentMissing){State="Workstation missing";Robot.Activity=OfficeTask.Reading;}
                else State=Station.Activity==OfficeTask.Meeting?"Discussing quarterly targets":"Working";
            }
            if(wiping&&!Blinded){wiping=false;Robot.Reaching=Cup;}
            if(Partner&&Time.time>=attentionUntil)Robot.LookTarget=Partner.Robot.Head?Partner.Robot.Head.position:Partner.transform.position+Vector3.up*1.3f;
            else if(Time.time<attentionUntil)Robot.LookTarget=Office.Game.Player.transform.position+Vector3.up*(Office.Game.Player.Crouched?WorkerController.CrouchEyeHeight:WorkerController.EyeHeight);
            else if(Station&&Station.Activity==OfficeTask.Meeting)
            {
                OfficeEmployee colleague=Office.Employees.Find(e=>e!=this&&e.Station&&e.Station.RoomId==Station.RoomId&&!e.Travelling);
                Robot.LookTarget=colleague?colleague.transform.position+Vector3.up*1.2f:Vector3.zero;
            }
            else Robot.LookTarget=Vector3.zero;
            Robot.Talking=Office.IsMeetingSpeaker(this)||PhraseShowing;
            if(Robot.Activity==OfficeTask.Present)Robot.PointAt=Office.BoardPoint(Time.time);
            Robot.Mood=AngerLevel;
            Robot.Speed=Vector3.Distance(transform.position,previous)/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;
            if(Cup&&CoffeeStage<0){Cup.transform.position=transform.position+transform.forward*.36f+transform.right*.23f+Vector3.up*1.10f;Cup.transform.rotation=Quaternion.identity;Robot.Reaching=true;Robot.ReachTarget=Cup.transform.position+Vector3.up*.07f;}
            Robot.Animate(Time.deltaTime);
            PoseWeapon();
            if(Robot.Speed>.3f&&Time.time>nextStep&&distance<12){Office.PlayTone(transform.position,110,.022f);nextStep=Time.time+.48f;}
            if(!Travelling&&Station&&Station.Activity==OfficeTask.Typing&&Time.time>nextWorkSound&&distance<12){Office.PlayTone(transform.position,280+EmployeeId%5*17,.025f);nextWorkSound=Time.time+.4f+(float)random.NextDouble();}
        }
        void Walk()
        {
            Robot.Seated=false;motor.enabled=true;
            while(route.Count>0&&(new Vector2(transform.position.x-route[0].x,transform.position.z-route[0].z)).magnitude<.085f&&Mathf.Abs(transform.position.y-route[0].y)<.5f){route.RemoveAt(0);bestWaypointDistance=float.MaxValue;stuckTime=0;}
            if(route.Count==0){if(!Chasing)Arrive();return;}
            Vector3 delta=route[0]-transform.position;delta.y=0;Vector3 heading=delta.normalized;
            bool wait=false;
            foreach(OfficeDoor door in Office.Doors)if(Vector3.Distance(door.transform.position,transform.position)<2.5f&&!door.RequestPass(this))wait=true;
            Vector3 separation=Vector3.zero;
            foreach(OfficeEmployee other in Office.Employees)
            {
                if(other==this||Mathf.Abs(other.transform.position.y-transform.position.y)>.7f)continue;
                Vector3 away=transform.position-other.transform.position;away.y=0;float gap=away.magnitude;
                if(gap>.85f)continue;
                if(gap>.01f)separation+=away.normalized*(.85f-gap)*1.3f;
                if(other.Travelling&&gap<.65f&&Vector3.Dot(heading,-away.normalized)>.35f&&EmployeeId>other.EmployeeId)wait=true;
            }
            Vector3 desired=(heading+separation).normalized;
            Vector3 before=transform.position;
            if(!wait)
            {
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(desired),160*Time.deltaTime);
                float alignment=Mathf.Clamp(Vector3.Dot(transform.forward,desired),.25f,1);
                motor.Move((desired*(Chasing?2.1f:1.22f)*alignment+Vector3.down*5)*Time.deltaTime);
            }
            else
            {
                State="Yielding";
                Vector3 side=Vector3.Cross(Vector3.up,heading)*.3f;
                if(OfficeNavigation.Clear(transform.position+side))motor.Move((side+Vector3.down*5)*Time.deltaTime);
            }
            float remaining=Vector3.Distance(transform.position,route[0]);
            if(remaining<bestWaypointDistance-.05f){bestWaypointDistance=remaining;stuckTime=0;}else stuckTime+=Time.deltaTime;
            if(stuckTime>3.5f&&Chasing){stuckTime=0;route.Clear();nextChasePlan=0;return;}
            if(stuckTime>3.5f)
            {
                stuckTime=0;recoveryAttempts++;
                OfficeTaskPoint target=recoveryAttempts<3?Station:HomeStation;
                route.Clear();if(!TryTravel(target)){nextTask=Time.time+3+EmployeeId*.13f;State="Replanning around obstruction";}
            }
        }
        // Standing still: glaring at the player, wiping foam off their face, or chatting with a colleague.
        void Hold()
        {
            Vector3 face=Vector3.zero;
            if(Blinded)
            {
                State="Wiping foam off";wiping=true;Robot.Reaching=true;
                if(Robot.Head)Robot.ReachTarget=Robot.Head.position+transform.forward*.2f-Vector3.up*.05f;
            }
            else if(Partner){State="Chatting";face=Partner.transform.position;}
            else{State="Glaring";face=Office.Game.Player.transform.position;}
            Vector3 toward=face-transform.position;toward.y=0;
            if(face!=Vector3.zero&&toward.sqrMagnitude>.01f&&!Robot.Seated)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(toward),240*Time.deltaTime);
        }
        // Hallway conversation partner, set by the floor while two colleagues stop to talk.
        public OfficeEmployee Partner;
        void Arrive()
        {
            if(!Station)return;
            // No teleport through scenery: only snap the final few centimetres of an already traversed route.
            transform.position=Station.transform.position;transform.rotation=Station.transform.rotation;Physics.SyncTransforms();
            Robot.Seated=Station.Seated;Robot.Activity=Station.Activity;CompletedTrips++;recoveryAttempts=0;
            nextTask=Time.time+(Station==HomeStation?35+(float)random.NextDouble()*65:8+(float)random.NextDouble()*10);
        }
        public bool TryTravel(OfficeTaskPoint next)
        {
            if(!next||next.ReservedByPlayer||(next.Occupant&&next.Occupant!=this)||(next.HomeOwner&&next.HomeOwner!=this))return false;
            if(!Office.CanTravel(this,next)){LastRouteFailure="Traffic budget";return false;}
            List<Vector3> planned=new List<Vector3>();if(!OfficeNavigation.Build(this,next,planned)){LastRouteFailure="No collision-free route";return false;}
            if(Station&&Station.Occupant==this)Station.Occupant=null;
            LastRouteFailure=null;Station=next;next.Occupant=this;bestWaypointDistance=float.MaxValue;route.Clear();route.AddRange(planned);Robot.Seated=false;State="Walking to "+next.Activity;return true;
        }
        void ChooseTask()
        {
            nextTask=Time.time+8+(float)random.NextDouble()*17;
            if(Station!=HomeStation){TryTravel(HomeStation);return;}
            if(Cup){Destroy(Cup.gameObject);Cup=null;Robot.Reaching=false;}
            List<OfficeTaskPoint> choices=Office.Stations.FindAll(s=>s.Available&&s.RoomId>1&&s.RoomId!=Office.Plan.TargetRoom&&s.RoomId!=Office.Plan.MeetingRoom&&s.Coffee&&Office.Map.Manifest.Rooms[s.RoomId].Layer==Office.Map.Manifest.Rooms[HomeRoom].Layer);
            choices.Sort((a,b)=>(a.transform.position-transform.position).sqrMagnitude.CompareTo((b.transform.position-transform.position).sqrMagnitude));
            if(choices.Count>4)choices.RemoveRange(4,choices.Count-4);
            while(choices.Count>0){int index=random.Next(choices.Count);OfficeTaskPoint next=choices[index];choices.RemoveAt(index);if(TryTravel(next))return;}
        }
        public void TickCoffee(float dt)
        {
            OfficeCoffeeStation station=Station?Station.Coffee:null;if(!station)return;
            if(CoffeeStage<0)
            {
                if(Cup)return;
                Cup=Office.Kit.A.Group(Office.transform,"Employee coffee cup",Vector3.zero).gameObject.AddComponent<OfficeCoffeeCup>();Cup.Build(Office.Kit.A);
                Cup.transform.position=station.CupStorage;coffeeTime=0;CoffeeStage=0;
                stream=Office.Kit.A.Round(Office.transform,"Coffee pouring stream",Vector3.zero,new Vector3(.011f,.03f,.011f),Office.Kit.A.Wood).transform;
            }
            coffeeTime+=dt;Robot.Reaching=true;
            if(coffeeTime<2){CoffeeStage=0;State="Opening mug cupboard";station.SetOpen(coffeeTime/1.2f);Cup.transform.position=station.CupStorage;}
            else if(coffeeTime<4){CoffeeStage=1;State="Taking a clean cup";station.SetOpen(1-(coffeeTime-2)/2);Cup.transform.position=Vector3.Lerp(station.CupStorage,station.PourPoint,(coffeeTime-2)/2);}
            else if(coffeeTime<9){CoffeeStage=2;State="Filling coffee";Cup.transform.position=station.PourPoint;Cup.SetFill((coffeeTime-4)/5);station.SetOpen(0);}
            else if(coffeeTime<11){CoffeeStage=3;State="Lifting coffee";Cup.transform.position=Vector3.Lerp(station.PourPoint,Robot.Head.position+transform.forward*.19f+Vector3.up*.10f,(coffeeTime-9)/2);}
            else if(coffeeTime<13){CoffeeStage=4;State="Drinking coffee";Cup.SetFill(1-(coffeeTime-11)*.13f);Cup.transform.rotation=Quaternion.Euler(-22,transform.eulerAngles.y,0);}
            else
            {
                CoffeeStage=-1;station.SetOpen(0);if(stream)Destroy(stream.gameObject);stream=null;Robot.Reaching=true;nextTask=Time.time;State="Taking coffee back to desk";return;
            }
            if(stream){stream.gameObject.SetActive(CoffeeStage==2);stream.position=station.PourPoint+Vector3.up*.145f;}
            if(CoffeeStage!=4)Cup.transform.rotation=Quaternion.identity;
            Robot.ReachTarget=Cup.transform.position+Vector3.up*.065f;
        }
        void OnDestroy()
        {
            if(Station&&Station.Occupant==this)Station.Occupant=null;if(HomeStation&&HomeStation.HomeOwner==this)HomeStation.HomeOwner=null;
            if(Cup)Destroy(Cup.gameObject);if(stream)Destroy(stream.gameObject);
            if(weapon)Destroy(weapon.gameObject);if(spoken)Destroy(spoken);
        }
    }
}






