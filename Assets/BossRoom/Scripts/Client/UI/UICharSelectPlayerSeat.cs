using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Client
{
    /// <summary>
    /// Controls one of the eight "seats" on the character-select screen (the boxes along the bottom).
    /// </summary>
    public class UICharSelectPlayerSeat : MonoBehaviour
    {
        [SerializeField]
        GameObject m_InactiveStateVisuals;
        [SerializeField]
        GameObject m_ActiveStateVisuals;
        [SerializeField]
        Image m_PlayerNumberHolder;
        [SerializeField]
        Text m_PlayerNameHolder;
        [SerializeField]
        Image m_Glow;
        [SerializeField]
        Image m_Checkbox;
        [SerializeField]
        Button m_Button;
        [SerializeField]
        Animator m_Animator;
        [SerializeField]
        string m_AnimatorTriggerWhenLockedIn = "LockedIn";

        // just a way to designate which seat we are -- the leftmost seat on the lobby UI is index 0, the next one is index 1, etc.
        int m_SeatIndex;

        // playerNumber of who is sitting in this seat right now. 0-based; e.g. this is 0 for Player 1, 1 for Player 2, etc. Meaningless when m_State is Inactive (and in that case it is set to -1 for clarity)
        int m_PlayerNumber;

        // the last SeatState we were assigned
        CharSelectData.SeatState m_State;

        // once this is true, we're never clickable again!
        bool m_IsPermanentlyDisabled;

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

        public void PermanentlyDisableInteraction()
        {
            m_Button.interactable = false;
            m_IsPermanentlyDisabled = true;
        }

        void ConfigureStateGraphics()
        {
            if (m_State == CharSelectData.SeatState.Inactive)
            {
                m_InactiveStateVisuals.SetActive(true);
                m_ActiveStateVisuals.SetActive(false);
                m_Glow.gameObject.SetActive(false);
                m_Checkbox.gameObject.SetActive(false);
                m_PlayerNameHolder.gameObject.SetActive(false);
                m_Button.interactable = m_IsPermanentlyDisabled ? false : true;
            }
            else // either active or locked-in... these states are visually very similar
            {
                m_InactiveStateVisuals.SetActive(false);
                m_PlayerNumberHolder.sprite = ClientCharSelectState.Instance.IdentifiersForEachPlayerNumber[m_PlayerNumber].Indicator;
                m_ActiveStateVisuals.SetActive(true);
                m_Glow.gameObject.SetActive(false);
                m_Checkbox.gameObject.SetActive(false);
                m_PlayerNameHolder.gameObject.SetActive(true);
                m_PlayerNameHolder.color = ClientCharSelectState.Instance.IdentifiersForEachPlayerNumber[m_PlayerNumber].Color;
                m_Button.interactable = m_IsPermanentlyDisabled ? false : true;

                if (m_State == CharSelectData.SeatState.LockedIn)
                {
                    m_Glow.color = ClientCharSelectState.Instance.IdentifiersForEachPlayerNumber[m_PlayerNumber].Color;
                    m_Glow.gameObject.SetActive(true);
                    m_Checkbox.gameObject.SetActive(true);
                    m_Button.interactable = false;
                    if (m_Animator)
                        m_Animator.SetTrigger(m_AnimatorTriggerWhenLockedIn);
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
