using System;
using System.Collections;
using System.IO;
using TheElevator.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheElevator.Editor
{
    public sealed class GenerationLabWindow : EditorWindow
    {
        int seed = 104729, layer;
        MapSize size = MapSize.Standard;
        FloorGenerationProfile profile;
        FloorContentCatalog catalog;
        MapManifest manifest;
        string status = "Generate a graph to inspect a floor without entering Play mode.";
        Scene preview;
        GeneratedFloor generated;
        Workshop workshop;

        [MenuItem("The Elevator/Generation Lab")]
        public static void Open() { GetWindow<GenerationLabWindow>("Generation Lab"); }
        void OnEnable() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }
        void OnDisable() { EditorApplication.playModeStateChanged -= OnPlayModeChanged; ClearPreview(); }
        void OnPlayModeChanged(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingEditMode) ClearPreview(); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("CIVIC WORKS / FLOOR GENERATION", EditorStyles.boldLabel);
            seed = EditorGUILayout.IntField("Seed", seed);
            size = (MapSize)EditorGUILayout.EnumPopup("Scale preset", size);
            profile = (FloorGenerationProfile)EditorGUILayout.ObjectField("Profile override", profile, typeof(FloorGenerationProfile), false);
            catalog = (FloorContentCatalog)EditorGUILayout.ObjectField("Content catalog", catalog, typeof(FloorContentCatalog), false);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate graph")) GenerateGraph();
            if (GUILayout.Button("Random seed")) { seed = Environment.TickCount; GenerateGraph(); }
            if (GUILayout.Button("Replay saved")) Replay();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || manifest == null))
            { if (GUILayout.Button("Build 3D preview")) BuildPreview(); }
            if (GUILayout.Button("Clear preview")) ClearPreview();
            using (new EditorGUI.DisabledScope(manifest == null)) { if (GUILayout.Button("Export manifest")) Export(); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(status, MessageType.Info);
            if (manifest == null) return;
            layer = EditorGUILayout.IntSlider("Vertical level", layer, 0, manifest.Recipe.Settings.VerticalLayers - 1);
            EditorGUILayout.LabelField("Version / fingerprint", manifest.Recipe.GenerationVersion + " / " + manifest.StructureHash.Substring(0,16));
            Rect graph = GUILayoutUtility.GetRect(100,10000,180,10000);
            EditorGUI.DrawRect(graph, new Color(0.045f,0.065f,0.075f));
            MapGraphGUI.Draw(manifest, new Rect(graph.x + 15,graph.y + 15,graph.width - 30,graph.height - 30),layer);
        }
        void GenerateGraph()
        {
            try
            {
                MapRecipe recipe = MapRecipe.Default(seed,size);
                FloorGenerationProfile selected = profile ? profile : Resources.Load<FloorGenerationProfile>("Generation/" + size);
                if (selected) recipe.Settings = JsonUtility.FromJson<GenerationSettings>(JsonUtility.ToJson(selected.Settings));
                FloorContentCatalog theme = catalog ? catalog : Resources.Load<FloorContentCatalog>("Generation/CivicWorks");
                if (theme) { recipe.Modules = theme.Modules; recipe.Districts = theme.Districts; recipe.ThemeId = theme.ThemeId; recipe.ThemeRevision = theme.ThemeRevision; recipe.ThemePayload = theme.ThemePayload; }
                recipe = JsonUtility.FromJson<MapRecipe>(JsonUtility.ToJson(recipe));
                manifest = new MacroLayoutGenerator().Generate(recipe);
                status = manifest.Rooms.Count + " rooms / " + manifest.CycleCount + " loops / " + manifest.EstimatedWalkableSquareMeters +
                    " approximate m2 before props / deepest room " + manifest.FurthestDistance + " links from the lift. Attempt " + (manifest.Attempt + 1) + ".";
                layer = Mathf.Clamp(layer,0,recipe.Settings.VerticalLayers - 1);
            }
            catch (Exception e) { manifest = null; status = e.Message; }
        }
        void Replay()
        {
            try
            {
                GenerationRecord record = GenerationRecordStore.Load();
                MapManifest result = new MacroLayoutGenerator().Generate(record.Recipe);
                if (!record.Matches(result)) throw new Exception("Replay fingerprint mismatch.");
                manifest = result; seed = record.Recipe.Seed; status = "Saved recipe replayed; all fingerprints match.";
            }
            catch (Exception e) { status = e.Message; }
        }
        void BuildPreview()
        {
            ClearPreview();
            try
            {
                preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                GameObject root = new GameObject("Generated preview (cleared before Play)");
                SceneManager.MoveGameObjectToScene(root, preview);
                generated = root.AddComponent<GeneratedFloor>(); generated.Initialize(manifest);
                workshop = new Workshop();
                IEnumerator build = new FloorGeometryBuilder(workshop, catalog ? catalog : Resources.Load<FloorContentCatalog>("Generation/CivicWorks"))
                    .Build(generated,null,null);
                while (build.MoveNext()) { }
                Selection.activeGameObject = root;
                if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0,2,4),Vector3.one * 20),false);
                status = "3D preview created in a separate unsaved scene. It is cleared before Play; your prototype scene is unchanged.";
            }
            catch (Exception e) { ClearPreview(); status = e.ToString(); }
        }
        void ClearPreview()
        {
            if (preview.IsValid() && preview.isLoaded) EditorSceneManager.CloseScene(preview,true);
            generated = null;
            if (workshop != null) { workshop.Dispose(); workshop = null; }
        }
        void Export()
        {
            string directory = Path.GetFullPath("TestResults"); Directory.CreateDirectory(directory);
            string path = Path.Combine(directory,"map-" + manifest.Recipe.Seed + ".json");
            File.WriteAllText(path,JsonUtility.ToJson(manifest,true));
            status = "Exported complete graph / module assignments / content sockets to " + path;
        }
    }
}
