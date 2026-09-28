using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TheElevator.Office;
namespace TheElevator.Editor
{
 [InitializeOnLoad] public static class CabinValidation
 {
  const string Key="Elevator.CabinValidation";static int stage,seed;static float wait;static string hash;
  static CabinValidation(){EditorApplication.update+=Tick;}
  public static void Run(){EditorSceneManager.OpenScene(OfficeTools.ScenePath);SessionState.SetBool(Key,true);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+180);EditorApplication.EnterPlaymode();}
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static void Tick(){if(!Application.isBatchMode||!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
   try{
    Check(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+"deadline",0),"Cabin validation deadline");
    var game=UnityEngine.Object.FindFirstObjectByType<DescentGame>();if(!game||!game.CurrentOffice||!game.CurrentMap.Ready||game.Phase==DescentGame.RunPhase.Generating)return;if(game.Paused)game.SetPaused(false);
    var office=game.CurrentOffice;
    if(stage==0){
     Check(!game.UseManualSeed,"Player launch randomizes the seed");seed=game.ActiveSeed;hash=game.CurrentMap.Manifest.StructureHash;game.Begin();
     Check(office.DeskKeycard,"Reception access card is always present");
     var buttons=game.GetComponentsInChildren<ElevatorButton>();Check(buttons.Length==51&&buttons.Select(b=>b.Number).Distinct().Count()==51,"All 51 distinct buttons exist");
     Check(game.HighestUnlocked==1&&!game.CanSelectFloor(0)&&!game.CanSelectFloor(2)&&!game.CanSelectFloor(50),"Only level one initially unlocked; hub reserved");
     foreach(var button in buttons){Vector3 start=button.transform.position-button.transform.forward*.45f;Check(Physics.Raycast(start,button.transform.forward,out RaycastHit hit,.6f)&&hit.collider.GetComponent<ElevatorButton>()==button,"Exact ray reaches button "+button.Number);}
     Vector3 gap=(buttons[1].transform.position+buttons[2].transform.position)*.5f;Check(Physics.Raycast(gap-buttons[1].transform.forward*.45f,buttons[1].transform.forward,out RaycastHit gapHit,.6f)&&!gapHit.collider.GetComponent<ElevatorButton>(),"Aiming between numbers cannot select a floor");
     var notebook=game.GetComponentInChildren<FieldNotebook>();Check(notebook,"Physical field notebook");notebook.Open();Check(game.Player.ReadingNotebook,"Notebook can be picked up");notebook.Close();Check(!game.Player.ReadingNotebook,"Notebook returns to table");
          Check(game.Player.Stamina==1,"Infinite stamina preserved");
     wait=Time.time;stage=1;return;
    }
    if(stage==1){if(Time.time-wait<.7f)return;Directory.CreateDirectory("TestResults/Office");
     Check(game.Player.Model.gameObject.activeSelf,"Full body remains active in first person");
     var bodyRenderers=game.Player.Model.GetComponentsInChildren<Renderer>();
     Check(bodyRenderers.Count(r=>r.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)>5,"Head and arms retain complete first-person shadow");
     Check(bodyRenderers.Count(r=>r.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.On)>5,"Torso and legs remain visible");
     Check(game.Player.Hands.GetComponentsInChildren<Transform>().Count(t=>t.name=="Mitten paddle")==2,"Original game uses bean-style mitten hands");

     game.Player.Hands.gameObject.SetActive(false);OfficeTools.Capture(game.transform,"TestResults/Office/cabin-redesign.png",new Vector3(-2.4f,1.8f,-8.7f),new Vector3(2.6f,1.65f,-3.2f));
     foreach(MeshFilter model in game.Player.Hands.GetComponentsInChildren<MeshFilter>(true))foreach(Vector3 v in model.sharedMesh.vertices)Check(!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z),"Finite hand geometry");
     game.Player.Hands.gameObject.SetActive(true);OfficeTools.Capture(game.transform,"TestResults/Office/hands-idle.png",game.Player.View.transform.position,game.Player.View.transform.position+game.Player.View.transform.forward);
     var book=game.GetComponentInChildren<FieldNotebook>();book.Open();book.SendMessage("LateUpdate");OfficeTools.Capture(game.transform,"TestResults/Office/notebook-reading.png",game.Player.View.transform.position,game.Player.View.transform.position+game.Player.View.transform.forward);book.Close();
     var card=office.DeskKeycard;OfficeTools.Capture(game.transform,"TestResults/Office/reception-keycard.png",card.transform.position+Vector3.up*.6f-card.transform.forward*.65f,card.transform.position);
     card.Take();Check(office.BadgeLevel>=2,"Reception card grants access");
     office.Objective.transform.position=new Vector3(0,1.1f,-6);Physics.SyncTransforms();Check(office.RequiredRecovered,"Contract asset fits redesigned cabin");Check(game.CanSelectFloor(2)&&game.HighestUnlocked==2&&!game.CanSelectFloor(3),"Completion unlocks exactly the next floor");
     game.SelectFloor(2);Check(game.Phase==DescentGame.RunPhase.Closing,"Unlocked button starts departure");stage=2;return;
    }
    if(stage==2){if(game.FloorIndex!=1||game.Phase!=DescentGame.RunPhase.Exploring)return;Check(game.HighestUnlocked==2&&!game.CanSelectFloor(3),"Second floor does not auto-unlock third");Check(game.ActiveSeed!=seed&&game.CurrentMap.Manifest.StructureHash!=hash,"Next floor generates distinct geometry");
     var old=game;UnityEngine.Object.DestroyImmediate(old.gameObject);var root=new GameObject("Fresh run validation");root.AddComponent<DescentGame>();stage=3;return;
    }
    if(stage==3){Check(game.ActiveSeed!=seed&&game.CurrentMap.Manifest.StructureHash!=hash,"Fresh run has a new seed and layout");Check(game.HighestUnlocked==1,"Fresh run starts at floor one");Finish(true,"CABIN PASS: randomized starts/layouts, exact 0-50 button rays and gaps, locked hub/floors, reception card, notebook pickup/return, infinite stamina, completion unlock, transit and second floor.");}
   }catch(Exception e){Finish(false,e.ToString());}
  }
  static void Finish(bool success,string message){SessionState.SetBool(Key,false);File.WriteAllText("TestResults/Office/cabin-validation.txt",message);if(success)Debug.Log(message);else Debug.LogError(message);EditorApplication.Exit(success?0:1);}
 }
}
