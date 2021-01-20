using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Client
{
    public class UICharSelectController : MonoBehaviour
    {
        #region Variables set in editor
        [SerializeField]
        [Tooltip("Connect to dummy character model in the char-select screen")]
        private BossRoom.Visual.ModelAppearanceSetter m_InSceneCharacter;

        [SerializeField]
        [Tooltip("Collection of 8 portrait-box UI elements, one for each potential lobby member")]
        private List<UICharSelectPlayerStateBox> m_PlayerStateBoxes;
        [SerializeField]
        private Text m_PlayerNumberText;
        [SerializeField]
        private Text m_ClassNameText;
        [SerializeField]
        private Button m_ClassButtonTank;
        [SerializeField]
        private Button m_ClassButtonMage;
        [SerializeField]
        private Button m_ClassButtonRogue;
        [SerializeField]
        private Button m_ClassButtonArcher;
        [SerializeField]
        private Button m_GenderButtonMale;
        [SerializeField]
        private Button m_GenderButtonFemale;

        [SerializeField]
        private List<GameObject> m_HideTheseUIElementsWhenLobbyIsLockedIn;
        [SerializeField]
        private List<GameObject> m_ShowTheseUIElementsWhenLobbyIsLockedIn;

        [SerializeField]
        [Tooltip("Message shown in the char-gen screen. {0} will be replaced with the player's seat number")]
        private string m_WelcomeMsg = "Welcome, P{0}!";
        #endregion

        private Dictionary<CharacterTypeEnum, Button> m_ClassButtons;
        private CharacterTypeEnum m_Class;
        private CharSelectData.SlotState m_SlotState;
        private bool m_IsMale;

        private void Awake()
        {
            Debug.Log("CharSelectUIController.Awake");
            m_ClassButtons = new Dictionary<CharacterTypeEnum, Button>
            {
                [ CharacterTypeEnum.TANK ] = m_ClassButtonTank,
                [ CharacterTypeEnum.MAGE ] = m_ClassButtonMage,
                [ CharacterTypeEnum.ROGUE ] = m_ClassButtonRogue,
                [ CharacterTypeEnum.ARCHER ] = m_ClassButtonArcher,
            };
        }

        private void Start()
        {
            m_Class = CharacterTypeEnum.TANK;
            m_IsMale = true;

            CreateStateList();
            if (ClientCharSelectState.Instance == null)
                Debug.LogError("No CharSelectData in scene!");
            ClientCharSelectState.Instance.CharSelectData.OnAssignedLobbyIndex += OnAssignedLobbyIndex;
            ClientCharSelectState.Instance.CharSelectData.OnCharSelectSlotChanged += OnCharSelectSlotChanged;
            ClientCharSelectState.Instance.CharSelectData.OnLobbyLockedIn += OnLobbyLockedIn;

            m_InSceneCharacter.SetModel(m_Class, m_IsMale);
            SetButtonInteractibleness();
        }

        private void OnDestroy()
        {
            if (ClientCharSelectState.Instance)
            {
                ClientCharSelectState.Instance.CharSelectData.OnAssignedLobbyIndex -= OnAssignedLobbyIndex;
                ClientCharSelectState.Instance.CharSelectData.OnCharSelectSlotChanged -= OnCharSelectSlotChanged;
                ClientCharSelectState.Instance.CharSelectData.OnLobbyLockedIn -= OnLobbyLockedIn;
            }
        }

        private void CreateStateList()
        {
            for (int x = 0; x < m_PlayerStateBoxes.Count; ++x)
            {
                m_PlayerStateBoxes[ x ].SetPlayerSlotIndex(x);
                var slotData = ClientCharSelectState.Instance.CharSelectData.GetCharSelectSlot(x);
                m_PlayerStateBoxes[ x ].SetClassAndState(slotData.Class, slotData.State);
            }
        }

        private void SetClass(CharacterTypeEnum newClass)
        {
            // TODO: these labels will be pulled from class data?
            string className = "";
            switch (newClass)
            {
            case CharacterTypeEnum.TANK:
                className = "TANK";
                break;
            case CharacterTypeEnum.MAGE:
                className = "MAGE";
                break;
            case CharacterTypeEnum.ROGUE:
                className = "ROGUE";
                break;
            case CharacterTypeEnum.ARCHER:
                className = "ARCHER";
                break;
            default:
                className = "error";
                Debug.LogError("Can't handle class " + newClass);
                break;
            }
            m_ClassNameText.text = className;

            m_Class = newClass;
            m_SlotState = CharSelectData.SlotState.ACTIVE;
            m_InSceneCharacter.SetModel(m_Class, m_IsMale);
            m_InSceneCharacter.PerformUIGesture(Visual.ModelAppearanceSetter.UIGesture.Selected);
            SetButtonInteractibleness();
            ClientCharSelectState.Instance.ChangeSlot(m_Class, m_IsMale, m_SlotState);
        }

        private void SetSlotState(CharSelectData.SlotState newSlotState)
        {
            m_SlotState = newSlotState;
            SetButtonInteractibleness();
            ClientCharSelectState.Instance.ChangeSlot(m_Class, m_IsMale, m_SlotState);
            if (newSlotState == CharSelectData.SlotState.LOCKEDIN)
            {
                m_InSceneCharacter.PerformUIGesture(Visual.ModelAppearanceSetter.UIGesture.LockedIn);
            }
        }

        private void SetGender(bool newIsMale)
        {
            m_IsMale = newIsMale;
            m_InSceneCharacter.SetModel(m_Class, m_IsMale);
            m_InSceneCharacter.PerformUIGesture(Visual.ModelAppearanceSetter.UIGesture.Selected);
            SetButtonInteractibleness();
            ClientCharSelectState.Instance.ChangeSlot(m_Class, m_IsMale, m_SlotState);
        }

        private void SetButtonInteractibleness()
        {
            foreach (var btn in m_ClassButtons.Values)
            {
                btn.interactable = true;
            }
            m_ClassButtons[ m_Class ].interactable = false;

            m_GenderButtonMale.interactable = !m_IsMale;
            m_GenderButtonFemale.interactable = m_IsMale;
        }

        #region UI Event callbacks (these are directly called by the UI elements in CharSelect scene)
        public void OnClickClassButtonTank()
        {
            SetClass(CharacterTypeEnum.TANK);
        }

        public void OnClickClassButtonCaster()
        {
            SetClass(CharacterTypeEnum.MAGE);
        }

        public void OnClickClassButtonRogue()
        {
            SetClass(CharacterTypeEnum.ROGUE);
        }

        public void OnClickClassButtonArcher()
        {
            SetClass(CharacterTypeEnum.ARCHER);
        }

        public void OnClickLock()
        {
            SetSlotState(CharSelectData.SlotState.LOCKEDIN);
        }

        public void OnClickMale()
        {
            SetGender(true);
        }

        public void OnClickFemale()
        {
            SetGender(false);
        }
        #endregion

        #region networking callbacks
        private void OnAssignedLobbyIndex(int index)
        {
            m_PlayerNumberText.text = string.Format(m_WelcomeMsg, (index + 1));
            SetClass(CharacterTypeEnum.TANK);
        }

        private void OnCharSelectSlotChanged(int slotIdx, CharSelectData.CharSelectSlot slot)
        {
            m_PlayerStateBoxes[ slotIdx ].SetClassAndState(slot.Class, slot.State);
        }

        private void OnLobbyLockedIn()
        {
            foreach (var go in m_HideTheseUIElementsWhenLobbyIsLockedIn)
            {
                go.SetActive(false);
            }
            foreach (var go in m_ShowTheseUIElementsWhenLobbyIsLockedIn)
            {
                go.SetActive(true);
            }
        }
        #endregion

    }

}

