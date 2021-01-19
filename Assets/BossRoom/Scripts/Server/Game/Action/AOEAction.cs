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
        foreach (var detectedImp in colliders)
        {
            var enemy = detectedImp.GetComponent<ServerCharacter>();
            enemy.ReceiveHP(m_Parent, -actionDescription.Amount);
        }
        return ActionConclusion.Stop;
    }

    public override bool Update()
    {
        return ActionConclusion.Stop;
    }
}
