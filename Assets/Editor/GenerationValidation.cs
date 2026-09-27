using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TheElevator.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheElevator.Editor
{
    public static class GenerationValidation
    {
        [MenuItem("The Elevator/Validate Generated Geometry")]
        public static void ValidateGeometry()
        {
            foreach (MapSize size in Enum.GetValues(typeof(MapSize))) ValidateGeometry(size,104729);
            ValidateGeometry(MapSize.Standard,-17);
        }

        static void ValidateGeometry(MapSize size, int seed)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before geometry validation.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Workshop workshop = new Workshop(); GameObject root = null;
            try
            {
                MapManifest manifest = new MacroLayoutGenerator().Generate(MapRecipe.Default(seed,size));
                root = new GameObject("Geometry checks"); SceneManager.MoveGameObjectToScene(root,scene);
                GeneratedFloor floor = root.AddComponent<GeneratedFloor>(); floor.Initialize(manifest);
                IEnumerator build = new FloorGeometryBuilder(workshop,Resources.Load<FloorContentCatalog>("Generation/CivicWorks")).Build(floor,null,null);
                while (build.MoveNext()) { }
                Physics.SyncTransforms();
                int checkedSegments = 0, floorHits = 0;
                foreach (MapRoom room in manifest.Rooms)
                {
                    Vector3 anchor = floor.Anchor(room);
                    Require(!Physics.CheckCapsule(anchor + Vector3.up * 0.48f,anchor + Vector3.up * 1.57f,0.37f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"Blocked room anchor " + room.Id);
                    Require(Physics.Raycast(anchor + Vector3.up * 0.5f,Vector3.down,1f),"No floor at room anchor " + room.Id); floorHits++;
                }
                foreach (MapLink link in manifest.Links)
                {
                    List<Vector3> route = floor.Route(link.A,link.B);
                    for (int i = 1; i < route.Count; i++)
                    {
                        Vector3 delta = route[i] - route[i - 1];
                        if (delta.magnitude < 0.01f) continue;
                        // A 0.74 m wide, 1.83 m high body must fit along every authored route.
                        Require(!Physics.CapsuleCast(route[i - 1] + Vector3.up * 0.52f,route[i - 1] + Vector3.up * 1.61f,0.37f,
                            delta.normalized,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"Blocked link " + link.A + "-" + link.B + " segment " + i);
                        for (float t = 0; t <= 1; t += 0.2f)
                        {
                            Vector3 position = Vector3.Lerp(route[i - 1],route[i],t);
                            Require(Physics.Raycast(position + Vector3.up * 0.6f,Vector3.down,1.1f),"Unsupported route " + link.A + "-" + link.B + " segment=" + i + " t=" + t + " at=" + position); floorHits++;
                        }
                        checkedSegments++;
                    }
                }
                string directory = Path.GetFullPath("TestResults"); Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,"validated-map.json"),JsonUtility.ToJson(manifest,true));
                if (size == MapSize.Standard && seed == 104729) CaptureView(floor,directory);
                Debug.Log("GENERATION GEOMETRY PASSED: " + size + " seed=" + seed + ": " + manifest.Rooms.Count + " rooms, " + checkedSegments + " capsule-tested segments, " + floorHits + " floor-support probes.");
            }
            finally
            {
                if (root) UnityEngine.Object.DestroyImmediate(root);
                workshop.Dispose();
                EditorSceneManager.CloseScene(scene,true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        static void CaptureView(GeneratedFloor floor, string directory)
        {
            // Capture from player eye height, never an overhead shot standing in for first-person QA.
            GameObject go = new GameObject("Validation camera"); SceneManager.MoveGameObjectToScene(go,floor.gameObject.scene);
            Camera camera = go.AddComponent<Camera>();
            camera.transform.position = new Vector3(0,1.7f,-1);
            camera.transform.rotation = Quaternion.Euler(0,0,0);
            camera.fieldOfView = 75; camera.nearClipPlane = 0.08f; camera.farClipPlane = 150;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Workshop.Ink;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f,0.4f,0.42f);
            RenderTexture target = RenderTexture.GetTemporary(1280,720,24);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(directory,"generated-arrival.png"),image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(go);
            }
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

        [MenuItem("The Elevator/Create Missing Generation Assets")]
        public static void CreateDefaults()
        {
            const string path = "Assets/Resources/Generation";
            Directory.CreateDirectory(path); AssetDatabase.Refresh();
            foreach (MapSize size in Enum.GetValues(typeof(MapSize)))
            {
                string assetPath = path + "/" + size + ".asset";
                if (AssetDatabase.LoadAssetAtPath<FloorGenerationProfile>(assetPath)) continue;
                FloorGenerationProfile asset = ScriptableObject.CreateInstance<FloorGenerationProfile>(); asset.Settings = GenerationSettings.Preset(size);
                AssetDatabase.CreateAsset(asset,assetPath);
            }
            if (!AssetDatabase.LoadAssetAtPath<FloorContentCatalog>(path + "/CivicWorks.asset"))
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FloorContentCatalog>(),path + "/CivicWorks.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("GENERATION DEFAULT ASSETS READY");
        }

        public static void BatchValidate()
        {
            CreateDefaults();
            ProjectTools.ValidateProject();
            ValidateGeometry();
        }
    }
}
