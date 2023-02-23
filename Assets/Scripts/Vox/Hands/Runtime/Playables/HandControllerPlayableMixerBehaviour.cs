using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Vox.Hands
{
    public class HandControllerPlayableMixerBehaviour : PlayableBehaviour
    {
        public HandController bindObject = null;

        // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playerData == null)
            {
                //Debug.Log("トラックから渡されるデータがnull");
                return;
            }

            var trackBinding = playerData as HandController;
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
                Debug.LogError($"Time: {playable.GetTime() * 60} - HandControllerがありません");
                return;
            }

            var inputCount = playable.GetInputCount();

            var handPose = new HandPoseData();

            trackBinding.Processing = true;

            bool doAction = false;
            for (var i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var inputPlayable =
                    (ScriptPlayable<HandControllerPlayableBehaviour>) playable.GetInput(i);
                var input = inputPlayable.GetBehaviour();

                if (inputWeight > 0.0f)
                {
                    doAction = true;
                    handPose.WeightedAddPose(inputWeight, ref input.handPose);
                }
            }

            if (doAction)
            {
                trackBinding.SetHandPose(ref handPose);
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
            if (bindObject)
            {
                bindObject.InitializeRuntimeControl();
                bindObject.Processing = true;
            }
            base.OnBehaviourPlay(playable, info);
        }
    }
}