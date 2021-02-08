using System;
using UnityEngine;

namespace BossRoom.Visual
{
    /// Final step in the AoE action flow. Please see AoEActionInput for the first step and more details on overall flow
    public class AOEActionFX : ActionFX
    {
        public AOEActionFX(ref ActionRequestData data, ClientCharacterVisualization parent)
            : base(ref data, parent) { }

        public override bool Start()
        {
            m_Parent.OurAnimator.SetTrigger(Description.Anim);
            var actionDescription = GameDataSource.Instance.ActionDataByType[m_Data.ActionTypeEnum];
            var vfxObject = GameObject.Instantiate(actionDescription.Spawns[0], m_Data.Position, Quaternion.identity);
            vfxObject.transform.localScale = new Vector3(actionDescription.Radius, actionDescription.Radius, actionDescription.Radius);
            return ActionConclusion.Stop;
        }

        public override bool Update()
        {
            throw new Exception("This should not execute");
        }
    }
}
