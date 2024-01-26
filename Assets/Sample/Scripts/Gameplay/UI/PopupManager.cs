using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Handles the display of Popup messages. Instantiates and reuses popup panel prefabs to allow displaying multiple
    /// messages in succession.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        [SerializeField]
        GameObject m_PopupPanelPrefab;

        [SerializeField]
        GameObject m_Canvas;

        List<PopupPanel> m_PopupPanels = new List<PopupPanel>();

        static PopupManager s_Instance;

        const float k_Offset = 30;
        const float k_MaxOffset = 200;

        void Awake()
        {
            if (s_Instance != null) throw new Exception("Invalid state, instance is not null");
            s_Instance = this;
            DontDestroyOnLoad(m_Canvas);
        }

        void OnDestroy()
        {
            s_Instance = null;
        }

        /// <summary>
        /// Displays a popup message with the specified title and main text.
        /// </summary>
        /// <param name="titleText">The title text at the top of the panel</param>
        /// <param name="mainText"> The text just under the title- the main body of text</param>
        /// <param name="closeableByUser"></param>
        public static PopupPanel ShowPopupPanel(string titleText, string mainText, bool closeableByUser = true)
        {
            if (s_Instance != null)
            {
                return s_Instance.DisplayPopupPanel(titleText, mainText, closeableByUser);
            }

            Debug.LogError($"No PopupPanel instance found. Cannot display message: {titleText}: {mainText}");
            return null;
        }

        PopupPanel DisplayPopupPanel(string titleText, string mainText, bool closeableByUser)
        {
            var popup = GetNextAvailablePopupPanel();
            if (popup != null)
            {
                popup.SetupPopupPanel(titleText, mainText, closeableByUser);
            }

            return popup;
        }

        PopupPanel GetNextAvailablePopupPanel()
        {
            int nextAvailablePopupIndex = 0;
            // Find the index of the first PopupPanel that is not displaying and has no popups after it that are currently displaying
            for (int i = 0; i < m_PopupPanels.Count; i++)
            {
                if (m_PopupPanels[i].IsDisplaying)
                {
                    nextAvailablePopupIndex = i + 1;
                }
            }

            if (nextAvailablePopupIndex < m_PopupPanels.Count)
            {
                return m_PopupPanels[nextAvailablePopupIndex];
            }

            // None of the current PopupPanels are available, so instantiate a new one
            var popupGameObject = Instantiate(m_PopupPanelPrefab, gameObject.transform);
            popupGameObject.transform.position += new Vector3(1, -1) * (k_Offset * m_PopupPanels.Count % k_MaxOffset);
            var popupPanel = popupGameObject.GetComponent<PopupPanel>();
            if (popupPanel != null)
            {
                m_PopupPanels.Add(popupPanel);
            }
            else
            {
                Debug.LogError("PopupPanel prefab does not have a PopupPanel component!");
            }

            return popupPanel;
        }
    }
}
