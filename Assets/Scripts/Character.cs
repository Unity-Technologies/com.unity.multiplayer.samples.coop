using MLAPI;
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

        SetMovementTarget(new Vector3(5, 0, 5));
    }

    private void SyncNetworkEvents(float time, float deltaTime)
    {
        _interpolation.AddValue(time, transform.position);
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
}
