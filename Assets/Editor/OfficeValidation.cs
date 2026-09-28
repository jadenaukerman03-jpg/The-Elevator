using System;
using System.Collections;
using System.IO;
using TheElevator.Generation;
using TheElevator.Office;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheElevator.Editor
{
    [InitializeOnLoad]
    public static class OfficeValidation
    {
        const string Active="Office.PlayValidation";
        static int stage,frames;
        static double measureStart;
        static float originalPower;
        static OfficeCargo recovered;
        static OfficeEmployee supervisor;
        static OfficeFloor office;
        static string firstHash;
        static OfficeValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void RunGeometry()
        {
            OfficeTools.CreateAssets();if(Application.isBatchMode)EditorSceneManager.OpenScene(OfficeTools.ScenePath);UnityEngine.Object.FindFirstObjectByType<DescentGame>().UseManualSeed=true;int recipes=0,geometry=0;
            foreach(MapSize size in Enum.GetValues(typeof(MapSize)))for(int seed=-10;seed<40;seed++)
            {
                MapManifest map=new MacroLayoutGenerator().Generate(OfficeTools.Recipe(seed,size));OfficePlan a=OfficePlan.Build(map),b=OfficePlan.Build(map);
                Require(a.Hash==b.Hash,"Office plan deterministic");Require(a.ExtractionRoute.Count>1,"Extraction route exists");recipes++;
            }
            foreach(MapSize size in new[]{MapSize.Small,MapSize.Standard,MapSize.Extreme})foreach(int seed in new[]{104729,-17})
            {
                Scene previous=SceneManager.GetActiveScene(),scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);Workshop workshop=new Workshop();GameObject root=null;
                try
                {
                    MapManifest map=new MacroLayoutGenerator().Generate(OfficeTools.Recipe(seed,size));root=new GameObject("Office physical validation");SceneManager.MoveGameObjectToScene(root,scene);
                    GeneratedFloor floor=root.AddComponent<GeneratedFloor>();floor.Initialize(map);
                    IEnumerator builder=new FloorGeometryBuilder(workshop,AssetDatabase.LoadAssetAtPath<FloorContentCatalog>(OfficeTools.CatalogPath)).Build(floor,null,null);while(builder.MoveNext()){}
                    OfficeFloor generated=root.GetComponent<OfficeFloor>();generated.Assemble(null);Require(generated.Employees.Count>=4,"Office populated");
                    foreach(OfficeDoor door in generated.Doors)door.SetOpenForValidation(true);Physics.SyncTransforms();
                    foreach(MapLink edge in map.Links)
                    {
                        var route=floor.Route(edge.A,edge.B);
                        for(int i=1;i<route.Count;i++)
                        {
                            Vector3 delta=route[i]-route[i-1];if(delta.sqrMagnitude<.001f)continue;
                            Require(!Physics.CapsuleCast(route[i-1]+Vector3.up*.52f,route[i-1]+Vector3.up*1.61f,.37f,delta.normalized,out RaycastHit hit,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"Blocked office route "+size+"/"+seed+" "+edge.A+"-"+edge.B+" "+(hit.collider?hit.collider.name:""));
                        }
                    }
                    geometry++;Debug.Log("OFFICE GEOMETRY PASS / "+size+" / "+seed+" / "+generated.Employees.Count+" employees / "+generated.FitResult);
                }
                finally{if(root)UnityEngine.Object.DestroyImmediate(root);workshop.Dispose();EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
            }
            Directory.CreateDirectory("TestResults/Office");File.WriteAllText("TestResults/Office/geometry-validation.txt",recipes+" repeatable office recipes; "+geometry+" physical office maps; all graph routes and objective envelopes validated.");
            Debug.Log("OFFICE VALIDATION PASSED / "+recipes+" recipes / "+geometry+" physical maps");
        }
        public static void RunPlay()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch mode.");
            OfficeTools.CreateAssets();
            Require(EditorBuildSettings.scenes.Length>0 && EditorBuildSettings.scenes[0].enabled && EditorBuildSettings.scenes[0].path==OfficeTools.ScenePath,"Default build starts in office");
            EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");UnityEngine.Object.FindFirstObjectByType<DescentGame>().UseManualSeed=true;
            var startup=UnityEngine.Object.FindFirstObjectByType<DescentGame>();
            Require(startup.ContentCatalog && startup.ContentCatalog.ThemeId=="morrow-office","Legacy entry scene starts in office");
            startup.ContentCatalog=null; // Exercise the runtime fallback, not a test-only assignment.
            SessionState.SetBool(Active,true);SessionState.SetFloat(Active+".deadline",(float)EditorApplication.timeSinceStartup+180);EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!Application.isBatchMode||!SessionState.GetBool(Active,false))return;
            try
            {
                Require(EditorApplication.timeSinceStartup<SessionState.GetFloat(Active+".deadline",0),"Office Play test timeout");
                if(!EditorApplication.isPlaying)return;
                DescentGame game=UnityEngine.Object.FindFirstObjectByType<DescentGame>();if(!game||!game.CurrentMap||!game.CurrentMap.Ready||game.Phase==DescentGame.RunPhase.Generating)return;
                if(game.Paused)game.SetPaused(false);
                office=game.CurrentOffice;Require(office,"Office runtime exists");
                if(stage==0)
                {
                    Require(game.Phase==DescentGame.RunPhase.Briefing,"Office load completed");Require(office.Objective&&office.Plan.TargetType==2,"Signature vending objective");Require(office.Employees.Count>=4,"Occupied office");
                    firstHash=office.Plan.Hash;originalPower=game.Power;game.Begin();game.RequestDeparture();Require(game.Phase==DescentGame.RunPhase.Exploring,"Cannot depart without mandatory target");
                    supervisor=office.Employees.Find(e=>e.Supervisor);Require(supervisor&&supervisor.HasBadge,"Reachable supervisor carries badge");
                    office.Doors[0].Use();Require(!office.Doors[0].Unlocked,"Invalid clearance cannot open door");
                    Vector3 witnessed=supervisor.transform.position+supervisor.transform.forward*2;
                    game.Player.Teleport(witnessed);Require(!supervisor.CanPickpocket(game.Player.transform),"Front pickpocket denied");
                    OfficeEmployee distant=office.Employees.Find(e=>Vector3.Distance(e.transform.position,witnessed)>20);float prior=distant?distant.Suspicion:0;
                    office.ReportAction(witnessed,20,null);Require(supervisor.Suspicion>0,"Witness reacts to visible action");if(distant)Require(distant.Suspicion==prior,"Distant employee does not learn action telepathically");
                    game.Player.Teleport(supervisor.transform.position-supervisor.transform.forward*1.1f);
                    Require(supervisor.CanPickpocket(game.Player.transform),"Behind-NPC pickpocket envelope");supervisor.Pickpocket(3);
                    Require(!supervisor.HasBadge&&office.BadgeLevel==2,"Badge transferred to player");
                    office.Doors[0].Use();Require(office.Doors[0].Unlocked,"Credential opens intended door");
                    recovered=office.Objective;recovered.ReleaseMount();
                    game.Player.Teleport(new Vector3(0,.08f,-6.5f));
                    recovered.Item.Body.position=new Vector3(2,1.3f,-7);recovered.transform.position=recovered.Item.Body.position;recovered.Item.Body.linearVelocity=Vector3.zero;recovered.Item.Body.rotation=Quaternion.identity;Physics.SyncTransforms();
                    Require(office.RequiredRecovered,"Entire objective fits inside cabin");
                    game.RecountCargo();Require(game.CargoValue>=recovered.Item.Value,"Objective uses existing cargo accounting");
                    measureStart=EditorApplication.timeSinceStartup;frames=Time.frameCount;stage=1;return;
                }
                if(stage==1)
                {
                    if(EditorApplication.timeSinceStartup-measureStart<4)return;
                    Directory.CreateDirectory("TestResults/Office");File.WriteAllText("TestResults/Office/play-performance.txt",(Time.frameCount-frames)+" Unity game frames / "+(EditorApplication.timeSinceStartup-measureStart).ToString("F2")+" seconds. This is a headless editor smoke sample, not player FPS.\n"+office.Employees.Count+" employees; "+office.Kit.A.Pieces+" modeled pieces; "+office.GenerationMilliseconds+" ms generation; "+UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576+" MiB Unity allocated memory.");
                    game.RequestDeparture();Require(game.Phase==DescentGame.RunPhase.Closing,"Recovered objective permits departure");Time.timeScale=10;stage=2;return;
                }
                if(stage==2&&game.FloorIndex==1&&game.Phase==DescentGame.RunPhase.Exploring)
                {
                    Require(recovered&&DescentGame.InCabin(recovered.transform.position),"Vending machine persists through descent");Require(game.Power<originalPower,"Heavy transport retains power cost");
                    Require(office.Objective!=recovered&&!office.RequiredRecovered,"Next floor has a new required asset");Require(office.BadgeLevel==1,"Default badges expire per floor");Require(office.Plan.Hash!=firstHash,"Next office recipe changes");
                    game.RegenerateMap(104730,MapSize.Small,false);stage=3;return;
                }
                if(stage==3)
                {
                    Require(office.Plan.TargetType==0&&office.FitResult.StartsWith("PASS"),"Third objective type validates actual physical envelope");
                    Finish(true,"OFFICE PLAY PASSED / load, occupied stations, mandatory extraction gate, front rejection, behind-target badge theft, witness-local suspicion, credential door, actual bounds for all three objectives, valuation, heavy-object persistence, power, new objective and expiring badges.");
                }
            }
            catch(Exception error){Finish(false,error.ToString());}
        }
        static void Require(bool pass,string message){if(!pass)throw new Exception(message);}
        static void Log(string message,string trace,LogType type)
        {
            if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("TheElevator."))return;
            if(SessionState.GetBool(Active,false)&&(type==LogType.Exception||type==LogType.Error||type==LogType.Assert))Finish(false,message+"\n"+trace);
        }
        static void Finish(bool pass,string message)
        {
            SessionState.SetBool(Active,false);Time.timeScale=1;Directory.CreateDirectory("TestResults/Office");File.WriteAllText("TestResults/Office/play-validation.txt",message);
            if(pass)Debug.Log(message);else Debug.LogError(message);EditorApplication.Exit(pass?0:1);
        }
    }
}






