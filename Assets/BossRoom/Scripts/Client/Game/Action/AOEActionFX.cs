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

    }

    public override bool Update()
    {
        return false;
    }
}
