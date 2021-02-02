using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used by simple visual FX to self-destruct.
/// </summary>
namespace BossRoom
{
    public class TimedSelfDestruct : MonoBehaviour
    {
        [SerializeField]
        private float m_LifespanSeconds;

        private void Update()
        {
            m_LifespanSeconds -= Time.deltaTime;
            if (m_LifespanSeconds <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

}
