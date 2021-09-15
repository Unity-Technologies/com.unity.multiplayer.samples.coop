using UnityEngine;
using TMPro;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// This is responsible for displaying and updating the player's chosen name.  Currently, the game does not allow just any
    /// input for a name, but instead creates a randomized 2 word combination for the player.  The player is then able to click randomize
    /// to receive a new name
    /// </summary>
    public class NameDisplay : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI m_CurrentName;

        /// <summary>
        /// This is where we pull our name data from
        /// </summary>
        [SerializeField]
        private NameGenerationData m_NameData;

        public void Start()
        {
            ChooseNewName();
        }

        public string GetCurrentName()
        {
            return m_CurrentName.text;
        }

        /// <summary>
        /// Called to randomly select a new name for the player and displays it.
        /// </summary>
        public void ChooseNewName()
        {
            var firstWord = m_NameData.FirstWordList[Random.Range(0, m_NameData.FirstWordList.Length - 1)];
            var secondWord = m_NameData.SecondWordList[Random.Range(0, m_NameData.SecondWordList.Length - 1)];

            m_CurrentName.text = firstWord + " " + secondWord;
        }
    }
}
