using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using TheElevator.Generation;
using TheElevator.Office;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheElevator.Editor
{
    public static class OfficeTools
    {
        public const string ScenePath="Assets/Scenes/OfficeShowcase.unity";
        public const string CatalogPath="Assets/Resources/Generation/MorrowOffice.asset";
        [MenuItem("The Elevator/Office/Build Windows Showcase")]
        public static void BuildPlayer()
        {
            CreateAssets();Directory.CreateDirectory("Builds/Office");
            UnityEditor.Build.Reporting.BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Office/TheElevatorOffice.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Office player build failed.");
            UnityEngine.Debug.Log("OFFICE WINDOWS BUILD PASSED");
        }
        [MenuItem("The Elevator/Office/Open Showcase")]
        public static void Open()
        {
            if(EditorApplication.isPlaying)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            CreateAssets();EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("The Elevator/Play Updated Office")]
        public static void PlayUpdatedOffice()
        {
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            CreateAssets();EditorSceneManager.OpenScene(ScenePath);EditorApplication.isPlaying=true;
        }
        public static void CreateAssets()
        {
            if(!AssetDatabase.LoadAssetAtPath<FloorContentCatalog>(CatalogPath))
            {
                FloorContentCatalog theme=ScriptableObject.CreateInstance<FloorContentCatalog>();theme.ThemeId="morrow-office";theme.ThemeRevision=2;theme.ThemePayload=JsonUtility.ToJson(new OfficeConfig());
                theme.WallColor=new Color(.65f,.66f,.59f);theme.FloorColor=new Color(.19f,.23f,.21f);
                foreach(DistrictSpec department in theme.Districts)department.WetFloor=false;
                AssetDatabase.CreateAsset(theme,CatalogPath);AssetDatabase.SaveAssets();
            }
            if(!File.Exists(ScenePath))
            {
                Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                DescentGame game=new GameObject("Morrow office bootstrap").AddComponent<DescentGame>();game.ContentCatalog=AssetDatabase.LoadAssetAtPath<FloorContentCatalog>(CatalogPath);game.UseManualSeed=false;game.ManualSeed=104729;game.MapScale=MapSize.Small;
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
        }
        public static MapRecipe Recipe(int seed,MapSize size)
        {
            MapRecipe recipe=MapRecipe.Default(seed,size);FloorContentCatalog catalog=AssetDatabase.LoadAssetAtPath<FloorContentCatalog>(CatalogPath);
            recipe.ThemeId=catalog.ThemeId;recipe.ThemeRevision=catalog.ThemeRevision;recipe.ThemePayload=catalog.ThemePayload;recipe.Districts=catalog.Districts;return recipe;
        }
        [MenuItem("The Elevator/Office/Render Showcase Evidence")]
        public static void RenderShowcase()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play before rendering the showcase.");
            CreateAssets();
            if(Application.isBatchMode)EditorSceneManager.OpenScene(ScenePath);
            string output=Path.GetFullPath("TestResults/Office");Directory.CreateDirectory(output);
            Scene previous=SceneManager.GetActiveScene(),scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            Workshop workshop=new Workshop();GameObject root=null;
            try
            {
                Stopwatch watch=Stopwatch.StartNew();MapManifest manifest=new MacroLayoutGenerator().Generate(Recipe(104729,MapSize.Small));
                root=new GameObject("Office visual showcase");SceneManager.MoveGameObjectToScene(root,scene);
                GeneratedFloor floor=root.AddComponent<GeneratedFloor>();floor.Initialize(manifest);
                IEnumerator builder=new FloorGeometryBuilder(workshop,AssetDatabase.LoadAssetAtPath<FloorContentCatalog>(CatalogPath)).Build(floor,null,null);
                while(builder.MoveNext()){}
                OfficeFloor office=root.GetComponent<OfficeFloor>();office.Assemble(null);
                foreach(OfficeEmployee employee in office.Employees)employee.Robot.Animate(1);
                Transform machine=office.Kit.A.Group(floor.RoomRoots[office.Plan.TargetRoom],"Preview of mandatory asset",new Vector3(3.5f,.13f,3.5f));office.Kit.Vending(machine);
                long elapsed=watch.ElapsedMilliseconds;
                Light sun=new GameObject("Office broad fill").AddComponent<Light>();sun.transform.SetParent(root.transform);sun.type=LightType.Directional;sun.intensity=.6f;sun.color=new Color(.79f,.85f,.84f);sun.transform.rotation=Quaternion.Euler(48,-25,0);sun.shadows=LightShadows.Soft;
                QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=35;QualitySettings.pixelLightCount=6;QualitySettings.antiAliasing=4;
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.16f,.19f,.18f);RenderSettings.fog=false;
                Capture(root.transform,output+"/after-lobby.png",floor.Center(manifest.Rooms[0])+new Vector3(0,1.7f,-4.8f),floor.Center(manifest.Rooms[0])+new Vector3(-1,1.4f,3.8f));
                Capture(root.transform,output+"/after-workroom.png",floor.Center(manifest.Rooms[2])+new Vector3(0,1.7f,-3.5f),floor.Center(manifest.Rooms[2])+new Vector3(-3.2f,1.1f,3.6f));
                Capture(root.transform,output+"/after-vending.png",machine.position+new Vector3(-2.7f,1.6f,-3.8f),machine.position+Vector3.up*1.05f);
                OfficeEmployee supervisor=office.Employees[0];Capture(root.transform,output+"/after-robot.png",supervisor.transform.position+supervisor.transform.forward*.7f+supervisor.transform.right*1.7f+Vector3.up*1.65f,supervisor.transform.position+Vector3.up*1.13f);
                string metrics="Seed 104729 / High / "+manifest.Rooms.Count+" rooms / "+office.Employees.Count+" NPCs / "+office.Kit.A.Pieces+" authored pieces before batching / "+elapsed+" ms editor construction\n"+office.FitResult+"\nStatic preview renderers: "+root.GetComponentsInChildren<Renderer>().Length+"\nNo gameplay frame-rate claim from offscreen rendering.";
                File.WriteAllText(output+"/showcase-metrics.txt",metrics);File.WriteAllText(output+"/office-plan.json",JsonUtility.ToJson(office.Plan,true));UnityEngine.Debug.Log("OFFICE SHOWCASE RENDERED\n"+metrics);
            }
            finally{if(root)UnityEngine.Object.DestroyImmediate(root);workshop.Dispose();EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        public static void Capture(Transform parent,string path,Vector3 position,Vector3 target)
        {
            GameObject go=new GameObject("Evidence camera");go.transform.SetParent(parent);Camera camera=go.AddComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=66;camera.nearClipPlane=.08f;camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.07f,.07f);
            Light[] lights=parent.GetComponentsInChildren<Light>();Array.Sort(lights,(a,b)=>(a.transform.position-position).sqrMagnitude.CompareTo((b.transform.position-position).sqrMagnitude));int shadowed=0;
            foreach(Light light in lights)if(light.type==LightType.Point){light.shadows=shadowed++<2?LightShadows.Soft:LightShadows.None;}
            RenderTexture rt=RenderTexture.GetTemporary(1600,900,24);RenderTexture old=RenderTexture.active;Texture2D image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}



