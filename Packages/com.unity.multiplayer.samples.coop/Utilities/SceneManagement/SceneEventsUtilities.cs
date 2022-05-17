using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneEventsUtilities
{
    public static Action OnAnySceneSpawned;

    public static void Initialize()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneSpawnedWrapper;
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnGameObjectSpawnedWrapper;
        foreach (var eventType in (SceneEventType[])Enum.GetValues(typeof(SceneEventType)))
        {
            s_AllGOEvents[eventType] = new();
        }
    }

    public static void Teardown()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneSpawnedWrapper;
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnGameObjectSpawnedWrapper;
        s_AllGOEvents.Clear();
    }

    static void OnSceneSpawnedWrapper(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted) return;
        OnAnySceneSpawned?.Invoke();
    }

    static Dictionary<SceneEventType, List<(GameObject, Action<SceneEvent>, string)>> s_AllGOEvents = new();

    // This requires the gameobject to be present when this events happen. if it lives in a scene that gets unloaded, it won't get the unloadcomplete event.
    public static void RegisterGameObjectSpawn(GameObject go, Action<SceneEvent> toExecute, SceneEventType eventType)
    {
        RegisterGameObjectSpawn(go, toExecute, eventType, go.scene.name);
    }

    public static void RegisterGameObjectSpawn(GameObject go, Action<SceneEvent> toExecute, SceneEventType eventType, string sceneName)
    {
        s_AllGOEvents[eventType].Add((go, toExecute, sceneName));
    }

    static void OnGameObjectSpawnedWrapper(SceneEvent sceneEvent)
    {
        var allToExecute = s_AllGOEvents[sceneEvent.SceneEventType];
        for (var i = allToExecute.Count - 1; i >= 0; i--)
        {
            var (gameObject, toExecute, sceneNameToMonitor) = allToExecute[i];
            if (gameObject == null)
            {
                allToExecute.RemoveAt(i);
                continue;
            }
            if (gameObject.scene.name == sceneEvent.SceneName || sceneEvent.SceneName == sceneNameToMonitor)
            {
                toExecute?.Invoke(sceneEvent);
            }
        }
    }

    public static void UnregisterGameObjectSpawn(GameObject go, Action<SceneEvent> toExecute, SceneEventType eventType)
    {
        UnregisterGameObjectSpawn(go, toExecute, eventType, go.scene.name);
    }

    public static void UnregisterGameObjectSpawn(GameObject go, Action<SceneEvent> toExecute, SceneEventType eventType, string sceneNameToMonitor)
    {
        s_AllGOEvents[eventType].Remove((go, toExecute, sceneNameToMonitor));
    }
}
