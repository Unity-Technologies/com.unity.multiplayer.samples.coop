using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Server-side logic for a floor switch (a/k/a "pressure plate").
    /// This script should be attached to a physics trigger.
    /// </summary>
    [RequireComponent(typeof(Collider)), RequireComponent(typeof(NetworkFloorSwitchState))]
    public class ServerFloorSwitch : NetworkBehaviour
    {
        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        Collider m_Collider;

        [SerializeField]
        NetworkFloorSwitchState m_FloorSwitchState;

        List<Collider> m_RelevantCollidersInTrigger = new List<Collider>();

        const string k_AnimatorPressedDownBoolVarName = "IsPressed";

        [SerializeField, HideInInspector]
        int m_AnimatorPressedDownBoolVarID;

        void Awake()
        {
            m_Collider.isTrigger = true;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
            }

            FloorSwitchStateChanged(false, m_FloorSwitchState.IsSwitchedOn.Value);

            m_FloorSwitchState.IsSwitchedOn.OnValueChanged += FloorSwitchStateChanged;
        }

        void OnTriggerEnter(Collider other)
        {
            // no need to check for layer; layer matrix has been configured to only allow FloorSwitch x PC interactions
            m_RelevantCollidersInTrigger.Add(other);
        }

        void OnTriggerExit(Collider other)
        {
            m_RelevantCollidersInTrigger.Remove(other);
        }

        void FixedUpdate()
        {
            // it's possible that the Colliders in our trigger have been destroyed, while still inside our trigger.
            // In this case, OnTriggerExit() won't get called for them! We can tell if a Collider was destroyed
            // because its reference will become null. So here we remove any nulls and see if we're still active.
            m_RelevantCollidersInTrigger.RemoveAll(col => col == null);
            m_FloorSwitchState.IsSwitchedOn.Value = m_RelevantCollidersInTrigger.Count > 0;
        }

        void FloorSwitchStateChanged(bool previousValue, bool newValue)
        {
            m_Animator.SetBool(m_AnimatorPressedDownBoolVarID, newValue);
        }

        void OnValidate()
        {
            m_AnimatorPressedDownBoolVarID = Animator.StringToHash(k_AnimatorPressedDownBoolVarName);
        }
    }
}
