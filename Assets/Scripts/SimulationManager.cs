using MLAPI;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Singleton { get; private set; }

    public delegate void SimulationUpdateDelegate(float time, float deltaTime);

    /// <summary>
    /// Main game simulation such as processing inputs, running AI, ticking state machines etc. An event is definitely not what we want to use because we have no order, but FixedUpdate isn't as well.
    /// </summary>

    public event SimulationUpdateDelegate OnSimulationUpdate;
    /// <summary>
    /// Called after simulation is done replicate game state such as positions to the client.
    /// </summary>
    public event SimulationUpdateDelegate OnSyncNetworkEvents;

    [SerializeField]
    private int tickRate = 20;

    private float _networkTickDelta;
    private float _lastNetworkTickTime;

    private void OnEnable()
    {
        Singleton = this;
        _networkTickDelta = 1f / tickRate;
    }


    // Update is called once per frame
    void Update()
    {

        // The goal here is to sync this up with MLAPIs synchronization but the current network tick model in MLAPI is broken.
        while (NetworkingManager.Singleton.NetworkTime - _lastNetworkTickTime >= _networkTickDelta)
        {
            _lastNetworkTickTime += _networkTickDelta;

            Physics.Simulate(_networkTickDelta);
            OnSimulationUpdate?.Invoke(_lastNetworkTickTime,_networkTickDelta);

            OnSyncNetworkEvents?.Invoke(_lastNetworkTickTime, _networkTickDelta);
        }
    }
}
