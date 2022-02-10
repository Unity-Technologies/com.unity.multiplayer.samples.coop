using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Server-side logic for a door. This particular type of door
    /// is opened when a player stands on a floor switch.
    /// (Assign the floor switches for this door in the editor.)
    /// </summary>
    [RequireComponent(typeof(NetworkDoorState))]
    public class ServerSwitchedDoor : MonoBehaviour, IServerOnlyMonoBehaviour
    {
        [SerializeField]
        NetworkFloorSwitchState[] m_SwitchesThatOpenThisDoor;

        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        NetworkDoorState m_NetworkDoorState;

        const string k_AnimatorDoorOpenBoolVarName = "IsOpen";

        [SerializeField, HideInInspector]
        int m_AnimatorDoorOpenBoolID;

        void Awake()
        {
            // don't let Update() run until after OnNetworkSpawn()
            enabled = false;

            if (m_SwitchesThatOpenThisDoor.Length == 0)
                Debug.LogError("Door has no switches and can never be opened!", gameObject);
        }

        void Update()
        {
            var isAnySwitchOn = false;
            foreach (var floorSwitch in m_SwitchesThatOpenThisDoor)
            {
                if (floorSwitch && floorSwitch.IsSwitchedOn.Value)
                {
                    isAnySwitchOn = true;
                    break;
                }
            }

            m_NetworkDoorState.IsOpen.Value = isAnySwitchOn;
        }

        void DoorStateChanged(bool previousValue, bool newValue)
        {
            m_Animator.SetBool(m_AnimatorDoorOpenBoolID, newValue);
        }

        void OnValidate()
        {
            m_AnimatorDoorOpenBoolID = Animator.StringToHash(k_AnimatorDoorOpenBoolVarName);
        }

        public void SetEnabled(bool enable)
        {
            enabled = enable;
            if (enable)
            {
                DoorStateChanged(false, m_NetworkDoorState.IsOpen.Value);

                m_NetworkDoorState.IsOpen.OnValueChanged += DoorStateChanged;
            }
            else
            {
                m_NetworkDoorState.IsOpen.OnValueChanged -= DoorStateChanged;
            }
        }
    }
}
