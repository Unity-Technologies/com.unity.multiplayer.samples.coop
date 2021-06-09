using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// A runtime list of <see cref="ServerCharacter"/> objects that is populated only on the server.
    /// </summary>
    [CreateAssetMenu]
    public class ServerCharacterRuntimeCollection : RuntimeCollection<ServerCharacter>
    {

    }
}
