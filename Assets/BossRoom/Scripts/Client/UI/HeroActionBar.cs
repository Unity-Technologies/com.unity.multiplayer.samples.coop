using MLAPI;
using MLAPI.Spawning;
using UnityEngine;
using UnityEngine.Assertions;
using SkillTriggerStyle = BossRoom.Client.ClientInputSender.SkillTriggerStyle;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides logic for a Hero Action Bar with attack, skill button and a button to open emotes panel
    /// This bar tracks button clicks on hero action buttons for later use by ClientInputSender
    /// </summary>
    public class HeroActionBar : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("All buttons in this action bar")]
        private UIHUDButton[] m_Buttons;

        [SerializeField]
        [Tooltip("The Emote panel will be enabled or disabled when clicking the last button")]
        private GameObject m_EmotePanel;

        /// <summary>
        /// Our input-sender. Initialized in RegisterInputSender()
        /// </summary>
        private Client.ClientInputSender m_InputSender;

        /// <summary>
        /// Cached reference to local player's net state. 
        /// We find the Sprites to use by checking the Skill1, Skill2, and Skill3 members of our chosen CharacterClass
        /// </summary>
        private NetworkCharacterState m_NetState;

        /// <summary>
        /// Each button has a UITooltipDetector; we cache references to these to avoid having to call GetComponent<> repeatedly
        /// </summary>
        private Client.UITooltipDetector[] m_Tooltips;

        /// <summary>
        /// If we have another player selected, this is a reference to their stats; if anything else is selected, this is null
        /// </summary>
        private NetworkCharacterState m_SelectedPlayerNetState;

        /// <summary>
        /// If m_SelectedPlayerNetState is non-null, this indicates whether we think they're alive. (Updated every frame)
        /// </summary>
        private bool m_WasSelectedPlayerAliveDuringLastUpdate;

        /// <summary>
        /// Called during startup by the ClientInputSender. In response, we cache the provided
        /// inputSender and self-initialize.
        /// </summary>
        /// <param name="inputSender"></param>
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

        void Update()
        {
            // If we have another player selected, see if their aliveness state has changed,
            // and if so, update the interactiveness of the first button

            if (!m_SelectedPlayerNetState) { return; }

            bool isAliveNow = m_SelectedPlayerNetState.NetworkLifeState.Value == LifeState.Alive;
            if (isAliveNow != m_WasSelectedPlayerAliveDuringLastUpdate)
            {
                // this will update the icons so that button 1's interactiveness is correct
                UpdateAllIcons();
            }

            m_WasSelectedPlayerAliveDuringLastUpdate = isAliveNow;
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

            ActionType button1Action = m_NetState.CharacterData.Skill1;
            if (m_SelectedPlayerNetState && !m_WasSelectedPlayerAliveDuringLastUpdate)
            {
                button1Action = ActionType.GeneralRevive;
            }

            switch (buttonIndex)
            {
                case 0: m_InputSender.RequestAction(button1Action, SkillTriggerStyle.UI); break;
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

            ActionType button1Action = m_NetState.CharacterData.Skill1;
            if (m_SelectedPlayerNetState && !m_WasSelectedPlayerAliveDuringLastUpdate)
            {
                button1Action = ActionType.GeneralRevive;
            }

            switch (buttonIndex)
            {
                case 0: m_InputSender.RequestAction(button1Action, SkillTriggerStyle.UIRelease); break;
                case 1: m_InputSender.RequestAction(m_NetState.CharacterData.Skill2, SkillTriggerStyle.UIRelease); break;
                case 2: m_InputSender.RequestAction(m_NetState.CharacterData.Skill3, SkillTriggerStyle.UIRelease); break;
            }
        }

        /// <summary>
        /// Updates all the action buttons and caches info about the currently-selected entity (when appropriate):
        /// stores info in m_SelectedPlayerNetState and m_WasSelectedPlayerAliveDuringLastUpdate
        /// </summary>
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
                // we have another player selected! In that case we want to reflect that our basic Action is a Revive, not an attack!
                // But we need to know if the player is alive... if so, the button should be disabled (for better player communication)

                var charState = selection.GetComponent<NetworkCharacterState>();
                Assert.IsNotNull(charState); // all PlayerObjects should have a NetworkCharacterState component

                bool isAlive = charState.NetworkLifeState.Value == LifeState.Alive;
                UpdateIcon(0, ActionType.GeneralRevive, !isAlive);

                // we'll continue to monitor our selected player every frame to see if their life-state changes.
                m_SelectedPlayerNetState = charState;
                m_WasSelectedPlayerAliveDuringLastUpdate = isAlive;
            }
            else
            {
                m_SelectedPlayerNetState = null;
                m_WasSelectedPlayerAliveDuringLastUpdate = false;
            }
        }

        void UpdateIcon(int slotIdx, ActionType actionType, bool isClickable = true)
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
                m_Buttons[slotIdx].interactable = isClickable;
                m_Buttons[slotIdx].image.sprite = sprite;
                if (m_Tooltips[slotIdx])
                {
                    m_Tooltips[slotIdx].SetText(description);
                }
            }
            
        }

    }
}
