using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PanicBuying
{
    public class PanicUtil
    {
        static public ClientRpcParams MakeClientRpcParams(ulong InClientId)
        {
            return new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { InClientId } } };
        }
    }
}
