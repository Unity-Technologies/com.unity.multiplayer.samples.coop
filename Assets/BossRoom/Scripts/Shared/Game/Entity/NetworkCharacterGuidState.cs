using System;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour component to send/receive Character GUIDs from server to clients.
    /// </summary>
    public class NetworkCharacterGuidState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<byte[]> CharacterGuidArray = new NetworkVariable<byte[]>(new byte[0]);

        public event Action<Guid> CharacterGuidChanged;

        void Awake()
        {
            CharacterGuidArray.OnValueChanged += OnValueChanged;
        }

        void OnValueChanged(byte[] previousValue, byte[] newValue)
        {
            var characterGuid = new Guid(newValue);

            CharacterGuidChanged?.Invoke(characterGuid);
        }
    }
}
