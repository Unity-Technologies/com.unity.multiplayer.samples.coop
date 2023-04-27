using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.BossRoom.Utils;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class UIProfileSelector : MonoBehaviour
    {
        [SerializeField]
        ProfileListItemUI m_ProfileListItemPrototype;
        [SerializeField]
        InputField m_NewProfileField;
        [SerializeField]
        Button m_CreateProfileButton;
        [SerializeField]
        CanvasGroup m_CanvasGroup;
        [SerializeField]
        Graphic m_EmptyProfileListLabel;

        List<ProfileListItemUI> m_ProfileListItems = new List<ProfileListItemUI>();

        [Inject] IObjectResolver m_Resolver;
        [Inject] ProfileManager m_ProfileManager;

        // Authentication service only accepts profile names of 30 characters or under 
        const int k_AuthenticationMaxProfileLength = 30;

        void Awake()
        {
            m_ProfileListItemPrototype.gameObject.SetActive(false);
            Hide();
            m_CreateProfileButton.interactable = false;
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the join code text.
        /// </summary>
        public void SanitizeProfileNameInputText()
        {
            m_NewProfileField.text = SanitizeProfileName(m_NewProfileField.text);
            m_CreateProfileButton.interactable = m_NewProfileField.text.Length > 0 && !m_ProfileManager.AvailableProfiles.Contains(m_NewProfileField.text);
        }

        string SanitizeProfileName(string dirtyString)
        {
            var output = Regex.Replace(dirtyString, "[^a-zA-Z0-9]", "");
            return output[..Math.Min(output.Length, k_AuthenticationMaxProfileLength)];
        }

        public void OnNewProfileButtonPressed()
        {
            var profile = m_NewProfileField.text;
            if (!m_ProfileManager.AvailableProfiles.Contains(profile))
            {
                m_ProfileManager.CreateProfile(profile);
                m_ProfileManager.Profile = profile;
            }
            else
            {
                PopupManager.ShowPopupPanel("Could not create new Profile", "A profile already exists with this same name. Select one of the already existing profiles or create a new one.");
            }
        }

        public void InitializeUI()
        {
            EnsureNumberOfActiveUISlots(m_ProfileManager.AvailableProfiles.Count);
            for (var i = 0; i < m_ProfileManager.AvailableProfiles.Count; i++)
            {
                var profileName = m_ProfileManager.AvailableProfiles[i];
                m_ProfileListItems[i].SetProfileName(profileName);
            }

            m_EmptyProfileListLabel.enabled = m_ProfileManager.AvailableProfiles.Count == 0;
        }

        void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - m_ProfileListItems.Count;

            for (int i = 0; i < delta; i++)
            {
                CreateProfileListItem();
            }

            for (int i = 0; i < m_ProfileListItems.Count; i++)
            {
                m_ProfileListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }

        void CreateProfileListItem()
        {
            var listItem = Instantiate(m_ProfileListItemPrototype.gameObject, m_ProfileListItemPrototype.transform.parent)
                .GetComponent<ProfileListItemUI>();
            m_ProfileListItems.Add(listItem);
            listItem.gameObject.SetActive(true);
            m_Resolver.Inject(listItem);
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
            m_NewProfileField.text = "";
            InitializeUI();
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }
    }
}
