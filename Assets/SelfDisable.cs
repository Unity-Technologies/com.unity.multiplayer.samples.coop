using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Will Disable this game object once active after the delay duration has passed.
/// </summary>
public class SelfDisable : MonoBehaviour
{
    public float DisabledDelay;
    float disableTimestamp;

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= disableTimestamp)
        {
          gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
    disableTimestamp = Time.time + DisabledDelay;
    }
}
