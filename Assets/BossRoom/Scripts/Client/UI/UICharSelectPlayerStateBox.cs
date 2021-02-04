using UnityEngine;

namespace BossRoom.Client
{
    public class UICharSelectPlayerStateBox : MonoBehaviour
    {
        [SerializeField]
        private UICharSelectPlayerStateBoxSettings m_StateInactive;
        [SerializeField]
        private UICharSelectPlayerStateBoxSettings m_StateTankActive;
        [SerializeField]
        private UICharSelectPlayerStateBoxSettings m_StateTankLockedIn;
        [SerializeField]
        private UICharSelectPlayerStateBoxSettings m_StateMageActive;
        [SerializeField]
        private UICharSelectPlayerStateBoxSettings m_StateMageLockedIn;
        [SerializeField]
        private UICharSelectPlayerStateBoxSettings m_StateRogueActive;
        [SerializeField]
        private UICharSelectPlayerStateBoxSettings m_StateRogueLockedIn;
        [SerializeField]
        private UICharSelectPlayerStateBoxSettings m_StateArcherActive;
        [SerializeField]
        private UICharSelectPlayerStateBoxSettings m_StateArcherLockedIn;
        [SerializeField]
        [Tooltip("Label shown in each character-slot in the lobby. {0} will be replaced with that slot's number")]
        private string m_SeatNumberMsg = "P{0}";

        private int m_PlayerIndex; // 0-based; e.g. this is 0 for Player 1, 1 for Player 2, etc.
        private CharacterTypeEnum m_CharacterClass;
        private CharSelectData.SlotState m_State;
        private UICharSelectPlayerStateBoxSettings m_CurStateGraphics;

        public void SetPlayerSlotIndex(int idx)
        {
            m_PlayerIndex = idx;
            ChooseAndUpdateState();
        }

        public void SetClassAndState(CharacterTypeEnum playerClass, CharSelectData.SlotState state)
        {
            m_CharacterClass = playerClass;
            m_State = state;
            ChooseAndUpdateState();
        }

        private void ChooseAndUpdateState()
        {
            if (m_State == CharSelectData.SlotState.Inactive)
            {
                UpdateState(m_StateInactive);
            }
            else if (m_State == CharSelectData.SlotState.Active)
            {
                switch (m_CharacterClass)
                {
                    case CharacterTypeEnum.Tank:
                        UpdateState(m_StateTankActive);
                        break;
                    case CharacterTypeEnum.Mage:
                        UpdateState(m_StateMageActive);
                        break;
                    case CharacterTypeEnum.Rogue:
                        UpdateState(m_StateRogueActive);
                        break;
                    case CharacterTypeEnum.Archer:
                        UpdateState(m_StateArcherActive);
                        break;
                    default:
                        Debug.LogError("UI can't handle class " + m_CharacterClass);
                        break;
                }
            }
            else if (m_State == CharSelectData.SlotState.LockedIn)
            {
                switch (m_CharacterClass)
                {
                    case CharacterTypeEnum.Tank:
                        UpdateState(m_StateTankLockedIn);
                        break;
                    case CharacterTypeEnum.Mage:
                        UpdateState(m_StateMageLockedIn);
                        break;
                    case CharacterTypeEnum.Rogue:
                        UpdateState(m_StateRogueLockedIn);
                        break;
                    case CharacterTypeEnum.Archer:
                        UpdateState(m_StateArcherLockedIn);
                        break;
                    default:
                        Debug.LogError("UI can't handle class " + m_CharacterClass);
                        break;
                }
            }
        }

        private void UpdateState(UICharSelectPlayerStateBoxSettings stateGraphics)
        {
            if (m_CurStateGraphics != null)
            {
                m_CurStateGraphics.GraphicsRoot.SetActive(false);
            }
            m_CurStateGraphics = stateGraphics;
            m_CurStateGraphics.GraphicsRoot.SetActive(true);
            if (m_State == CharSelectData.SlotState.Inactive)
                m_CurStateGraphics.PlayerIndexText.text = "";
            else
                m_CurStateGraphics.PlayerIndexText.text = string.Format(m_SeatNumberMsg, (m_PlayerIndex + 1));
        }
    }
}
