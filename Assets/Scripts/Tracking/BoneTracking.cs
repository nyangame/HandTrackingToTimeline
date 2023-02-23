using HandPoseTransfer;
using HandPoseTransfer.OculusQuest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Vox.Hands;

public class BoneTracking : MonoBehaviour
{
    static BoneTracking _instance;
    static public BoneTracking Instance => _instance;

    float _time = 0.0f;
    List<TrackingOVRFrameInfo> _leftHandInfo = new List<TrackingOVRFrameInfo>();
    List<TrackingOVRFrameInfo> _rightHandInfo = new List<TrackingOVRFrameInfo>();
    List<TrackingFrameInfo> _humanoidFrameInfo = new List<TrackingFrameInfo>();

    public event Action<int> OnPlay;

    public bool IsAvailable { get; private set; }
    public bool IsRecording { get; private set; }

    [SerializeField] OVRCameraRig _camera;
    [SerializeField] OVRHand _LeftOVRHand;
    [SerializeField] OVRHand _RightOVRHand;
    [SerializeField] TextMeshPro _CountObject;

    [SerializeField] bool _VisualizeBones;
    [SerializeField] GameObject _XYZAxisPrefab;
    [SerializeField] float _AxisObjectScale = 0.2f;
    [SerializeField] string _targetScene;

    OVRSkeleton _LeftHandOVRSkelton;
    OVRSkeleton _RightHandOVRSkelton;
    IDictionary<HumanBodyBones, Transform> _Skeleton;

    Quaternion BodyRotation;
    public Vector3 BodyPositionCorrection = new Vector3(0.0f, -0.1f, 0.0f);

    HumanPoseHandler _SrcPoseHandler;
    HumanPose _SourcePose;

    TrackingTarget _targetObject;

    HandTrackedPlayableClip _recordingTarget;
    TimelineClip _targetClip;

    // Start is called before the first frame update
    void Start()
    {
        _instance = this;

        _LeftHandOVRSkelton = _LeftOVRHand.GetComponent<OVRSkeleton>();
        _RightHandOVRSkelton = _RightOVRHand.GetComponent<OVRSkeleton>();

        _leftHandInfo.Capacity = 50000;
        _rightHandInfo.Capacity = 50000;

        IsAvailable = false;
        _CountObject.text = "setup";
    }

    private void OnDestroy()
    {
        if (IsRecording)
        {
            Save();
        }
    }

    IEnumerator CountDown()
    {
        yield return new WaitForSeconds(1);
        _CountObject.text = 3.ToString();

        yield return new WaitForSeconds(1);
        _CountObject.text = 2.ToString();

        yield return new WaitForSeconds(1);
        _CountObject.text = 1.ToString();

        yield return new WaitForSeconds(1);
        _CountObject.text = "";

        RecordStart();
        OnPlay?.Invoke(0);
        yield break;
    }

    void RecordFrame()
    {
        TrackingOVRFrameInfo right = new TrackingOVRFrameInfo();
        TrackingOVRFrameInfo left = new TrackingOVRFrameInfo();
        TrackingFrameInfo info = new TrackingFrameInfo();

        right.Time = left.Time = _time;
        right.Info = createInfo(_LeftHandOVRSkelton.Bones);
        left.Info = createInfo(_RightHandOVRSkelton.Bones);

        if (_LeftOVRHand.IsTracked)
        {
            UpdateHandSkeletonBones(true);
        }
        if (_RightOVRHand.IsTracked)
        {
            UpdateHandSkeletonBones(false);
        }

        _SrcPoseHandler.GetHumanPose(ref _SourcePose);
        //_TargetPoseHandler.GetHumanPose(ref _TargetPose);

        // Update avatar hand pose
        for (int i = 55; i < 95; i++)
        {
            //_TargetPose.muscles[i] = _SourcePose.muscles[i];
            info.Info.Add(new HumanoidInfo() { Id = i, Muscle = _SourcePose.muscles[i] });
        }

        _rightHandInfo.Add(right);
        _leftHandInfo.Add(left);
        _humanoidFrameInfo.Add(info);

        if (_targetObject)
        {
            _targetObject.SetHandPose(ref _SourcePose);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_LeftHandOVRSkelton.IsInitialized) return;
        if (!_RightHandOVRSkelton.IsInitialized) return;

        if(!IsAvailable)
        {
            Setup();

            StartCoroutine("CountDown");
            _CountObject.text = "ready.";
        }

        if (IsRecording)
        {
            RecordFrame();
            _time += Time.deltaTime;
        }
    }


