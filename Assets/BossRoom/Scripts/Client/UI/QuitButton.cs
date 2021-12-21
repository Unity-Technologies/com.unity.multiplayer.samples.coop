using System;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitButton : MonoBehaviour
{
    [SerializeField]
    TMP_Text m_QuitButtonText;

    void OnEnable()
    {
        m_QuitButtonText.text = NetworkManager.Singleton.IsListening ? "Disconnect" : "Exit Game";
    }

    public void Quit()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            // first disconnect then return to menu
            var gameNetPortal = GameObject.FindGameObjectWithTag("GameNetPortal").GetComponent<GameNetPortal>();
            gameNetPortal.RequestDisconnect();
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            Application.Quit();
        }
    }
}
