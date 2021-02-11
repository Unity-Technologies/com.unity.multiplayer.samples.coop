using System;

namespace BossRoom.Server
{
    /// <summary>
    /// Stores game-state information when switching between scenes. The ending
    /// scene calls SetRelayObject() on an arbitrary information object, and the
    /// new scene calls GetRelayObject() to retrieve it.
    /// </summary>
    public class GameStateRelay
    {
        private static Object s_RelayObject = null;

        /// <summary>
        /// Retrieves the last-set relay object and clears its reference to it.
        /// (Calling this again without setting a new relay object will return null!)
        /// </summary>
        public static Object GetRelayObject()
        {
            Object ret = s_RelayObject;
            s_RelayObject = null;
            return ret;
        }

        /// <summary>
        /// Stores a relay object to be retrieved in a later scene.
        /// </summary>
        /// <param name="o"></param>
        public static void SetRelayObject(Object o)
        {
            s_RelayObject = o;
        }

    }
}