    void Initialize()
    {
        var skeletonBuilder = new SkeletonBuilder(this.transform);
        skeletonBuilder.AddBasicSkeleton(new SkeletonBuilderParams());

        var leftHandRotation = Quaternion.Euler(0, 180, 180);
        skeletonBuilder.UpdateRotation(HumanBodyBones.LeftHand, leftHandRotation);

        var rightHandRotation = Quaternion.Euler(0, 0, 0);
        skeletonBuilder.UpdateRotation(HumanBodyBones.RightHand, rightHandRotation);

        AddHandSkeletonBones(skeletonBuilder, true); // Left

        AddHandSkeletonBones(skeletonBuilder, false); // Right

        _Skeleton = skeletonBuilder.Skeleton;

        var avatar = AvatarBuilder.BuildHumanAvatar(this.gameObject, skeletonBuilder.GetHumanDescription());
        _SrcPoseHandler = new HumanPoseHandler(avatar, this.transform);

        if (_VisualizeBones && _XYZAxisPrefab != null)
        {
            foreach (var bone in skeletonBuilder.Skeleton.Values)
            {
                GameObject axis = GameObject.Instantiate(_XYZAxisPrefab, Vector3.zero, Quaternion.identity, bone);
                axis.transform.localScale = new Vector3(_AxisObjectScale, _AxisObjectScale, _AxisObjectScale);
                axis.transform.localPosition = Vector3.zero;
                axis.transform.localRotation = Quaternion.identity;
            }
        }
    }

    void AddHandSkeletonBones(SkeletonBuilder skeletonBuilder, bool isLeftHand)
    {
        OVRSkeleton skeleton = isLeftHand ? _LeftHandOVRSkelton : _RightHandOVRSkelton;

        Dictionary<HumanBodyBones, OVRSkeleton.BoneId> handBoneIdMap;
        handBoneIdMap = isLeftHand ? _LeftHandBoneIdMap : _RightHandBoneIdMap;

        foreach (HumanBodyBones boneKey in handBoneIdMap.Keys)
        {
            OVRSkeleton.BoneId ovrBoneId = handBoneIdMap[boneKey];
            if ((int)ovrBoneId < skeleton.Bones.Count)
            {
                Transform skeletonBone = skeleton.BindPoses[(int)ovrBoneId].Transform;
                Vector3 localPosition = skeletonBone.localPosition;
                Quaternion localRotation = skeletonBone.localRotation;

                // Local pose composition
                if ((ovrBoneId == OVRSkeleton.BoneId.Hand_Thumb2) || (ovrBoneId == OVRSkeleton.BoneId.Hand_Pinky1))
                {
                    short parentBoneIndex = skeleton.Bones[(int)ovrBoneId].ParentBoneIndex;
                    Transform parentBone = skeleton.Bones[parentBoneIndex].Transform;
                    localPosition = parentBone.localPosition + parentBone.localRotation * localPosition;
                    localRotation = parentBone.localRotation * localRotation;
                }

                skeletonBuilder.Add(boneKey, _HandBoneParent[boneKey], localPosition, localRotation);
            }
        }
    }

