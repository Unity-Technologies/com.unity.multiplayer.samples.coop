using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class UIProfileSelector : MonoBehaviour
    {
        [SerializeField]
        ProfileListItemUI m_ProfileListItemPrototype;
        [SerializeField]
        InputField m_NewProfileField;
        [SerializeField]
        CanvasGroup m_CanvasGroup;
        [SerializeField]
        Graphic m_EmptyProfileListLabel;

        List<ProfileListItemUI> m_ProfileListItems = new List<ProfileListItemUI>();

        IInstanceResolver m_Container;
        ProfileManager m_ProfileManager;

        [Inject]
        void InjectDependency(IInstanceResolver container, ProfileManager profileManager)
        {
            m_Container = container;
            m_ProfileManager = profileManager;
        }

        void Awake()
        {
            m_ProfileListItemPrototype.gameObject.SetActive(false);
        }

        void Start()
        {
            Show();
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the join code text.
        /// </summary>
        public void SanitizeProfileNameInputText()
        {
            m_NewProfileField.text = SanitizeProfileName(m_NewProfileField.text);
        }

        string SanitizeProfileName(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^a-zA-Z0-9]", "");
        }

        public void OnNewProfileButtonPressed()
        {
            if (!m_ProfileManager.AvailableProfiles.Contains(m_NewProfileField.text))
            {
                m_ProfileManager.Profile = m_NewProfileField.text;
            }
            else
            {
                PopupManager.ShowPopupPanel("Could not create new Profile", "A profile already exists with this same. Select one of the already existing profiles or create a new one.");
            }
        }

        void InitializeUI()
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
            m_Container.InjectIn(listItem);
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
