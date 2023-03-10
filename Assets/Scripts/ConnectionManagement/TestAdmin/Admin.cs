using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// this works because of the following
    /// disable scene management for that client (refuse all scene changes)
    /// have special spawning outside of NGO's. All prefabs removed from NetworkManager and custom instance handler for that particular player
    /// ConnectionApproval has been updated host side to react to this special connection payload to spawn the appropriate player host side.
    /// </summary>
    public class Admin : NetworkBehaviour
    {
        [SerializeField]
        private NetworkManager m_NetworkManager;

        // trick to keep being in that admin scene and not be imposed the scene by the host
        private bool DontChangeScene(int sceneindex, string scenename, LoadSceneMode loadscenemode)
        {
            return false;
        }

        // by having the admin player spawned differently than the rest, we ensure other clients don't spawn this, only this client and the server
        private class AdminSpawner : INetworkPrefabInstanceHandler
        {
            private readonly Admin m_Instance;

            public AdminSpawner(Admin instance)
            {
                m_Instance = instance;
            }
            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                return m_Instance.NetworkObject;
            }

            public void Destroy(NetworkObject networkObject)
            {
                // todo?
                Debug.Log("should we destroy admin? TODO");
            }
        }
        [ContextMenu("Connect")]
        void Connect()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                isAdminConnection = true,
                // todo player ID?
                isDebug = Debug.isDebugBuild
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);


            // TODO
            // note, I've removed all prefabs from NetworkManager's config, to make dynamic spawns fail
            // only issue is I still see bandwidth used for netvar deltas and RPCs coming from the host
            // NGO also vomits a LOT of errors when not being able to spawn something
            // admin should be hidden nicely from other clients and admin should be able to tell the host to not send it everything else.
            // There's a static NetworkShow that allows sending a list of NetworkObjects, it's too bad there's no global CheckObjectVisibility
            // We can hide all objects when a client connects, but this won't hide newly spawned objects after that connection
            // ServerRPC works though, I'm able to get info from the host without actually swiching to its scene and without other clients seeing me.

            m_NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
            var utp = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData("127.0.0.1", 9998);

            if (!m_NetworkManager.StartClient())
            {
                throw new Exception("NetworkManager StartClient failed");
            }

            // the following can only be done after StartClient
            m_NetworkManager.SceneManager.VerifySceneBeforeLoading += DontChangeScene;
            var adminPrefab = m_NetworkManager.GetComponent<AdminPrefabHolder>().prefab.gameObject;
            m_NetworkManager.PrefabHandler.AddHandler(adminPrefab, new AdminSpawner(this));

            void UnsubscribeOnDisconnectSelf(ulong client)
            {
                if (client != m_NetworkManager.LocalClientId) return;

                m_NetworkManager.OnClientDisconnectCallback -= UnsubscribeOnDisconnectSelf;
                m_NetworkManager.SceneManager.VerifySceneBeforeLoading -= DontChangeScene;
                m_NetworkManager.PrefabHandler.RemoveHandler(m_NetworkManager.GetComponent<AdminPrefabHolder>().prefab.NetworkObject);
            }

            m_NetworkManager.OnClientDisconnectCallback += UnsubscribeOnDisconnectSelf;
        }

        [ContextMenu("Server RPC")]
        void DoIt()
        {
            GetInfoFromServerServerRpc();
        }

        [ServerRpc]
        void GetInfoFromServerServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ResponseClientRpc(NetworkManager.Singleton.ConnectedClients.Count - 1, new ClientRpcParams() // -1 since we don't count the admin
            {
                Send = new ClientRpcSendParams()
                {
                    TargetClientIds = new[] {serverRpcParams.Receive.SenderClientId}
                }
            });
        }

        [ClientRpc]
        void ResponseClientRpc(int playerCount, ClientRpcParams _ = default)
        {
            Debug.Log($"Server tells us there are {playerCount} players connected");
        }

        //// the following works, commenting because of noise
        // private NetworkVariable<float> m_ServerTimeSinceLevelLoad = new();
        //
        // private void FixedUpdate()
        // {
        //     if (IsServer)
        //     {
        //         m_ServerTimeSinceLevelLoad.Value = Time.timeSinceLevelLoad;
        //     }
        //     else
        //     {
        //         Debug.Log($"Time since level load server side {m_ServerTimeSinceLevelLoad.Value}");
        //     }
        // }
    }
}
