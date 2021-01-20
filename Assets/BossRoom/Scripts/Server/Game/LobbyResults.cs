using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossRoom.Server
{
    /// <summary>
    /// Records the results of the lobby screen: all the players' choices.
    /// This is used when setting up the in-game characters.
    /// (It's a singleton so that it persists beyond the char-select scene.)
    /// </summary>
    class LobbyResults
    {
        private static LobbyResults s_Instance;
        public static LobbyResults GetInstance()
        {
            if (s_Instance == null)
                s_Instance = new LobbyResults();
            return s_Instance;
        }

        public struct CharSelectChoice
        {
            public CharacterTypeEnum Class;
            public bool IsMale;
            public CharSelectChoice(CharacterTypeEnum Class, bool IsMale)
            {
                this.Class = Class;
                this.IsMale = IsMale;
            }
        }
        private Dictionary<ulong, CharSelectChoice> m_Choices = new Dictionary<ulong, CharSelectChoice>();

        public CharSelectChoice GetCharSelectChoiceForClient(ulong clientId)
        {
            CharSelectChoice returnValue;
            if (!m_Choices.TryGetValue(clientId, out returnValue))
            {
                // We don't know about this client ID! That probably means they joined the game late!
                // We don't yet handle this scenario (e.g. showing them a "wait for next game" screen, maybe?),
                // so for now we just let them join. We'll give them some generic char-gen choices.
                returnValue = new CharSelectChoice(CharacterTypeEnum.TANK, true);
                m_Choices.Add(clientId, returnValue);
            }
            return returnValue;
        }

        public void SetCharSelectChoiceForClient(ulong clientId, CharSelectChoice choices)
        {
            m_Choices[ clientId ] = choices;
        }

    }
}
