using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace WitShells.DialogsManager
{
    [Serializable]
    public class DialogueClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Required Dialog")]
        [SerializeField] private DialogObject dialog;

        [Header("Events")]
        [SerializeField] private UnityEvent onClipStart;
        [SerializeField] private UnityEvent onClipEnd;

        public DialogObject Dialog => dialog;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<DialogueBehavior>.Create(graph);

            DialogueBehavior dialogueBehavior = playable.GetBehaviour();
            dialogueBehavior.Dialog = dialog;

            DialogManager.Instance.playableDirector = owner.GetComponent<PlayableDirector>();

            return playable;
        }

        public override double duration
        {
            get => dialog ? dialog.AudioLength : 0;
        }
    }
}