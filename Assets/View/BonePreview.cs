using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BonePreview))]
public class BonePreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var script = target as BonePreview;
        if (!script) return;

        if (script.IsSetup)
        {
            GUILayout.Label("フレーム:" + script.CurrentFrame);
            int prev = (int)script.CurrentFrame;
            script.CurrentFrame = Mathf.Floor(GUILayout.HorizontalScrollbar(script.CurrentFrame, 1.0f, script.MinFrame, script.MaxFrame));
            if (prev != (int)script.CurrentFrame)
            {
                script.UpdateFrame();
            }
        }

        if (GUILayout.Button("読み込む"))
        {
            script.Load();
        }
    }
}