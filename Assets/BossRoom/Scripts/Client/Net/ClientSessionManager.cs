using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEngine;
using MLAPI.SceneManagement;

namespace BossRoom.Client
{
    public class ClientSessionManager : MonoBehaviour
    {
        public GameNetPortal m_Portal;

        private ClientGameNetPortal m_ClientGameNetPortal;

        private HashSet<ulong> m_ClientIDs = new HashSet<ulong>();

        // Start is called before the first frame update
        void Start()
        {
            if (m_Portal == null)
            {
                gameObject.SetActive(false);
                return;
            }

            m_ClientGameNetPortal = m_Portal.GetComponent<ClientGameNetPortal>();

            m_Portal.NetManager.OnClientConnectedCallback += OnClientConnected;
            m_Portal.NetManager.OnClientDisconnectCallback += OnClientDisconnected;

            DontDestroyOnLoad(this);
        }

        private void OnDestroy()
        {
            m_Portal.NetManager.OnClientConnectedCallback -= OnClientConnected;
            m_Portal.NetManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void OnClientConnected(ulong clientID)
        {
            if (!m_ClientIDs.Contains(clientID))
            {
                m_ClientIDs.Add(clientID);
                return;
            }

            
        }

        private void OnClientDisconnected(ulong clientID)
        {
            if (m_ClientIDs.Contains(clientID))
            {
                m_ClientIDs.Remove(clientID);
            }
        }
    }
}