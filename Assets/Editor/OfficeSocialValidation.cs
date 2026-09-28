using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TheElevator.Office;
using TheElevator.Generation;
namespace TheElevator.Editor
{
    [InitializeOnLoad]
    public static class OfficeSocialValidation
    {
        const string Key="Office.SocialValidation";
        static int stage;
        static float started,nextTrace;
        static OfficeEmployee traveller;
        static OfficeTaskPoint coffee;
        static OfficeFloor office;
        static OfficeDoor publicDoor;
        static int beforeTrips;
        static OfficeSocialValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run()
        {
            if(!Application.isBatchMode)throw new Exception("Isolated batch test only");
            EditorSceneManager.OpenScene(OfficeTools.ScenePath);UnityEngine.Object.FindFirstObjectByType<DescentGame>().UseManualSeed=true;
            SessionState.SetBool(Key,true);SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+180);EditorApplication.EnterPlaymode();
        }
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!Application.isBatchMode)return;
            try
            {
                Check(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+".deadline",0),"Social validation timeout / stage "+stage);
                if(!EditorApplication.isPlaying)return;
                DescentGame game=UnityEngine.Object.FindFirstObjectByType<DescentGame>();
                if(!game||!game.CurrentOffice||!game.CurrentMap.Ready)return;
                office=game.CurrentOffice;if(game.Paused)game.SetPaused(false);
                if(stage==0)
                {
                    game.Begin();Check(game.FloorIndex==0,"Office is first floor");
                    Check(!office.Employees.Exists(e=>e.HomeRoom==0),"Lobby is unoccupied");
                    Check(office.Employees.FindAll(e=>e.HomeRoom==1).Count==1,"Reception has one supervisor");
                    HashSet<OfficeTaskPoint> homes=new HashSet<OfficeTaskPoint>();HashSet<float> clocks=new HashSet<float>();
                    foreach(OfficeEmployee e in office.Employees){Check(homes.Add(e.HomeStation),"Unique home station");Check(clocks.Add(e.NextTaskTime),"Staggered schedules");}
                    game.Player.Teleport(new Vector3(0,.08f,-6.5f));Check(game.Player.SetCrouched(true)&&game.Player.Crouched,"Crouch collider state");
                    GameObject roof=new GameObject("Crouch ceiling test");roof.transform.position=game.Player.transform.position+Vector3.up*1.40f;roof.AddComponent<BoxCollider>().size=new Vector3(2,.12f,2);Physics.SyncTransforms();
                    Check(!game.Player.SetCrouched(false)&&game.Player.Crouched,"Stand-up blocked under ceiling");UnityEngine.Object.DestroyImmediate(roof);Physics.SyncTransforms();Check(game.Player.SetCrouched(false),"Stand-up under clear ceiling");
                    publicDoor=office.Doors.Find(d=>d.RequiredClearance==0);Check(publicDoor,"Manual room door exists");publicDoor.Use();Check(publicDoor.RequestedOpen,"Public door opens without card");publicDoor.Use();Check(!publicDoor.RequestedOpen,"Public door closes on second use");
                    OfficeEmployee owner=office.Employees.Find(e=>e.Station&&e.Station.Equipment&&e.Station.Equipment.GetComponent<SalvageItem>());Check(owner,"Occupied computer exists");
                    var item=owner.Station.Equipment.GetComponent<SalvageItem>();Check(item,"Work computer is loot");
                    float prior=owner.Suspicion;item.PickUp(game.Player);Check(owner.Suspicion>=prior+60,"Occupied computer theft strongly alerts owner");Check(office.SuspicionLevel>=60,"Suspicion meter reflects witness");
                    item.Release(false);foreach(OfficeEmployee e in office.Employees)e.Suspicion=0;
                    bool planned=false;
                    foreach(OfficeEmployee e in office.Employees)
                    {
                        if(e.Supervisor||e.HomeRoom==office.Plan.MeetingRoom)continue;
                        foreach(OfficeTaskPoint point in office.Stations)
                        {
                            if(!point.Coffee||!point.Available||point.RoomId==office.Plan.TargetRoom)continue;
                            if(e.TryTravel(point)){traveller=e;coffee=point;planned=true;break;}
                        }
                        if(planned)break;
                    }
                    Check(planned,"At least one real furniture-safe coffee route");beforeTrips=traveller.CompletedTrips;started=Time.time;stage=1;Time.timeScale=2;return;
                }
                if(stage==1)
                {
                    if(Time.time>nextTrace){nextTrace=Time.time+10;Debug.Log("SOCIAL NAV "+(Time.time-started)+" "+traveller.transform.position+" / "+traveller.NavigationStatus+" / "+traveller.State);}
                    // Continue full movement even when the player is back in the lift.
                    if(traveller.CoffeeStage==2&&traveller.Cup.Fill>.15f&&traveller.Cup.Fill<.95f)
                    {
                        Check(traveller.CompletedTrips>beforeTrips,"Employee physically reaches coffee station");
                        Check(traveller.Cup.Liquid.gameObject.activeSelf,"Coffee visibly fills in open cup");
                        string output="TestResults/Office";Directory.CreateDirectory(output);
                        OfficeTools.Capture(office.transform,output+"/social-coffee.png",coffee.transform.position+coffee.transform.right*1.4f-coffee.transform.forward*.9f+Vector3.up*1.8f,coffee.transform.position+coffee.transform.forward*.7f+Vector3.up*1.2f);
                        stage=2;return;
                    }
                    if(Time.time-started>119)
                    {
                        OfficeTools.Capture(office.transform,"TestResults/Office/navigation-stuck.png",traveller.transform.position+Vector3.up*4-traveller.transform.forward*4,traveller.transform.position+Vector3.up*.7f);
                        foreach(Collider hit in Physics.OverlapSphere(traveller.transform.position+Vector3.up*.8f,.7f))Debug.Log("SOCIAL BLOCKER "+hit.name+" "+hit.bounds);
                    }
                    Check(Time.time-started<120,"Coffee travel made no progress / "+traveller.State+" / "+traveller.transform.position+" target "+coffee.transform.position+" trips "+traveller.CompletedTrips);
                }
                if(stage==2)
                {
                    if(Time.time>nextTrace){nextTrace=Time.time+10;Debug.Log("SOCIAL RETURN "+(Time.time-started)+" "+traveller.transform.position+" / "+traveller.NavigationStatus+" / "+traveller.LastRouteFailure);}
                    if(traveller.Cup&&traveller.CoffeeStage<0&&traveller.Station==traveller.HomeStation&&!traveller.Travelling)
                    {
                        Check(traveller.Cup.Fill>0,"Employee carries remaining drink home");
                        int meetings=0;bool desk=false,person=false;
                        for(int seed=100;seed<130;seed++)
                        {
                            var map=new MacroLayoutGenerator().Generate(OfficeTools.Recipe(seed,MapSize.Small));
                            OfficePlan plan=OfficePlan.Build(map);
                            if(plan.MeetingRoom>=0)
                            {
                                meetings++;
                                Check(map.Neighbors(plan.MeetingRoom).Count==1,"Meeting room is a dead end with one door");
                                var room=map.Rooms[plan.MeetingRoom];int ax=room.X+OfficePlan.PortX[plan.MeetingAnnexSide],az=room.Z+OfficePlan.PortZ[plan.MeetingAnnexSide];
                                Check(!map.Rooms.Exists(r=>r.X==ax&&r.Z==az),"Meeting room extension occupies an empty cell");
                            }
                            desk|=plan.DeskBadge;person|=!plan.DeskBadge;
                        }
                        Check(meetings>=20,"Most floors have a two-room meeting room");Check(desk&&person,"Desk and person badge variants exist");
                        game.Player.Teleport(new Vector3(0,.08f,-6.5f));game.RegenerateMap(104728,MapSize.Small,false);stage=3;return;
                    }
                    Check(Time.time-started<160,"Coffee employee did not return home / "+traveller.State);
                }
                if(stage==3)
                {
                    if(game.Phase==DescentGame.RunPhase.Generating||game.ActiveSeed!=104728)return;
                    Check(office.DeskKeycard&&!office.Employees[0].HasBadge,"Visible desk keycard replaces supervisor pocket badge");
                    office.DeskKeycard.Take();Check(office.BadgeLevel==2&&office.DeskKeycard.Taken,"Desk keycard pickup grants access");
                    Finish(true,"OFFICE SOCIAL PASS: sparse common areas, reserved homes, independent schedules, crouch/headroom, toggle doors, occupied-computer reaction, suspicion meter, physical coffee trip/fill/drink/return, seeded meetings and both badge variants.");
                }
            }
            catch(Exception e){Finish(false,e.ToString());}
        }
        static void Log(string message,string trace,LogType type)
        {
            if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("TheElevator."))return;
            if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))Finish(false,message+"\n"+trace);
        }
        static void Finish(bool success,string message)
        {
            SessionState.SetBool(Key,false);Time.timeScale=1;Directory.CreateDirectory("TestResults/Office");File.WriteAllText("TestResults/Office/social-validation.txt",message);
            if(success)Debug.Log(message);else Debug.LogError(message);EditorApplication.Exit(success?0:1);
        }
    }
}




