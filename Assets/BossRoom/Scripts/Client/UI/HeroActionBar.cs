using MLAPI;
using MLAPI.Spawning;
using UnityEngine;
using SkillTriggerStyle = BossRoom.Client.ClientInputSender.SkillTriggerStyle;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides logic for a Hero Action Bar with attack, skill button and a button to open emotes panel
    /// This bar tracks button clicks on hero action buttons for later use by ClientInputSender
    /// </summary>
    public class HeroActionBar : MonoBehaviour
    {
        // All buttons in this action bar
        [SerializeField]
        private UIHUDButton[] m_Buttons;

        // The Emote panel will be enabled or disabled when clicking the last button
        [SerializeField]
        private GameObject m_EmotePanel;

        private BossRoom.Client.ClientInputSender m_InputSender;

        // We find the Sprites to use by checking the Skill1, Skill2, and Skill3 members of our chosen CharacterClass
        private NetworkCharacterState m_NetState;

        // Each button has a UITooltipDetector; we cache references to these to avoid having to call GetComponent<> repeatedly
        private Client.UITooltipDetector[] m_Tooltips;

        private bool m_IsOtherPlayerSelected;

        public void RegisterInputSender(Client.ClientInputSender inputSender)
        {
            if (m_InputSender != null)
            {
                Debug.LogWarning($"Multiple ClientInputSenders in scene? Discarding sender belonging to {m_InputSender.gameObject.name} and adding it for {inputSender.gameObject.name} ");
            }

            m_InputSender = inputSender;
            m_NetState = m_InputSender.GetComponent<NetworkCharacterState>();
            m_NetState.TargetId.OnValueChanged += OnSelectionChanged;
            UpdateAllIcons();
        }

        void OnEnable()
        {
            m_Tooltips = new Client.UITooltipDetector[m_Buttons.Length];
            for (int i = 0; i < m_Buttons.Length; ++i)
            {
                m_Buttons[i].ButtonID = i;
                m_Buttons[i].OnPointerDownEvent += OnButtonClickedDown;
                m_Buttons[i].OnPointerUpEvent += OnButtonClickedUp;
                m_Tooltips[i] = m_Buttons[i].GetComponent<Client.UITooltipDetector>();
            }
        }

        void OnDisable()
        {
            for (int i = 0; i < m_Buttons.Length; ++i)
            {
                m_Buttons[i].OnPointerDownEvent -= OnButtonClickedDown;
                m_Buttons[i].OnPointerUpEvent -= OnButtonClickedUp;
            }
        }

        void OnDestroy()
        {
            if (m_NetState)
            {
                m_NetState.TargetId.OnValueChanged -= OnSelectionChanged;
            }
        }

        void OnSelectionChanged(ulong oldSelectionNetworkId, ulong newSelectionNetworkId)
        {
            UpdateAllIcons();
        }

        void OnButtonClickedDown(int buttonIndex)
        {
            if (buttonIndex == 3)
            {
                return; // this is the "emote" button; we won't do anything until they let go of the button
            }

            if (m_InputSender == null)
            {
                //nothing to do past this point if we don't have an InputSender.
                return;
            }

            switch (buttonIndex)
            {
                case 0: m_InputSender.RequestAction(m_IsOtherPlayerSelected ? ActionType.GeneralRevive : m_NetState.CharacterData.Skill1, SkillTriggerStyle.UI); break;
                case 1: m_InputSender.RequestAction(m_NetState.CharacterData.Skill2, SkillTriggerStyle.UI); break;
                case 2: m_InputSender.RequestAction(m_NetState.CharacterData.Skill3, SkillTriggerStyle.UI); break;
            }
        }

        void OnButtonClickedUp(int buttonIndex)
        {
            if (buttonIndex == 3)
            {
                m_EmotePanel.SetActive(!m_EmotePanel.activeSelf);
                return;
            }

            if (m_InputSender == null)
            {
                //nothing to do past this point if we don't have an InputSender.
                return;
            }

            switch (buttonIndex)
            {
                case 0: m_InputSender.RequestAction(m_IsOtherPlayerSelected ? ActionType.GeneralRevive : m_NetState.CharacterData.Skill1, SkillTriggerStyle.UIRelease); break;
                case 1: m_InputSender.RequestAction(m_NetState.CharacterData.Skill2, SkillTriggerStyle.UIRelease); break;
                case 2: m_InputSender.RequestAction(m_NetState.CharacterData.Skill3, SkillTriggerStyle.UIRelease); break;
            }
        }

        void UpdateAllIcons()
        {
            UpdateIcon(0, m_NetState.CharacterData.Skill1);
            UpdateIcon(1, m_NetState.CharacterData.Skill2);
            UpdateIcon(2, m_NetState.CharacterData.Skill3);

            if (m_NetState.TargetId.Value != 0
                && NetworkSpawnManager.SpawnedObjects.TryGetValue(m_NetState.TargetId.Value, out NetworkObject selection)
                && selection != null
                && selection.IsPlayerObject
                && selection.NetworkObjectId != m_NetState.NetworkObjectId)
            {
                m_IsOtherPlayerSelected = true;
                // we have a different player selected! In that case we want to reflect that our basic Action is a Revive, not an attack
                UpdateIcon(0, ActionType.GeneralRevive);
            }
            else
            {
                m_IsOtherPlayerSelected = false;
            }
        }

        void UpdateIcon(int slotIdx, ActionType actionType)
        {
            // first find the info we need (sprite and description)
            Sprite sprite = null;
            string description = "";

            if (actionType != ActionType.None)
            {
                var desc = GameDataSource.Instance.ActionDataByType[actionType];
                sprite = desc.Icon;
                description = desc.Description;
            }

            // set up UI elements appropriately
            if (sprite == null)
            {
                m_Buttons[slotIdx].gameObject.SetActive(false);
            }
            else
            {
                m_Buttons[slotIdx].gameObject.SetActive(true);
                m_Buttons[slotIdx].image.sprite = sprite;
                if (m_Tooltips[slotIdx])
                {
                    m_Tooltips[slotIdx].SetText(description);
                }
            }
        }

    }
}
