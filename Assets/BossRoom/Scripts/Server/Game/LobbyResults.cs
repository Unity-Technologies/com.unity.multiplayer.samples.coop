using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
        public readonly Dictionary<ulong, CharSelectChoice> Choices = new Dictionary<ulong, CharSelectChoice>();
    }
}
