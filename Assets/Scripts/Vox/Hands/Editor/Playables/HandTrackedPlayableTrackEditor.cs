using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using Vox.Hands;

[CustomTimelineEditor(typeof(HandTrackedPlayableTrack))]
public class HandTrackedPlayableTrackEditor : TrackEditor
{
    public override void OnTrackChanged(TrackAsset track)
    {
        base.OnTrackChanged(track);


    }

    public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
    {
        base.OnCreate(track, copiedFrom);
    }
}