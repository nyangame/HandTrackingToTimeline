using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

class HandAnimationCreator : MonoBehaviour
{
    [Serializable]
    class FingerNode
    {
        public string NodeName;
        public float Muscle;
    }

    [Serializable]
    class Finger
    {
        public List<FingerNode> Nodes = new List<FingerNode>();
    }

    [Serializable]
    class Hand
    {
        public List<FingerNode> FingerNodes = new List<FingerNode>();
    }


    [SerializeField] Animator _humanoid;
    [SerializeField] Text _targetFinger;
    [SerializeField] float _power = 0.001f;
    [SerializeField] float _camPower = 0.01f;
    [SerializeField] Vector3 _camOffset = new Vector3(0,0,1);
    [SerializeField] Vector3 _camRot = new Vector3(0,0,0);
    [SerializeField] Hand _left;
    [SerializeField] Hand _right;

    HumanPoseHandler _targetPose;
    int index = 55;
    bool _isPush = false;

    public void Start()
    {
        _targetPose = new HumanPoseHandler(_humanoid.avatar, _humanoid.gameObject.transform);
        _targetFinger.text = HumanTrait.MuscleName[index];

        HumanPose pose = new HumanPose();
        _targetPose.GetHumanPose(ref pose);

        if (_left.FingerNodes.Count == 0 && _right.FingerNodes.Count == 0)
        {
            for (int i = 55; i < 95; ++i)
            {
                if (i < 75)
                {
                    _left.FingerNodes.Add(new FingerNode() { NodeName = HumanTrait.MuscleName[i], Muscle = pose.muscles[i] });
                }
                else
                {
                    _right.FingerNodes.Add(new FingerNode() { NodeName = HumanTrait.MuscleName[i], Muscle = pose.muscles[i] });
                }
            }
        }
        else
        {
            for (int i = 55; i < 95; ++i)
            {
                if (i < 75)
                {
                    pose.muscles[i] = _left.FingerNodes[i - 55].Muscle;
                }
                else
                {
                    pose.muscles[i] = _right.FingerNodes[i - 75].Muscle;
                }
            }
            _targetPose.SetHumanPose(ref pose);
        }

        CameraUpdate();
    }

    public void Update()
    {
        ActionLoop();
    }

    void CameraUpdate()
    {
        HumanBodyBones target = 0;
        int baseId = 55;
        int basenode = (int)HumanBodyBones.LeftThumbProximal;
        if (index >= 75)
        {
            baseId = 75;
            basenode = (int)HumanBodyBones.RightThumbProximal;
        }

        int finger = (index - baseId) / 4;
        int tgt = (index - baseId) % 4;
        if (tgt == 0) tgt = 1;
        tgt--;
        target = (HumanBodyBones)(basenode + finger * 3 + tgt);

        Transform tra = _humanoid.GetBoneTransform(target).transform;
        Camera.main.transform.position = tra.position + Quaternion.Euler(_camRot) * _camOffset;
        Camera.main.transform.LookAt(tra);
    }

    private void ActionLoop()
    {
        if (Gamepad.current == null) return;
        Vector2 input = Gamepad.current.leftStick.ReadValue();
        Vector2 camera = Gamepad.current.rightStick.ReadValue();

        bool left = Gamepad.current.leftShoulder.isPressed;
        bool right = Gamepad.current.rightShoulder.isPressed;
        //横軸は指の位置をいじる
        if (left || right)
        {
            if (!_isPush)
            {
                _isPush = true;
                if (left)
                {
                    index--;
                    if (index < 55) index = 55;
                }
                if (right)
                {
                    index++;
                    if (index > 95) index = 95;
                }

                _targetFinger.text = HumanTrait.MuscleName[index];

                CameraUpdate();
            }
        }
        else
        {
            _isPush = false;
        }

        //縦軸は指のmuscleをいじる
        if (input.y != 0.0f)
        {
            if (_targetPose != null)
            {
                HumanPose pose = new HumanPose();
                _targetPose.GetHumanPose(ref pose);
                
                pose.muscles[index] += input.y * _power;

                if (index < 75)
                {
                    _left.FingerNodes[index - 55].Muscle = pose.muscles[index];
                }
                else
                {
                    _right.FingerNodes[index - 75].Muscle = pose.muscles[index];
                }

                // Update avatar human pose
                _targetPose.SetHumanPose(ref pose);
            }
        }

        if(camera.magnitude > 0)
        {
            _camRot += new Vector3(camera.y, camera.x, 0) * _camPower;
            CameraUpdate();
        }
    }
}
