using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Vox.Hands
{
    [Serializable]
    public class HandTrackedPlayableBehaviour : PlayableBehaviour
    {
        //public HandPosePresetsAsset presets;
        //public HandPoseData handPose;
        //private HandType HandType = HandType.LeftHand;

        public bool isRecording = false;
        public string trackingFile = "";

        public bool isInitPose = true;

        [NonSerialized] public TimelineClip Clip;
    }
}