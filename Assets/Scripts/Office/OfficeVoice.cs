using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.Office
{
    // The office language: made-up syllables, synthesized. Nobody can understand it; the speech bubble shows
    // what is meant. Tone follows anger: calm (levels 1-2), upset (3-4) and yelling (5).
    public static class OfficeVoice
    {
        public enum Tone { Calm, Upset, Yelling }
        public static Tone ToneFor(int anger) { return anger >= 5 ? Tone.Yelling : anger >= 3 ? Tone.Upset : Tone.Calm; }

        const int Rate = 22050;
        static readonly float[] Lengths = { .8f, 1.3f, 1.9f, 2.6f };
        const int Variants = 3;
        static readonly Dictionary<int, AudioClip> clips = new Dictionary<int, AudioClip>();
        // Formants (F1, F2) of the five vowels the language uses: a, e, i, o, u.
        static readonly Vector2[] Vowels = { new Vector2(800, 1200), new Vector2(500, 1900), new Vector2(320, 2300), new Vector2(500, 900), new Vector2(350, 780) };

        // Roughly how long a line takes to say.
        public static float Duration(string text, Tone tone)
        {
            float perCharacter = tone == Tone.Calm ? .055f : tone == Tone.Upset ? .048f : .045f;
            return Mathf.Clamp(.6f + text.Length * perCharacter, 1.2f, 5f);
        }

        // A babble clip about as long as the line; clips are made once and shared by everyone (voices differ by pitch).
        public static AudioClip Clip(Tone tone, float seconds, int variant)
        {
            int length = 0;
            for (int i = 1; i < Lengths.Length; i++) if (Mathf.Abs(Lengths[i] - seconds) < Mathf.Abs(Lengths[length] - seconds)) length = i;
            variant = Mathf.Abs(variant) % Variants;
            int key = ((int)tone * Lengths.Length + length) * Variants + variant;
            if (clips.TryGetValue(key, out AudioClip clip) && clip) return clip;
            clip = Synthesize(tone, Lengths[length], key * 7919 + 17);
            clips[key] = clip;
            return clip;
        }

        static AudioClip Synthesize(Tone tone, float seconds, int seed)
        {
            System.Random random = new System.Random(seed);
            float[] samples = new float[Mathf.CeilToInt(seconds * Rate)];
            float basePitch = tone == Tone.Calm ? 165 : tone == Tone.Upset ? 205 : 265;
            float swing = tone == Tone.Calm ? .08f : tone == Tone.Upset ? .18f : .3f;
            float loud = tone == Tone.Calm ? .45f : tone == Tone.Upset ? .65f : .9f;
            float phase = 0;
            int cursor = 0;
            while (cursor < samples.Length)
            {
                // One syllable: a short consonant burst, then a voiced vowel with a pitch contour.
                float syllable = tone == Tone.Calm ? Range(random, .15f, .23f) : tone == Tone.Upset ? Range(random, .12f, .18f) : Range(random, .1f, .16f);
                int count = Mathf.Min(samples.Length - cursor, Mathf.RoundToInt(syllable * Rate));
                Vector2 vowel = Vowels[random.Next(Vowels.Length)];
                float startPitch = basePitch * (1 + Range(random, -swing, swing)), endPitch = startPitch * (1 + Range(random, -swing, swing * (tone == Tone.Yelling ? 1.4f : 1)));
                int burst = Mathf.RoundToInt(Range(random, .012f, .03f) * Rate);
                float noise = 0;
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)count;
                    float f0 = Mathf.Lerp(startPitch, endPitch, t);
                    phase += 2 * Mathf.PI * f0 / Rate;
                    if (phase > 2 * Mathf.PI * 64) phase -= 2 * Mathf.PI * 64;
                    float voiced = 0;
                    for (int k = 1; k <= 10; k++)
                    {
                        float h = k * f0;
                        float weight = Mathf.Exp(-Sq((h - vowel.x) / 170)) + .75f * Mathf.Exp(-Sq((h - vowel.y) / 230)) + .12f / k;
                        voiced += Mathf.Sin(phase * k) * weight;
                    }
                    float envelope = Mathf.Clamp01(i / (.018f * Rate)) * Mathf.Clamp01((count - i) / (.045f * Rate));
                    float sample = voiced * .22f * envelope;
                    if (i < burst) { noise = noise * .6f + Range(random, -1f, 1f) * .4f; sample = sample * (i / (float)burst) + noise * .35f * (1 - i / (float)burst); }
                    if (tone == Tone.Yelling) sample = (float)System.Math.Tanh(sample * 2.4f) * .8f;
                    samples[cursor + i] = sample * loud;
                }
                cursor += count;
                // Short gaps between syllables, longer ones between words.
                cursor += Mathf.RoundToInt((random.NextDouble() < .28 ? Range(random, .07f, .14f) : Range(random, .01f, .03f)) * Rate);
            }
            AudioClip clip = AudioClip.Create("Office language / " + tone, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float Sq(float x) { return x * x; }
        static float Range(System.Random random, float min, float max) { return min + (float)random.NextDouble() * (max - min); }

        // Loudness of a clip, for checks.
        public static float Loudness(AudioClip clip)
        {
            float[] data = new float[clip.samples];
            clip.GetData(data, 0);
            double sum = 0;
            foreach (float s in data) sum += s * s;
            return Mathf.Sqrt((float)(sum / Mathf.Max(1, data.Length)));
        }
    }
}
