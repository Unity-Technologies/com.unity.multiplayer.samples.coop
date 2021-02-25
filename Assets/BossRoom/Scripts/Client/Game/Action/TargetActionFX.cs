using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// The TargetActionFX runs persistently on the local player, and will attach target reticules to the player's active target. 
    /// </summary>
    public class TargetActionFX : ActionFX
    {
        private GameObject m_TargetReticule;
        private ulong m_CurrentTarget;
        private NetworkCharacterState m_ParentState;

        private const float k_ReticuleGroundHeight = 0.2f;

        public TargetActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent)
        {
        }

        public override bool Start()
        {
            m_ParentState = m_Parent.Parent.GetComponent<NetworkCharacterState>();
            return true;
        }

        public override bool Update()
        {
            if( m_CurrentTarget != m_ParentState.TargetId.Value )
            {
                m_CurrentTarget = m_ParentState.TargetId.Value;
                if (MLAPI.Spawning.SpawnManager.SpawnedObjects.TryGetValue(m_CurrentTarget, out MLAPI.NetworkedObject targetObject ) )
                {
                    var targetChar = targetObject.GetComponent<BossRoom.Client.ClientCharacter>();
                    if( targetChar != null )
                    {
                        ValidateReticule(targetObject);
                        m_TargetReticule.SetActive(true);
                        m_TargetReticule.transform.parent = targetChar.ChildVizObject.transform; //attach to the GRAPHICS GameObject of the target. 
                        m_TargetReticule.transform.localPosition = new Vector3(0, k_ReticuleGroundHeight, 0);
                    }

                }
                else
                {
                    m_TargetReticule.transform.parent = null;
                    m_TargetReticule.SetActive(false);
                }
            }

            return true;
        }

        /// <summary>
        /// Ensures that the TargetReticule GameObject exists. This must be done prior to enabling it because it can be destroyed
        /// "accidentally" if its parent is destroyed while it is detached. 
        /// </summary>
        private void ValidateReticule(MLAPI.NetworkedObject targetObject)
        {
            if( m_TargetReticule == null )
            {
                m_TargetReticule = Object.Instantiate(m_Parent.TargetReticule);
            }

            bool target_isnpc = targetObject.GetComponent<NetworkCharacterState>().CharacterData.IsNpc;
            bool myself_isnpc = m_ParentState.CharacterData.IsNpc;
            bool hostile = target_isnpc != myself_isnpc;

            m_TargetReticule.GetComponent<MeshRenderer>().material = hostile ? m_Parent.ReticuleHostileMat : m_Parent.ReticuleFriendlyMat;
        }


        public override void Cancel()
        {
            GameObject.Destroy(m_TargetReticule);
        }

    }
}
