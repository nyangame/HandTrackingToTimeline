using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Vox.Hands;

[RequireComponent(typeof(Animator))]
//[RequireComponent(typeof(HandController))]
public class TrackingTarget : MonoBehaviour
{
    public bool IsInit { get; private set; } = false;
    List<HandController> _ctrls = new List<HandController>();

    HandController _leftHandCtrl;
    HandController _rightHandCtrl;

    Animator _animator;
    HumanPoseHandler _poseHandler;

    HumanPose _targetPose;
    HumanPose _initPose;
    Vector3 _initPos;
    Quaternion _initRot;
    bool _isUpdate = false;

    void Awake()
    {
        Setup();
    }

    void SetTargetPoseHandler()
    {
        _animator = GetComponent<Animator>();
        _poseHandler = new HumanPoseHandler(_animator.avatar, _animator.transform);
        _poseHandler.GetHumanPose(ref _initPose);
        _poseHandler.GetHumanPose(ref _targetPose);
        _initPos = _animator.GetBoneTransform(HumanBodyBones.Hips).position;
        _initRot = _animator.GetBoneTransform(HumanBodyBones.Hips).rotation;
    }

    void Setup()
    {
        _ctrls = GetComponents<HandController>().ToList();
        if(_ctrls.Count != 2)
        {
            Debug.LogWarning("HandControllerが両手についていません");
        }

        foreach (var h in _ctrls)
        {
            if (h.Hand == HandType.LeftHand)
            {
                _leftHandCtrl = h;
            }
            else
            {
                _rightHandCtrl = h;
            }
        }

        SetTargetPoseHandler();
        IsInit = true;
        Debug.Log("Setup");
    }


    public bool Processing { get; set; } = false;

    public void SetHandPose(double time)
    {
        var frame = RecordPlayer.Instance.GetNearestFrameInfo(time);

        _poseHandler.GetHumanPose(ref _targetPose);

        // Update avatar hand pose
        foreach(var f in frame.Info)
        {
            _targetPose.muscles[(int)f.Id] = f.Muscle;
        }

        // Update avatar human pose
        _targetPose.bodyPosition = _initPose.bodyPosition;
        _targetPose.bodyRotation = _initPose.bodyRotation;
        _isUpdate = true;

        _poseHandler.SetHumanPose(ref _targetPose);
    }

    public void SetHandPose(ref HumanPose srcPose)
    {
        _poseHandler.GetHumanPose(ref _targetPose);

        for (int i = 55; i < 95; i++)
        {
            _targetPose.muscles[i] = srcPose.muscles[i];
        }

        // Update avatar human pose
        _targetPose.bodyPosition = _initPose.bodyPosition;
        _targetPose.bodyRotation = _initPose.bodyRotation;
        _isUpdate = true;

        _poseHandler.SetHumanPose(ref _targetPose);
    }

    private void LateUpdate()
    {
        _poseHandler.SetHumanPose(ref _targetPose);
    }

    public void InitializeRuntimeControl(bool isRecord, string clip)
    {
        if(isRecord)
        {

        }
        else
        {
            RecordPlayer.Instance.Load(clip);
        }

        if(!IsInit)
        {
            Setup();
        }
    }

    public void Revert()
    {
        IsInit = false;
    }
}
