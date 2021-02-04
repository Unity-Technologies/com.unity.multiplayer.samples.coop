using MLAPI.NetworkedVar.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Client
{
    public class UICharSelectController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Reference to dummy character model in the char-select screen")]
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
        private List<GameObject> m_HideTheseUIElementsOnFatalLobbyError;
        [SerializeField]
        private List<GameObject> m_ShowTheseUIElementsOnFatalLobbyError;
        [SerializeField]
        private Text m_FatalLobbyErrorText;
        [SerializeField]
        [Tooltip("Error shown when lobby is full")]
        private string m_FatalErrorLobbyFullMsg = "Error: lobby is full! You cannot play.";

        [SerializeField]
        [Tooltip("Message shown in the char-gen screen. {0} will be replaced with the player's seat number")]
        private string m_WelcomeMsg = "Welcome, P{0}!";

        private Dictionary<CharacterTypeEnum, Button> m_ClassButtons;
        private CharacterTypeEnum m_Class;
        private CharSelectData.SlotState m_SlotState;
        private bool m_IsMale;

        private void Awake()
        {
            m_ClassButtons = new Dictionary<CharacterTypeEnum, Button>
            {
                [CharacterTypeEnum.Tank] = m_ClassButtonTank,
                [CharacterTypeEnum.Mage] = m_ClassButtonMage,
                [CharacterTypeEnum.Rogue] = m_ClassButtonRogue,
                [CharacterTypeEnum.Archer] = m_ClassButtonArcher,
            };
        }

        private void Start()
        {
            m_Class = CharacterTypeEnum.Tank;
            m_IsMale = true;

            SetupStateBoxes();
            if (ClientCharSelectState.Instance == null)
                Debug.LogError("No CharSelectData in scene!");
            ClientCharSelectState.Instance.CharSelectData.OnAssignedLobbyIndex += OnAssignedLobbyIndex;
            ClientCharSelectState.Instance.CharSelectData.CharacterSlots.OnListChanged += OnCharSelectSlotChanged;
            ClientCharSelectState.Instance.CharSelectData.IsLobbyLocked.OnValueChanged += OnLobbyLockedInChanged;
            ClientCharSelectState.Instance.CharSelectData.OnFatalLobbyError += OnFatalLobbyError;

            m_InSceneCharacter.SetModel(m_Class, m_IsMale);
            SetButtonInteractibleness();
        }

        private void OnDestroy()
        {
            if (ClientCharSelectState.Instance)
            {
                ClientCharSelectState.Instance.CharSelectData.OnAssignedLobbyIndex -= OnAssignedLobbyIndex;
                ClientCharSelectState.Instance.CharSelectData.CharacterSlots.OnListChanged -= OnCharSelectSlotChanged;
                ClientCharSelectState.Instance.CharSelectData.IsLobbyLocked.OnValueChanged -= OnLobbyLockedInChanged;
                ClientCharSelectState.Instance.CharSelectData.OnFatalLobbyError -= OnFatalLobbyError;
            }
        }

        private void SetupStateBoxes()
        {
            for (int i = 0; i < m_PlayerStateBoxes.Count && i < CharSelectData.k_MaxLobbyPlayers; ++i)
            {
                m_PlayerStateBoxes[i].SetPlayerSlotIndex(i);
                var slotData = ClientCharSelectState.Instance.CharSelectData.CharacterSlots[i];
                m_PlayerStateBoxes[i].SetClassAndState(slotData.Class, slotData.State);
            }
        }

        private void SetClass(CharacterTypeEnum newClass)
        {
            // TODO: these labels will be pulled from class data?
            string className = "";
            switch (newClass)
            {
                case CharacterTypeEnum.Tank:
                    className = "TANK";
                    break;
                case CharacterTypeEnum.Mage:
                    className = "MAGE";
                    break;
                case CharacterTypeEnum.Rogue:
                    className = "ROGUE";
                    break;
                case CharacterTypeEnum.Archer:
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
            m_ClassButtons[m_Class].interactable = false;

            m_GenderButtonMale.interactable = !m_IsMale;
            m_GenderButtonFemale.interactable = m_IsMale;
        }


        // UI Event callbacks (these are directly called by the UI elements in CharSelect scene)
        public void OnClickClassButtonTank()
        {
            SetClass(CharacterTypeEnum.Tank);
        }

        public void OnClickClassButtonCaster()
        {
            SetClass(CharacterTypeEnum.Mage);
        }

        public void OnClickClassButtonRogue()
        {
            SetClass(CharacterTypeEnum.Rogue);
        }

        public void OnClickClassButtonArcher()
        {
            SetClass(CharacterTypeEnum.Archer);
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


        // networking callbacks
        private void OnAssignedLobbyIndex(int index)
        {
            m_PlayerNumberText.text = string.Format(m_WelcomeMsg, (index + 1));
            SetClass(CharacterTypeEnum.Tank);
        }

        private void OnCharSelectSlotChanged(NetworkedListEvent<CharSelectData.CharSelectSlot> changeEvent)
        {
            m_PlayerStateBoxes[changeEvent.index].SetClassAndState(changeEvent.value.Class, changeEvent.value.State);
        }

        private void OnLobbyLockedInChanged(bool wasLockedIn, bool isLockedIn)
        {
            foreach (var go in m_HideTheseUIElementsWhenLobbyIsLockedIn)
            {
                go.SetActive(!isLockedIn);
            }
            foreach (var go in m_ShowTheseUIElementsWhenLobbyIsLockedIn)
            {
                go.SetActive(isLockedIn);
            }
        }

        private void OnLobbyLockedIn()
        {
        }

        private void OnFatalLobbyError(CharSelectData.FatalLobbyError error)
        {
            foreach (var go in m_HideTheseUIElementsOnFatalLobbyError)
            {
                go.SetActive(false);
            }
            foreach (var go in m_ShowTheseUIElementsOnFatalLobbyError)
            {
                go.SetActive(true);
            }

            switch (error)
            {
                case CharSelectData.FatalLobbyError.LOBBY_FULL:
                    m_FatalLobbyErrorText.text = m_FatalErrorLobbyFullMsg;
                    break;
                default:
                    Debug.LogError("Unknown fatal error " + error);
                    break;
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_PlayerStateBoxes.Count != CharSelectData.k_MaxLobbyPlayers)
            {
                Debug.LogError("There should be exactly " + CharSelectData.k_MaxLobbyPlayers + " entries in the Player State Boxes list");
            }
            for (int i = 0; i < m_PlayerStateBoxes.Count; ++i)
            {
                if (m_PlayerStateBoxes[i] == null)
                {
                    Debug.LogError("Entry index " + i + " Player State Boxes list is null");
                }
            }
            if (!m_InSceneCharacter)
            {
                Debug.LogError("In Scene Character not set!");
            }
        }
#endif

    }

}

