using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace WitShells.LiveMic
{
    public class LiveMicroPhone : MonoBehaviour
    {
        [Header("Audio Source")]
        [SerializeField] private AudioSource audioSource;
        public AudioSource AudioSource => audioSource;

        [Header("Microphone Settings")]
        [SerializeField] private string selectedDevice;
        [SerializeField] private int micIndex;

        [SerializeField] private int sampleRate = 48000;

        private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        private void OnEnable()
        {
            selectedDevice = Microphone.devices[micIndex];
            AudioSource.loop = true;
            AudioSource.mute = false;
            AudioSource.clip = Microphone.Start(selectedDevice, true, 1, sampleRate);
            AudioSource.Play();
        }

        private void OnDisable()
        {
            AudioSource.Stop();
            Microphone.End(selectedDevice);
        }


        private void FixedUpdate()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            _mainThreadActions.Enqueue(() => AudioInputMainThread(data, channels));
        }

        private void AudioInputMainThread(float[] data, int channels)
        {
            if (AudioSource == null || !AudioSource.isPlaying) return;
            AudioSource.clip.GetData(data, AudioSource.timeSamples);
        }

        public void GetMicrophoneDevices(out string[] devices)
        {
            devices = Microphone.devices;
        }

        public void SetMicrophoneDevice(int index)
        {
            if (index < 0 || index >= Microphone.devices.Length) return;
            micIndex = index;
            selectedDevice = Microphone.devices[micIndex];

            if (isActiveAndEnabled)
            {
                AudioSource.Stop();
                Microphone.End(selectedDevice);
                AudioSource.clip = Microphone.Start(selectedDevice, true, 1, sampleRate);
                AudioSource.Play();
            }
        }

        public void GetSpeakerDevices(out string[] devices)
        {
            devices = AudioSettings.GetConfiguration().speakerMode.ToString().Split(',');
        }

        public void SetSampleRate(int rate)
        {
            sampleRate = rate;

            if (isActiveAndEnabled)
            {
                AudioSource.Stop();
                Microphone.End(selectedDevice);
                AudioSource.clip = Microphone.Start(selectedDevice, true, 1, sampleRate);
                AudioSource.Play();
            }
        }

#if UNITY_EDITOR

        [ContextMenu("Next Microphone Device")]
        public void NextMicrophoneDevice()
        {
            int nextIndex = (micIndex + 1) % Microphone.devices.Length;
            SetMicrophoneDevice(nextIndex);
            Debug.Log($"Switched to microphone device: {Microphone.devices[nextIndex]}");
        }

        [ContextMenu("Previous Microphone Device")]
        public void PreviousMicrophoneDevice()
        {
            int prevIndex = (micIndex - 1 + Microphone.devices.Length) % Microphone.devices.Length;
            SetMicrophoneDevice(prevIndex);
            Debug.Log($"Switched to microphone device: {Microphone.devices[prevIndex]}");
        }

#endif
    }
}