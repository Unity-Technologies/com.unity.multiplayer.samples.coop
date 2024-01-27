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
        [SerializeField]
        private CustomPanel joinRoomPanel;

        [SerializeField]
        private TextMeshProUGUI joinCodeText;

        [SerializeField]
        public ConfigurationPanel optionPanel;

        [SerializeField]
        private GameObject blackPanel;

        private EventListener<NetworkStateWorkEvent> networkWorkListener = new();

        private void Awake()
        {
            networkWorkListener.StartListen((e) =>
            {
                blackPanel.SetActive(e.working);
            });
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
        }

        public void OnOptionButtonClick()
        {
            optionPanel.gameObject.SetActive(true);
            optionPanel.InitUI();
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



