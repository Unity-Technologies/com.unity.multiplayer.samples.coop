using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// This is a temporary abstraction for different shared network states that all handle movement. This
    /// way a single client-side component can be used to update the local client transform. This can be dispensed with
    /// once Netcode for GameObjects handles client-side movement internally.
    /// </summary>
    public interface INetMovement
    {
        /// <summary>
        /// The current transform position of this entity.
        /// </summary>
        public NetworkVariable<Vector3> NetworkPosition { get; }

        /// <summary>
        /// The networked rotation of this entity. This reflects the authorative rotation on the server.
        /// </summary>
        public NetworkVariable<float> NetworkRotationY { get; }

        /// <summary>
        /// The current speed of this entity in m/s.
        /// </summary>
        public NetworkVariable<float> NetworkMovementSpeed { get; }

        public void InitNetworkPositionAndRotationY(Vector3 initPosition, float initRotationY);
    }
}
