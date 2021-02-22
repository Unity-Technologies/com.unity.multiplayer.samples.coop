using System.Collections.Generic;

namespace BossRoom.Server
{
    /// <summary>
    /// Simple data-storage of the choices made in the lobby screen for all players
    /// in the lobby. This object is passed from the lobby scene to the gameplay
    /// scene, so that the game knows how to set up the players' characters.
    /// </summary>
    public class LobbyResults
    {
        public readonly Dictionary<ulong, CharSelectChoice> Choices = new Dictionary<ulong, CharSelectChoice>();

        public struct CharSelectChoice
        {
            public int PlayerNumber;
            public CharacterTypeEnum Class;
            public int Appearance;

            public CharSelectChoice(int playerNumber, CharacterTypeEnum characterClass, int appearanceIdx)
            {
                PlayerNumber = playerNumber;
                Class = characterClass;
                Appearance = appearanceIdx;
            }
        }
    }
}
