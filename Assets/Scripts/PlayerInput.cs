using MLAPI;
using MLAPI.Messaging;
using System;
using UnityEngine;

public class PlayerInput : NetworkedBehaviour
{
    private Character character;

    public void Awake()
    {
        character = GetComponent<Character>();
    }

    public override void NetworkStart()
    {
        if (IsClient)
        {
            SimulationManager.Singleton.OnSimulationUpdate += SimulationUpdate;
        }
    }

    private void SimulationUpdate(float time, float deltaTime)
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                InvokeServerRpc(SendPlayerInput, hit.point);
            }
        }
    }

    [ServerRPC]
    public void SendPlayerInput(Vector3 position)
    {
        character.SetMovementTarget(position);
    }
}
