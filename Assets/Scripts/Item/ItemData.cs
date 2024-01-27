using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PanicBuying
{
    public struct ItemData : INetworkSerializeByMemcpy
    {
        int Id;
    }
}
