using System;
using Unity.Netcode;
using UnityEngine;

public static class SceneEventsUtilities
{
    public static Action OnAnySceneSpawned;

    public static void Initialize()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneSpawnedWrapper;
    }

    static void OnSceneSpawnedWrapper(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted) return;
        OnAnySceneSpawned?.Invoke();
    }
}
