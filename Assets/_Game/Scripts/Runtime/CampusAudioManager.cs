using System;
using UnityEngine;

namespace CampusPulse
{
    /// <summary>
    /// Self-contained procedural audio engine for Campus Pulse 2030.
    /// Synthesizes high-fidelity ambient soundscapes, crisis alarms, electrical hums,
    /// and tactile UI feedback clips entirely in memory without external audio assets.
    /// </summary>
    public sealed class CampusAudioManager : MonoBehaviour
    {
        public static CampusAudioManager Instance { get; private set; }

        AudioSource musicSource;
        AudioSource ambientSource;
        AudioSource sfxSource;
        AudioSource alarmSource;

        AudioClip ambientClip;
        AudioClip alarmClip;
        AudioClip clickClip;
        AudioClip successClip;
        AudioClip electricClip;
        AudioClip waterClip;
        AudioClip introFanfareClip;

        public float MasterVolume { get; private set; } = 0.85f;
        public float SfxVolume { get; private set; } = 0.9f;
        public float MusicVolume { get; private set; } = 0.7f;
        public bool IsMuted { get; private set; } = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            musicSource = gameObject.AddComponent<AudioSource>();
            ambientSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
            alarmSource = gameObject.AddComponent<AudioSource>();

            musicSource.loop = true;
            ambientSource.loop = true;
            alarmSource.loop = true;

            GenerateClips();
            ApplyVolumes();

            if (ambientClip)
            {
                ambientSource.clip = ambientClip;
                ambientSource.Play();
            }
        }

        public void SetMute(bool mute)
        {
            IsMuted = mute;
            ApplyVolumes();
        }

        public void ToggleMute() => SetMute(!IsMuted);

        public void SetMasterVolume(float vol)
        {
            MasterVolume = Mathf.Clamp01(vol);
            ApplyVolumes();
        }

        void ApplyVolumes()
        {
            float m = IsMuted ? 0f : MasterVolume;
            if (musicSource) musicSource.volume = m * MusicVolume;
            if (ambientSource) ambientSource.volume = m * MusicVolume * 0.5f;
            if (sfxSource) sfxSource.volume = m * SfxVolume;
            if (alarmSource) alarmSource.volume = m * SfxVolume * 0.75f;
        }

        public void PlayClick()
        {
            if (sfxSource && clickClip && !IsMuted) sfxSource.PlayOneShot(clickClip, 0.6f);
        }

        public void PlaySuccess()
        {
            if (sfxSource && successClip && !IsMuted) sfxSource.PlayOneShot(successClip, 0.85f);
        }

        public void PlayElectricSpark()
        {
            if (sfxSource && electricClip && !IsMuted) sfxSource.PlayOneShot(electricClip, 0.75f);
        }

        public void PlayIntroTheme()
        {
            if (musicSource && introFanfareClip && !IsMuted)
            {
                musicSource.clip = introFanfareClip;
                musicSource.Play();
            }
        }

        public void SetAlarmActive(bool active)
        {
            if (!alarmSource || !alarmClip) return;
            if (active && !alarmSource.isPlaying)
            {
                alarmSource.clip = alarmClip;
                alarmSource.Play();
            }
            else if (!active && alarmSource.isPlaying)
            {
                alarmSource.Stop();
            }
        }

        void GenerateClips()
        {
            try
            {
                ambientClip = SynthesizeAmbient(44100, 4.0f);
                alarmClip = SynthesizeAlarm(44100, 1.2f);
                clickClip = SynthesizeClick(44100, 0.04f);
                successClip = SynthesizeChime(44100, 0.8f);
                electricClip = SynthesizeElectricHum(44100, 0.6f);
                introFanfareClip = SynthesizeIntroTheme(44100, 6.0f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Audio] Procedural synthesis warning: " + ex.Message);
            }
        }

        // 1. Ambient outdoor wind/drone
        static AudioClip SynthesizeAmbient(int sampleRate, float lengthSeconds)
        {
            int samples = (int)(sampleRate * lengthSeconds);
            float[] data = new float[samples];
            System.Random rand = new System.Random(2030);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Layered low-frequency sinusoidal wind + filtered noise
                float wind = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.15f
                           + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.08f
                           + Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.04f;
                float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.035f;
                // Gentle modulation
                float envelope = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 0.3f * t);
                data[i] = (wind + noise) * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create("AmbientCampus", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // 2. Dual-tone emergency klaxon (880 Hz / 660 Hz alternating)
        static AudioClip SynthesizeAlarm(int sampleRate, float lengthSeconds)
        {
            int samples = (int)(sampleRate * lengthSeconds);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (t % 0.6f < 0.3f) ? 880f : 660f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                // Pulse envelope
                float pulse = Mathf.PingPong(t * 3.33f, 1f);
                data[i] = wave * pulse * 0.35f;
            }

            AudioClip clip = AudioClip.Create("CrisisAlarm", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // 3. Tactile UI click
        static AudioClip SynthesizeClick(int sampleRate, float lengthSeconds)
        {
            int samples = (int)(sampleRate * lengthSeconds);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 120f);
                float wave = Mathf.Sin(2f * Mathf.PI * 1400f * t);
                data[i] = wave * decay * 0.5f;
            }

            AudioClip clip = AudioClip.Create("UiClick", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // 4. Ascending harmonic chime (C5 - E5 - G5 - C6)
        static AudioClip SynthesizeChime(int sampleRate, float lengthSeconds)
        {
            int samples = (int)(sampleRate * lengthSeconds);
            float[] data = new float[samples];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float total = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float noteStart = n * 0.12f;
                    if (t >= noteStart)
                    {
                        float noteT = t - noteStart;
                        float decay = Mathf.Exp(-noteT * 6f);
                        total += Mathf.Sin(2f * Mathf.PI * notes[n] * noteT) * decay * 0.25f;
                    }
                }
                data[i] = Mathf.Clamp(total, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SuccessChime", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // 5. Electric arc / transformer hum
        static AudioClip SynthesizeElectricHum(int sampleRate, float lengthSeconds)
        {
            int samples = (int)(sampleRate * lengthSeconds);
            float[] data = new float[samples];
            System.Random rand = new System.Random(42);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float hum = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.3f
                          + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.2f
                          + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.1f;
                float crackle = (rand.NextDouble() > 0.96) ? ((float)rand.NextDouble() * 2f - 1f) * 0.4f : 0f;
                float decay = Mathf.Exp(-t * 3f);
                data[i] = (hum + crackle) * decay;
            }

            AudioClip clip = AudioClip.Create("ElectricHum", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // 6. Cinematic Intro Theme (Deep warm chords)
        static AudioClip SynthesizeIntroTheme(int sampleRate, float lengthSeconds)
        {
            int samples = (int)(sampleRate * lengthSeconds);
            float[] data = new float[samples];
            float[] chordFreqs = { 130.81f, 196.00f, 261.63f, 329.63f, 392.00f }; // C3, G3, C4, E4, G4

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * (t / lengthSeconds)); // Bell curve
                float chord = 0f;
                foreach (float f in chordFreqs)
                {
                    chord += Mathf.Sin(2f * Mathf.PI * f * t) * 0.15f;
                }
                data[i] = chord * envelope;
            }

            AudioClip clip = AudioClip.Create("IntroTheme", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
