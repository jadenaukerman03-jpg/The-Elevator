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
                    Check(participants.TrueForAll(e=>e.Suspicion>=14&&e.AngerLevel>=2),"Walking into the meeting angers every attendee");
                    float prior=participants[0].Suspicion;office.CheckMeetingEntry(0);game.Player.Teleport(new Vector3(0,.08f,-6.5f));office.CheckMeetingEntry(0);
                    game.Player.Teleport(office.Map.Center(office.Map.Manifest.Rooms[room]));office.CheckMeetingEntry(0);
                    Check(participants[0].Suspicion==prior,"Stepping in and out does not farm anger");
                    var subject=participants[0];OfficeTools.Capture(office.transform,"TestResults/Office/synthetic-face.png",subject.Robot.Head.position+subject.transform.forward*.52f+subject.transform.right*.18f+Vector3.up*.15f,subject.Robot.Head.position+Vector3.up*.15f);
                    Finish(true,"OFFICE INTERACTION PASS: visible first-person hands, multiple grip profiles, selected equipment only, nearby pickup, gentle tap, capped charged throw, weight scaling, charge cancellation, two-room meeting with presenter, entry anger with cooldown.");
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


