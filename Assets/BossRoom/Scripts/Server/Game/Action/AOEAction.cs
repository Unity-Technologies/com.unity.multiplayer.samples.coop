using BossRoom;
using BossRoom.Server;
using UnityEngine;

public class AOEAction : Action
{
    public AOEAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
    {
    }

    public override bool Start()
    {
        var actionDescription = ActionData.ActionDescriptions[m_Data.ActionTypeEnum][0];

        // Note: could have a non alloc version of this overlap sphere where we statically store our collider array, but since this is a self
        // destroyed object, the complexity added doesn't seem worth it for now.
        var colliders = Physics.OverlapSphere(m_Data.Position, actionDescription.Radius, LayerMask.GetMask("NPCs"));
        m_Data.TargetIds = new ulong[colliders.Length]; // we reset target IDs here, so clients can't set their own target ID list.
        for (int i = 0; i < colliders.Length; i++)
        {
            var enemy = colliders[i].GetComponent<ServerCharacter>();
            enemy.ReceiveHP(m_Parent, -actionDescription.Amount);
            m_Data.TargetIds[i++] = enemy.NetworkId;
        }
        // broadcasting to all players including myself. Client is authoritative on input, but not on the actual gameplay effect of that input.
        m_Parent.NetState.ServerBroadcastAction(ref Data);
        return ActionConclusion.Stop;
    }

    public override bool Update()
    {
        return ActionConclusion.Stop;
    }
}
