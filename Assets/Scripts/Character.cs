using MLAPI;
using MLAPI.Messaging;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Character : NetworkedBehaviour
{
    [SerializeField]
    private float movementSpeed;

    private NavMeshAgent _navMeshAgent;
    private NavMeshPath _path;

    private PositionInterpolation _interpolation;

    public void Awake()
    {
        _path = new NavMeshPath();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _interpolation = GetComponentInChildren<PositionInterpolation>();
    }

    ///<inheritdoc/>
    public override void NetworkStart()
    {
        base.NetworkStart();

        SimulationManager.Singleton.OnSimulationUpdate += SimulationUpdate;
        SimulationManager.Singleton.OnSyncNetworkEvents += SyncNetworkEvents;

        _navMeshAgent.updateRotation = false;
        _navMeshAgent.updatePosition = true;

        if (!IsServer)
        {
            Destroy(_navMeshAgent);
        }
    }

    private void SyncNetworkEvents(float time, float deltaTime)
    {
        if (IsServer)
        {
            _interpolation.AddValue(time, transform.position);
            InvokeClientRpcOnEveryoneExcept<float, Vector3>(ReplicatePosition, NetworkingManager.Singleton.ServerClientId, time, transform.position);
        }

    }

    public void SetMovementTarget(Vector3 position)
    {
        _navMeshAgent.CalculatePath(position, _path);
    }

    private void SimulationUpdate(float time, float deltaTime)
    {
        if (IsServer)
        {
            NavigationMovement(deltaTime);
        }
    }

    private void NavigationMovement(float deltaTime)
    {
        var corners = _path.corners; // TODO: maybe use non-alloc version

        if (corners.Any() && Vector3.SqrMagnitude(corners[corners.Length - 1]- transform.position) < 1)
        {
            _path = new NavMeshPath();
            corners = _path.corners;
        }

        var direction = corners.Length > 1 ? (corners[1]- corners[0]).normalized : Vector3.zero;
        var movement = direction * movementSpeed * deltaTime;

        _navMeshAgent.Move(movement);
    }

    [ClientRPC]
    private void ReplicatePosition(float time, Vector3 value)
    {
        // The time which we receive here is server time. TODO We need to match it with our client time.
        _interpolation.AddValue(time, value);
    }
}
