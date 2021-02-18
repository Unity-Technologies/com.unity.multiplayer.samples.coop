using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.SceneManagement;

public class SceneTransitionHandler : NetworkedBehaviour
{
    static public SceneTransitionHandler sceneTransitionHandler { get; internal set; }

    [HideInInspector]
    public delegate void ClientLoadedSceneDelegateHandler(ulong clientId);
    [HideInInspector]
    public event ClientLoadedSceneDelegateHandler clientLoadedScene;

    private SceneSwitchProgress m_SceneProgress;

    private void Awake()
    {
        if(sceneTransitionHandler != this && sceneTransitionHandler != null)
        {
            GameObject.Destroy(sceneTransitionHandler.gameObject);
        }
        sceneTransitionHandler = this;
    }

    /// <summary>
    /// Switches to a new scene
    /// </summary>
    /// <param name="scenename"></param>
    public void SwitchScene(string scenename)
    {
       m_SceneProgress = NetworkSceneManager.SwitchScene(scenename);

       m_SceneProgress.OnClientLoadedScene += SceneProgress_OnClientLoadedScene;
    }



    /// <summary>
    /// Invoked when a client has finished loading a scene
    /// </summary>
    /// <param name="clientId"></param>
    private void SceneProgress_OnClientLoadedScene(ulong clientId)
    {
        if(clientLoadedScene != null)
        {
            clientLoadedScene.Invoke(clientId);
        }
    }
}
