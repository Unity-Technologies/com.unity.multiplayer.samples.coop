using System;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class UIQuitPanel : MonoBehaviour
    {
        [SerializeField]
        Text m_QuitButtonText;

        void OnEnable()
        {
            m_QuitButtonText.text = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening ?
                "Leave session?" :
                "Exit Game?";
        }

        public void Quit()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                // first disconnect then return to menu
                var gameNetPortal = GameNetPortal.Instance;
                if (gameNetPortal != null)
                {
                    gameNetPortal.RequestDisconnect();
                }
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                Application.Quit();
            }

            gameObject.SetActive(false);
        }
    }
}
