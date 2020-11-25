using UnityEngine;
using MLAPI;

[RequireComponent(typeof(NetworkingManager))]
public class NetworkingManagerHUD : MonoBehaviour
{
    void OnGUI()
    {

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkingManager.Singleton.IsClient && !NetworkingManager.Singleton.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host")) NetworkingManager.Singleton.StartHost();
        if (GUILayout.Button("Client")) NetworkingManager.Singleton.StartClient();
        if (GUILayout.Button("Server")) NetworkingManager.Singleton.StartServer();
    }

    void StatusLabels()
    {
        string mode = NetworkingManager.Singleton.IsHost ? "Host" : NetworkingManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " + NetworkingManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}

