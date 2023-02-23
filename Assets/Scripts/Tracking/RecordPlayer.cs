using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class RecordPlayer
{
    static RecordPlayer _instance = new RecordPlayer();
    static public RecordPlayer Instance => _instance;
    private RecordPlayer() { }

    TrackingSave _save;

    public bool IsLoad { get; private set; } = false;

    public void Load(string trackingFile)
    {
        var path = Directory.GetCurrentDirectory() + "/TrackingSave/";
        using (var sr = new StreamReader(path + trackingFile))
        {
            _save = JsonUtility.FromJson<TrackingSave>(sr.ReadToEnd());
        }
        IsLoad = true;
    }

    public TrackingFrameInfo GetNearestFrameInfo(double t)
    {
        int frame = (int)Math.Floor(t * 60.0);
        return _save.HumanoidInfo[frame];
    }
}
