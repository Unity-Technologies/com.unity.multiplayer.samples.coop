using System;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class UIDebugCheats : MonoBehaviour
    {
        [SerializeField]
        DebugCheatsState m_DebugCheatsState;

        [SerializeField]
        GameObject m_DebugCheatsPanel;

        [SerializeField]
        KeyCode m_OpenWindowKeyCode = KeyCode.Return;


        public void SpawnEnemy()
        {
            m_DebugCheatsState.SpawnEnemy?.Invoke(NetworkManager.Singleton.LocalClientId);
        }

        public void SpawnBoss()
        {
            m_DebugCheatsState.SpawnBoss?.Invoke(NetworkManager.Singleton.LocalClientId);
        }

        public void GoToPostGame()
        {
            m_DebugCheatsState.GoToPostGame?.Invoke(NetworkManager.Singleton.LocalClientId);
        }

        public void ToggleGodMode()
        {
            m_DebugCheatsState.ToggleGodMode?.Invoke(NetworkManager.Singleton.LocalClientId);
        }

        void Update()
        {
            if (m_OpenWindowKeyCode != KeyCode.None && Input.GetKeyDown(m_OpenWindowKeyCode))
            {
                m_DebugCheatsPanel.SetActive(!m_DebugCheatsPanel.activeSelf);
            }
        }
    }
}
#endif
