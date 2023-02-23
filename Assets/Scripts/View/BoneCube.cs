using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class BoneCube : MonoBehaviour
{
    public int ID;
    int _currentFrame;
    BoneInfo _info;

    static public BoneCube Build(int id, Transform parent = null)
    {
        GameObject bc = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("BoneCube"));
        BoneCube script = bc.GetComponent<BoneCube>();
        bc.transform.parent = parent;
        bc.transform.localPosition = Vector3.zero;
        bc.transform.localRotation = Quaternion.identity;
        script.ID = id;
        return script;
    }

    public void Setup(int frame, BoneInfo info)
    {
        _currentFrame = frame;
        _info = info;

        this.transform.localPosition = _info.Position * 100;
        this.transform.localRotation = _info.Rotation;
    }
}
