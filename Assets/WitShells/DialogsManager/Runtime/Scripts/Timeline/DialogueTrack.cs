
using UnityEngine;
using UnityEngine.Timeline;

namespace WitShells.DialogsManager
{
    [TrackColor(0.1f, 0.6f, 0.1f)]
    [TrackClipType(typeof(DialogueClip))]
    [TrackBindingType(typeof(GameObject))]
    public class DialogueTrack : TrackAsset
    {
        
    }
}