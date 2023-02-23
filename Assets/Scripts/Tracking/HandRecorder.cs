using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Vox.Hands;

public class HandRecorder : MonoBehaviour
{
    static HandRecorder _instance;
    public static HandRecorder Instance { get { return _instance; } } 

    [SerializeField] Camera _camera;
    [SerializeField] PlayableDirector _director;

    TrackingTarget _targetObject;

    HandTrackedPlayableClip _recordingTarget;
    TimelineClip _targetClip;

    void Start()
    {
        _instance = this;
        //タイムラインの再生を録画まで止める
        if (_director)
        {
            IEnumerable<TrackAsset> tracks = (_director.playableAsset as TimelineAsset).GetOutputTracks();
            foreach (var t in tracks)
            {
                var cls = t.GetClips();
                foreach (var c in cls)
                {
                    if (c.asset.GetType() == typeof(HandTrackedPlayableClip))
                    {
                        _recordingTarget = c.asset as HandTrackedPlayableClip;
                        if (_recordingTarget.template.isRecording)
                        {
                            _targetClip = c;

                            var track = t as HandTrackedPlayableTrack;
                            _targetObject = track._trackBinding;
                        }
                        else
                        {
                            _recordingTarget = null;
                        }
                    }
                }
            }

            if (_recordingTarget)
            {
                _director.Stop();
                double t = _targetClip.start - 1.0f;
                if (t < 0) t = 0.0f;
                _director.time = t;

                SceneManager.LoadScene("HandTrackingScene", LoadSceneMode.Additive);
                Destroy(_camera.gameObject);

                BoneTracking.Instance.OnPlay += (int p) =>
                {
                    _director.Play();
                };
            }
        }
    }
}
