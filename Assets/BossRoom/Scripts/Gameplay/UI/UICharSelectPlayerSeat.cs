using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Controls one of the eight "seats" on the character-select screen (the boxes along the bottom).
    /// </summary>
    public class UICharSelectPlayerSeat : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_InactiveStateVisuals;
        [SerializeField]
        private GameObject m_ActiveStateVisuals;
        [SerializeField]
        private Image m_PlayerNumberHolder;
        [SerializeField]
        private TextMeshProUGUI m_PlayerNameHolder;
        [SerializeField]
        private Image m_Glow;
        [SerializeField]
        private Image m_Checkbox;
        [SerializeField]
        private Button m_Button;
        [SerializeField]
        private Animator m_Animator;
        [SerializeField]
        private string m_AnimatorTriggerWhenLockedIn = "LockedIn";
        [SerializeField]
        private string m_AnimatorTriggerWhenUnlocked = "Unlocked";

        [SerializeField]
        private CharacterTypeEnum m_CharacterClass;

        // just a way to designate which seat we are -- the leftmost seat on the lobby UI is index 0, the next one is index 1, etc.
        private int m_SeatIndex;

        // playerNumber of who is sitting in this seat right now. 0-based; e.g. this is 0 for Player 1, 1 for Player 2, etc. Meaningless when m_State is Inactive (and in that case it is set to -1 for clarity)
        private int m_PlayerNumber;

        // the last SeatState we were assigned
        private CharSelectData.SeatState m_State;

        // once this is true, we're never clickable again!
        private bool m_IsDisabled;

        public void Initialize(int seatIndex)
        {
            m_SeatIndex = seatIndex;
            m_State = CharSelectData.SeatState.Inactive;
            m_PlayerNumber = -1;
            ConfigureStateGraphics();
        }

        public void SetState(CharSelectData.SeatState state, int playerIndex, string playerName)
        {
            if (state == m_State && playerIndex == m_PlayerNumber)
                return; // no actual changes

            m_State = state;
            m_PlayerNumber = playerIndex;
            m_PlayerNameHolder.text = playerName;
            if (m_State == CharSelectData.SeatState.Inactive)
                m_PlayerNumber = -1;
            ConfigureStateGraphics();
        }

        public bool IsLocked()
        {
            return m_State == CharSelectData.SeatState.LockedIn;
        }

        public void SetDisableInteraction(bool disable)
        {
            m_Button.interactable = !disable;
            m_IsDisabled = disable;

            if (!disable)
            {
                // if we were locked move to unlocked state
                PlayUnlockAnim();
            }
        }

        private void PlayLockAnim()
        {
            if (m_Animator)
            {
                m_Animator.ResetTrigger(m_AnimatorTriggerWhenUnlocked);
                m_Animator.SetTrigger(m_AnimatorTriggerWhenLockedIn);
            }
        }

        private void PlayUnlockAnim()
        {
            if (m_Animator)
            {
                m_Animator.ResetTrigger(m_AnimatorTriggerWhenLockedIn);
                m_Animator.SetTrigger(m_AnimatorTriggerWhenUnlocked);
            }
        }

        private void ConfigureStateGraphics()
        {
            if (m_State == CharSelectData.SeatState.Inactive)
            {
                m_InactiveStateVisuals.SetActive(true);
                m_ActiveStateVisuals.SetActive(false);
                m_Glow.gameObject.SetActive(false);
                m_Checkbox.gameObject.SetActive(false);
                m_PlayerNameHolder.gameObject.SetActive(false);
                m_Button.interactable = m_IsDisabled ? false : true;
                PlayUnlockAnim();
            }
            else // either active or locked-in... these states are visually very similar
            {
                m_InactiveStateVisuals.SetActive(false);
                m_PlayerNumberHolder.sprite = ClientCharSelectState.Instance.m_IdentifiersForEachPlayerNumber[m_PlayerNumber].Indicator;
                m_ActiveStateVisuals.SetActive(true);

                m_PlayerNameHolder.gameObject.SetActive(true);
                m_PlayerNameHolder.color = ClientCharSelectState.Instance.m_IdentifiersForEachPlayerNumber[m_PlayerNumber].Color;
                m_Button.interactable = m_IsDisabled ? false : true;

                if (m_State == CharSelectData.SeatState.LockedIn)
                {
                    m_Glow.color = ClientCharSelectState.Instance.m_IdentifiersForEachPlayerNumber[m_PlayerNumber].Color;
                    m_Glow.gameObject.SetActive(true);
                    m_Checkbox.gameObject.SetActive(true);
                    m_Button.interactable = false;
                    PlayLockAnim();
                }
                else
                {
                    m_Glow.gameObject.SetActive(false);
                    m_Checkbox.gameObject.SetActive(false);
                    PlayUnlockAnim();
                }
            }
        }

        // Called directly by Button in UI
        public void OnClicked()
        {
            ClientCharSelectState.Instance.OnPlayerClickedSeat(m_SeatIndex);
        }

    }
}
