using UnityEngine;
namespace TheElevator.Office
{
    // The office language: made-up syllables, synthesized per phrase in each speaker's own voice. Nobody can
    // understand it; the speech bubble shows what is meant. The melody follows the words (questions rise,
    // exclamations punch) and the tone follows anger: calm (levels 1-2), upset (3-4) and yelling (5 and up).
    public static class OfficeVoice
    {
        public enum Tone { Calm, Upset, Yelling }
        public static Tone ToneFor(int anger) { return anger >= 5 ? Tone.Yelling : anger >= 3 ? Tone.Upset : Tone.Calm; }

        const int Rate = 22050, Harmonics = 14;
        // Formants glide from one vowel into the next over about 35 ms.
        static readonly float Glide = 1 - Mathf.Exp(-1f / (.035f * Rate));
        // Vowel formants (F1, F2, F3): a, e, i, o, u, and the lazy "uh".
        static readonly Vector3[] Vowels = {
            new Vector3(750, 1250, 2600), new Vector3(520, 1850, 2550), new Vector3(330, 2250, 3000),
            new Vector3(520, 950, 2500), new Vector3(360, 820, 2400), new Vector3(600, 1300, 2500) };
        enum Consonant { None, Stop, Hiss, Hum, Glide }

        // How long a phrase takes to say.
        public static float Duration(string text, Tone tone)
        {
            float perCharacter = tone == Tone.Calm ? .06f : tone == Tone.Upset ? .052f : .048f;
            return Mathf.Clamp(.45f + text.Length * perCharacter, .7f, 4.5f);
        }

        // A spoken phrase. voice is the speaker's natural pitch in Hz (about 95 for a deep voice, 240 for a high one);
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
            float[] gains = new float[Harmonics + 1];
            float seconds = Duration(text, tone);
            int seed = speaker * 7919 + (text ?? "").GetHashCode();
            System.Random random = new System.Random(seed);
            float[] samples = new float[Mathf.CeilToInt((seconds + .15f) * Rate)];
            bool question = text != null && text.TrimEnd().EndsWith("?"), exclaim = text != null && text.TrimEnd().EndsWith("!");
            // Anger raises the pitch, widens the melody, speeds up the syllables and pushes the voice.
            float raise = tone == Tone.Calm ? 1 : tone == Tone.Upset ? 1.18f : 1.45f;
            float melody = tone == Tone.Calm ? .12f : tone == Tone.Upset ? .22f : .34f;
            float pace = tone == Tone.Calm ? 1 : tone == Tone.Upset ? .85f : .72f;
            float loud = tone == Tone.Calm ? .5f : tone == Tone.Upset ? .68f : .92f;
            float strain = tone == Tone.Yelling ? .35f : tone == Tone.Upset ? .12f : 0;
            if (exclaim) { loud *= 1.08f; melody *= 1.15f; }
            float phase = 0, jitter = 0, breath = 0, low = 0;
            Vector3 formant = Vowels[5];
            int cursor = Mathf.RoundToInt(.03f * Rate), end = Mathf.RoundToInt(seconds * Rate);
            bool stressed = true;
            while (cursor < end)
            {
                // Syllable shape: optional consonant onset, a vowel, and sometimes a nasal or hiss coda.
                Consonant onset = (Consonant)random.Next(5);
                Vector3 target = Vowels[random.Next(Vowels.Length)];
                float length = (stressed ? Range(random, .17f, .26f) : Range(random, .1f, .17f)) * pace;
                int count = Mathf.Min(end - cursor, Mathf.RoundToInt(length * Rate));
                // Phrase melody: a gentle fall across the phrase, a lift on stressed syllables, a rise on questions.
                float progress = cursor / (float)end;
                float contour = 1 + melody * .5f - melody * progress + (stressed ? melody * .45f : 0) + (question && progress > .7f ? melody * 2.2f * (progress - .7f) / .3f : 0);
                float pitch = voice * raise * contour;
                float onsetLength = onset == Consonant.Stop ? .025f : onset == Consonant.Hiss ? .075f : onset == Consonant.Hum ? .05f : onset == Consonant.Glide ? .04f : 0;
                int onsetSamples = Mathf.Min(count, Mathf.RoundToInt(onsetLength * Rate));
                float syllableLoud = loud * (stressed ? 1 : .75f) * Range(random, .85f, 1.05f);
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)count, now = (cursor + i) / (float)Rate;
                    // Coarticulation: formants glide from the last vowel into this one.
                    formant = Vector3.Lerp(formant, target, Glide);
                    jitter = jitter * .999f + Range(random, -1, 1) * .0006f;
                    float f0 = pitch * (1 + jitter + Mathf.Sin(now * 2 * Mathf.PI * 5.3f) * (.008f + strain * .01f)) * (1 - t * .04f);
                    phase += 2 * Mathf.PI * f0 / Rate;
                    if (phase > Mathf.PI * 2000) phase -= Mathf.PI * 2000;
                    // Glottal source: harmonics with a natural roll-off, shaped by three vowel resonances.
                    // (Harmonic weights are refreshed every 32 samples; they change slowly.)
                    if ((i & 31) == 0)
                    {
                        Vector3 f = onset == Consonant.Hum && i < onsetSamples ? new Vector3(260, 1100, 2400) : formant;
                        for (int k = 1; k <= Harmonics; k++)
                        {
                            float h = k * f0;
                            gains[k] = h > 4200 ? 0 : (Peak(h, f.x, 90) + .7f * Peak(h, f.y, 120) + .35f * Peak(h, f.z, 180)) / Mathf.Pow(k, 1.1f - strain * .5f);
                        }
                    }
                    // sin(k x) for every harmonic by rotating (sin x, cos x): two trig calls per sample instead of fourteen.
                    float s1 = Mathf.Sin(phase), c1 = Mathf.Cos(phase), sk = s1, ck = c1, voiced = 0;
                    for (int k = 1; k <= Harmonics; k++)
                    {
                        voiced += sk * gains[k];
                        float next = sk * c1 + ck * s1; ck = ck * c1 - sk * s1; sk = next;
                    }
                    // Breathiness and a little strain noise keep it from sounding like a synth.
                    breath = breath * .7f + Range(random, -1, 1) * .3f;
                    float envelope = Mathf.SmoothStep(0, 1, Mathf.Clamp01(i / (.02f * Rate))) * Mathf.SmoothStep(0, 1, Mathf.Clamp01((count - i) / (.05f * Rate)));
                    float sample = (voiced * .2f + breath * (.035f + strain * .05f)) * envelope;
                    if (i < onsetSamples)
                    {
                        float c = i / (float)onsetSamples;
                        if (onset == Consonant.Stop) { sample *= c; sample += Range(random, -1, 1) * .25f * (1 - c); }
                        else if (onset == Consonant.Hiss) { float n = Range(random, -1, 1); sample = sample * c * c + (n - low) * .16f * (1 - c * .8f); low = n; }
                        else if (onset == Consonant.Hum) sample *= .55f;
                        else if (onset == Consonant.Glide) formant = Vector3.Lerp(new Vector3(320, 700, 2300), target, c);
                    }
                    if (strain > 0) sample = (float)System.Math.Tanh(sample * (1 + strain * 3)) / (1 + strain);
                    samples[cursor + i] += sample * syllableLoud;
                }
                cursor += count;
                // Words: a short break every two or three syllables; the first syllable of each word is stressed.
                bool wordEnd = random.NextDouble() < .38;
                stressed = wordEnd;
                cursor += Mathf.RoundToInt((wordEnd ? Range(random, .05f, .11f) * pace : Range(random, .005f, .02f)) * Rate);
            }
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
    }
}
