using System.Collections;
using System.Collections.Generic;
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

    public override void Start()
    {
        Debug.Log($"AoE action FX Start, affecting characters {m_Data.TargetIds}");
        m_Parent.OurAnimator.SetTrigger(Description.Anim);
    }

    public override bool Update()
    {
        Debug.Log("AoE action FX Update");
        return ActionConclusion.Stop;
    }
}
