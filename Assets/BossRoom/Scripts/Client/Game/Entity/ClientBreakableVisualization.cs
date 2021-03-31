using MLAPI;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Visualization class for Breakables. Breakables work by swapping a "broken" prefab at the moment of breakage. The broken prefab
    /// then handles the pesky details of actually falling apart.
    /// </summary>
    public class ClientBreakableVisualization : NetworkBehaviour
    {
        [SerializeField]
        private GameObject m_BrokenPrefab;

        [SerializeField]
        [Tooltip("We use this transform's position and rotation when creating the prefab. (Defaults to self)")]
        private Transform m_BrokenPrefabPos;

        [SerializeField]
        private GameObject[] m_UnbrokenGameObjects;

        [SerializeField]
        private NetworkBreakableState m_NetState;

        private GameObject m_CurrentBrokenVisualization;

        public override void NetworkStart()
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
                    //todo: don't dramatically break on entry to scene, if already broken.
                    PerformBreak();
                }

            }
        }

        private void OnBreakableStateChanged(bool wasBroken, bool isBroken)
        {
            if (!wasBroken && isBroken)
            {
                PerformBreak();
            }
            else if (wasBroken && !isBroken)
            {
                PerformUnbreak();
            }
        }

        private void OnDestroy()
        {
            if (m_NetState)
            {
                m_NetState.IsBroken.OnValueChanged -= OnBreakableStateChanged;
            }
        }

        private void PerformBreak()
        {
            foreach (var gameObject in m_UnbrokenGameObjects)
            {
                if (gameObject)
                    gameObject.SetActive(false);
            }

            if (m_CurrentBrokenVisualization)
                Destroy(m_CurrentBrokenVisualization); // just a safety check, should be null when we get here

            if (m_BrokenPrefab)
                m_CurrentBrokenVisualization = Instantiate(m_BrokenPrefab, m_BrokenPrefabPos.position, m_BrokenPrefabPos.rotation, transform);
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
