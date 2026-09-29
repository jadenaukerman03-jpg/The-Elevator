using System.IO;
using UnityEditor;
using UnityEngine;
using TheElevator.Office;
namespace TheElevator.Editor
{
    // Writes the made-up office language to WAV files so voices can be listened to outside the game:
    // a few speakers saying the same lines calm, upset and yelling. Also reports how long synthesis takes.
    public static class VoiceSamples
    {
        const string Folder = "TestResults/Voices";

        [MenuItem("The Elevator/Export Voice Samples")]
        public static void Export()
        {
            Directory.CreateDirectory(Folder);
            string[] lines = { "Hey, did you finish the quarterly report?", "Seriously? Again?", "Give me back my computer!" };
            float[] voices = { 110, 165, 225 };
            string[] names = { "deep", "middle", "high" };
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int count = 0;
            double slowest = 0;
            for (int v = 0; v < voices.Length; v++)
            {
                // One file per voice: calm, upset and yelling back to back with short pauses.
                var all = new System.Collections.Generic.List<float>();
                for (int tone = 0; tone < 3; tone++)
                {
                    var one = System.Diagnostics.Stopwatch.StartNew();
                    AudioClip clip = OfficeVoice.Speak(lines[tone], (OfficeVoice.Tone)tone, voices[v], v + 1);
                    slowest = System.Math.Max(slowest, one.Elapsed.TotalMilliseconds);
                    float[] data = new float[clip.samples]; clip.GetData(data, 0);
                    all.AddRange(data); all.AddRange(new float[clip.frequency / 2]);
                    count++;
                    Object.DestroyImmediate(clip);
                }
                Write(Path.Combine(Folder, "voice-" + names[v] + ".wav"), all.ToArray(), 22050);
            }
            string report = "VOICE SAMPLES / " + count + " phrases in " + watch.ElapsedMilliseconds + " ms / slowest phrase " + slowest.ToString("F1") + " ms";
            File.WriteAllText(Path.Combine(Folder, "timing.txt"), report);
            Debug.Log(report);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Write(string path, float[] samples, int rate)
        {
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                int bytes = samples.Length * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
                foreach (float s in samples) writer.Write((short)(Mathf.Clamp(s, -1, 1) * 32000));
            }
        }
    }
}
