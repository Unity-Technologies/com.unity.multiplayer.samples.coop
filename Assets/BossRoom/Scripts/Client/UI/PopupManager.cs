using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Handles the display of Popup messages. Instantiates and reuses popup panel prefabs to allow displaying multiple
    /// messages in succession.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        [SerializeField]
        GameObject m_PopupPanelPrefab;

        List<PopupPanel> m_PopupPanels = new List<PopupPanel>();

        static PopupManager s_Instance;

        const float k_Offset = 30;
        const float k_MaxOffset = 300;

        void Awake()
        {
            if (s_Instance != null) throw new Exception("Invalid state, instance is not null");
            s_Instance = this;
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
        public static void ShowPopupPanel(string titleText, string mainText)
        {
            if (s_Instance != null)
            {
                s_Instance.DisplayPopupPanel(titleText, mainText);
            }
            else
            {
                Debug.LogError($"No PopupPanel instance found. Cannot display message: {titleText}: {mainText}");
            }
        }

        void DisplayPopupPanel(string titleText, string mainText)
        {
            GetNextAvailablePopupPanel()?.SetupPopupPanel(titleText, mainText);
        }

        PopupPanel GetNextAvailablePopupPanel()
        {
            PopupPanel popupPanel = null;
            foreach (var popup in m_PopupPanels)
            {
                if (popupPanel == null && !popup.IsDisplaying)
                {
                    popupPanel = popup;
                }
                else if (popup.IsDisplaying)
                {
                    popupPanel = null;
                }
            }

            if (popupPanel == null)
            {
                var popupGameObject = Instantiate(m_PopupPanelPrefab, gameObject.transform);
                popupGameObject.transform.position += new Vector3(1, -1) * (k_Offset * m_PopupPanels.Count % k_MaxOffset);
                popupPanel = popupGameObject.GetComponent<PopupPanel>();
                if (popupPanel == null)
                {
                    Debug.LogError("PopupPanel prefab does not have a PopupPanel component!");
                }
                m_PopupPanels.Add(popupPanel);
            }

            return popupPanel;
        }
    }
}
