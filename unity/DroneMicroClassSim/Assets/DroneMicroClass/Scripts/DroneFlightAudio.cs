using UnityEngine;

namespace DroneMicroClass
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SimpleFlightController))]
    public sealed class DroneFlightAudio : MonoBehaviour
    {
        [Header("Recorded rotor clips")]
        [SerializeField] private AudioClip startupClip;
        [SerializeField] private AudioClip hoverLoop;
        [SerializeField] private AudioClip flightLoop;
        [SerializeField] private AudioClip shutdownClip;

        [Header("Spatial audio")]
        [SerializeField, Range(0f, 1f)] private float maximumVolume = 0.58f;
        [SerializeField, Min(0.1f)] private float minimumDistance = 2.5f;
        [SerializeField, Min(1f)] private float maximumDistance = 120f;
        [SerializeField, Range(0f, 1f)] private float dopplerLevel = 0.18f;
        [SerializeField, Min(0.05f)] private float volumeResponseTime = 0.28f;
        [SerializeField, Min(0.05f)] private float shutdownResponseTime = 1.8f;

        private const string StartupResourcePath = "Audio/drone-rotor-start";
        private const string HoverResourcePath = "Audio/drone-rotor-hover-loop";
        private const string FlightResourcePath = "Audio/drone-rotor-flight-loop";
        private const string ShutdownResourcePath = "Audio/drone-rotor-stop";
        private const float FullFlightBlendSpeed = 4f;
        private const float StartThreshold = 0.04f;
        private const float StopThreshold = 0.03f;
        private const int SampleRate = 44100;
        private const float LoopDurationSeconds = 2f;
        private static AudioClip generatedRotorLoop;

        private SimpleFlightController flight;
        private AudioSource hoverSource;
        private AudioSource flightSource;
        private AudioSource transientSource;
        private float hoverVolumeVelocity;
        private float flightVolumeVelocity;
        private bool rotorActive;

        private void Awake()
        {
            flight = GetComponent<SimpleFlightController>();
            LoadRecordedClips();
            EnsureAudioSources();
        }

        private void OnEnable()
        {
            LoadRecordedClips();
            EnsureAudioSources();
            rotorActive = false;
            SetLoopVolumesImmediately(0f, 0f);
        }

        private void OnDisable()
        {
            StopSource(hoverSource);
            StopSource(flightSource);
            StopSource(transientSource);
            rotorActive = false;
        }

        private void Update()
        {
            if (flight == null || hoverSource == null || flightSource == null)
            {
                return;
            }

            float spin = Mathf.Clamp01(flight.RotorSpin);
            if (!rotorActive && spin >= StartThreshold)
            {
                rotorActive = true;
                PlayTransition(startupClip, 0.98f);
                StartLoops();
            }
            else if (rotorActive && spin <= StopThreshold)
            {
                rotorActive = false;
                PlayTransition(shutdownClip, 1f);
            }

            float speedBlend = Mathf.InverseLerp(0.35f, FullFlightBlendSpeed, flight.HorizontalSpeed);
            speedBlend = Mathf.SmoothStep(0f, 1f, speedBlend);
            float transitionDuck = transientSource != null && transientSource.isPlaying ? 0.62f : 1f;
            float baseVolume = rotorActive ? maximumVolume * spin * transitionDuck : 0f;
            float hoverTarget = baseVolume * Mathf.Lerp(1f, 0.34f, speedBlend);
            float flightTarget = baseVolume * speedBlend;
            float response = rotorActive ? volumeResponseTime : shutdownResponseTime;

            hoverSource.volume = Mathf.SmoothDamp(
                hoverSource.volume,
                hoverTarget,
                ref hoverVolumeVelocity,
                Mathf.Max(0.05f, response));
            flightSource.volume = Mathf.SmoothDamp(
                flightSource.volume,
                flightTarget,
                ref flightVolumeVelocity,
                Mathf.Max(0.05f, response));
            hoverSource.pitch = Mathf.Lerp(0.92f, 1.04f, spin);
            flightSource.pitch = Mathf.Lerp(0.96f, 1.08f, speedBlend);
        }

        private void LoadRecordedClips()
        {
            startupClip ??= Resources.Load<AudioClip>(StartupResourcePath);
            hoverLoop ??= Resources.Load<AudioClip>(HoverResourcePath);
            flightLoop ??= Resources.Load<AudioClip>(FlightResourcePath);
            shutdownClip ??= Resources.Load<AudioClip>(ShutdownResourcePath);
            hoverLoop ??= GetGeneratedRotorLoop();
            flightLoop ??= GetGeneratedRotorLoop();
        }

        private void EnsureAudioSources()
        {
            Transform audioTransform = transform.Find("Drone Spatial Audio");
            if (audioTransform == null)
            {
                GameObject audioObject = new GameObject("Drone Spatial Audio");
                audioTransform = audioObject.transform;
                audioTransform.SetParent(transform, false);
            }

            AudioSource[] sources = audioTransform.GetComponents<AudioSource>();
            hoverSource = GetOrAddSource(audioTransform.gameObject, sources, 0);
            sources = audioTransform.GetComponents<AudioSource>();
            flightSource = GetOrAddSource(audioTransform.gameObject, sources, 1);
            sources = audioTransform.GetComponents<AudioSource>();
            transientSource = GetOrAddSource(audioTransform.gameObject, sources, 2);

            ConfigureSource(hoverSource, true);
            ConfigureSource(flightSource, true);
            ConfigureSource(transientSource, false);
            hoverSource.clip = hoverLoop;
            flightSource.clip = flightLoop;
        }

        private static AudioSource GetOrAddSource(GameObject owner, AudioSource[] existing, int index)
        {
            return index < existing.Length ? existing[index] : owner.AddComponent<AudioSource>();
        }

        private void ConfigureSource(AudioSource source, bool loop)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minimumDistance;
            source.maxDistance = Mathf.Max(minimumDistance + 1f, maximumDistance);
            source.dopplerLevel = dopplerLevel;
            source.spread = 35f;
        }

        private void StartLoops()
        {
            if (!hoverSource.isPlaying)
            {
                hoverSource.Play();
            }

            if (!flightSource.isPlaying)
            {
                flightSource.Play();
            }
        }

        private void PlayTransition(AudioClip clip, float pitch)
        {
            if (clip == null || transientSource == null)
            {
                return;
            }

            transientSource.Stop();
            transientSource.clip = clip;
            transientSource.pitch = pitch;
            transientSource.volume = maximumVolume;
            transientSource.Play();
        }

        private void SetLoopVolumesImmediately(float hover, float flightVolume)
        {
            if (hoverSource != null)
            {
                hoverSource.volume = hover;
            }

            if (flightSource != null)
            {
                flightSource.volume = flightVolume;
            }
        }

        private static void StopSource(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
            }
        }

        private static AudioClip GetGeneratedRotorLoop()
        {
            if (generatedRotorLoop != null)
            {
                return generatedRotorLoop;
            }

            int sampleCount = Mathf.RoundToInt(SampleRate * LoopDurationSeconds);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)SampleRate;
                float modulation = Mathf.Sin(Mathf.PI * 2f * 2f * time) * 0.08f;
                float sample =
                    Mathf.Sin(Mathf.PI * 2f * 55f * time + modulation) * 0.38f
                    + Mathf.Sin(Mathf.PI * 2f * 110f * time) * 0.24f
                    + Mathf.Sin(Mathf.PI * 2f * 220f * time + 0.35f) * 0.13f
                    + Mathf.Sin(Mathf.PI * 2f * 440f * time + modulation * 2f) * 0.05f;
                samples[i] = sample * 0.72f;
            }

            generatedRotorLoop = AudioClip.Create(
                "Procedural Drone Rotor",
                sampleCount,
                1,
                SampleRate,
                false);
            generatedRotorLoop.SetData(samples, 0);
            return generatedRotorLoop;
        }
    }
}
