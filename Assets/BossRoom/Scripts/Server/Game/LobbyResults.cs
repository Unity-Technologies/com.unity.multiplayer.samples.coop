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
        public struct CharSelectChoice
        {
            public CharacterTypeEnum Class;
            public bool IsMale;
            public CharSelectChoice(CharacterTypeEnum Class, bool IsMale)
            {
                this.Class = Class;
                this.IsMale = IsMale;
            }

            /// <summary>
            /// Return the Appearance integer for this CharSelect choice. This number has meaning to the CharacterSwap script that contains the
            /// arrays of different appearance options. 
            /// </summary>
            public int GetAppearance()
            {
                switch (this.Class)
                {
                    case CharacterTypeEnum.Archer: return IsMale ? 0 : 1;
                    case CharacterTypeEnum.Mage: return IsMale ? 2 : 3;
                    case CharacterTypeEnum.Rogue: return IsMale ? 4 : 5;
                    case CharacterTypeEnum.Tank: return IsMale ? 6 : 7;
                    default: throw new System.NotImplementedException("don't recognize character class: " + this.Class);
                }
            }
        }
        public readonly Dictionary<ulong, CharSelectChoice> Choices = new Dictionary<ulong, CharSelectChoice>();
    }
}
