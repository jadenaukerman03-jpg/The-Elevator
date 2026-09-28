using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TheElevator.SoftOffice;
namespace TheElevator.Editor
{
 [InitializeOnLoad] public static class SoftTargetValidation
 {
  const string Key="SoftTarget.Validation";static int stage;static float start;static SoftPickup prop;static Vector3 initial;
  static SoftTargetValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
  public static void Run(){SoftTargetTools.Create();EditorSceneManager.OpenScene(SoftTargetTools.Scene);SessionState.SetBool(Key,true);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+90);EditorApplication.EnterPlaymode();}
  static void Log(string message,string trace,LogType kind){if(!SessionState.GetBool(Key,false)||trace.Contains("UnityEditor.Search.SearchDatabase"))return;if(kind==LogType.Exception||kind==LogType.Error||kind==LogType.Assert){SessionState.SetBool(Key,false);EditorApplication.Exit(1);}}
  static void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
  static void Tick(){if(!Application.isBatchMode||!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
   try{
    Check(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+"deadline",0),"Target timeout");var scene=UnityEngine.Object.FindFirstObjectByType<SoftOfficeTarget>();if(!scene||!scene.Player)return;var player=scene.Player;
    if(stage==0){Check(scene.GetComponentsInChildren<SoftRobot>().Length==2,"Two original character studies");prop=scene.GetComponentInChildren<SoftPickup>();Check(prop&&prop.GetComponent<Collider>().enabled,"Physical pickup exists");prop.Body.position=player.View.transform.position+player.View.transform.forward*1.5f;initial=prop.Body.position;player.Grab(prop);start=Time.time;stage=1;return;}
    if(stage==1){if(Time.time-start<1.0f)return;Check(player.Held==prop&&!prop.Body.isKinematic&&prop.GetComponent<Collider>().enabled,"Spring pickup retains collision and dynamic simulation");Check(Vector3.Distance(prop.Body.position,initial)>.1f,"Spring physically moves prop");Check(prop.Body.linearVelocity.magnitude<10,"Bounded spring response");player.Release(100);Check(!player.Held&&prop.Body.linearVelocity.magnitude<=9.1f,"Throw strength capped");start=Time.time;stage=2;return;}
    if(stage==2){if(Time.time-start<1.0f)return;Check(float.IsFinite(prop.transform.position.x)&&prop.transform.position.y>-.5f,"Released prop stays finite and on floor");SessionState.SetBool(Key,false);Debug.Log("SOFT TARGET PLAY PASS / dynamic collision-retaining grab, finite spring, capped throw, floor collision, original robot and hand scene");EditorApplication.Exit(0);}
   }catch(Exception e){SessionState.SetBool(Key,false);Debug.LogError(e);EditorApplication.Exit(1);}
  }
 }
}

