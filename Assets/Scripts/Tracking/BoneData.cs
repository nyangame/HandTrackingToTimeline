using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
class BoneInfo
{
    public OVRSkeleton.BoneId Id;
    public short ParentBoneIndex;
    public Vector3 Position;
    public Quaternion Rotation;
}

[System.Serializable]
public class HumanoidInfo
{
    public int Id;
    public float Muscle;
}

[System.Serializable]
class TrackingOVRFrameInfo
{
    public float Time;
    public List<BoneInfo> Info = new List<BoneInfo>();
}

[System.Serializable]
public class TrackingFrameInfo
{
    public List<HumanoidInfo> Info = new List<HumanoidInfo>();
}

[System.Serializable]
class TrackingSave
{
    public List<TrackingFrameInfo> HumanoidInfo;

    public List<TrackingOVRFrameInfo> Left;
    public List<TrackingOVRFrameInfo> Right;
}