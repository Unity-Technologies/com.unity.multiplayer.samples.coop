using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;

public class SamplePlayer : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            gameObject.AddComponent<CameraController>();
        }
    }
}
