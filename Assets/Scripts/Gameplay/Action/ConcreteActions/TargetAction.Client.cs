using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    public partial class TargetAction
    {
        private GameObject m_TargetReticule;
        private ulong m_CurrentTarget;
        private ulong m_NewTarget;
        private NetworkCharacterState m_ParentState;

        private const float k_ReticuleGroundHeight = 0.2f;

        public override bool OnStartClient(ClientCharacterVisualization parent)
        {
            base.OnStartClient(parent);
            m_ParentState = parent.NetState;

            m_ParentState.TargetId.OnValueChanged += OnTargetChanged;
            m_ParentState.GetComponent<Client.ClientInputSender>().ActionInputEvent += OnActionInput;

            return true;
        }

        private void OnTargetChanged(ulong oldTarget, ulong newTarget)
        {
            m_NewTarget = newTarget;
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            if (m_CurrentTarget != m_NewTarget)
            {
                m_CurrentTarget = m_NewTarget;

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_CurrentTarget, out NetworkObject targetObject))
                {
                    var targetEntity = targetObject != null ? targetObject.GetComponent<ITargetable>() : null;
                    if (targetEntity != null)
                    {
                        ValidateReticule(parent, targetObject);
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
        void ValidateReticule(ClientCharacterVisualization parent, NetworkObject targetObject)
        {
            if (m_TargetReticule == null)
            {
                m_TargetReticule = Object.Instantiate(parent.TargetReticulePrefab);
            }

            bool target_isnpc = targetObject.GetComponent<ITargetable>().IsNpc;
            bool myself_isnpc = m_ParentState.CharacterClass.IsNpc;
            bool hostile = target_isnpc != myself_isnpc;

            m_TargetReticule.GetComponent<MeshRenderer>().material = hostile ? parent.ReticuleHostileMat : parent.ReticuleFriendlyMat;
        }

        public override void CancelClient(ClientCharacterVisualization parent)
        {
            GameObject.Destroy(m_TargetReticule);

            m_ParentState.TargetId.OnValueChanged -= OnTargetChanged;
            if (m_ParentState.TryGetComponent(out Client.ClientInputSender inputSender))
            {
                inputSender.ActionInputEvent -= OnActionInput;
            }
        }

        private void OnActionInput(ActionRequestData data)
        {
            //this method runs on the owning client, and allows us to anticipate our new target for purposes of FX visualization.
            if (GameDataSource.Instance.GetActionPrototypeByID(data.ActionID).IsGeneralTargetAction)
            {
                m_NewTarget = data.TargetIds[0];
            }
        }
    }
}
