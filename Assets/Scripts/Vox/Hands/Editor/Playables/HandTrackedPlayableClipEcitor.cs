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

[CustomTimelineEditor(typeof(HandTrackedPlayableClip))]
public class HandTrackedPlayableClipEcitor : ClipEditor
{
    public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
    {
        var c = clip.asset as HandTrackedPlayableClip;
        var t = track as HandTrackedPlayableTrack;
        TrackingTarget trackBinding = t._trackBinding;
        var directors = GameObject.FindObjectsOfType<PlayableDirector>();
        var director = directors.Where(d => d.playableAsset?.GetInstanceID() == track.timelineAsset.GetInstanceID());
        if (!trackBinding)
        {
            if (director.Count() > 0)
            {
                trackBinding = director.First().GetGenericBinding(track) as TrackingTarget;
            }
        }

        if (trackBinding)
        {
            clip.displayName = trackBinding.name;
            //Debug.Log("HandTrackedPlayableClipEcitor - OnCreate");
            if (trackBinding && c.template != null)
            {
                Debug.Log($"{trackBinding.gameObject.name}のポーズを自動設定しました");

                //trackBinding.SetCurrentFrameToTimelineTrack(ref c.template.handPose);
                
                c.template.isInitPose = false;
            }
        }

        base.OnCreate(clip, track, clonedFrom);
    }
}