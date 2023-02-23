using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Vox.Hands;

public class HandUpdator : MonoBehaviour
{
    List<HandController> _ctrls = new List<HandController>();
    HandController _hookTarget;
    public HandController Target => _hookTarget;

    void Start()
    {
        Setup(null);
    }

    public void LateUpdate()
    {
        InnerUpdate();
    }

    public void Setup(HandController hook)
    {
        _ctrls.Clear();
        _ctrls = GetComponents<HandController>().ToList();
        _ctrls.ForEach(c => c.OtherControllerIsRunning = true);
        _hookTarget = hook;

        Debug.Log($"{gameObject.name} - HandUpdatorにコントロールを委譲しました");
    }

    public void InnerUpdate()
    {
        _ctrls.ForEach(c => {
            if (c.Processing)
            {
                c.UpdatePose();
            }
        });

        _ctrls.ForEach(c =>
        {
            if (c.Processing)
            {
                c.UpdateRotate();
            }
        });
    }
}
