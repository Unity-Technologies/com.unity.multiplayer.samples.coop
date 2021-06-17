using System;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour component to send/receive GUIDs from server to clients.
    /// </summary>
    public class NetworkGuidState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<byte[]> CharacterGuidArray = new NetworkVariable<byte[]>(new byte[0]);

        public event Action<Guid> GuidChanged;

        void Awake()
        {
            CharacterGuidArray.OnValueChanged += OnValueChanged;
        }

        void OnValueChanged(byte[] previousValue, byte[] newValue)
        {
            if (newValue == null || newValue.Length == 0)
            {
                // not a valid Guid
                return;
            }

            var guid = new Guid(newValue);

            GuidChanged?.Invoke(guid);
        }
    }
}
