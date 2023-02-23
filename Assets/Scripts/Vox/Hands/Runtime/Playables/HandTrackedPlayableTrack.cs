using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Vox.Hands
{
    [TrackColor(1f, 0.4f, 0f)]
    [TrackClipType(typeof(HandTrackedPlayableClip))]
    [TrackBindingType(typeof(TrackingTarget))]
    public class HandTrackedPlayableTrack : TrackAsset
    {
        public TrackingTarget _trackBinding;

        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            //Debug.Log("CreatePlayable");
            return base.CreatePlayable(graph, gameObject, clip);
        }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            //Debug.Log("CreateTrackMixer");
            var playable = ScriptPlayable<HandTrackedPlayableMixerBehaviour>.Create(graph, inputCount);
            var trackBinding = go.GetComponent<PlayableDirector>().GetGenericBinding(this) as TrackingTarget;
            playable.GetBehaviour().bindObject = trackBinding;
            _trackBinding = trackBinding;

            var clips = GetClips();
            foreach (var clip in clips)
            {
                // CreateTrackMixer内で、各クリップの自身のTimelineClipをPlayableAssetに渡す
                var loopClip = clip.asset as HandTrackedPlayableClip;
                loopClip.clipPassthrough = clip;
            }

            //Debug.Log("trackBinding");

            return playable;
        }
    }
}