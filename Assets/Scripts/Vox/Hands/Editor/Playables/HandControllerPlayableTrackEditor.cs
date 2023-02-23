using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using Vox.Hands;

[CustomTimelineEditor(typeof(HandControllerPlayableTrack))]
public class HandControllerPlayableTrackEcitor : TrackEditor
{
    public override void OnTrackChanged(TrackAsset track)
    {
        base.OnTrackChanged(track);


    }

    public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
    {
        //Debug.Log("add track");
        /*
        var c = clip.asset as HandControllerPlayableClip;
        clip.displayName = "aaaaa";
        */
        base.OnCreate(track, copiedFrom);
    }
}