using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Vox.Hands
{
    public enum HandType
    {
        LeftHand,
        RightHand
    }

    [Serializable]
    public struct FingerPoseData
    {
        [Range(-2f, 2f), Tooltip("指の開きぐあい")]
        public float spread;
        [Range(-1f, 1f), Tooltip("指の根元の回転補正")]
        public float rollSide;
        [Range(-1f, 1f), Tooltip("指の捻りぐあい")]
        public float rollTwist;
        [Range(-2f, 2f), Tooltip("第一関節の曲げ具合")]
        public float muscle1;
        [Range(-2f, 2f), Tooltip("第二関節の曲げ具合")]
        public float muscle2;
        [Range(-2f, 2f), Tooltip("第三関節の曲げ具合")]
        public float muscle3;
        
        public void WeightedAddPose(float weight, ref FingerPoseData data)
        {
            spread += weight * data.spread;
            rollSide += weight * data.rollSide;
            rollTwist += weight * data.rollTwist;
            muscle1 += weight * data.muscle1;
            muscle2 += weight * data.muscle2;
            muscle3 += weight * data.muscle3;
        }
    }

    [Serializable]
    public struct HandPoseData
    {
        public FingerPoseData thumb;
        public FingerPoseData index;
        public FingerPoseData middle;
        public FingerPoseData ring;
        public FingerPoseData little;

        public const int HumanFingerCount = 5;

        public FingerPoseData this[int idx]
        {
            get
            {
                if (idx < 0 || idx >= HumanFingerCount)
                {
                    throw new IndexOutOfRangeException();
                }

                switch (idx)
                {
                    case 0:
                        return thumb;
                    case 1:
                        return index;
                    case 2:
                        return middle;
                    case 3:
                        return ring;
                    default:
                        return little;
                }
            }

            set
            {
                if (idx < 0 || idx >= HumanFingerCount)
                {
                    throw new IndexOutOfRangeException();
                }
                switch (idx)
                {
                    case 0:
                        thumb = value;
                        break;
                    case 1:
                        index = value;
                        break;
                    case 2:
                        middle = value;
                        break;
                    case 3:
                        ring = value;
                        break;
                    default:
                        little = value;
                        break;
                }
            }
        }

        public void WeightedAddPose(float weight, ref HandPoseData data)
        {
            thumb.WeightedAddPose(weight, ref data.thumb);
            index.WeightedAddPose(weight, ref data.index);
            middle.WeightedAddPose(weight, ref data.middle);
            ring.WeightedAddPose(weight, ref data.ring);
            little.WeightedAddPose(weight, ref data.little);
        }
    }

    public class HandRuntimeControl
    {
        private readonly int[] m_handBoneIndexMap;
        private readonly HumanPoseHandler m_poseHandler;
        private readonly GameObject m_rootObject;
        private Animator m_animator;
        private HumanPose m_humanPose;
        private HumanPose m_humanPoseInit;

        public HandType HandType { private set; get; }
        
        protected int[] fingerMuscleIndex = { 55, 59, 63, 67, 71 };    // 指のMuscle配列の先頭
        protected HumanBodyBones[] finglerBoneL =
        {
            HumanBodyBones.LeftThumbProximal,
            HumanBodyBones.LeftIndexProximal,
            HumanBodyBones.LeftMiddleProximal,
            HumanBodyBones.LeftRingProximal,
            HumanBodyBones.LeftLittleProximal,

            HumanBodyBones.LeftHand,
        };
        protected HumanBodyBones[] finglerBoneR =
        {
            HumanBodyBones.RightThumbProximal,
            HumanBodyBones.RightIndexProximal,
            HumanBodyBones.RightMiddleProximal,
            HumanBodyBones.RightRingProximal,
            HumanBodyBones.RightLittleProximal,

            HumanBodyBones.RightHand,
        };

        public HandRuntimeControl(GameObject rootObject, Animator animator, HandType handType)
        {
            HandType = handType;

            m_animator = animator;
            m_rootObject = rootObject;
            m_poseHandler = new HumanPoseHandler(animator.avatar, rootObject.transform);
            m_handBoneIndexMap = new int[20];

            var indexMuscleFingerBegin = handType == HandType.LeftHand ? 55 : 75;

            for (var i = 0; i < m_handBoneIndexMap.Length; ++i)
            {
                m_handBoneIndexMap[i] = indexMuscleFingerBegin + i;
            }

            //Debug.Log("Build Controller");
        }

        public void SetInitialPose(HumanPose initPose)
        {
            m_humanPoseInit = initPose;
        }

        public void Revert()
        {
            m_poseHandler?.SetHumanPose(ref m_humanPoseInit);
        }

        public void UpdateHandPose(ref HandPoseData handPose)
        {
            m_poseHandler.GetHumanPose(ref m_humanPose);

            var hipBone = m_animator.GetBoneTransform(HumanBodyBones.Hips);
            var position = hipBone.position;//m_animator.transform.position;
            var rotation = hipBone.rotation;//m_animator.transform.rotation;
            //var scale = m_animator.transform.localScale;

            m_animator.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_animator.transform.localScale = Vector3.one;

            m_humanPose.bodyPosition = m_animator.bodyPosition;
            m_humanPose.bodyRotation = m_animator.bodyRotation;

            //hipBone.rotation = rotation;
            //hipBone.position = position + hipBone.up * hipBone.position.y; // * scale.y;
            //hipBone.localScale = scale;

            var i = 0;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.thumb.muscle1;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.thumb.spread;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.thumb.muscle2;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.thumb.muscle3;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.index.muscle1;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.index.spread;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.index.muscle2;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.index.muscle3;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.middle.muscle1;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.middle.spread;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.middle.muscle2;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.middle.muscle3;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.ring.muscle1;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.ring.spread;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.ring.muscle2;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.ring.muscle3;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.little.muscle1;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.little.spread;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.little.muscle2;
            m_humanPose.muscles[m_handBoneIndexMap[i++]] = handPose.little.muscle3;
            
            m_poseHandler.SetHumanPose(ref m_humanPose);

            var hipBone_new = m_animator.GetBoneTransform(HumanBodyBones.Hips);
            hipBone_new.position = position;
            hipBone_new.rotation = rotation;
        }

        public void UpdateRotate(ref HandPoseData handPose)
        {
            HumanBodyBones[] finglerBone = HandType == HandType.LeftHand ? finglerBoneL : finglerBoneR;
            /*
            var boneT = m_animator.GetBoneTransform(finglerBoneL[4]);
            Debug.Log($"{m_animator.gameObject.name} . BL - {boneT.rotation.ToString()}");
            boneT = m_animator.GetBoneTransform(finglerBoneR[4]);
            Debug.Log($"{m_animator.gameObject.name} . BR - {boneT.rotation.ToString()}");
            */

            SetFingerRoll(finglerBone[0], finglerBone[0] + 1, handPose.thumb.rollSide, handPose.thumb.rollTwist);
            SetFingerRoll(finglerBone[1], finglerBone[1] + 1, handPose.index.rollSide, handPose.index.rollTwist);
            SetFingerRoll(finglerBone[2], finglerBone[2] + 1, handPose.middle.rollSide, handPose.middle.rollTwist);
            SetFingerRoll(finglerBone[3], finglerBone[3] + 1, handPose.ring.rollSide, handPose.ring.rollTwist);
            SetFingerRoll(finglerBone[4], finglerBone[4] + 1, handPose.little.rollSide, handPose.little.rollTwist);

            /*
            boneT = m_animator.GetBoneTransform(finglerBoneL[4]);
            Debug.Log($"{m_animator.gameObject.name} . L - {boneT.rotation.ToString()}");
            boneT = m_animator.GetBoneTransform(finglerBoneR[4]);
            Debug.Log($"{m_animator.gameObject.name} . R - {boneT.rotation.ToString()}");
            */
        }

        // 指ロール処理
        protected void SetFingerRoll(HumanBodyBones bone, HumanBodyBones target, float rollSd, float rollTw)
        {
            bool isLeft = HandType == HandType.LeftHand;
            var boneT = m_animator.GetBoneTransform(bone);
            var targetT = m_animator.GetBoneTransform(target);
            float sign = isLeft ? -1f : 1f;

            if (boneT != null && targetT != null)
            {
                //Debug.Log($"{m_animator.gameObject.name} . {HandType.ToString()} - {bone.ToString()} - RollSide:{rollSd} / RollTwist:{rollTw}");
                //Debug.Log($"x:{boneT.rotation.x} y:{boneT.rotation.y} z:{boneT.rotation.z}");
                //Debug.Log((boneT.position - targetT.position));

                var rot = Quaternion.AngleAxis(rollTw * 90f * sign, (boneT.position - targetT.position));
                /*
                rot *= Quaternion.AngleAxis(rollSd * 90f * sign, Quaternion.Inverse(rot) * boneT.up);
                boneT.Rotate(rot.eulerAngles, Space.World);
                */
                
                boneT.Rotate((boneT.position - targetT.position), rollTw * 90f * sign, Space.World);
                boneT.Rotate(Quaternion.Inverse(rot) * boneT.up, rollSd * 90f * sign, Space.World);
            }
        }

        public void UpdateData(ref HandPoseData handPose)
        {
            m_poseHandler.GetHumanPose(ref m_humanPose);

            var i = 0;
            handPose.thumb.muscle1 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.thumb.spread = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.thumb.muscle2 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.thumb.muscle3 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.index.muscle1 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.index.spread = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.index.muscle2 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.index.muscle3 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.middle.muscle1 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.middle.spread = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.middle.muscle2 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.middle.muscle3 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.ring.muscle1 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.ring.spread = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.ring.muscle2 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.ring.muscle3 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.little.muscle1 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.little.spread = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.little.muscle2 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
            handPose.little.muscle3 = m_humanPose.muscles[m_handBoneIndexMap[i++]];
        }
    }
}