    void UpdateHandSkeletonBones(bool isLeftHand)
    {
        OVRSkeleton skeleton = isLeftHand ? _LeftHandOVRSkelton : _RightHandOVRSkelton;

        Dictionary<HumanBodyBones, OVRSkeleton.BoneId> handBoneIdMap;
        handBoneIdMap = isLeftHand ? _LeftHandBoneIdMap : _RightHandBoneIdMap;

        foreach (HumanBodyBones boneKey in handBoneIdMap.Keys)
        {
            OVRSkeleton.BoneId ovrBoneId = handBoneIdMap[boneKey];
            if ((int)ovrBoneId < skeleton.Bones.Count)
            {
                Transform skeletonBone = skeleton.Bones[(int)ovrBoneId].Transform;
                Quaternion localRotation = skeletonBone.localRotation;

                // Local pose composition
                if ((ovrBoneId == OVRSkeleton.BoneId.Hand_Thumb2) || (ovrBoneId == OVRSkeleton.BoneId.Hand_Pinky1))
                {
                    short parentBoneIndex = skeleton.Bones[(int)ovrBoneId].ParentBoneIndex;
                    Transform parentBone = skeleton.Bones[parentBoneIndex].Transform;
                    localRotation = parentBone.localRotation * localRotation;
                }

                _Skeleton[boneKey].localRotation = localRotation;
            }
        }
    }

    public void Setup()
    {
        if (!_LeftHandOVRSkelton.IsInitialized) return;
        if (!_RightHandOVRSkelton.IsInitialized) return;

        Initialize();
        
        IsAvailable = true;
        Debug.Log("Setup Complete");
    }

    List<BoneInfo> createInfo(ICollection<OVRBone> bones)
    {
        List<BoneInfo> ret = new List<BoneInfo>();
        foreach (var b in bones)
        {
            BoneInfo info = new BoneInfo();
            info.Id = b.Id;
            info.ParentBoneIndex = b.ParentBoneIndex;
            info.Position = b.Transform.localPosition;
            info.Rotation = b.Transform.localRotation;
            ret.Add(info);
        }
        return ret;
    }

    public void Save()
    {
        Debug.Log("SAVE TRACKING DATA!!");
        TrackingSave save = new TrackingSave();
        //save.Left = _leftHandInfo;
        //save.Right = _rightHandInfo;
        save.HumanoidInfo = _humanoidFrameInfo;

        var str = JsonUtility.ToJson(save);

        var path = Directory.GetCurrentDirectory() + "/TrackingSave";
        var file = string.Format("{0}/trackingdata_{1}.json", path, DateTime.Now.ToString().Replace("/","_").Replace(":", "_"));
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        using (var sr = new StreamWriter(file))
        {
            sr.Write(str);
        }
        Debug.Log("DATA PATH:" + file);
    }

    public void Record(TrackingTarget target, double t)
    {
        _time = (float)t;
        _targetObject = target;
        RecordFrame();
    }

    public void RecordStart()
    {
        _time = 0.0f;
        IsRecording = true;
    }
    public void RecordStop()
    {
        IsRecording = false;
        Save();
    }










    static readonly Dictionary<HumanBodyBones, OVRSkeleton.BoneId> _LeftHandBoneIdMap = new Dictionary<HumanBodyBones, OVRSkeleton.BoneId>()
    {
        {HumanBodyBones.LeftThumbProximal,      OVRSkeleton.BoneId.Hand_Thumb0},
        {HumanBodyBones.LeftThumbIntermediate,  OVRSkeleton.BoneId.Hand_Thumb2},
        {HumanBodyBones.LeftThumbDistal,        OVRSkeleton.BoneId.Hand_Thumb3},
        {HumanBodyBones.LeftIndexProximal,      OVRSkeleton.BoneId.Hand_Index1},
        {HumanBodyBones.LeftIndexIntermediate,  OVRSkeleton.BoneId.Hand_Index2},
        {HumanBodyBones.LeftIndexDistal,        OVRSkeleton.BoneId.Hand_Index3},
        {HumanBodyBones.LeftMiddleProximal,     OVRSkeleton.BoneId.Hand_Middle1},
        {HumanBodyBones.LeftMiddleIntermediate, OVRSkeleton.BoneId.Hand_Middle2},
        {HumanBodyBones.LeftMiddleDistal,       OVRSkeleton.BoneId.Hand_Middle3},
        {HumanBodyBones.LeftRingProximal,       OVRSkeleton.BoneId.Hand_Ring1},
        {HumanBodyBones.LeftRingIntermediate,   OVRSkeleton.BoneId.Hand_Ring2},
        {HumanBodyBones.LeftRingDistal,         OVRSkeleton.BoneId.Hand_Ring3},
        {HumanBodyBones.LeftLittleProximal,     OVRSkeleton.BoneId.Hand_Pinky1},
        {HumanBodyBones.LeftLittleIntermediate, OVRSkeleton.BoneId.Hand_Pinky2},
        {HumanBodyBones.LeftLittleDistal,       OVRSkeleton.BoneId.Hand_Pinky3},
    };

