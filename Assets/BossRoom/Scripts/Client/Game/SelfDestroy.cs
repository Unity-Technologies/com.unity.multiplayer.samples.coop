using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestroy : MonoBehaviour
{
    [SerializeField]
    float m_DestroyDelay;
    float m_DestroyTimestamp;

    void Update()
    {
        // This visualizer object should self destroy once the VFX is done
        // A future optimization could be to pool these game objects. We can't just reuse one instance and enable/disable it
        // since multiple characters could need to spawn it at the same time.
        if (Time.time >= m_DestroyTimestamp)
        {
            Destroy(this);
        }
    }

    void Start()
    {
        m_DestroyTimestamp = Time.time + m_DestroyDelay;
    }
}
