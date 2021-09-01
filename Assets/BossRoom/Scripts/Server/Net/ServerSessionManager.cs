using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEngine;
using MLAPI.SceneManagement;

namespace BossRoom.Server
{
    public struct SessionPlayerData
    {
        ulong PlayerClientID;
        string PlayerName;
        Vector3 PlayerPosition;
        Vector3 PlayerRotation;
        bool IsConnected;

        public SessionPlayerData(ulong clientID, string name, Vector3 position, Vector3 rotation, bool isConnected = false)
        {
            PlayerClientID = clientID;
            PlayerName = name;
            PlayerPosition = position;
            PlayerRotation = rotation;
            IsConnected = isConnected;
        }
    }

    public class ServerSessionManager : MonoBehaviour
    {
        public GameNetPortal m_Portal;
        private ServerGameNetPortal m_ServerPortal;

        [SerializeField]
        PersistentPlayerRuntimeCollection m_PersistentPlayerRuntimeCollection;

        private Dictionary<ulong, SessionPlayerData> m_ClientIDs = new Dictionary<ulong, SessionPlayerData>();

        private PersistentPlayer persistentPlayer = new PersistentPlayer();

        void Start()
        {
            // If the GameNetPortal is unassigned something went wrong, disable the gameobject.
            // Same for the PresistentPlayerRuntimeCollection asset.
            // Alterativley we could try to find the portal and assign it to m_Portal, and disable if that fails.
            if (m_Portal == null || m_PersistentPlayerRuntimeCollection == null) {
                gameObject.SetActive(false);
                return;
            }

            // We need to get the ServerGameNetPortal from the assigned m_Portal object to access player data.
            m_ServerPortal = m_Portal.GetComponent<ServerGameNetPortal>();

            // Here we subscribe to the OnClientConnectedCallback and OnClientConnectedCallback with the functions we define in this class.
            m_Portal.NetManager.OnClientConnectedCallback += OnClientConnected;
            m_Portal.NetManager.OnClientDisconnectCallback += OnClientDisconnected;

            // Lastly we add the session manager to DontDestroyOnLoad.
            DontDestroyOnLoad(this);
        }

        private void OnNotifyServerClientLoadedScene(SceneSwitchProgress progress, ulong clientId)
        {
            Debug.Log(clientId);
        }

        private void OnDestroy()
        {
            // If the session manager is destroyed we need to unsubscribe from the connect / disconnect callbacks.
            m_Portal.NetManager.OnClientConnectedCallback -= OnClientConnected;
            m_Portal.NetManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void OnClientConnected(ulong clientID)
        {

            var playerData = m_ServerPortal.GetPlayerData(clientID);
            if (!m_ClientIDs.ContainsKey(clientID))
            {
                var sessionPlayerData = new SessionPlayerData(clientID, "unassigned", Vector3.zero, Vector3.zero, true);
                
                m_ClientIDs.Add(clientID, sessionPlayerData);
            }
            //m_PersistentPlayerRuntimeCollection.TryGetPlayer(clientID, out persistentPlayer);
            //Debug.Log(persistentPlayer);
        }

        private void OnClientDisconnected(ulong clientID)
        {
            if (m_ClientIDs.ContainsKey(clientID))
            {
                var playerData = m_ServerPortal.GetPlayerData(clientID);
                var sessionPlayerData = new SessionPlayerData(clientID, "bob", Vector3.zero, Vector3.zero, false);
                m_ClientIDs[clientID] = sessionPlayerData;
            }
        }
    }
}