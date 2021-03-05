using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Server-side logic for a floor switch (a/k/a "pressure plate").
/// This script should be attached to a physics trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkFloorSwitchState))]
public class ServerFloorSwitch : NetworkBehaviour
{
    private Collider m_Collider;
    private NetworkFloorSwitchState m_FloorSwitchState;
    private int m_CachedPlayerLayerIdx;
    private int m_CachedHeavyObjectLayerIdx;

    private List<Collider> m_RelevantCollidersInTrigger = new List<Collider>();

    private void Awake()
    {
        m_Collider = GetComponent<Collider>();
        m_Collider.isTrigger = true;

        m_FloorSwitchState = GetComponent<NetworkFloorSwitchState>();

        m_CachedPlayerLayerIdx = LayerMask.NameToLayer("PCs");
        if (m_CachedPlayerLayerIdx == -1)
            Debug.LogError("Project does not have a layer named 'PCs'");
        m_CachedHeavyObjectLayerIdx = LayerMask.NameToLayer("HeavyObject");
        if (m_CachedHeavyObjectLayerIdx == -1)
            Debug.LogError("Project does not have a layer named 'HeavyObject'");
    }

    public override void NetworkStart()
    {
        if (!IsServer)
        {
            enabled = false;
        }
    }

    private bool IsColliderAbleToTriggerSwitch(Collider collider)
    {
        return collider.gameObject.layer == m_CachedPlayerLayerIdx || collider.gameObject.layer == m_CachedHeavyObjectLayerIdx;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsColliderAbleToTriggerSwitch(other))
        {
            m_RelevantCollidersInTrigger.Add(other);
            m_FloorSwitchState.IsSwitchedOn.Value = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        m_RelevantCollidersInTrigger.Remove(other);
        m_FloorSwitchState.IsSwitchedOn.Value = m_RelevantCollidersInTrigger.Count > 0;
    }

    private void FixedUpdate()
    {
        // it's possible that the Colliders in our trigger have been destroyed, while still inside our trigger.
        // In this case, OnTriggerExit() won't get called for them! We can tell if a Collider was destroyed
        // because its reference will become null. So here we remove any nulls and see if we're still active.
        m_RelevantCollidersInTrigger.RemoveAll(collider => { return collider == null; });
        m_FloorSwitchState.IsSwitchedOn.Value = m_RelevantCollidersInTrigger.Count > 0;
    }
}