    static readonly Dictionary<HumanBodyBones, OVRSkeleton.BoneId> _RightHandBoneIdMap = new Dictionary<HumanBodyBones, OVRSkeleton.BoneId>()
    {
        {HumanBodyBones.RightThumbProximal,      OVRSkeleton.BoneId.Hand_Thumb0},
        {HumanBodyBones.RightThumbIntermediate,  OVRSkeleton.BoneId.Hand_Thumb2},
        {HumanBodyBones.RightThumbDistal,        OVRSkeleton.BoneId.Hand_Thumb3},
        {HumanBodyBones.RightIndexProximal,      OVRSkeleton.BoneId.Hand_Index1},
        {HumanBodyBones.RightIndexIntermediate,  OVRSkeleton.BoneId.Hand_Index2},
        {HumanBodyBones.RightIndexDistal,        OVRSkeleton.BoneId.Hand_Index3},
        {HumanBodyBones.RightMiddleProximal,     OVRSkeleton.BoneId.Hand_Middle1},
        {HumanBodyBones.RightMiddleIntermediate, OVRSkeleton.BoneId.Hand_Middle2},
        {HumanBodyBones.RightMiddleDistal,       OVRSkeleton.BoneId.Hand_Middle3},
        {HumanBodyBones.RightRingProximal,       OVRSkeleton.BoneId.Hand_Ring1},
        {HumanBodyBones.RightRingIntermediate,   OVRSkeleton.BoneId.Hand_Ring2},
        {HumanBodyBones.RightRingDistal,         OVRSkeleton.BoneId.Hand_Ring3},
        {HumanBodyBones.RightLittleProximal,     OVRSkeleton.BoneId.Hand_Pinky1},
        {HumanBodyBones.RightLittleIntermediate, OVRSkeleton.BoneId.Hand_Pinky2},
        {HumanBodyBones.RightLittleDistal,       OVRSkeleton.BoneId.Hand_Pinky3},
    };

    static readonly Dictionary<HumanBodyBones, HumanBodyBones> _HandBoneParent = new Dictionary<HumanBodyBones, HumanBodyBones>()
    {
        // Left hand
        {HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftHand},
        {HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbProximal},
        {HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbIntermediate},
        {HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftHand},
        {HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexProximal},
        {HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate},
        {HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftHand},
        {HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleProximal},
        {HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleIntermediate},
        {HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftHand},
        {HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingProximal},
        {HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate},
        {HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftHand},
        {HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleProximal},
        {HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleIntermediate},
        // Right hand
        {HumanBodyBones.RightThumbProximal, HumanBodyBones.RightHand},
        {HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal},
        {HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate},
        {HumanBodyBones.RightIndexProximal, HumanBodyBones.RightHand},
        {HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal},
        {HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate},
        {HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightHand},
        {HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal},
        {HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate},
        {HumanBodyBones.RightRingProximal, HumanBodyBones.RightHand},
        {HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal},
        {HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate},
        {HumanBodyBones.RightLittleProximal, HumanBodyBones.RightHand},
        {HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal},
        {HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate},
    };
}
