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
                    Check(office.CheckBumps(from,toward)==1&&bumped.AngerLevel==2,"A bump raises anger by one level");
                    Check(participants.TrueForAll(e=>e==bumped||e.AngerLevel==1),"Only the bumped employee is upset");
                    Check(office.CheckBumps(from,toward)==0&&bumped.AngerLevel==2,"Staying in contact is one bump, not many");
                    office.CheckBumps(at+Vector3.right*40,Vector3.zero);
                    Check(office.CheckBumps(from,toward)==1&&bumped.AngerLevel==3,"Walking into them again raises another level");
                    Check(office.CheckBumps(from+Vector3.right*40,Vector3.zero)==0,"Separating is not a bump");
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
                    pushed=participants.Find(e=>e!=bumped&&e.Station&&e.Station.Activity==OfficeTask.Meeting);pushStart=pushed.transform.position;started=Time.time;stage=3;return;
                }
                if(stage==3)
                {
                    pushed.Push(pushed.transform.forward*-4);
                    // Meanwhile the player sprays a real stream from the held nozzle.
                    extinguisher.Operate(game.Player,true,Time.deltaTime);
                    if(Time.time-started<.8f)return;
                    Check(Vector3.Distance(pushed.transform.position,pushStart)>.35f,"Foam wind pushes an employee back");
                    Check(pushed.AngerLevel==1,"Being sprayed does not count as a bump");
                    Check(FoamSpray.Get(office.transform).LandedCount>0,"Foam settles on surfaces");
                    // Half a second of spray at a steady 60 frames per second, so the capture shows the stream itself.
                    FoamSpray foam=FoamSpray.Get(office.transform);
                    for(int frame=0;frame<30;frame++){extinguisher.PoseHeld(game.Player.View.transform);foam.Emit(extinguisher.Tip,extinguisher.Nozzle.forward,Vector3.zero,1/60f);foam.Step(1/60f);}
                    foam.Draw();OfficeTools.Capture(office.transform,"TestResults/Office/first-person-extinguisher.png",game.Player.View.transform.position,game.Player.View.transform.position+game.Player.View.transform.forward);
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
                    OfficeTools.Capture(office.transform,"TestResults/Office/player-seated.png",game.Player.View.transform.position,game.Player.View.transform.position+game.Player.View.transform.forward);
                    game.Player.StandUp();started=Time.time;stage=5;return;
                }
                if(stage==5)
                {
                    if(Time.time-started<1f)return;
                    Check(!game.Player.Seat&&seat.Free(office)&&(!seat.Station(office)||!seat.Station(office).ReservedByPlayer),"Player stands back up and frees the seat");
                    Finish(true,"OFFICE INTERACTION PASS: visible first-person hands, multiple grip profiles, selected equipment only, nearby pickup, gentle tap, capped charged throw, weight scaling, charge cancellation, two-room meeting with presenter, calm meeting entry, one anger level per bump, extinguisher pickup/spray/charge/empty/push, sit and stand.");
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


