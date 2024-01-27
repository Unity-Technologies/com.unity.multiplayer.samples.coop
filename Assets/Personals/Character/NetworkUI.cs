using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class NetworkUI: MonoBehaviour
    {
        //* 각 버튼들을 컨트롤하기 위해 선언해둠
        //* SerializeField니까 에디터에서 끌어다가 지정해주면 됨
        [SerializeField] private Button serverBtn;
        [SerializeField] private Button hostBtn;
        [SerializeField] private Button clientBtn;

        private void Awake()
        {
            serverBtn.onClick.AddListener(() => {
                NetworkManager.Singleton.StartHost();
            });
            hostBtn.onClick.AddListener(() => {
                NetworkManager.Singleton.StartClient();
            });
            clientBtn.onClick.AddListener(() => {
                NetworkManager.Singleton.Shutdown();
            });
        }
    }
}
