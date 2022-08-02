using System;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// Final step in the AoE action flow. Please see AoEActionInput for the first step and more details on overall flow
    public class AoeActionFX : ActionFX
    {
        public AoeActionFX(ref ActionRequestData data, ClientCharacterVisualization clientParent)
            : base(ref data, clientParent) { }

        public override bool OnStartClient()
        {
            base.OnStartClient();
            GameObject.Instantiate(c_Description.Spawns[0], m_CData.Position, Quaternion.identity);
            return ActionConclusion.Stop;
        }

        public override bool OnUpdateClient()
        {
            throw new Exception("This should not execute");
        }
    }
}
