using System;
using UnityEngine;

public abstract class Interpolation<T> : MonoBehaviour
{
    public abstract Func<T, T, float, T> LerpFunction { get;}

    private (float, T) _last; 
    private (float, T) _previous;

    public T GetValueForTime(float time)
    {
        float timeSincePrevious = time - _previous.Item1;
        float t = (timeSincePrevious / (_last.Item1 - _previous.Item1) - 1f);

        return LerpFunction(_previous.Item2, _last.Item2, t);
    }

    public void AddValue(float time, T value)
    {
        _previous = _last;
        _last = (time, value);
    }

}
