using BossRoom;
using BossRoom.Server;
using MLAPI;
using MLAPI.Spawning;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Area-of-effect attack Action. The attack is centered on a point provided by the client.
/// </summary>
public class AoeAction : Action
{
    /// <summary>
    /// Cheat prevention: to ensure that players don't perform AoEs outside of their attack range,
    /// we ensure that the target is less than Range meters away from the player, plus this "fudge
    /// factor" to accomodate miscellaneous minor movement.
    /// </summary>
    const float k_MaxDistanceDivergence = 1;

    bool m_DidAoE;

    public AoeAction(ServerCharacter parent, ref ActionRequestData data)
        : base(parent, ref data) { }

    public override bool Start()
    {
        float distanceAway = Vector3.Distance(m_Parent.transform.position, Data.Position);
        if (distanceAway > Description.Range+k_MaxDistanceDivergence)
        {
            Debug.LogError($"Hacking detected?! (Object ID {m_Parent.NetworkObjectId}) {Description.ActionTypeEnum}'s AoE range is {Description.Range} but we were given a point that's {distanceAway} away from us. Canceling AoE");
            return ActionConclusion.Stop;
        }

        // broadcasting to all players including myself.
        // We don't know our actual targets for this attack until it triggers, so the client can't use the TargetIds list (and we clear it out for clarity).
        // This means we are responsible for triggering reaction-anims ourselves, which we do in PerformAoe()
        Data.TargetIds = new ulong[0];
        m_Parent.NetState.RecvDoActionClientRPC(Data);
        return ActionConclusion.Continue;
    }

    public override bool Update()
    {
        if (TimeRunningSeconds >= Description.ExecTimeSeconds && !m_DidAoE)
        {
            // actually perform the AoE attack
            m_DidAoE = true;
            PerformAoE();
        }
        return ActionConclusion.Continue;
    }

    private void PerformAoE()
    {
        // Note: could have a non alloc version of this overlap sphere where we statically store our collider array, but since this is a self
        // destroyed object, the complexity added to have a static pool of colliders that could be called by multiplayer players at the same time
        // doesn't seem worth it for now.
        var colliders = Physics.OverlapSphere(m_Data.Position, Description.Radius, LayerMask.GetMask("NPCs"));
        for (var i = 0; i < colliders.Length; i++)
        {
            var enemy = colliders[i].GetComponent<IDamageable>();
            if (enemy != null)
            {
                // make the target "flinch", assuming they're a living enemy
                var networkObject = NetworkSpawnManager.SpawnedObjects[enemy.NetworkObjectId];
                if (networkObject)
                {
                    var state = networkObject.GetComponent<NetworkCharacterState>();
                    if (state)
                    {
                        state.RecvPerformHitReactionClientRPC();
                    }
                }

                // actually deal the damage
                enemy.ReceiveHP(m_Parent, -Description.Amount);
            }
        }
    }
}
