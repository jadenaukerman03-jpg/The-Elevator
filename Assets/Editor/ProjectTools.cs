using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheElevator.Editor
{
    public static class ProjectTools
    {
        const string ScenePath = OfficeTools.ScenePath;

        [MenuItem("The Elevator/Open Prototype")]
        public static void OpenPrototype()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("The Elevator/Build Windows Player")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/TheElevator.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed. Review the Unity console.");
            Debug.Log("Built The Elevator: Builds/Windows/TheElevator.exe");
        }

        // Unity.exe -batchmode -nographics -quit -projectPath ... -executeMethod TheElevator.Editor.ProjectTools.ValidateProject
        public static void ValidateProject()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            int bootstraps = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) > 0)
                    throw new Exception("Scene has missing scripts.");
                bootstraps += root.GetComponents<DescentGame>().Length;
            }
            if (bootstraps != 1) throw new Exception("Expected exactly one DescentGame bootstrap.");
            if (!Shader.Find("Elevator/PrototypeSurface")) throw new Exception("Prototype shader missing.");
            if (!Shader.Find("Elevator/WorldLabel")) throw new Exception("World lettering shader missing.");
            if (!RunRules.CanDepart(24, 180)) throw new Exception("Departure rule failed.");
            Debug.Log("ELEVATOR VALIDATION PASSED: scripts imported, bootstrap linked, shader present, rules valid.");
        }
    }
}
