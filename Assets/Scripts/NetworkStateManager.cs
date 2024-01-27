using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Services;

namespace PanicBuying
{
    [RequireComponent(typeof(Unity.Netcode.NetworkManager))]
    public class NetworkStateManager : MonoBehaviour
    {
        [SerializeField]
        private NetworkState state;

        private void Awake()
        {
            state.Initialize();
        }
    }
}
