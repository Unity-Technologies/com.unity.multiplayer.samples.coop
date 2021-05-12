using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    public interface INetworkSubscribable<T>
    {
        void AddListener(NetworkVariable<T>.OnValueChangedDelegate action);

        void RemoveListener(NetworkVariable<T>.OnValueChangedDelegate action);
    }
}
