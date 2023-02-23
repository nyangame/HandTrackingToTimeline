using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using HandPoseTransfer;

/// <summary>
/// NOTE: 後で親指と小指の補正入れる
/// </summary>
public class BonePreview : MonoBehaviour
{
    [SerializeField] string _filePath;
    [SerializeField] GameObject _leftRoot;
    [SerializeField] GameObject _rightRoot;
    [SerializeField] Animator _humanAnimator;
    [SerializeField] Transform _traRoot;
    [SerializeField] int _cbFrame;

    bool _isSetup = false;
    TrackingSave _save = null;

    public bool IsSetup => _isSetup;
    public float CurrentFrame { get; set; }
    public float MinFrame { get; private set; }
    public float MaxFrame { get; private set; }

    List<BoneCube> _left;
    List<BoneCube> _right;

    List<Quaternion> _localRotationLeft = new List<Quaternion>();
    List<Quaternion> _localRotationRight = new List<Quaternion>();

    Dictionary<int, BoneCube> _transformMap = new Dictionary<int, BoneCube>();

    HumanPoseHandler _SrcPoseHandler;
    HumanPoseHandler _TargetPoseHandler;
    HumanPose _SourcePose;
    HumanPose _TargetPose;

    private void Awake()
    {
        Load();
    }

    List<BoneCube> createHands(GameObject root, List<TrackingOVRFrameInfo> data)
    {
        List<BoneCube> ret = new List<BoneCube>();
        foreach (var r in data)
        {
            if (r.Info.Count == 0) continue;

            foreach (var b in r.Info)
            {
                Transform p;
                var parent = ret.Where(bo => bo.ID == (int)b.ParentBoneIndex).Select(bo => bo.transform);
                if (parent.Count() == 0) p = root.transform;
                else p = parent.Single();

                var bc = BoneCube.Build((int)b.Id, p);
                bc.Setup(0, b);
                ret.Add(bc);
            }
            break;
        }

        return ret;
    }

    void updateHands(List<BoneCube> nodes, TrackingOVRFrameInfo data)
    {
        if (data.Info.Count == 0) return;

        for(int i=0; i< nodes.Count; ++i)
        {
            nodes[i].Setup(0, data.Info[i]);
        }
    }
    
    IDictionary<HumanBodyBones, Transform> _Skeleton;
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
    }

    void AddHandSkeletonBones(SkeletonBuilder skeletonBuilder, bool isLeft)
    {
        //Humanoidのデータにマッピング

        //24～
        int offset = 39;
        if (isLeft) offset = 24;

        var bones = isLeft ? _left : _right;

        for (int i = 0; i < 15; ++i)
        {
            Vector3 localPosition = bones[mappingTable[i]].transform.localPosition;
            Quaternion localRotation = bones[mappingTable[i]].transform.localRotation;

            HumanBodyBones key = (HumanBodyBones)(i + offset);
            skeletonBuilder.Add(key, _HandBoneParent[key], localPosition, localRotation);
        }
    }

    static int[] mappingTable =
    {
        3,4,5,
        6,7,8,
        9,10,11,
        12,13,14,
        16,17,18
    };

    public void Load()
    {
        ResetNodes();
        var path = Directory.GetCurrentDirectory() + "/TrackingSave/";
        using (var sr = new StreamReader(path + _filePath))
        {
            _save = JsonUtility.FromJson<TrackingSave>(sr.ReadToEnd());
        }
        
        //
        if (_save.Left.Count == 0 && _save.Right.Count == 0)
        {
            Debug.LogError("data error");
            return;
        }
        
        _right = createHands(_rightRoot, _save.Right);
        _left = createHands(_leftRoot, _save.Left);

        for (int i = 0; i < 1000; ++i)
        {
            if (_save.Left[i].Info.Count == 0 || _save.Right[i].Info.Count == 0) continue;

            CurrentFrame = (float)i;
            MinFrame = (float)i;
            break;
        }

        CurrentFrame = _cbFrame;
        updateHands(_left, _save.Left[(int)CurrentFrame]);
        updateHands(_right, _save.Right[(int)CurrentFrame]);
        Initialize();
        
        _TargetPoseHandler = new HumanPoseHandler(_humanAnimator.avatar, _humanAnimator.gameObject.transform);

        _isSetup = true;
        MaxFrame = _save.Left.Count;
    }

    public void UpdateFrame()
    {
        if (_save == null) return;

        updateHands(_left, _save.Left[(int)CurrentFrame]);
        updateHands(_right, _save.Right[(int)CurrentFrame]);

        // Update hand pose
        UpdateHandSkeletonBones(true);
        UpdateHandSkeletonBones(false);
        
        if (_TargetPoseHandler != null)
        {
            // Get current human poses
            _SrcPoseHandler.GetHumanPose(ref _SourcePose);
            _TargetPoseHandler.GetHumanPose(ref _TargetPose);

            // Update avatar hand pose
            for (int i = 55; i < 95; i++)
            {
                _TargetPose.muscles[i] = _SourcePose.muscles[i];
            }

            // Update avatar human pose
            _TargetPoseHandler.SetHumanPose(ref _TargetPose);
        }
    }


    void UpdateHandSkeletonBones(bool isLeft)
    {
        //Humanoidのデータにマッピング

        //24～
        int offset = 39;
        if (isLeft) offset = 24;

        var bones = isLeft ? _left : _right;
        
        for (int i = 0; i < 15; ++i)
        {
            Vector3 localPosition = bones[mappingTable[i]].transform.localPosition;
            Quaternion localRotation = bones[mappingTable[i]].transform.localRotation;

            _Skeleton[(HumanBodyBones)(i+offset)].localRotation = localRotation;
        }
    }

    private void ResetNodes()
    {
        for(int i=0; i< _leftRoot.transform.childCount; ++i)
        {
            DestroyImmediate(_leftRoot.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < _rightRoot.transform.childCount; ++i)
        {
            DestroyImmediate(_rightRoot.transform.GetChild(i).gameObject);
        }
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
