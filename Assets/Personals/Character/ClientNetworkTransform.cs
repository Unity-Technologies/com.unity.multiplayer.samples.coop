using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
