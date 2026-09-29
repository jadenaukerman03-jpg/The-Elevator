using UnityEngine;
namespace TheElevator.Office
{
    // The office language: a soft, murmured gibberish, synthesized per phrase in each speaker's own voice. Nobody
    // can understand it; the speech bubble shows what is meant.
    //
    // It is built to be easy to listen to: a rounded, hummed tone with the highs rolled off, a pitch melody that
    // flows across the whole phrase (questions lift at the end), syllables that swell and fade instead of
    // starting abruptly, and mostly soft consonants (m, n, l, w). Anger makes it brighter, quicker and louder:
    // calm (levels 1-2), upset (3-4) and yelling (5 and up).
    public static class OfficeVoice
    {
        public enum Tone { Calm, Upset, Yelling }
        public static Tone ToneFor(int anger) { return anger >= 5 ? Tone.Yelling : anger >= 3 ? Tone.Upset : Tone.Calm; }

        const int Rate = 22050, Harmonics = 10;
        // Vowel colour (F1, F2): a, e, i, o, u, and the lazy "uh".
        static readonly Vector2[] Vowels = { new Vector2(700, 1200), new Vector2(500, 1700), new Vector2(360, 2000), new Vector2(480, 900), new Vector2(380, 800), new Vector2(560, 1250) };

        // How long a phrase takes to say.
        public static float Duration(string text, Tone tone)
        {
            float perCharacter = tone == Tone.Calm ? .058f : tone == Tone.Upset ? .05f : .046f;
            return Mathf.Clamp(.45f + text.Length * perCharacter, .7f, 4.5f);
        }

        // voice is the speaker's natural pitch in Hz (about 105 for a deep voice, 230 for a high one);
        // the same speaker saying the same phrase always sounds the same.
        public static AudioClip Speak(string text, Tone tone, float voice, int speaker) { return ToClip(Samples(text, tone, voice, speaker), tone); }

