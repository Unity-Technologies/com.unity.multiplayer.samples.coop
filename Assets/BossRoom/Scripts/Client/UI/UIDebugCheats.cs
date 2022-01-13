using System;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class UIDebugCheats : MonoBehaviour
    {
        [SerializeField]
        DebugCheatsMediator m_DebugCheatsMediator;

        [SerializeField]
        GameObject m_DebugCheatsPanel;

        [SerializeField]
        KeyCode m_OpenWindowKeyCode;

        const int k_NbTouchesToOpenWindow = 4;

        public void SpawnEnemy()
        {
            m_DebugCheatsMediator.SpawnEnemyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        public void SpawnBoss()
        {
            m_DebugCheatsMediator.SpawnBossServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        public void GoToPostGame()
        {
            m_DebugCheatsMediator.GoToPostGameServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        void Update()
        {
            if (Input.touchCount == k_NbTouchesToOpenWindow ||
                m_OpenWindowKeyCode != KeyCode.None && Input.GetKeyDown(m_OpenWindowKeyCode))
            {
                m_DebugCheatsPanel.SetActive(!m_DebugCheatsPanel.activeSelf);
            }
        }
    }
}
#endif
