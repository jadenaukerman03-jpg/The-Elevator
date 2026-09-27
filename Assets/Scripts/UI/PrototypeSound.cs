using System.Collections.Generic;
using UnityEngine;

namespace TheElevator
{
    public sealed class PrototypeSound : MonoBehaviour
    {
        AudioSource source;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        public void Initialize()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0;
            source.playOnAwake = false;
        }
        public void Play(int frequency, float seconds, float volume)
        {
            if (!source) return;
            string key = frequency + ":" + seconds;
            if (!clips.TryGetValue(key, out AudioClip clip))
            {
                const int rate = 22050;
                int count = Mathf.CeilToInt(rate * seconds);
                float[] samples = new float[count];
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / rate;
                    float envelope = Mathf.Min(1, t / 0.008f) * Mathf.Pow(1 - (float)i / count, 2);
                    samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2) * envelope;
                }
                clip = AudioClip.Create(key, count, 1, rate, false);
                clip.SetData(samples, 0);
                clips.Add(key, clip);
            }
            source.PlayOneShot(clip, volume);
        }
        void OnDestroy() { foreach (AudioClip clip in clips.Values) Destroy(clip); }
    }
}
