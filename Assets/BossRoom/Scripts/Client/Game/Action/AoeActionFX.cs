using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// Final step in the AoE action flow. Please see AoEActionInput for the first step and more details on overall flow
    public class AoeActionFX : ActionFX
    {
        public AoeActionFX(ref ActionRequestData data, ClientCharacterVisualization parent)
            : base(ref data, parent) { }

        public override bool Start()
        {
            base.Start();
            GameObject.Instantiate(Description.Spawns[0], m_Data.Position, Quaternion.identity);
            return ActionConclusion.Stop;
        }

        public override bool Update()
        {
            throw new Exception("This should not execute");
        }
    }
}
