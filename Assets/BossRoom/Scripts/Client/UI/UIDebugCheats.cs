using System;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Provides a way to open/close a popup window and GameEvents to raise debug cheat commands to be caught by
    /// listeners. This class is only available in the editor or for development builds
    /// </summary>
    public class UIDebugCheats : MonoBehaviour
    {
        [SerializeField]
        GameObject m_DebugCheatsPanel;

        [SerializeField]
        KeyCode m_OpenWindowKeyCode;

        [SerializeField]
        GameEvent m_SpawnEnemyGameEvent;

        [SerializeField]
        GameEvent m_SpawnBossGameEvent;

        [SerializeField]
        GameEvent m_GoToPostGameGameEvent;

        const int k_NbTouchesToOpenWindow = 4;

        public void SpawnEnemy()
        {
            m_SpawnEnemyGameEvent.Raise();
        }

        public void SpawnBoss()
        {
            m_SpawnBossGameEvent.Raise();
        }

        public void GoToPostGame()
        {
            m_GoToPostGameGameEvent.Raise();
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
