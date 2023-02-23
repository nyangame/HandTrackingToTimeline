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
                //Debug.Log("�g���b�N����n�����f�[�^��null");
                return;
            }

            var trackBinding = playerData as TrackingTarget;
            if (!trackBinding)
            {
                if (bindObject)
                {
                    Debug.Log("HandController�̎Q�Ƃ��O���󂯎��̒l�ŎQ�Ƃ��Ă��܂�(�ʏ�ƈقȂ鋓��)");
                    trackBinding = bindObject;
                }
            }

            if (!trackBinding)
            {
                Debug.LogError($"Time: {playable.GetTime() * 60} - bind�Ώۂ�����܂���");
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

            //����Clip�͊�{Weight��0��1�̑z��
            var inputCount = playable.GetInputCount();
            double time = playable.GetTime();

            if (inputCount > 0)
            {
                doAction = playable.GetInputWeight(0) > 0.1f;
            }

            if (doAction)
            {
                trackBinding.Processing = true;

                //�N���b�v�̊J�n���Ԃ���E��
                double t = time - data.Clip.start;

                //�^�悩�ۑ������̂��Đ���
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
                Debug.Log("HandController��Update���~�o���܂���ł���");
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