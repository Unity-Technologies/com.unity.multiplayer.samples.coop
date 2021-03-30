using MLAPI;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// This script handles the logic for a simple "single-shot" breakable object like a pot.
    /// It could easily be extended to take multiple hits by giving it a hit point value, or made to only
    /// take damage from certain enemy types, by adding some filter variable as a serialized field.
    /// </summary>
    [RequireComponent(typeof(NetworkBreakableState))]
    public class ServerBreakableLogic : NetworkBehaviour, IDamageable
    {
        public override void NetworkStart()
        {
            if (!IsServer)
            {
                enabled = false;
            }
        }

        public void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            if (HP < 0)
            {
                //any damage at all is enough to slay me.
                GetComponent<NetworkBreakableState>().IsBroken.Value = true;

                //don't let us take another blow.
                GetComponent<Collider>().enabled = false;
            }
        }

    }


}

