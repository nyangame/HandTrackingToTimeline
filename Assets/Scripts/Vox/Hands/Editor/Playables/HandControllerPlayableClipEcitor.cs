using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Vox.Hands;

[CustomTimelineEditor(typeof(HandControllerPlayableClip))]
public class HandControllerPlayableClipEcitor : ClipEditor
{
    public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
    {
        var c = clip.asset as HandControllerPlayableClip;
        var t = track as HandControllerPlayableTrack;
        HandController trackBinding = t._trackBinding;
        var directors = GameObject.FindObjectsOfType<PlayableDirector>();
        var director = directors.Where(d => d.playableAsset?.GetInstanceID() == track.timelineAsset.GetInstanceID());
        if (!trackBinding)
        {
            if (director.Count() > 0)
            {
                trackBinding = director.First().GetGenericBinding(track) as HandController;
            }
        }

        if (trackBinding)
        {
            clip.displayName = trackBinding.Hand.ToString();
            //Debug.Log("HandControllerPlayableClipEcitor - OnCreate");
            if (trackBinding && c.template != null)
            {
                Debug.Log($"{trackBinding.gameObject.name}の{trackBinding.Hand.ToString()}のポーズを自動設定しました");
                trackBinding.SetCurrentFrameToTimelineTrack(ref c.template.handPose);
                c.template.isInitPose = false;
            }
        }
        base.OnCreate(clip, track, clonedFrom);
    }
}