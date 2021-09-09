using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.Transports;
using MLAPI.Transports.LiteNetLib;
using MLAPI.Transports.PhotonRealtime;
using MLAPI.Transports.UNET;
using Photon.Realtime;

namespace BossRoom.Client
{
    /// <summary>
    /// Client side logic for a GameNetPortal. Contains implementations for all of GameNetPortal's S2C RPCs.
    /// </summary>
    [RequireComponent(typeof(GameNetPortal))]
    public class ClientSessionManager : MonoBehaviour
    {
        private GameNetPortal m_Portal;

        void Start()
        {
            m_Portal = GetComponent<GameNetPortal>();

            m_Portal.ClientSceneChanged += ClientSceneChanged;
        }

        void OnDestroy()
        {
            if (m_Portal != null)
            {
                m_Portal.ClientSceneChanged -= ClientSceneChanged;
            }
        }

        private void ClientSceneChanged(ulong clientID, int sceneIndex)
        {

        }
    }
}
