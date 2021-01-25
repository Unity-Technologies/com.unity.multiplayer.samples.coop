using System;
using BossRoom;
using BossRoom.Visual;
using UnityEngine;

/*
 * Final step in the AoE action flow. Please see AoEActionInput for the first step and more details on overall flow
 */
public class AOEActionFX : ActionFX
{
    public AOEActionFX(ref ActionRequestData data, ClientCharacterVisualization parent)
        : base(ref data, parent) { }

    public override bool Start()
    {
        m_Parent.OurAnimator.SetTrigger(Description.Anim);
        var actionDescription = GameDataSource.Instance.ActionDataByType[m_Data.ActionTypeEnum];
        var VFXObject = GameObject.Instantiate(actionDescription.ActionVisualization, m_Data.Position, Quaternion.identity);
        VFXObject.transform.localScale = new Vector3(actionDescription.Radius, actionDescription.Radius, actionDescription.Radius);
        return ActionConclusion.Stop;
    }

    public override bool Update()
    {
        throw new Exception("This should not execute");
    }
}
