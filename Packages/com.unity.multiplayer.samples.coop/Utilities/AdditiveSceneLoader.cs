using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This NetworkBehavior, when added to a gameobject containing a collider (or multiple colliders) with the IsTrigger
/// property On, loads a scene additively when there is at least one gameobject with the "Player" tag that enters its
/// collider, and unloads it when all players leave the collider, after a specified cooldown to prevent it from
/// repeatedly loading and unloading the same scene.
/// </summary>
public class AdditiveSceneLoader : NetworkBehaviour
{
    const float k_CooldownDuration = 5.0f;

    [SerializeField]
    string sceneName;

    List<ulong> m_PlayersInTrigger;
    
    bool m_IsLoaded;
    
    bool m_IsCooldown;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += RemovePlayer;
            m_PlayersInTrigger = new List<ulong>();
        }
        else
        {
            enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
        }
    }

    void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (!m_IsCooldown)
        {
            if (m_IsLoaded && m_PlayersInTrigger.Count == 0)
            {
                NetworkManager.SceneManager.UnloadScene(SceneManager.GetSceneByName(sceneName));
                m_IsLoaded = false;
            }
            else if (!m_IsLoaded && m_PlayersInTrigger.Count > 0)
            {
                NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                m_IsLoaded = true;
                // Add this delay to prevent players entering and leaving the collider repeatedly from continually load/unloading the scene
                StartCoroutine(Cooldown());
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out NetworkObject no))
        {
            m_PlayersInTrigger.Add(no.OwnerClientId);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")  && other.TryGetComponent(out NetworkObject no))
        {
            RemovePlayer(no.OwnerClientId);
        }
    }

    void RemovePlayer(ulong clientId)
    {
        m_PlayersInTrigger.Remove(clientId);
    }

    IEnumerator Cooldown()
    {
        m_IsCooldown = true;
        yield return new WaitForSeconds(k_CooldownDuration);
        m_IsCooldown = false;
    }
}
