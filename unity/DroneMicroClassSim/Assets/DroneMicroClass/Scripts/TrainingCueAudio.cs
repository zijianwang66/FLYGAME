using UnityEngine;

namespace DroneMicroClass
{
    [DisallowMultipleComponent]
    public sealed class TrainingCueAudio : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float cueVolume = 0.32f;

        private const int SampleRate = 44100;
        private AudioSource source;
        private AudioClip startClip;
        private AudioClip checkpointClip;
        private AudioClip deviationClip;
        private AudioClip completeClip;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = cueVolume;

            startClip = CreateToneSequence("Training Start", new[] { 520f, 660f }, 0.12f);
            checkpointClip = CreateToneSequence("Checkpoint Passed", new[] { 880f }, 0.14f);
            deviationClip = CreateToneSequence("Course Deviation", new[] { 320f, 230f }, 0.13f);
            completeClip = CreateToneSequence("Training Complete", new[] { 620f, 780f, 980f }, 0.14f);
        }

        public void PlayStart()
        {
            Play(startClip, 0.72f);
        }

        public void PlayCheckpoint()
        {
            Play(checkpointClip, 0.58f);
        }

        public void PlayDeviation()
        {
            Play(deviationClip, 0.62f);
        }

        public void PlayComplete()
        {
            if (source != null)
            {
                source.Stop();
            }

            Play(completeClip, 0.78f);
        }

        private void Play(AudioClip clip, float scale)
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip, Mathf.Clamp01(scale));
            }
        }

        private static AudioClip CreateToneSequence(string clipName, float[] frequencies, float noteDuration)
        {
            int noteSamples = Mathf.Max(1, Mathf.RoundToInt(noteDuration * SampleRate));
            int gapSamples = Mathf.RoundToInt(0.025f * SampleRate);
            int totalSamples = frequencies.Length * noteSamples + Mathf.Max(0, frequencies.Length - 1) * gapSamples;
            float[] samples = new float[totalSamples];
            int cursor = 0;
            for (int note = 0; note < frequencies.Length; note++)
            {
                float frequency = frequencies[note];
                for (int i = 0; i < noteSamples; i++)
                {
                    float normalized = i / (float)Mathf.Max(1, noteSamples - 1);
                    float envelope = Mathf.Sin(Mathf.PI * normalized);
                    envelope *= envelope;
                    float time = i / (float)SampleRate;
                    samples[cursor + i] = envelope * (
                        Mathf.Sin(Mathf.PI * 2f * frequency * time) * 0.42f
                        + Mathf.Sin(Mathf.PI * 4f * frequency * time) * 0.08f);
                }

                cursor += noteSamples + gapSamples;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
