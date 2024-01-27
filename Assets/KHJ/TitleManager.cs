using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public enum State
    {
        Join,
        Option,
        Normal
    }

    [SerializeField]
    private CustomPanel joinRoomPanel;

    [SerializeField]
    private TextMeshProUGUI joinCodeText;

    [SerializeField]
    public ConfigurationPanel optionPanel;

    private static State _currentState;

    private void Start()
    {
        _currentState = State.Normal;
    }

    public static void SetState(State state)
    {
        _currentState=state;
    }

    public void OnCreateRoomButtonClick()
    {
        SceneManager.LoadScene($"RoomScene");
        //RoomScene에 들어가면 JoinCode를 생성하는것으로 생각
    }

    public void OnJoinRoomButtonClick()
    {
        joinRoomPanel.gameObject.SetActive(true);
        _currentState = State.Join;
    }

    public void Join()
    {
        // string code = joinCodeText.text; //Join Code
        // SceneManager.LoadScene($"RoomScene");
        //JoinCode 입력후 Join 시도
    }

    public void OnOptionButtonClick()
    {
        optionPanel.gameObject.SetActive(true);
        optionPanel.InitUI();
        _currentState = State.Option;
    }

    public void OnExitButtonClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

