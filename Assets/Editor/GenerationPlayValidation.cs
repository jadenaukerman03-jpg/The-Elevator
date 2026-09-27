using System;
using System.IO;
using TheElevator.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheElevator.Editor
{
    // Batch-only integration test; never enters Play mode in the user's interactive editor.
    [InitializeOnLoad]
    public static class GenerationPlayValidation
    {
        const string ActiveKey = "Elevator.GenerationPlayValidation";
        static int stage;
        static string structure;
        static SalvageItem cargo;
        static float startingPower;
        static int expectedCargo;

        static GenerationPlayValidation()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this test in an isolated batch-mode project.");
            EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");
            DescentGame game = UnityEngine.Object.FindFirstObjectByType<DescentGame>();
            game.ContentCatalog = Resources.Load<FloorContentCatalog>("Generation/CivicWorks"); // Explicit legacy fixture; normal startup is the office.
            game.UseManualSeed = true; game.ManualSeed = 104729;
            game.MapScale = MapSize.Standard; game.ProfileOverride = null; game.UseProceduralFloors = true;
            SessionState.SetBool(ActiveKey,true);
            SessionState.SetFloat(ActiveKey + ".deadline",(float)EditorApplication.timeSinceStartup + 180);
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            if (!Application.isBatchMode || !SessionState.GetBool(ActiveKey,false)) return;
            try
            {
                Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(ActiveKey + ".deadline",0),"Play validation timed out.");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                DescentGame game = UnityEngine.Object.FindFirstObjectByType<DescentGame>();
                if (!game || !game.CurrentMap || !game.CurrentMap.Ready || game.Phase == DescentGame.RunPhase.Generating) return;
                if (game.Paused) game.SetPaused(false);
                if (stage == 0)
                {
                    Require(game.Phase == DescentGame.RunPhase.Briefing,"Initial briefing follows generation.");
                    Require(game.CurrentMap.Manifest.Rooms.Count == 72 && game.Clock == 1500,"Standard profile and clock.");
                    Require(game.Items.Count > 0 && game.Guards.Count > 0,"Gameplay content populated.");
                    Require(GenerationRecordStore.Load().Matches(game.CurrentMap.Manifest),"Saved record reproduces generated fingerprints.");
                    structure = game.CurrentMap.Manifest.StructureHash;
                    game.Begin(); stage = 1; return;
                }
                if (stage == 1)
                {
                    Require(game.Phase == DescentGame.RunPhase.Exploring,"Clock In starts exploration.");
                    Require(!game.Player.Model.gameObject.activeSelf,"First-person hides own worker body.");
                    Require(Vector3.Distance(game.Player.View.transform.position,game.Player.transform.position + Vector3.up * 1.7f) < 0.08f,"First-person eye height.");
                    ObjectiveTerminal terminal = game.CurrentMap.GetComponentInChildren<ObjectiveTerminal>();
                    Require(terminal != null,"Survey objective exists.");
                    terminal.Use(game); terminal.Use(game);
                    Require(game.CurrentMap.CompletedObjectives == 1,"Objective only completes once.");
                    cargo = game.Items.Find(item => item && !item.IsBattery);
                    Require(cargo != null,"Recoverable cargo exists.");
                    cargo.Body.position = new Vector3(2,0.8f,-7);
                    cargo.transform.position = cargo.Body.position;
                    cargo.Body.linearVelocity = Vector3.zero;
                    expectedCargo = cargo.Value;
                    game.RecountCargo(); Require(game.CargoValue == expectedCargo,"Cargo recorded inside cabin.");
                    game.RegenerateMap(0,MapSize.Standard,true); stage = 2; return;
                }
                if (stage == 2)
                {
                    Require(game.CurrentMap.Manifest.StructureHash == structure,"Replay retains structure.");
                    Require(game.CurrentMap.CompletedObjectives == 0,"New floor resets survey state.");
                    game.RecountCargo(); Require(cargo && game.CargoValue == expectedCargo,"Cargo survives regeneration. Actual value=" + game.CargoValue + ", expected=" + expectedCargo + ", cargo=" + (cargo ? cargo.transform.position.ToString() : "destroyed"));
                    game.RegenerateMap(-71,MapSize.Small,false); stage = 3; return;
                }
                if (stage == 3)
                {
                    Require(game.CurrentMap.Manifest.Rooms.Count == 18 && game.ActiveSeed == -71,"Debug seed and size apply.");
                    Require(game.Clock > 475 && game.Clock <= 480,"Small profile timing applies.");
                    Require(game.CurrentMap.Manifest.StructureHash != structure,"Different recipe changes structure.");
                    startingPower = game.Power;
                    game.RequestDeparture(); Require(game.Phase == DescentGame.RunPhase.Closing,"Early departure starts closing.");
                    Time.timeScale = 12; stage = 4; return;
                }
                if (stage == 4 && game.FloorIndex == 1 && game.Phase == DescentGame.RunPhase.Exploring)
                {
                    game.RecountCargo();
                    Require(cargo && game.CargoValue == expectedCargo,"Recovered cargo survives actual descent.");
                    Require(game.Power < startingPower && game.Player.InCabin,"Departure consumes power and retains worker.");
                    Require(game.CurrentMap.Manifest.Rooms.Count == 18 && game.ActiveSeed == -71 + 104729,"Next stop generates next deterministic seed.");
                    Finish(true,"PLAY VALIDATION PASSED: loading, first-person camera, content, objective idempotence, saved replay, regeneration, profile switching, cargo persistence, power and next-floor generation.");
                }
            }
            catch (Exception error) { Finish(false,error.ToString()); }
        }

        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void OnLog(string message, string trace, LogType type)
        {
            // Unity 6.3's search index can throw during batch startup, before the game runs.
            // Preserve it in the editor log; it is not a game-runtime exception.
            if (trace.Contains("UnityEditor.Search.SearchDatabase") && !trace.Contains("TheElevator.")) return;
            if (SessionState.GetBool(ActiveKey,false) && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
                Finish(false,message + "\n" + trace);
        }
        static void Finish(bool success, string message)
        {
            SessionState.SetBool(ActiveKey,false); Time.timeScale = 1;
            Directory.CreateDirectory("TestResults"); File.WriteAllText("TestResults/play-validation.txt",message);
            if (success) Debug.Log(message); else Debug.LogError(message);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}

