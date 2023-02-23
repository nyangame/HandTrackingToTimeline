using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Vox.Hands
{
    [Serializable]
    public class HandTrackedPlayableClip : PlayableAsset, ITimelineClipAsset
    {
        [NonSerialized] public TimelineClip clipPassthrough = null;
        public HandTrackedPlayableBehaviour template = new HandTrackedPlayableBehaviour();

        public ClipCaps clipCaps => ClipCaps.All;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            template.Clip = clipPassthrough;
            var playable = ScriptPlayable<HandTrackedPlayableBehaviour>.Create(graph, template);

            /*
            //いまのポーズを取得して設定しておく
            //ここではできなかったよ…
            script.
            script.handPose
            //
            */

            return playable;
        }
    }
}