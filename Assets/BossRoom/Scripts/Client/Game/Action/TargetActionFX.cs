using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Spawning;

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

                if (NetworkSpawnManager.SpawnedObjects.TryGetValue(m_CurrentTarget, out NetworkObject targetObject ) )
                {
                    var targetEntity = targetObject != null ? targetObject.GetComponent<ITargetable>() : null;
                    if( targetEntity != null )
                    {
                        ValidateReticule(targetObject);
                        m_TargetReticule.SetActive(true);

                        var parentTransform = targetObject.transform;
                        if( targetObject.TryGetComponent(out Client.ClientCharacter clientCharacter))
                        {
                            //for characters, attach the reticule to the child graphics object.
                            parentTransform = clientCharacter.ChildVizObject.transform;
                        }

                        m_TargetReticule.transform.parent = parentTransform;
                        m_TargetReticule.transform.localPosition = new Vector3(0, k_ReticuleGroundHeight, 0);
                    }

                }
                else
                {
                    // null check here in case the target was destroyed along with the target reticule
                    if (m_TargetReticule != null)
                    {
                        m_TargetReticule.transform.parent = null;
                        m_TargetReticule.SetActive(false);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Ensures that the TargetReticule GameObject exists. This must be done prior to enabling it because it can be destroyed
        /// "accidentally" if its parent is destroyed while it is detached.
        /// </summary>
        private void ValidateReticule(NetworkObject targetObject)
        {
            if( m_TargetReticule == null )
            {
                m_TargetReticule = Object.Instantiate(m_Parent.TargetReticule);
            }

            bool target_isnpc = targetObject.GetComponent<ITargetable>().IsNpc;
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
