using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using TheElevator.SoftOffice;
namespace TheElevator.Editor
{
 public static class SoftTargetTools
 {
  public const string Scene="Assets/Scenes/SoftOfficeTarget.unity";
  [MenuItem("The Elevator/Art Direction/Open Soft Office Target")]
  public static void Open(){if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;Create();EditorSceneManager.OpenScene(Scene);}
  [MenuItem("The Elevator/Play Latest Target",false,0)]
  public static void Play()
  {
   if(EditorApplication.isPlaying){EditorApplication.playModeStateChanged-=ResumePlay;EditorApplication.playModeStateChanged+=ResumePlay;EditorApplication.isPlaying=false;return;}
   if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   OfficeTools.CreateAssets();EditorSceneManager.OpenScene(OfficeTools.ScenePath);EditorApplication.isPlaying=true;
  }
  static void ResumePlay(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode)return;EditorApplication.playModeStateChanged-=ResumePlay;EditorApplication.delayCall+=Play;}
  public static void Create(){if(File.Exists(Scene))return;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);new GameObject("Soft office visual target").AddComponent<SoftOfficeTarget>();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);}
  public static void Render()
  {
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);var root=new GameObject("Soft office target");var target=root.AddComponent<SoftOfficeTarget>();target.Build(false);Directory.CreateDirectory("TestResults/SoftOffice");
   Capture(root.transform,"TestResults/SoftOffice/overview.png",new Vector3(9,7,-11),new Vector3(-.3f,1.2f,1.0f));
   Capture(root.transform,"TestResults/SoftOffice/employee.png",new Vector3(-2.2f,1.6f,-.7f),new Vector3(-3.8f,1.1f,1.5f));
   var playerRoot=new GameObject("First person study");playerRoot.transform.SetParent(root.transform);playerRoot.transform.position=new Vector3(.5f,.15f,-2.6f);var player=playerRoot.AddComponent<SoftTargetPlayer>();player.Build(target.Art);player.SendMessage("LateUpdate");
   // Explicitly position viewmodel for edit-mode capture (delta time is zero).
   player.LeftHand.position=player.View.transform.TransformPoint(new Vector3(-.27f,-.31f,.56f));player.RightHand.position=player.View.transform.TransformPoint(new Vector3(.27f,-.31f,.56f));player.LeftHand.rotation=Quaternion.Euler(35,12,12);player.RightHand.rotation=Quaternion.Euler(35,-12,-12);
   player.LeftHand.GetComponent<SoftGlove>().Pose(false,0);player.RightHand.GetComponent<SoftGlove>().Pose(false,0);
   Capture(root.transform,"TestResults/SoftOffice/first-person.png",player.View.transform.position,player.View.transform.position+Vector3.forward);
   player.LeftHand.gameObject.SetActive(false);player.RightHand.gameObject.SetActive(false);
   Capture(root.transform,"TestResults/SoftOffice/looking-down.png",player.View.transform.position,playerRoot.transform.position+new Vector3(0,0,.4f));
   foreach(MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())foreach(Vector3 v in filter.sharedMesh.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z))throw new Exception("Invalid target geometry");
   Debug.Log("SOFT TARGET RENDER PASS / finite meshes / "+root.GetComponentsInChildren<SoftRobot>().Length+" original robots / "+root.GetComponentsInChildren<SoftPickup>().Length+" physical props");UnityEngine.Object.DestroyImmediate(root);
  }
  static void Capture(Transform parent,string path,Vector3 position,Vector3 target)
  {
   var go=new GameObject("Target evidence camera");go.transform.SetParent(parent);var camera=go.AddComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=66;camera.nearClipPlane=.06f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.65f,.76f,.77f);
   RenderTexture rt=RenderTexture.GetTemporary(1600,900,24);RenderTexture previous=RenderTexture.active;Texture2D picture=new Texture2D(1600,900,TextureFormat.RGB24,false);
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;picture.ReadPixels(new Rect(0,0,1600,900),0,0);picture.Apply();File.WriteAllBytes(path,picture.EncodeToPNG());}
   finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(picture);UnityEngine.Object.DestroyImmediate(go);}
  }
  public static void Build(){Create();Directory.CreateDirectory("Builds/SoftOffice");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SoftOffice/SoftOfficeTarget.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Soft target build failed");Debug.Log("SOFT TARGET BUILD PASS");}
 }
}