        public static AudioClip ToClip(float[] samples, Tone tone)
        {
            AudioClip clip = AudioClip.Create("Office language / " + tone, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // The raw samples of a phrase. Pure computation (no Unity objects), so it can run on a worker thread.
        public static float[] Samples(string text, Tone tone, float voice, int speaker)
        {
            text = text ?? "";
            float seconds = Duration(text, tone);
            System.Random random = new System.Random(speaker * 7919 + text.GetHashCode());
            float[] samples = new float[Mathf.CeilToInt((seconds + .2f) * Rate)];
            bool question = text.TrimEnd().EndsWith("?"), exclaim = text.TrimEnd().EndsWith("!");

            // Tone settings. Softness: how quickly harmonics fade (higher = rounder). Cutoff: the low-pass.
            float raise = tone == Tone.Calm ? 1 : tone == Tone.Upset ? 1.12f : 1.32f;
            float melody = tone == Tone.Calm ? .09f : tone == Tone.Upset ? .16f : .24f;
            float pace = tone == Tone.Calm ? 1.05f : tone == Tone.Upset ? .9f : .78f;
            float loud = tone == Tone.Calm ? .32f : tone == Tone.Upset ? .5f : .78f;
            float softness = tone == Tone.Calm ? 2.3f : tone == Tone.Upset ? 1.8f : 1.35f;
            float cutoff = tone == Tone.Calm ? 1500 : tone == Tone.Upset ? 2300 : 3400;
            float edge = tone == Tone.Yelling ? .25f : 0;
            if (exclaim) { loud *= 1.1f; melody *= 1.2f; }
            float lowpass = 1 - Mathf.Exp(-2 * Mathf.PI * cutoff / Rate);
            float glide = 1 - Mathf.Exp(-1f / (.05f * Rate));
            // A flowing melody: two slow waves with random phase, a gentle fall, and a lift at the end of questions.
            float wave1 = (float)random.NextDouble() * 6.28f, wave2 = (float)random.NextDouble() * 6.28f;

            float[] gains = new float[Harmonics + 1];
            float phase = 0, filtered = 0, filtered2 = 0;
            Vector2 formant = Vowels[5];
            int cursor = Mathf.RoundToInt(.04f * Rate), end = Mathf.RoundToInt(seconds * Rate);
            bool stressed = true;
            while (cursor < end)
            {
                Vector2 target = Vowels[random.Next(Vowels.Length)];
                float length = (stressed ? Range(random, .16f, .24f) : Range(random, .11f, .17f)) * pace;
                int count = Mathf.Min(end - cursor, Mathf.RoundToInt(length * Rate));
                // Soft onsets most of the time: a closed-mouth hum (m/n) or a glide (l/w). Angrier speech adds soft stops.
                int onset = random.Next(tone == Tone.Calm ? 3 : 4); // 0 none, 1 hum, 2 glide, 3 soft stop
                float syllableLoud = (stressed ? 1 : .72f) * Range(random, .88f, 1.03f);
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)count, progress = (cursor + i) / (float)end, now = (cursor + i) / (float)Rate;
                    formant = Vector2.Lerp(formant, target, glide);
                    float contour = 1 + melody * (.6f * Mathf.Sin(now * 2.1f + wave1) + .4f * Mathf.Sin(now * 4.7f + wave2))
                        + melody * (.5f - progress) + (stressed ? melody * .3f * Mathf.Sin(t * Mathf.PI) : 0)
                        + (question && progress > .65f ? melody * 2.4f * (progress - .65f) / .35f : 0);
                    float f0 = voice * raise * contour * (1 + Mathf.Sin(now * 2 * Mathf.PI * 4.6f) * .005f);
                    phase += 2 * Mathf.PI * f0 / Rate;
                    if (phase > Mathf.PI * 2000) phase -= Mathf.PI * 2000;
                    if ((i & 31) == 0)
                    {
                        // Closed mouth (hum) mutes the upper harmonics; open vowels bring them in gently.
                        bool closed = onset == 1 && t < .25f;
                        Vector2 f = closed ? new Vector2(250, 1000) : onset == 2 && t < .2f ? Vector2.Lerp(new Vector2(330, 750), formant, t / .2f) : formant;
                        for (int k = 1; k <= Harmonics; k++)
                        {
                            float h = k * f0;
                            gains[k] = h > cutoff * 1.6f ? 0 : (.4f + Peak(h, f.x, 180) + .5f * Peak(h, f.y, 280)) / Mathf.Pow(k, softness + (closed ? 1.2f : 0));
                        }
                    }
                    float s1 = Mathf.Sin(phase), c1 = Mathf.Cos(phase), sk = s1, ck = c1, voiced = 0;
                    for (int k = 1; k <= Harmonics; k++)
                    {
                        voiced += sk * gains[k];
                        float next = sk * c1 + ck * s1; ck = ck * c1 - sk * s1; sk = next;
                    }
                    // Each syllable swells and fades; a soft stop gives a tiny breathy push at the start.
                    // (Max: sin(pi) is a hair below zero in floats, and a negative to the power .7 is NaN.)
                    float envelope = Mathf.Pow(Mathf.Max(0, Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.08f))), .7f);
                    float sample = voiced * envelope;
                    if (onset == 3 && t < .06f) sample += Range(random, -1, 1) * .05f * (1 - t / .06f);
                    if (edge > 0) sample = (float)System.Math.Tanh(sample * (1 + edge * 2.5f)) / (1 + edge);
                    // Two gentle low-pass stages: no fizz, no buzz.
                    filtered += (sample - filtered) * lowpass;
                    filtered2 += (filtered - filtered2) * lowpass;
                    samples[cursor + i] += filtered2 * loud * syllableLoud * .9f;
                }
                cursor += count;
                // Words: a small breath every two or three syllables; the first syllable of each word is stressed.
                bool wordEnd = random.NextDouble() < .36;
                stressed = wordEnd;
                cursor += Mathf.RoundToInt((wordEnd ? Range(random, .06f, .12f) * pace : Range(random, 0f, .015f)) * Rate);
            }
            // Fade the very end so the phrase never clicks off.
            int tail = Mathf.Min(samples.Length, Mathf.RoundToInt(.03f * Rate));
            for (int i = 0; i < tail; i++) samples[samples.Length - 1 - i] *= i / (float)tail;
            return samples;
        }

        static float Peak(float frequency, float centre, float width) { float x = (frequency - centre) / width; return 1 / (1 + x * x); }
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

        // How bright a sound is: zero crossings per second (a harsh, buzzy voice crosses zero far more often).
        public static float Brightness(float[] data)
        {
            int crossings = 0;
            for (int i = 1; i < data.Length; i++) if ((data[i - 1] < 0) != (data[i] < 0)) crossings++;
            return crossings / (data.Length / (float)Rate);
        }
    }
}
