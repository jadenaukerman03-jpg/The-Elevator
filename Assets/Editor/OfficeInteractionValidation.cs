using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TheElevator.Office;
namespace TheElevator.Editor
{
    [InitializeOnLoad]
    public static class OfficeInteractionValidation
    {
        const string Key="Office.InteractionValidation";
        static int stage;
        static float started;
        static SalvageItem computer;
        static OfficeEmployee bumped,pushed;
        static Vector3 pushStart;
        static FireExtinguisher extinguisher;
        static OfficeSeat seat;
        static OfficeEmployee presenter,foamed,talkerA,talkerB;
        static Vector3 presenterStart;
        static OfficeEmployee rifleman,brute,victim;
        static int shotsBefore,swingsBefore,blastsBefore;
        static float lowestHealth;
        static WorkerController teammate;
        static OfficeEmployee rocketeer,target;
        static PlayerWeapon playerGun;
        static OfficeEmployee drawer;
        static bool earlyShot;
        static int gaspsBefore;
        static OfficeInteractionValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run()
        {
            if(!Application.isBatchMode)throw new Exception("Isolated validation only");
            EditorSceneManager.OpenScene(OfficeTools.ScenePath);UnityEngine.Object.FindFirstObjectByType<DescentGame>().UseManualSeed=true;SessionState.SetBool(Key,true);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);EditorApplication.EnterPlaymode();
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void Tick()
        {
            if(!Application.isBatchMode||!SessionState.GetBool(Key,false))return;
            try
            {
                Check(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+"deadline",0),"Interaction validation timeout");
                if(!EditorApplication.isPlaying)return;
                DescentGame game=UnityEngine.Object.FindFirstObjectByType<DescentGame>();if(!game||!game.CurrentOffice||!game.CurrentMap.Ready)return;
                if(game.Paused)game.SetPaused(false);OfficeFloor office=game.CurrentOffice;
                if(stage==0)
                {
                    game.Begin();Check(game.Player.Hands,"First person hand model exists");
                    int installed=0,recoverable=0;
                    foreach(OfficeTaskPoint task in office.Stations)if(task.Equipment){if(task.Equipment.GetComponent<SalvageItem>())recoverable++;else installed++;}
                    Check(installed>recoverable&&recoverable>0,"Only a selected minority of computers are stealable");
                    computer=game.Items.Find(i=>i.Title=="Morrow workstation computer");Check(computer,"Recoverable workstation computer");
                    Check(computer.GetComponentsInChildren<MeshFilter>().Length>20,"Pickup retains its mesh instead of being baked into room geometry");
                    computer.Body.position=game.Player.transform.position+Vector3.up*1.25f+Vector3.forward*.8f;computer.transform.position=computer.Body.position;
                    Check(game.Player.PickUp(computer),"Nearby selected item can be picked up");started=Time.time;stage=1;return;
                }
                if(stage==1)
                {
                    if(Time.time-started<.5f)return;
                    Check(game.Player.Hands.gameObject.activeSelf,"Hands visible in first person");Check(game.Player.Hands.GripName=="MONITOR SIDE GRIP","Computer chooses its grip profile");
                    Directory.CreateDirectory("TestResults/Office");OfficeTools.Capture(office.transform,"TestResults/Office/first-person-computer.png",game.Player.View.transform.position,game.Player.View.transform.position+game.Player.View.transform.forward);
                    HashSet<string> grips=new HashSet<string>();
                    foreach(SalvageItem item in game.Items)if(!item.GetComponent<OfficeCargo>()||!item.GetComponent<OfficeCargo>().Mandatory)grips.Add(FirstPersonHands.GripFor(item));
                    Check(grips.Count>=3,"Different available items select distinct grip profiles");
                    game.Player.BeginThrowCharge();game.Player.AdvanceThrowCharge(.01f);game.Player.ReleaseChargedThrow();Check(computer.Body.linearVelocity.magnitude<1,"Quick tap drops gently");
                    Check(game.Player.PickUp(computer),"Can recover dropped item");game.Player.BeginThrowCharge();game.Player.AdvanceThrowCharge(50);Check(game.Player.ThrowCharge==1,"Throw charge caps at 100 percent");
                    game.Player.ReleaseChargedThrow();float strong=computer.Body.linearVelocity.magnitude;Check(strong<=10.2f&&strong>3,"Charged throw has finite, stronger velocity");
                    Check(SalvageItem.ThrowSpeed(9,1)==SalvageItem.ThrowSpeed(9,100),"Overlong hold cannot exceed cap");Check(SalvageItem.ThrowSpeed(30,1)<SalvageItem.ThrowSpeed(3,1),"Heavy objects throw less far");
                    Check(game.Player.PickUp(computer),"Recover thrown item before simulation step");game.Player.BeginThrowCharge();game.Player.AdvanceThrowCharge(.8f);game.Player.Drop(false);Check(!game.Player.ChargingThrow&&game.Player.ThrowCharge==0,"E drop cancels charge");
                    int meetingSeed=-1;
                    for(int seed=100;seed<130;seed++)if(OfficePlan.Build(new TheElevator.Generation.MacroLayoutGenerator().Generate(OfficeTools.Recipe(seed,TheElevator.Generation.MapSize.Small))).MeetingRoom>=0){meetingSeed=seed;break;}
                    Check(meetingSeed>=0,"Meeting test seed available");game.RegenerateMap(meetingSeed,TheElevator.Generation.MapSize.Small,false);stage=2;return;
                }
                if(stage==2)
                {
                    if(game.Phase==DescentGame.RunPhase.Generating)return;
                    int room=office.Plan.MeetingRoom;Check(room>=0,"Meeting spawned");
                    var participants=office.Employees.FindAll(e=>e.HomeRoom==room);Check(participants.Count>=8,"Meeting table is mostly full");
                    Check(participants.Exists(e=>e.Station&&e.Station.Activity==OfficeTask.Present),"Presenter stands at the board");
                    Check(office.Doors.FindAll(d=>d.RoomA==room||d.RoomB==room).Count==1,"Meeting room has exactly one door");
                    Check(participants.TrueForAll(e=>e.AngerLevel==1),"Everyone starts calm");
                    game.Player.Teleport(office.Map.Center(office.Map.Manifest.Rooms[room]));office.CheckMeetingEntry(0);
                    Check(participants.TrueForAll(e=>e.AngerLevel==1),"Walking into the meeting upsets nobody");
                    // Bumping: one contact raises that one employee's anger by exactly one level.
                    bumped=participants.Find(e=>e.Station&&e.Station.Activity==OfficeTask.Meeting);Check(bumped,"Seated attendee to bump");
                    Vector3 at=bumped.transform.position,from=at-bumped.transform.forward*.6f,toward=(at-from).normalized*4;
                    float meterStart=office.BuildingAnger;
                    Check(office.CheckBumps(from,toward)==1&&bumped.AngerLevel==2,"A bump raises anger by one level");
                    Check(participants.TrueForAll(e=>e==bumped||e.AngerLevel==1),"Only the bumped employee is upset");
                    Check(office.CheckBumps(from,toward)==0&&bumped.AngerLevel==2,"Staying in contact is one bump, not many");
                    office.CheckBumps(at+Vector3.right*40,Vector3.zero);
                    Check(office.CheckBumps(from,toward)==1&&bumped.AngerLevel==3,"Walking into them again raises another level");
                    Check(office.CheckBumps(from+Vector3.right*40,Vector3.zero)==0,"Separating is not a bump");
                    Check(Mathf.Approximately(office.BuildingAnger-meterStart,2),"Each bump adds a point to the building's anger meter");
                    var subject=participants[0];OfficeTools.Capture(office.transform,"TestResults/Office/synthetic-face.png",subject.Robot.Head.position+subject.transform.forward*.52f+subject.transform.right*.18f+Vector3.up*.15f,subject.Robot.Head.position+Vector3.up*.15f);
                    // Fire extinguisher: cheap two-handed pickup with twelve seconds of foam.
                    SalvageItem mounted=game.Items.Find(i=>i&&i.GetComponent<FireExtinguisher>()&&i.Body.isKinematic);
                    if(mounted)OfficeTools.Capture(office.transform,"TestResults/Office/wall-extinguisher.png",mounted.transform.position+mounted.transform.forward*-1.4f+Vector3.up*.35f+mounted.transform.right*.4f,mounted.transform.position);
                    game.Player.Teleport(office.Map.Center(office.Map.Manifest.Rooms[room]));
                    extinguisher=FireExtinguisher.Create(game,office.Kit.A,office.transform,game.Player.transform.position+Vector3.up*1.1f+game.Player.transform.forward*.7f,Quaternion.identity);
                    SalvageItem canister=extinguisher.GetComponent<SalvageItem>();
                    Check(canister.Value==10&&canister.Body.isKinematic,"Extinguisher is a $10 item that hangs until taken");
                    Check(game.Player.PickUp(canister),"Player can pick up the extinguisher");
                    Check(FirstPersonHands.GripFor(canister)==FirstPersonHands.ExtinguisherGrip,"Extinguisher uses the canister-and-nozzle grip");
                    extinguisher.Operate(game.Player,true,5);Check(extinguisher.Spraying&&Mathf.Approximately(extinguisher.Remaining,7),"Spraying uses charge");
                    extinguisher.Operate(game.Player,false,3);Check(!extinguisher.Spraying&&Mathf.Approximately(extinguisher.Remaining,7),"Letting go keeps the remaining spray");
                    Check(Mathf.Approximately(extinguisher.Used,5f/12),"Usage gauge follows spray time");
                    Check(FoamSpray.Get(office.transform).FlyingCount>0,"Spraying emits foam");
                    Check(FireExtinguisher.InStream(Vector3.zero+Vector3.up*50,Vector3.forward,new Vector3(0,50,3),out float reach)&&!FireExtinguisher.InStream(Vector3.up*50,Vector3.forward,new Vector3(3,50,1),out reach),"Stream cone hits what it is aimed at");
                    // A steady shove: keep blowing on an employee for a moment and they slide away.
                    pushed=participants.Find(e=>e!=bumped&&e.Station&&e.Station.Activity==OfficeTask.Meeting);pushStart=pushed.transform.position;
                    // Walking into a standing employee shoves them along.
                    presenter=participants.Find(e=>e.Station&&e.Station.Activity==OfficeTask.Present);presenterStart=presenter.transform.position;
                    // Voices: three tones, getting louder as they get angrier.
                    const string sample="Could you please pass me that report?";
                    float calm=OfficeVoice.Loudness(OfficeVoice.Speak(sample,OfficeVoice.Tone.Calm,150,3)),upset=OfficeVoice.Loudness(OfficeVoice.Speak(sample,OfficeVoice.Tone.Upset,150,3)),yell=OfficeVoice.Loudness(OfficeVoice.Speak(sample,OfficeVoice.Tone.Yelling,150,3));
                    Check(calm>0&&upset>calm&&yell>upset,"Made-up language gets louder with anger ("+calm.ToString("F3")+" / "+upset.ToString("F3")+" / "+yell.ToString("F3")+")");
                    // Hallway chat between two standing colleagues.
                    List<OfficeEmployee> standing=office.Employees.FindAll(e=>!e.Robot.Seated&&e.AngerLevel<3&&e.Station&&e.Station.Activity!=OfficeTask.Present&&e.HomeRoom!=room);
                    Check(standing.Count>=2,"Two standing employees to chat");talkerA=standing[0];talkerB=standing[1];
                    Check(office.StartConversation(talkerA,talkerB)&&talkerA.Partner==talkerB,"Colleagues can stop and talk");
                    started=Time.time;stage=3;return;
                }
                if(stage==3)
                {
                    pushed.Push(pushed.transform.forward*-4);
                    if(Time.time-started<.5f){Vector3 toward=presenter.transform.forward;office.CheckBumps(presenter.transform.position+toward*.6f,-toward*4);}
                    // Meanwhile the player sprays a real stream from the held nozzle.
                    extinguisher.Operate(game.Player,true,Time.deltaTime);
                    if(Time.time-started<.8f)return;
                    Check(Vector3.Distance(pushed.transform.position,pushStart)>.35f,"Foam wind pushes an employee back");
                    Check(Vector3.Distance(presenter.transform.position,presenterStart)>.3f,"Running into a standing employee pushes them out of the way");
                    Check(FoamSpray.Get(office.transform).LandedCount>0,"Foam settles on surfaces");
                    // Half a second of spray at a steady 60 frames per second, so the capture shows the stream itself.
                    FoamSpray foam=FoamSpray.Get(office.transform);
                    for(int frame=0;frame<30;frame++){extinguisher.PoseHeld(game.Player.View.transform);foam.Emit(extinguisher.Tip,extinguisher.Nozzle.forward,Vector3.zero,1/60f,game.Player);foam.Step(1/60f);}
                    foam.Draw();OfficeTools.Capture(office.transform,"TestResults/Office/first-person-extinguisher.png",game.Player.View.transform.position,game.Player.View.transform.position+game.Player.View.transform.forward);
                    Check(game.Player.Hands.RightHandInside(extinguisher)==0,"Right hand holds the nozzle without passing through it ("+game.Player.Hands.RightHandInside(extinguisher)+" points inside)");
                    Transform hand=game.Player.Hands.RightHand,eye=game.Player.View.transform;
                    OfficeTools.Capture(office.transform,"TestResults/Office/nozzle-hand.png",hand.position+eye.right*.32f+eye.up*.06f+eye.forward*.05f,hand.position);
                    OfficeTools.Capture(office.transform,"TestResults/Office/nozzle-hand-top.png",hand.position+eye.up*.3f-eye.forward*.1f,hand.position);
                    // Foam in the face: instantly furious (level 5), blinded, foam stuck to them, yelling.
                    foamed=office.Employees.FindAll(e=>e.HomeRoom==office.Plan.MeetingRoom).Find(e=>e!=bumped&&e!=pushed&&e!=presenter&&e.AngerLevel==1);Check(foamed,"Calm attendee to spray");
                    Vector3 face=foamed.Robot.Head.TransformPoint(BeanRig.HeadCenter),shot=(face-(face+foamed.transform.forward*2)).normalized;
                    foam.Emit(face-shot*2,shot,Vector3.zero,.15f,game.Player);foam.Scan();
                    for(int frame=0;frame<30;frame++)foam.Step(1/60f);
                    Check(foamed.AngerLevel==5,"Foam hit sends an employee straight to anger 5");
                    Check(foamed.Blinded&&!foamed.Sees(game.Player.transform.position),"Foam in the face blinds them");
                    Check(foam.StuckCount>0,"Foam sticks to the person it hits");
                    Check(foamed.Speaking&&foamed.SpeechTone==OfficeVoice.Tone.Yelling,"A furious employee yells");
                    foamed.Robot.Mood=foamed.AngerLevel;foamed.Robot.Animate(.02f);foam.Draw();OfficeTools.Capture(office.transform,"TestResults/Office/foamed-face.png",face+foamed.transform.forward*.9f+Vector3.up*.1f+foamed.transform.right*.3f,face);
                    game.Player.Foam(1);Check(Mathf.Approximately(game.Player.FaceFoam,1),"Foam on a player's face covers their view");
                    extinguisher.Operate(game.Player,true,20);Check(extinguisher.Empty&&extinguisher.Used>=1,"Twelve seconds empties it");
                    extinguisher.Operate(game.Player,true,1);Check(!extinguisher.Spraying,"An empty extinguisher no longer sprays");
                    game.Player.Drop(false);
                    // Sitting: any free chair or sofa place; its employee station is held for the player.
                    seat=OfficeSeat.All.Find(s=>s.Free(office)&&office.Map.NearestRoom(s.transform.position).Id==office.Plan.MeetingRoom)??OfficeSeat.All.Find(s=>s.Free(office));
                    Check(seat,"A free seat exists");game.Player.Teleport(seat.transform.position-seat.transform.forward*-.9f);
                    Check(game.Player.Sit(seat)&&game.Player.Seat==seat&&!seat.Free(office),"Player sits down");
                    Check(!seat.Station(office)||seat.Station(office).ReservedByPlayer,"Seated player reserves the employee station");
                    started=Time.time;stage=4;return;
                }
                if(stage==4)
                {
                    if(Time.time-started<1f)return;
                    Check(Vector3.Distance(game.Player.transform.position,seat.transform.position)<.02f,"Player ends up on the seat");
                    Check(foamed.Chasing,"A furious employee gets up and goes after the player");
                    Check(!string.IsNullOrEmpty(presenter.Speech),"A shoved employee stops and says something");
                    Check(talkerA.Partner==talkerB&&(!string.IsNullOrEmpty(talkerA.Speech)),"Chatting colleagues take turns speaking");
                    OfficeTools.Capture(office.transform,"TestResults/Office/player-seated.png",game.Player.View.transform.position,game.Player.View.transform.position+game.Player.View.transform.forward);
                    game.Player.StandUp();started=Time.time;stage=5;return;
                }
                if(stage==5)
                {
                    if(Time.time-started<1f)return;
                    Check(!game.Player.Seat&&seat.Free(office)&&(!seat.Station(office)||!seat.Station(office).ReservedByPlayer),"Player stands back up and frees the seat");
                    // (The many offences checked below would fill the building meter; keep it under the riot point until the riot check.)
                    Check(!office.Riot,"No riot yet");office.SetAngerForValidation(0);
                    // First aid: heals half your health and is used up.
                    FirstAidKit kit=FirstAidKit.Create(game,office.Kit.A,office.transform,game.Player.transform.position+Vector3.up*1.1f+game.Player.transform.forward*.6f,Quaternion.identity);
                    SalvageItem kitItem=kit.GetComponent<SalvageItem>();Check(kitItem.Title=="First aid kit"&&kitItem.Body.isKinematic&&Mathf.Approximately(FirstAidKit.SpawnChance,.02f),"First aid kits hang on walls in one room in fifty");
                    game.Player.HealForValidation();game.Player.Damage(70,game.Player.transform.position+Vector3.forward,0);
                    Check(Mathf.Approximately(game.Player.Health,30)&&game.Player.BloodEdge>0,"Getting hurt flashes red at the screen edges");
                    Check(game.Player.PickUp(kitItem),"The player picks up the first aid kit");kit.Use(game.Player);
                    Check(Mathf.Approximately(game.Player.Health,80)&&!game.Player.Held,"The first aid kit heals half your health and is used up");
                    game.Player.HealForValidation();game.Player.Damage(80,game.Player.transform.position+Vector3.forward,0);
                    Check(game.Player.Health<25&&game.Player.BloodEdge>=.8f,"Below 25% health the red edges stay");
                    game.Player.HealForValidation();Check(game.Player.BloodEdge==0,"Healthy again, the red is gone");
                    // One phrase per bubble.
                    // One phrase per bubble, and one line at a time: a new line waits for the current one to finish.
                    OfficeEmployee quiet=office.Employees.Find(e=>!e.Dead&&!e.Speaking&&!e.Partner);
                    quiet.Say("Hey! Give that back.");Check(quiet.Speech=="Hey!"&&quiet.Speaking,"Speech shows one phrase at a time");
                    quiet.Say("Seriously?");Check(quiet.Speech=="Hey!"&&quiet.WaitingLines==1,"A new line waits until the current one is finished");
                    // Thrown things: 10 damage and straight to 5.
                    victim=office.Employees.Find(e=>!e.Dead&&e.Anger<2&&e.HomeRoom!=office.Plan.MeetingRoom);Check(victim,"Calm employee to throw at");
                    float health=victim.Health;victim.HitByThrown(victim.transform.position+Vector3.up);
                    Check(victim.Health==health-10&&victim.AngerLevel==5,"A thrown item does 10 damage and makes them furious");
                    Check(office.EmployeeAt(victim.transform.position+Vector3.up,.1f)==victim,"Thrown items find the person they hit");
                    // Losing the player: furious settles to on edge (4), and never back to normal.
                    victim.LastSawPlayerAgo(OfficeEmployee.LoseInterest+1);Check(victim.Anger==4,"Furious employees settle to 4 after losing sight of you");
                    victim.LastSawPlayerAgo(300);Check(victim.Anger==4,"Upset employees never go back to normal");
                    // Desk theft upsets only the owner: an award puts them at 3, a computer at 5.
                    OfficeCargo taken=Array.Find(office.GetComponentsInChildren<OfficeCargo>(),c=>!c.Mandatory&&c.Item&&c.Workstation&&c.Workstation.HomeOwner&&!c.Workstation.HomeOwner.Dead&&c.Workstation.HomeOwner.AtStation&&c.Workstation.HomeOwner.Anger<2&&c.Item.Title.ToLowerInvariant().Contains("award"))
                        ??Array.Find(office.GetComponentsInChildren<OfficeCargo>(),c=>!c.Mandatory&&c.Item&&c.Workstation&&c.Workstation.HomeOwner&&!c.Workstation.HomeOwner.Dead&&c.Workstation.HomeOwner.AtStation&&c.Workstation.HomeOwner.Anger<2);
                    Check(taken,"Desk item with its owner at the desk");
                    OfficeEmployee owner=taken.Workstation.HomeOwner;string title=taken.Item.Title.ToLowerInvariant();
                    Dictionary<OfficeEmployee,float> before=new Dictionary<OfficeEmployee,float>();foreach(OfficeEmployee e in office.Employees)before[e]=e.Anger;
                    office.WitnessTheft(taken);
                    int expected=title.Contains("computer")||title.Contains("laptop")?5:3;
                    Check(owner.AngerLevel==expected,"Taking "+title+" off the desk puts its owner at "+expected+" (got "+owner.AngerLevel+")");
                    Check(office.Employees.TrueForAll(e=>e==owner||e.Anger==before[e]),"Nobody else gets upset about desk theft");
                    Check(!string.IsNullOrEmpty(owner.Speech),"The owner says something about it");
                    // Repeated offences climb past 5; at 20 they snap and never cool down.
                    brute=office.Employees.Find(e=>!e.Dead&&e!=victim&&e!=owner&&e!=foamed&&e.HomeRoom==office.Plan.MeetingRoom&&e.Weapon==Arms.None);
                    for(int i=0;i<8&&!brute.Snapped;i++)brute.Offend(Grievance.Thrown);
                    Check(brute.Snapped&&brute.AngerLevel==5,"Repeated offences add up to the snapping point (20) while the face stays at 5");
                    if(brute.Weapon!=Arms.None)brute.ArmForValidation(Arms.None);
                    brute.LastSawPlayerAgo(300);Check(brute.Snapped,"Snapped employees never cool down");
                    // Rifle fire is deliberately inaccurate.
                    System.Random dice=new System.Random(5);int hits=0;Vector3 feet=new Vector3(0,300,0);
                    for(int i=0;i<400;i++)if(OfficeWeapons.PistolRound(feet+new Vector3(0,1.3f,-10),feet+Vector3.up*1.1f,feet,0,dice,out Vector3 d,out float a))hits++;
                    Check(hits>5&&hits<120,"An employee's rifle is very inaccurate at 10 m ("+hits+"/400 hits)");
                    // Live: an armed employee shoots at the player, an unarmed one throws punches.
                    rifleman=office.Employees.Find(e=>!e.Dead&&e!=brute&&e!=foamed&&e.HomeRoom==office.Plan.MeetingRoom&&e.Station&&e.Station.Activity==OfficeTask.Meeting);
                    rifleman.ArmForValidation(Arms.Pistol);Check(rifleman.Weapon==Arms.Pistol&&rifleman.Snapped,"Snapped employees can carry a rifle");
                    game.Player.Teleport(brute.transform.position+brute.transform.forward*.9f);game.Player.HealForValidation();
                    shotsBefore=OfficeWeapons.Shots;swingsBefore=OfficeWeapons.Swings;lowestHealth=100;started=Time.time;stage=6;return;
                }
                if(stage==6)
                {
                    lowestHealth=Mathf.Min(lowestHealth,game.Player.Health);game.Player.HealForValidation();
                    if(Time.time-started<1.8f&&OfficeWeapons.Shots>shotsBefore)earlyShot=true;
                    if(Time.time-started<4.5f)return;
                    Check(!earlyShot,"An armed employee aims for two seconds before the first shot");
                    Check(OfficeWeapons.Shots-shotsBefore<=4,"Then one shot a second ("+(OfficeWeapons.Shots-shotsBefore)+" in 4.5 s)");
                    Check(OfficeWeapons.Shots>shotsBefore,"The armed employee opens fire ("+(OfficeWeapons.Shots-shotsBefore)+" rounds)");
                    Check(OfficeWeapons.Swings>swingsBefore,"The unarmed employee swings at the player ("+(OfficeWeapons.Swings-swingsBefore)+" swings)");
                    Check(lowestHealth<100,"The player gets hurt (lowest health "+lowestHealth+")");
                    // Bazooka: a real rocket, and a 10 m blast that hurts employees too.
                    rocketeer=office.Employees.Find(e=>!e.Dead&&e!=brute&&e!=rifleman&&e.HomeRoom==office.Plan.MeetingRoom);
                    rocketeer.ArmForValidation(Arms.Bazooka);
                    blastsBefore=OfficeWeapons.Explosions;
                    OfficeWeapons.FireRocket(rocketeer,rocketeer.transform.position+Vector3.up*1.4f+rocketeer.transform.forward*.6f,rocketeer.transform.position+rocketeer.transform.forward*8+Vector3.up*1.1f,new System.Random(2));
                    float bystander=victim.Health;
                    OfficeWeapons.Explode(office,victim.transform.position+Vector3.up*.5f+victim.transform.forward*.8f);
                    Check(victim.Health<bystander,"Employees caught in a blast get hurt");
                    // Pulling a weapon: pale and wild-eyed for a moment, a close-up for the player, gasps from anyone watching.
                    Vector3 eye=game.Player.View.transform.position;drawer=null;float nearest=10;
                    foreach(OfficeEmployee e in office.Employees)
                    {
                        if(e.Dead||e==brute||e==rifleman||e==rocketeer||e.Weapon!=Arms.None)continue;
                        Vector3 chest=e.transform.position+Vector3.up*1.1f;float d=Vector3.Distance(eye,chest);
                        if(d<nearest&&!Physics.Linecast(eye,chest,~((1<<2)|(1<<8)),QueryTriggerInteraction.Ignore)){nearest=d;drawer=e;}
                    }
                    Check(drawer,"Someone close by to draw a weapon");
                    gaspsBefore=office.Gasps;drawer.DrawForValidation(Arms.Pistol);
                    Check(drawer.Drawing&&drawer.Robot.Crazed&&drawer.Weapon==Arms.Pistol,"Drawing a weapon, they go pale and wild-eyed");
                    Check(game.Player.InCutscene,"The player close by gets a look at their face");
                    Check(office.Gasps>gaspsBefore,"Everyone who sees the weapon gasps ("+(office.Gasps-gaspsBefore)+")");
                    started=Time.time;stage=7;return;
                }
                if(stage==7)
                {
                    game.Player.HealForValidation();
                    if(Time.time-started<3f)return;
                    Check(OfficeWeapons.Explosions>=blastsBefore+2,"The bazooka rocket explodes");
                    Check(!drawer.Drawing&&!drawer.Robot.Crazed&&drawer.AngerLevel==5&&!game.Player.InCutscene,"Weapon out, they are furious again and the close-up ends");
                    // Employees can be knocked out (200 health), and the player is out at 0.
                    Check(Mathf.Approximately(OfficeEmployee.MaxHealth,200)&&Mathf.Approximately(WorkerController.MaxHealth,100),"Employees have 200 health, players 100");
                    Transform gun=rifleman.transform;
                    OfficeTools.Capture(office.transform,"TestResults/Office/armed-employee.png",gun.position+gun.forward*1.8f+gun.right*.9f+Vector3.up*1.5f,gun.position+Vector3.up*1.1f);
                    victim.Damage(1000,victim.transform.position+Vector3.forward,0);Check(victim.Dead&&!victim.Chasing,"Employees can be knocked out");
                    Check(victim.Robot.Core&&!victim.Robot.Core.isKinematic,"A knocked-out employee goes ragdoll");
                    // Three armed employees per floor: two rifles (2% per offence once furious), one bazooka (1%).
                    Check(OfficeEmployee.Draws(Arms.Pistol,.019)&&!OfficeEmployee.Draws(Arms.Pistol,.021)&&OfficeEmployee.Draws(Arms.Bazooka,.009)&&!OfficeEmployee.Draws(Arms.Bazooka,.011)&&!OfficeEmployee.Draws(Arms.None,0),"Weapon odds: 2% rifle, 1% bazooka, nobody else");
                    Check(office.Employees.FindAll(e=>e.Sidearm==Arms.Pistol).Count==2&&office.Employees.FindAll(e=>e.Sidearm==Arms.Bazooka).Count==1,"Exactly two rifle carriers and one bazooka carrier per floor");
                    Check(Mathf.Approximately(OfficeWeapons.PistolDamage,24)&&Mathf.Approximately(OfficeWeapons.BlastDamage,90)&&Mathf.Approximately(OfficeWeapons.PunchDamage,5),"Employee damage: pistol 24, bazooka 90, fist 5");
                    // Chairs are solid to players (not to employees), and people at work stay solid too.
                    OfficeSeat chair=OfficeSeat.All.Find(s=>s&&s.GetComponent<BoxCollider>());
                    Check(chair&&chair.gameObject.layer==PhysicsLayers.Seats&&!Physics.GetIgnoreLayerCollision(PhysicsLayers.Player,PhysicsLayers.Seats)&&Physics.GetIgnoreLayerCollision(PhysicsLayers.Employees,PhysicsLayers.Seats),"Chairs block players but not employees");
                    OfficeEmployee sitter=office.Employees.Find(e=>!e.Dead&&e.Robot.Seated&&e.AtStation);
                    Check(sitter&&sitter.Motor.enabled,"A seated employee is solid");
                    // Walking into someone sitting in their chair annoys them.
                    float seatedBefore=sitter.Anger;Vector3 side=sitter.transform.right;
                    office.CheckBumps(sitter.transform.position+side*.95f,-side*4);
                    Check(sitter.Anger>seatedBefore,"Walking into someone in their chair annoys them");
                    // Knocked out, armed employees drop their weapons for anyone to pick up.
                    rifleman.Damage(1000,rifleman.transform.position+Vector3.forward,0);rocketeer.Damage(1000,rocketeer.transform.position+Vector3.forward,0);
                    PlayerWeapon[] loose=UnityEngine.Object.FindObjectsByType<PlayerWeapon>(FindObjectsSortMode.None);
                    Check(Array.Exists(loose,w=>w.Kind==Arms.Pistol&&w.GetComponent<SalvageItem>())&&Array.Exists(loose,w=>w.Kind==Arms.Bazooka&&w.GetComponent<SalvageItem>()),"Knocked-out employees drop their weapons as pickups");
                    playerGun=Array.Find(loose,w=>w.Kind==Arms.Pistol);
                    started=Time.time;stage=8;return;
                }
                if(stage==8)
                {
                    game.Player.HealForValidation();
                    if(Time.time-started<1.8f)return;
                    Transform body=victim.transform;
                    Vector3 inward=office.Map.Center(office.Map.NearestRoom(body.position))-body.position;inward.y=0;inward=inward.sqrMagnitude>.01f?inward.normalized:Vector3.forward;
                    OfficeTools.Capture(office.transform,"TestResults/Office/knocked-out.png",body.position+inward*2.2f+Vector3.up*1.5f,body.position+Vector3.up*.2f);
                    Check(victim.Robot.Core.position.y-body.position.y<.45f,"The ragdoll ends up lying on the floor (pelvis "+(victim.Robot.Core.position.y-body.position.y).ToString("F2")+" m up)");
                    // Pay shares: one body of four left behind costs 25%, one of three 33%.
                    Check(Mathf.Approximately(DescentGame.BodyCut(4,1),.25f)&&Mathf.Abs(DescentGame.BodyCut(3,1)-1/3f)<.001f&&DescentGame.BodyCut(4,0)==0,"A body left behind costs its share of the pay");
                    SalvageItem sample=game.Items.Find(i=>i&&i.Value>=100);int worth=sample.Value;sample.Deduct(.25f);
                    Check(sample.Value==Mathf.RoundToInt(worth*.75f),"Recovered items lose the missing share");
                    // A teammate goes down: they ragdoll, the shift goes on, and the body can be carried to the lift.
                    teammate=game.SpawnTeammate(game.Player.transform.position+game.Player.transform.right*1.2f);
                    teammate.Damage(1000,teammate.transform.position+Vector3.forward,4);
                    Check(teammate.Down&&teammate.Model.Ragdolled&&game.Phase!=DescentGame.RunPhase.Lost,"A downed teammate ragdolls and the shift goes on");
                    Check(Mathf.Approximately(game.BodyShortfall(),.5f),"Their body away from the lift is half the pay for a crew of two");
                    started=Time.time;stage=9;return;
                }
                if(stage==9)
                {
                    game.Player.HealForValidation();
                    if(Time.time-started<1f)return;
                    Check(game.Player.CarryBody(teammate)&&teammate.CarriedBy==game.Player,"The body can be picked up");
                    game.Player.Teleport(new Vector3(0,.08f,-6.5f));
                    started=Time.time;stage=10;return;
                }
                if(stage==10)
                {
                    if(Time.time-started<1f)return;
                    Check(DescentGame.InCabin(teammate.BodyPosition)&&game.BodyShortfall()==0,"Carried into the lift, the body costs nothing");
                    game.Player.DropBody();
                    Check(!teammate.CarriedBy,"The body can be put down");
                    // Pick up the dropped rifle and line up a shot at a still employee with a clear view.
                    int sight=~((1<<2)|(1<<8)|(1<<9)|(1<<10));Vector3 spot=Vector3.zero;target=null;
                    foreach(OfficeEmployee e in office.Employees)
                    {
                        if(e.Dead||e.Chasing||e.Travelling||!e.AtStation||e==brute)continue;
                        for(int k=0;k<8&&!target;k++)
                        {
                            Vector3 at=e.transform.position+Quaternion.Euler(0,k*45,0)*Vector3.forward*3.2f;
                            if(DescentGame.InCabin(at)||!OfficeNavigation.Clear(at)||Physics.Linecast(at+Vector3.up*1.34f,e.transform.position+Vector3.up*1.1f,sight,QueryTriggerInteraction.Ignore))continue;
                            target=e;spot=at;
                        }
                        if(target)break;
                    }
                    Check(target,"A clear shot at an employee");
                    game.Player.Teleport(spot);game.Player.FaceTowards(target.transform.position+Vector3.up*1.1f);game.Player.HealForValidation();
                    SalvageItem rifleItem=playerGun.GetComponent<SalvageItem>();rifleItem.Body.position=game.Player.transform.position+Vector3.up*1.2f+game.Player.transform.forward*.6f;rifleItem.transform.position=rifleItem.Body.position;
                    Check(game.Player.PickUp(rifleItem),"The player can pick up a dropped rifle");
                    started=Time.time;stage=11;return;
                }
                if(stage==11)
                {
                    game.Player.HealForValidation();
                    if(Time.time-started<.6f)return;
                    game.Player.FaceTowards(target.transform.position+Vector3.up*1.1f);
                    started=Time.time;stage=12;return;
                }
                if(stage==12)
                {
                    game.Player.HealForValidation();
                    if(Time.time-started<.2f)return;
                    playerGun.Operate(game.Player,true,true);
                    Check(target.Dead,"One round from a player's pistol knocks an employee out");
                    Check(playerGun.Ammo==OfficeWeapons.PistolMagazine-1,"The pistol uses a round per shot");
                    // A player's rocket blast is lethal across its radius.
                    OfficeEmployee far=office.Employees.Find(e=>!e.Dead&&Vector3.Distance(e.transform.position,game.Player.transform.position)>OfficeWeapons.BlastRadius+3);
                    OfficeWeapons.Explode(office,far.transform.position+Vector3.up*.5f,OfficeWeapons.PlayerBlastDamage);
                    Check(far.Dead,"A player's bazooka blast knocks out anyone in range");
                    game.Player.Drop(false);
                    // The building's anger meter: full at 50, and then everyone is after the players.
                    bool lounge=office.Plan.Rooms.Exists(r=>r.Kind==OfficeRoomKind.Lounge);int vending=Array.FindAll(office.GetComponentsInChildren<Transform>(true),t=>t.name=="Lounge vending machine").Length;
                    office.SetAngerForValidation(49);office.AddAnger(1);
                    Check(office.Riot&&office.Employees.TrueForAll(e=>e.Dead||e.Snapped),"A full meter turns the whole building against the players");
                    office.AddAnger(-10);Check(office.Riot,"The meter never goes down");
                    game.Player.Teleport(brute.transform.position+brute.transform.forward*3);game.Player.HealForValidation();
                    game.Player.Damage(1000,game.Player.transform.position+Vector3.forward,0);
                    Check(game.Player.Down&&game.Phase==DescentGame.RunPhase.Lost,"With nobody left standing, being knocked out ends the shift");
                    Check(game.Player.Model.Ragdolled,"The player ragdolls too");
                    Finish(true,"OFFICE INTERACTION PASS: visible first-person hands, multiple grip profiles, selected equipment only, nearby pickup, gentle tap, capped charged throw, weight scaling, charge cancellation, two-room meeting with presenter, calm meeting entry, one anger level per bump, shoving, extinguisher pickup/spray/charge/empty/push, hand clear of nozzle, sticky blinding foam and instant fury, chase, three voice tones, hallway chat, sit and stand, one phrase per bubble, owner-only desk theft (award 3, computer 5), thrown-item damage, cooling to 4 and never lower, snapping at 20, inaccurate rifle fire, punches, bazooka blast, knockouts, shift ends at 0 health, 2%/0.5% weapon odds, solid chairs and seated people, ragdolls, body carrying and pay shares, pistol aim delay and cadence, crazed weapon draw with close-up and gasps, first aid kits, blood-edge health, building anger meter and riot (lounge vending machines: "+vending+(lounge?"":" / no lounge on this seed")+").");
                }
            }
            catch(Exception error){Finish(false,error.ToString());}
        }
        static void Log(string message,string trace,LogType type)
        {
            if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("TheElevator."))return;
            if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))Finish(false,message+"\n"+trace);
        }
        static void Finish(bool success,string message)
        {
            SessionState.SetBool(Key,false);Directory.CreateDirectory("TestResults/Office");File.WriteAllText("TestResults/Office/interaction-validation.txt",message);if(success)Debug.Log(message);else Debug.LogError(message);EditorApplication.Exit(success?0:1);
        }
    }
}


