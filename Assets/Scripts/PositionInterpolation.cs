using System;
using UnityEngine;
using MLAPI;

[DefaultExecutionOrder(200)]
public class PositionInterpolation : Interpolation<Vector3>
{
    public override Func<Vector3, Vector3, float, Vector3> LerpFunction => Vector3.LerpUnclamped;

    private void Update()
    {
        transform.position = GetValueForTime(NetworkingManager.Singleton.NetworkTime);
    }
}
