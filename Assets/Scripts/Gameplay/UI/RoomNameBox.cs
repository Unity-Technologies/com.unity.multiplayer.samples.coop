using System;
using TMPro;
using Unity.BossRoom.UnityServices.Lobbies;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class RoomNameBox : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_RoomNameText;
        [SerializeField]
        Button m_CopyToClipboardButton;

        LocalLobby m_LocalLobby;
        string m_LobbyCode;

        [Inject]
        void InjectDependencies(LocalLobby localLobby)
        {
            m_LocalLobby = localLobby;
            m_LocalLobby.changed += OnLobbyChanged;
        }

        void Awake()
        {
            UpdateUI(m_LocalLobby);
        }

        void OnDestroy()
        {
            m_LocalLobby.changed -= OnLobbyChanged;
        }

        void OnLobbyChanged(LocalLobby localLobby, bool _)
        {
            UpdateUI(localLobby);
        }

        void UpdateUI(LocalLobby localLobby)
        {
            if (!string.IsNullOrEmpty(localLobby.LobbyCode))
            {
                m_LobbyCode = localLobby.LobbyCode;
                m_RoomNameText.text = $"Lobby Code: {m_LobbyCode}";
                gameObject.SetActive(true);
                m_CopyToClipboardButton.gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void CopyToClipboard()
        {
            GUIUtility.systemCopyBuffer = m_LobbyCode;
        }
    }
}
