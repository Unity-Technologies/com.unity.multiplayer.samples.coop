using System;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour component to send/receive GUIDs from server to clients.
    /// </summary>
    public class NetworkAvatarGuidState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariableGUID AvatarGuidArray = new NetworkVariableGUID();

        public event Action<Guid> GuidChanged;

        void Awake()
        {
            AvatarGuidArray.OnValueChanged += OnValueChanged;
        }

        void OnValueChanged(Guid oldValue, Guid newValue)
        {
            if (newValue.Equals(Guid.Empty))
            {
                // not a valid Guid
                return;
            }

            GuidChanged?.Invoke(newValue);
        }
    }
}
