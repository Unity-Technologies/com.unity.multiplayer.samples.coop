using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PanicBuying
{
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
            _currentState = state;
        }

        public void OnCreateRoomButtonClick()
        {
            //RoomScene에 들어가면 JoinCode를 생성하는것으로 생각

            CreateRoomButtonClicked e = new();

            Event.Emit(e);
        }

        public void OnJoinRoomButtonClick()
        {
            joinRoomPanel.gameObject.SetActive(true);
            _currentState = State.Join;
        }

        public void OnOptionButtonClick()
        {
            optionPanel.gameObject.SetActive(true);
            optionPanel.InitUI();
            _currentState = State.Option;
            OptionButtonClicked e = new();

            Event.Emit(e);

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
}



