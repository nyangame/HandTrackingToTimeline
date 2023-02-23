using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Vox.Hands
{
    /*
     * Asset stores key control config.
     */
    [ExecuteInEditMode]
    [RequireComponent(typeof(Animator))]
    public class HandController : MonoBehaviour
    {
        [SerializeField] private HandPosePresetsAsset m_preset;
        [SerializeField] private HandType m_handType = HandType.LeftHand;
        [SerializeField] public bool m_timelineEditMode = true;
        [SerializeField] public HandPoseData m_handPoseData; //戻り値だと変更できなかった

        public bool TimelineEditMode => m_timelineEditMode;
        public bool OtherControllerIsRunning { get; set; } //あんまりきれいじゃないけど外部からもらうにはこうするしかない…
        public bool Processing { get; set; } //あんまりきれいじゃないけど外部からもらうにはこうするしかない…

        private HandUpdator m_update; 
        private HandRuntimeControl m_runtimeControl;
        private Avatar m_avatar;

        private HumanPose m_humanPose;
        private HumanPose m_humanPoseInit;

        [SerializeField] private HandPoseData m_previewData;
        private HandPoseData m_basePose;

        public HandType Hand
        {
            get { return m_handType; }
            set
            {
                m_handType = value;
                InitializeRuntimeControl();
            }
        }

        public HandPosePresetsAsset Preset
        {
            get => m_preset;
            set => m_preset = value;
        }

        private void Awake()
        {
            OtherControllerIsRunning = false;

            //リファレンスポーズは最初に取得
            var animator = GetComponent<Animator>();
            var poseHandler = new HumanPoseHandler(animator.avatar, gameObject.transform);
            poseHandler.GetHumanPose(ref m_humanPoseInit);

            InitializeRuntimeControl();

#if UNITY_EDITOR
            m_update = GetComponent<HandUpdator>();
            if(m_update)
            {
                m_update.Setup(this);
            }
#endif
        }

        /// <summary>
        /// Set current hand pose as base pose for transformation. 
        /// </summary>
        public void SetCurrentAsBasePose()
        {
            m_basePose = m_handPoseData;
        }        
        
        /// <summary>
        /// Set base hand pose from currently assigned preset 
        /// </summary>
        public void SetBasePoseFromCurrentPreset(string poseName)
        {
            m_basePose = m_preset[poseName];
        }        

        /// <summary>
        /// Set base hand pose from currently assigned preset 
        /// </summary>
        public void SetBasePoseFromCurrentPreset(int index)
        {
            m_basePose = m_preset[index];
        }        
        
        /// <summary>
        /// Set base hand pose.
        /// </summary>
        public void SetBasePose(ref HandPoseData pose)
        {
            m_basePose = pose;
        }

        /// <summary>
        /// Get base hand pose.
        /// </summary>
        public void GetBasePose(ref HandPoseData pose)
        {
            pose.thumb.muscle1 = m_basePose.thumb.muscle1;
            pose.thumb.spread = m_basePose.thumb.spread;
            pose.thumb.muscle2 = m_basePose.thumb.muscle2;
            pose.thumb.muscle3 = m_basePose.thumb.muscle3;
            pose.index.muscle1 = m_basePose.index.muscle1;
            pose.index.spread = m_basePose.index.spread;
            pose.index.muscle2 = m_basePose.index.muscle2;
            pose.index.muscle3 = m_basePose.index.muscle3;
            pose.middle.muscle1 = m_basePose.middle.muscle1;
            pose.middle.spread = m_basePose.middle.spread;
            pose.middle.muscle2 = m_basePose.middle.muscle2;
            pose.middle.muscle3 = m_basePose.middle.muscle3;
            pose.ring.muscle1 = m_basePose.ring.muscle1;
            pose.ring.spread = m_basePose.ring.spread;
            pose.ring.muscle2 = m_basePose.ring.muscle2;
            pose.ring.muscle3 = m_basePose.ring.muscle3;
            pose.little.muscle1 = m_basePose.little.muscle1;
            pose.little.spread = m_basePose.little.spread;
            pose.little.muscle2 = m_basePose.little.muscle2;
            pose.little.muscle3 = m_basePose.little.muscle3;
        }

        /// <summary>
        /// Set hand pose.  
        /// </summary>
        /// <param name="poseName">name of pose in RuntimeHandPosePresetAsset.</param>
        /// <param name="t">lerp value of hand pose. 0.0f=base pose, 1.0f=poseName pose</param>
        public void SetHandPose(string poseName, float t = 1.0f)
        {
            var pose = m_preset[poseName];
            SetHandPose(ref pose, t);
        }

        /// <summary>
        /// Set hand pose.  
        /// </summary>
        /// <param name="poseIndex">index of pose in RuntimeHandPosePresetAsset.</param>
        /// <param name="t">lerp value of hand pose. 0.0f=base pose, 1.0f=poseName pose</param>
        public void SetHandPose(int poseIndex, float t = 1.0f)
        {
            var pose = m_preset[poseIndex];
            SetHandPose(ref pose, t);
        }
        
        /// <summary>
        /// Set hand pose.  
        /// </summary>
        /// <param name="data">hand pose</param>
        /// <param name="t">lerp value of hand pose. 0.0f=base pose, 1.0f=data</param>
        public void SetHandPose(ref HandPoseData data, float t = 1.0f)
        {
            if (m_runtimeControl == null || m_runtimeControl.HandType != m_handType)
            {
                InitializeRuntimeControl();
            }
            LerpHandPose(ref m_handPoseData, ref m_basePose, ref data, t);
        }

        private static void LerpHandPose(ref HandPoseData data, ref HandPoseData src, ref HandPoseData dst, float t)
        {
            switch (t)
            {
                case 0f:
                    data = src;
                    break;
                case 1.0f:
                    data = dst;
                    break;
                default:
                {
                    for (var i = 0; i < HandPoseData.HumanFingerCount; ++i)
                    {
                        data[i] = new FingerPoseData
                        {
                            muscle1 = Mathf.Lerp(src[i].muscle1, dst[i].muscle1, t),
                            muscle2 = Mathf.Lerp(src[i].muscle2, dst[i].muscle2, t),
                            muscle3 = Mathf.Lerp(src[i].muscle3, dst[i].muscle3, t),
                            spread = Mathf.Lerp(src[i].spread, dst[i].spread, t),
                            rollSide = Mathf.Lerp(src[i].rollSide, dst[i].rollSide, t),
                            rollTwist = Mathf.Lerp(src[i].rollTwist, dst[i].rollTwist, t)
                        };
                    }
                    break;
                }
            }
        }

        public void InitializeRuntimeControl()
        {
            var animator = GetComponent<Animator>();
            m_avatar = animator.avatar;

            if (m_avatar == null)
            {
                Debug.LogError("HandController:Animator component doesn't have a valid avatar configured.");
            }
            
            m_runtimeControl = new HandRuntimeControl(gameObject, animator, m_handType);
            m_runtimeControl.SetInitialPose(m_humanPoseInit);
        }
        
        private void LateUpdate()
        {
            if (OtherControllerIsRunning)
            {
                if(m_update?.Target == this)
                {
                    m_update.InnerUpdate();
                }
                return;
            }

            if (!Processing) return;
            UpdatePose();
            UpdateRotate();
            //Debug.Log($"Update {this.name} - {Hand}");
        }

        public void Revert()
        {
            m_runtimeControl?.Revert();
            UpdateData();
        }

        public void UpdatePose()
        {
            m_runtimeControl?.UpdateHandPose(ref m_handPoseData);
        }

        public void UpdateRotate()
        {
            m_runtimeControl?.UpdateRotate(ref m_handPoseData);
        }

        public void UpdateData()
        {
            m_runtimeControl?.UpdateData(ref m_handPoseData);
        }

        public void UpdatePreview()
        {
            m_runtimeControl?.UpdateData(ref m_previewData);
        }

        public void SetCurrentFrameToTimelineTrack(ref HandPoseData data)
        {
            m_runtimeControl?.UpdateData(ref data);
        }
    }
}