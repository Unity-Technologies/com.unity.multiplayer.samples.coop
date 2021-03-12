using UnityEngine;

/// <summary>
/// Will Disable this game object once active after the delay duration has passed.
/// </summary>
public class SelfDisable : MonoBehaviour
{
    [SerializeField]
    float m_DisabledDelay;
    float m_DisableTimestamp;

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= m_DisableTimestamp)
        {
            Debug.Log($"Click Feedback disabling at {Time.time}");
            gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        m_DisableTimestamp = Time.time + m_DisabledDelay;
        Debug.Log($"Enabling Click Feedback at: {Time.time}, will disable at {m_DisableTimestamp}");
    }
}
