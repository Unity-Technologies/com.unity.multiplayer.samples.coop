using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Infrastructure;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// A runtime list of <see cref="PersistentPlayer"/> objects that is populated both on clients and server.
    /// </summary>
    [CreateAssetMenu]
    public class ClientPlayerAvatarRuntimeCollection : RuntimeCollection<ClientPlayerAvatar>
    {
    }
}
