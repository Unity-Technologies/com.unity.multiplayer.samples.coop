using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// The TargetActionFX runs persistently on the local player, and will attach target reticules to the player's active target.
    /// </summary>
    public class TargetActionFX : ActionFX
    {
        private GameObject m_TargetReticule;
        private ulong m_CurrentTarget;
        private ulong m_NewTarget;
        private NetworkCharacterState m_ParentState;

        private const float k_ReticuleGroundHeight = 0.2f;

        public TargetActionFX(ref ActionRequestData data, ClientCharacterVisualization clientParent) : base(ref data, clientParent)
        {
        }

        public override bool OnStartClient()
        {
            base.OnStartClient();
            m_ParentState = m_ClientParent.NetState;

            m_ParentState.TargetId.OnValueChanged += OnTargetChanged;
            m_ParentState.GetComponent<Client.ClientInputSender>().ActionInputEvent += OnActionInput;

            return true;
        }

        private void OnTargetChanged(ulong oldTarget, ulong newTarget)
        {
            m_NewTarget = newTarget;
        }

        private void OnActionInput(ActionRequestData data)
        {
            //this method runs on the owning client, and allows us to anticipate our new target for purposes of FX visualization.
            if (data.ActionTypeEnum == ActionType.GeneralTarget)
            {
                m_NewTarget = data.TargetIds[0];
            }
        }

        public override bool OnUpdateClient()
        {
            if (m_CurrentTarget != m_NewTarget)
            {
                m_CurrentTarget = m_NewTarget;

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_CurrentTarget, out NetworkObject targetObject))
                {
                    var targetEntity = targetObject != null ? targetObject.GetComponent<ITargetable>() : null;
                    if (targetEntity != null)
                    {
                        ValidateReticule(targetObject);
                        m_TargetReticule.SetActive(true);

                        var parentTransform = targetObject.transform;
                        if (targetObject.TryGetComponent(out Client.ClientCharacter clientCharacter) && clientCharacter.ChildVizObject)
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
            if (m_TargetReticule == null)
            {
                m_TargetReticule = Object.Instantiate(m_ClientParent.TargetReticulePrefab);
            }

            bool target_isnpc = targetObject.GetComponent<ITargetable>().IsNpc;
            bool myself_isnpc = m_ParentState.CharacterClass.IsNpc;
            bool hostile = target_isnpc != myself_isnpc;

            m_TargetReticule.GetComponent<MeshRenderer>().material = hostile ? m_ClientParent.ReticuleHostileMat : m_ClientParent.ReticuleFriendlyMat;
        }


        public override void CancelClient()
        {
            GameObject.Destroy(m_TargetReticule);

            m_ParentState.TargetId.OnValueChanged -= OnTargetChanged;
            if (m_ParentState.TryGetComponent(out Client.ClientInputSender inputSender))
            {
                inputSender.ActionInputEvent -= OnActionInput;
            }
        }

    }
}
