using MLAPI;
using System.Collections;
using UnityEngine;

/// <summary>
/// Server-side logic for a floor switch (a/k/a "pressure plate").
/// This script should be attached to a physics trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkFloorSwitchState))]
public class ServerFloorSwitch : NetworkedBehaviour
{
    private Collider m_Collider;
    private NetworkFloorSwitchState m_FloorSwitchState;
    private int m_CachedPlayerLayerIdx;
    private int m_NumPlayersInTriggerThisFrame;
    private Coroutine m_StateCheckCoroutine;

    private void Awake()
    {
        m_Collider = GetComponent<Collider>();
        if (!m_Collider.isTrigger)
            Debug.LogError("ServerFloorSwitch's Collider is not set to be a Trigger.");

        m_FloorSwitchState = GetComponent<NetworkFloorSwitchState>();

        m_CachedPlayerLayerIdx = LayerMask.NameToLayer("PCs");
        if (m_CachedPlayerLayerIdx == -1)
            Debug.LogError("Project does not have a layer named 'PCs'");
    }

    public override void NetworkStart()
    {
        if (!IsServer)
        {
            enabled = false;
        }
        else
        {
            m_StateCheckCoroutine = StartCoroutine(CoroMaintainSwitchState());
        }
    }

    private void OnDestroy()
    {
        if (m_StateCheckCoroutine != null)
        {
            StopCoroutine(m_StateCheckCoroutine);
            m_StateCheckCoroutine = null;
        }
    }

    private IEnumerator CoroMaintainSwitchState()
    {
        yield return new WaitForFixedUpdate();
        while (enabled)
        {
            // Every physics frame, we need to know if players are in our collider. An easy
            // way to do that is to use OnTriggerStay(), which will be called every frame for each
            // collider in our trigger.
            //
            // So we basically just pause until the end of the physics frame. OnTriggerStay()
            // will have been called in the mean time, so we can see if it found anyone standing in us.
            m_NumPlayersInTriggerThisFrame = 0;

            // pause until the current physics frame has just finished
            yield return new WaitForFixedUpdate();

            m_FloorSwitchState.IsSwitchedOn.Value = m_NumPlayersInTriggerThisFrame > 0;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == m_CachedPlayerLayerIdx)
        {
            ++m_NumPlayersInTriggerThisFrame;
        }
    }

}
