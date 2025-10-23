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
    }
}