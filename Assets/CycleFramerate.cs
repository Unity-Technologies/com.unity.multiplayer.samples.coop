using System;
using UnityEngine;

public class CycleFramerate : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void CycleFramerates()
    {
        var targetFrameRate = Application.targetFrameRate;

        switch (targetFrameRate)
        {
            case -1:
                targetFrameRate = 60;
                break;
            case 60:
                targetFrameRate = 30;
                break;
            case 30:
                targetFrameRate = 16;
                break;
            case 16:
                targetFrameRate = -1;
                break;
        }

        Application.targetFrameRate = targetFrameRate;

        Debug.Log($"New target framerate: {targetFrameRate}");
    }
}
