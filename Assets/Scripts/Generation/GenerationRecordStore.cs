using System;
using System.IO;
using UnityEngine;

namespace TheElevator.Generation
{
    [Serializable]
    public sealed class GenerationRecord
    {
        public MapRecipe Recipe;
        public string ConfigurationHash, StructureHash, ContentHash;
        public int Attempt;
        public static GenerationRecord From(MapManifest manifest)
        {
            return new GenerationRecord { Recipe = manifest.Recipe, ConfigurationHash = manifest.ConfigurationHash,
                StructureHash = manifest.StructureHash, ContentHash = manifest.ContentHash, Attempt = manifest.Attempt };
        }
        // Transport-neutral handshake: compare before a future host releases the elevator doors.
        public bool Matches(MapManifest local)
        {
            return Recipe != null && Recipe.GenerationVersion == local.Recipe.GenerationVersion && Recipe.Seed == local.Recipe.Seed
                && ConfigurationHash == local.ConfigurationHash && StructureHash == local.StructureHash && ContentHash == local.ContentHash;
        }
    }

    public static class GenerationRecordStore
    {
        public static string DirectoryPath
        {
            get
            {
                // Isolated batch validation must not overwrite the player's replay record.
                if (Application.isEditor && Application.isBatchMode) return Path.GetFullPath("TestResults/GenerationRecords");
                if (Array.IndexOf(Environment.GetCommandLineArgs(),"--office-benchmark")>=0) return Path.GetFullPath(Path.Combine(Application.dataPath,"../OfficeBenchmark/Generation"));
                return Path.Combine(Application.persistentDataPath, "Generation");
            }
        }
        public static string LastPath { get { return Path.Combine(DirectoryPath, "last-success.json"); } }
        public static void Save(MapManifest manifest)
        {
            Directory.CreateDirectory(DirectoryPath);
            string temporary = LastPath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(GenerationRecord.From(manifest), true));
            if (File.Exists(LastPath)) File.Replace(temporary, LastPath, null);
            else File.Move(temporary, LastPath);
            foreach (string failure in manifest.FailedAttempts) LogFailure(manifest.Recipe, failure);
        }
        public static GenerationRecord Load()
        {
            if (!File.Exists(LastPath)) throw new IOException("No previous successful generation has been saved.");
            if (new FileInfo(LastPath).Length > 1024 * 1024) throw new IOException("Generation recipe file is unexpectedly large.");
            GenerationRecord record = JsonUtility.FromJson<GenerationRecord>(File.ReadAllText(LastPath));
            if (record == null) throw new IOException("Invalid generation record.");
            MacroLayoutGenerator.ValidateRecipe(record.Recipe);
            if (record.ConfigurationHash != record.Recipe.ConfigurationHash()) throw new IOException("Saved recipe fingerprint mismatch.");
            return record;
        }
        public static void LogFailure(MapRecipe recipe, string message)
        {
            Directory.CreateDirectory(DirectoryPath);
            File.AppendAllText(Path.Combine(DirectoryPath, "failed-seeds.jsonl"),
                JsonUtility.ToJson(new Failure { Recipe = recipe, Message = message, Utc = DateTime.UtcNow.ToString("o") }) + "\n");
        }
        [Serializable] sealed class Failure { public MapRecipe Recipe; public string Message, Utc; }
    }
}
