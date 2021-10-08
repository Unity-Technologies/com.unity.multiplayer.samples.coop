using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// NetworkBehaviour component to send/receive GUIDs from server to clients.
    /// </summary>
    public class NetworkAvatarGuidState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<NetworkGuid> AvatarGuidArray = new NetworkVariable<NetworkGuid>();

        public event Action<Guid> GuidChanged;

        void Awake()
        {
            AvatarGuidArray.OnValueChanged += OnValueChanged;
        }

        void OnValueChanged(NetworkGuid oldValue, NetworkGuid newValue)
        {
            Debug.Log($"NetworkBehaviour:NetworkAvatarGuidState:OnValueChanged old: {oldValue.FirstHalf}, new: {newValue.FirstHalf}");
            if (newValue.ToGuid().Equals(Guid.Empty))
            {
                // not a valid Guid
                return;
            }

            GuidChanged?.Invoke(newValue.ToGuid());
        }
    }
}
