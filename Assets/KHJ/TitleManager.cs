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
        public CustomPanel optionPanel;

        private static State _currentState;

        private void Start()
        {
            _currentState = State.Normal;
        }

        public static void SetState(State state)
        {
            _currentState = state;
        }

        public void OnCreateRoomButtonClicked()
        {
            //RoomScene에 들어가면 JoinCode를 생성하는것으로 생각

            CreateRoomButtonClicked e = new();

            Event.Emit(e);
        }

        public void OnJoinRoomButtonClicked()
        {
            JoinRoomButtonClicked e = new();

            Event.Emit(e);

            //joinRoomPanel.gameObject.SetActive(true);
            //_currentState = State.Join;
        }

        public void Join()
        {
            string code = joinCodeText.text; //Join Code

            JoinRoomSubmited e = new(code);

            Event.Emit(e);
        }

        public void OnOptionButtonClicked()
        {
            optionPanel.gameObject.SetActive(true);
            _currentState = State.Option;

            OptionButtonClicked e = new();

            Event.Emit(e);

        }

        public void OnExitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
    }
}



