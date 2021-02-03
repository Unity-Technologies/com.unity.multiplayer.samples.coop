using UnityEngine;

namespace BossRoom.Server
{
    public class EmoteAction : Action
    {
        public EmoteAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        /// <summary>
        /// Called when the Action starts actually playing (which may be after it is created, because of queueing).
        /// </summary>
        /// <returns>false if the action decided it doesn't want to run after all, true otherwise. </returns>
        public override bool Start()
        {
            m_Parent.NetState.ServerBroadcastAction(ref Data);
            return true;
        }

        /// <summary>
        /// Called each frame while the action is running.
        /// </summary>
        /// <returns>true to keep running, false to stop. The Action will stop by default when its duration expires, if it has a duration set. </returns>
        public override bool Update()
        {
            return true;
        }
    }
}
