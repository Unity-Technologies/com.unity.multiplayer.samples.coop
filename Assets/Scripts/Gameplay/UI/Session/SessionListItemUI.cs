using System;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// An individual Session UI in the list of available Sessions.
    /// </summary>
    public class SessionListItemUI : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_SessionNameText;
        [SerializeField]
        TextMeshProUGUI m_SessionCountText;

        [Inject]
        SessionUIMediator m_SessionUIMediator;

        ISessionInfo m_Data;

        public void SetData(ISessionInfo data)
        {
            m_Data = data;
            m_SessionNameText.SetText(data.Name);
            m_SessionCountText.SetText($"{data.MaxPlayers - data.AvailableSlots}/{data.MaxPlayers}");
        }

        public void OnClick()
        {
            m_SessionUIMediator.JoinSessionRequest(m_Data);
        }
    }
}
