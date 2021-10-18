using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Visualization class for Breakables. Breakables work by swapping a "broken" prefab at the moment of breakage. The broken prefab
    /// then handles the pesky details of actually falling apart.
    /// </summary>
    public class ClientBreakableVisualization : NetworkBehaviour
    {
        [SerializeField]
        private GameObject m_BrokenPrefab;

        [SerializeField][Tooltip("If set, will be used instead of BrokenPrefab when new players join, skipping transition effects.")]
        private GameObject m_PrebrokenPrefab;

        [SerializeField]
        [Tooltip("We use this transform's position and rotation when creating the prefab. (Defaults to self)")]
        private Transform m_BrokenPrefabPos;

        [SerializeField]
        private GameObject[] m_UnbrokenGameObjects;

        [SerializeField]
        private NetworkBreakableState m_NetState;

        private GameObject m_CurrentBrokenVisualization;

        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                enabled = false;
            }
            else
            {
                m_NetState.IsBroken.OnValueChanged += OnBreakableStateChanged;

                if (m_NetState.IsBroken.Value == true)
                {
                    PerformBreak(true);
                }

            }
        }

        private void OnBreakableStateChanged(bool wasBroken, bool isBroken)
        {
            if (!wasBroken && isBroken)
            {
                PerformBreak(false);
            }
            else if (wasBroken && !isBroken)
            {
                PerformUnbreak();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (m_NetState)
            {
                m_NetState.IsBroken.OnValueChanged -= OnBreakableStateChanged;
            }
        }

        private void PerformBreak(bool onStart)
        {
            foreach (var gameObject in m_UnbrokenGameObjects)
            {
                if (gameObject)
                    gameObject.SetActive(false);
            }

            if (m_CurrentBrokenVisualization)
                Destroy(m_CurrentBrokenVisualization); // just a safety check, should be null when we get here

            GameObject brokenPrefab = (onStart && m_PrebrokenPrefab != null) ? m_PrebrokenPrefab : m_BrokenPrefab;
            if (brokenPrefab)
            {
                m_CurrentBrokenVisualization = Instantiate(brokenPrefab, m_BrokenPrefabPos.position, m_BrokenPrefabPos.rotation, transform);
            }
        }

        private void PerformUnbreak()
        {
            if (m_CurrentBrokenVisualization)
            {
                Destroy(m_CurrentBrokenVisualization);
            }
            foreach (var gameObject in m_UnbrokenGameObjects)
            {
                if (gameObject)
                    gameObject.SetActive(true);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!m_NetState)
                m_NetState = GetComponent<NetworkBreakableState>();
            if (!m_BrokenPrefabPos)
                m_BrokenPrefabPos = transform;
        }
#endif
    }
}
