using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Vox.Hands
{
    [TrackColor(1f, 0.4f, 0f)]
    [TrackClipType(typeof(HandControllerPlayableClip))]
    [TrackBindingType(typeof(HandController))]
    public class HandControllerPlayableTrack : TrackAsset
    {
        public HandController _trackBinding;

        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            //Debug.Log("CreatePlayable");
            return base.CreatePlayable(graph, gameObject, clip);
        }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            //Debug.Log("CreateTrackMixer");
            var playable = ScriptPlayable<HandControllerPlayableMixerBehaviour>.Create(graph, inputCount);
            var trackBinding = go.GetComponent<PlayableDirector>().GetGenericBinding(this) as HandController;
            playable.GetBehaviour().bindObject = trackBinding;
            _trackBinding = trackBinding;
            //Debug.Log("trackBinding");

            return playable;
        }
    }
}