using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Vox.Hands
{
    public class HandTrackedPlayableMixerBehaviour : PlayableBehaviour
    {
        public TrackingTarget bindObject = null;

        HandTrackedPlayableBehaviour data;

        // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playerData == null)
            {
                //Debug.Log("トラックから渡されるデータがnull");
                return;
            }

            var trackBinding = playerData as TrackingTarget;
            if (!trackBinding)
            {
                if (bindObject)
                {
                    Debug.Log("HandControllerの参照を外部受け取りの値で参照しています(通常と異なる挙動)");
                    trackBinding = bindObject;
                }
            }

            if (!trackBinding)
            {
                Debug.LogError($"Time: {playable.GetTime() * 60} - bind対象がありません");
                return;
            }

            if (data == null)
            {
                var selectedPlayable = (ScriptPlayable<HandTrackedPlayableBehaviour>)playable.GetInput(0);
                data = selectedPlayable.GetBehaviour();
            }

            bool doAction = false;

            /*
            for (var i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var inputPlayable =
                    (ScriptPlayable<HandTrackedPlayableBehaviour>) playable.GetInput(i);
                var input = inputPlayable.GetBehaviour();

                if (inputWeight > 0.0f)
                {
                    doAction = true;
                    handPose.WeightedAddPose(inputWeight, ref input.handPose);
                }
            }
            */

            //このClipは基本Weightは0か1の想定
            var inputCount = playable.GetInputCount();
            double time = playable.GetTime();

            if (inputCount > 0)
            {
                doAction = playable.GetInputWeight(0) > 0.1f;
            }

            if (doAction)
            {
                trackBinding.Processing = true;

                //クリップの開始時間から拾う
                double t = time - data.Clip.start;

                //録画か保存したのを再生か
                if(data.isRecording)
                {
                    BoneTracking.Instance.Record(trackBinding, t);
                }
                else
                {
                    trackBinding.SetHandPose(t);
                }
            }
            else
            {
                trackBinding.Processing = false;
                trackBinding.Revert();
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            //Debug.Log("Clip - OnPlayableDestroy");
            base.OnPlayableDestroy(playable);

            if (bindObject)
            {
                bindObject.Processing = false;
                bindObject.Revert();
            }
            else
            {
                Debug.Log("HandControllerのUpdateを停止出来ませんでした");
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            //Debug.Log("Clip - OnBehaviourPlay");

            if (data == null)
            {
                var selectedPlayable = (ScriptPlayable<HandTrackedPlayableBehaviour>)playable.GetInput(0);
                data = selectedPlayable.GetBehaviour();
            }
            if (bindObject)
            {
                bindObject.InitializeRuntimeControl(data.isRecording, data.trackingFile);
                bindObject.Processing = true;
            }
            base.OnBehaviourPlay(playable, info);
        }
    }
}