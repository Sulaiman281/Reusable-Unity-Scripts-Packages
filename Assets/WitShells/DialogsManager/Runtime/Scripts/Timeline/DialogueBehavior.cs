using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using WitShells.DesignPatterns;

namespace WitShells.DialogsManager
{
    [Serializable]
    public class DialogueBehavior : PlayableBehaviour
    {
        public DialogObject Dialog;

        private AudioSource _audioSource;
        private ISubtitleTextUI _subtitleTextUI;
        private IDialogActionRequire _dialogActionRequire;

        public UnityEvent onClipStart;
        public UnityEvent onClipEnd;

        private int _attempts = 0;
        private double _clipStartTime = 0;

        public void Initialize(GameObject speaker)
        {
            if (speaker == null)
            {
                WitLogger.LogWarning("DialogueBehavior: Speaker GameObject is null.");
                return;
            }

            if (_subtitleTextUI == null)
            {
                _subtitleTextUI = speaker.GetComponent<ISubtitleTextUI>();
                if (_subtitleTextUI == null)
                {
                    WitLogger.LogWarning($"DialogueBehavior: Speaker '{speaker.name}' does not have an ISubtitleTextUI component.");
                }
            }

            if (_audioSource == null)
            {
                _audioSource = speaker.GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    WitLogger.LogWarning($"DialogueBehavior: Speaker '{speaker.name}' does not have an AudioSource component.");
                }
            }

            if (!Dialog.RequiresAction) return;

            if (_dialogActionRequire == null)
            {
                _dialogActionRequire = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(UnityEngine.FindObjectsSortMode.None)
                    .OfType<IDialogActionRequire>()
                    .FirstOrDefault(action => string.Equals(action.ActionId, Dialog.ActionId, StringComparison.OrdinalIgnoreCase));

                if (_dialogActionRequire == null)
                {
                    WitLogger.LogWarning($"DialogueBehavior: No IDialogActionRequire found for action ID '{Dialog.ActionId}'.");
                }
            }
        }

        private void Reset()
        {
            _audioSource = null;
            _subtitleTextUI = null;
            _dialogActionRequire = null;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _clipStartTime = playable.GetTime();
            onClipStart?.Invoke();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            Initialize(playerData as GameObject);

            if (Dialog == null) return;

            double time = playable.GetTime();
            double duration = playable.GetDuration();
            float normalizedTime = Mathf.Clamp01((float)(time / duration));

            // 1. SYNC TEXT (Typewriter effect synced to timeline)
            if (_subtitleTextUI != null)
            {
                int charactersToShow = Mathf.RoundToInt(normalizedTime * Dialog.Content.Length);
                string partialText = Dialog.Content.Substring(0, charactersToShow);
                _subtitleTextUI.SetSubtitleText(partialText);
            }

            // 2. SYNC AUDIO (Handles scrubbing and seeking)
            if (Dialog.Audio != null && _audioSource != null)
            {
                if (!_audioSource.isPlaying && info.weight > 0 && time < duration)
                {
                    _audioSource.clip = Dialog.Audio;
                    _audioSource.Play();
                }

                // If the audio gets out of sync (more than 0.1s), force it to match the timeline
                float targetTime = (float)time;
                if (Mathf.Abs(_audioSource.time - targetTime) > 0.1f && targetTime < Dialog.Audio.length)
                {
                    _audioSource.time = targetTime;
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            if (_subtitleTextUI != null)
            {
                _subtitleTextUI.SetSubtitleText(string.Empty);
            }

            if (!Dialog.RequiresAction) return;

            if (_dialogActionRequire != null)
            {
                WitLogger.Log($"DialogueBehavior: Checking action completion for dialog '{Dialog.DialogId}'.");
                if (_dialogActionRequire.IsActionComplete())
                {
                    WitLogger.Log($"DialogueBehavior: Action '{_dialogActionRequire.ActionId}' completed for dialog '{Dialog.DialogId}'.");
                }
                else
                {
                    _attempts++;
                    if (_attempts < DialogsSettings.Instance.MaxRetryAttempts)
                    {
                        var director = DialogManager.Instance.playableDirector;
                        if (director != null)
                        {
                            director.Pause();
                            DialogManager.Instance.HandleActionAndRewind(DialogsSettings.Instance.DialogActionRepeatDelay, _dialogActionRequire, Dialog.AudioLength);
                            WitLogger.Log($"DialogueBehavior: Action '{_dialogActionRequire.ActionId}' not completed. Repeating dialog '{Dialog.DialogId}'.");
                        }
                    }
                    return;
                }
            }
            else
            {
                WitLogger.LogWarning($"DialogueBehavior: Didn't Found IDialogActionRequire for dialog '{Dialog.DialogId}'.");
            }

            onClipEnd?.Invoke();
            _attempts = 0;
            Reset();
        }
    }
}