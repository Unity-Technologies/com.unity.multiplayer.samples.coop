using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.SceneManagement;

public class SceneTransitionHandler : NetworkedBehaviour
{
    static public SceneTransitionHandler sceneTransitionHandler { get; internal set; }
    static public bool HasSceneSwitched { get; internal set; }
    private void Awake()
    {
        if(sceneTransitionHandler != this && sceneTransitionHandler != null)
        {
            GameObject.Destroy(sceneTransitionHandler.gameObject);
        }
        sceneTransitionHandler = this;
        NetworkSceneManager.OnSceneSwitched += NetworkSceneManager_OnSceneSwitched;
        NetworkSceneManager.OnSceneSwitchStarted += NetworkSceneManager_OnSceneSwitchStarted;
    }

    public void SwitchScene(string scenename)
    {
        NetworkSceneManager.SwitchScene(scenename);
    }

    private void NetworkSceneManager_OnSceneSwitchStarted(AsyncOperation operation)
    {
        HasSceneSwitched = false;
    }
    private void NetworkSceneManager_OnSceneSwitched()
    {
        HasSceneSwitched = true;
    }
}
