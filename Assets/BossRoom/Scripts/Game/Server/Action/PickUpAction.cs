using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Action for picking up "Heavy" items. For simplicity, this class will perform both the pickup (reparenting) of a
    /// NetworkObject, as well as the drop (deparenting).
    /// </summary>
    public class PickUpAction : Action
    {
        RaycastHit[] m_RaycastHits = new RaycastHit[1];

        const string k_HeavyTag = "Heavy";

        const string k_NpcLayer = "NPCs";
        public PickUpAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override bool Start()
        {
            var pickUpObject = m_Parent.GetComponentInChildren<NetworkPickUpState>();

            // first, check if a pot has already been parented; if so, drop it
            if (pickUpObject)
            {
                pickUpObject.transform.SetParent(null);

                Data.TargetIds = null;

                if (!string.IsNullOrEmpty(Description.Anim))
                {
                    m_Parent.serverAnimationHandler.NetworkAnimator.ResetTrigger(Description.Anim);
                }

                // drop
                if (!string.IsNullOrEmpty(Description.Anim2))
                {
                    m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
                }

                return false;
            }

            var numResults = Physics.BoxCastNonAlloc(m_Parent.physicsWrapper.Transform.position,
                m_Parent.physicsWrapper.DamageCollider.bounds.extents,
                m_Parent.physicsWrapper.Transform.forward,
                m_RaycastHits,
                Quaternion.identity,
                Description.Range,
                1 << LayerMask.NameToLayer(k_NpcLayer));

            // collider must contain "Heavy" tag
            if (numResults == 0 || !m_RaycastHits[0].collider.TryGetComponent(out NetworkObject heavyNetworkObject) ||
                !m_RaycastHits[0].collider.gameObject.CompareTag(k_HeavyTag))
            {
                return false;
            }

            // found a suitable collider; make sure it is not already held by another player
            if (heavyNetworkObject.transform.parent != null &&
                heavyNetworkObject.transform.parent.TryGetComponent(out NetworkObject parentNetworkObject))
            {
                // pot already parented; return for now
                return false;
            }

            // found a suitable collider; try to child this NetworkObject
            if (!heavyNetworkObject.TrySetParent(m_Parent.transform))
            {
                return false;
            }

            Data.TargetIds = new ulong[] { heavyNetworkObject.NetworkObjectId };

            // clear current target on successful parenting attempt
            m_Parent.NetState.TargetId.Value = 0;

            // snap to face the right direction
            if (Data.Direction != Vector3.zero)
            {
                m_Parent.transform.forward = Data.Direction;
            }

            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.ResetTrigger(Description.Anim2);
            }

            // pickup
            if (!string.IsNullOrEmpty(Description.Anim))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
            }

            // try to set the heavy object follow the hand bone transform, through PositionConstraint component
            var positionConstraint = heavyNetworkObject.GetComponent<PositionConstraint>();
            if (positionConstraint)
            {
                if (m_Parent.TryGetComponent(out ClientCharacter clientCharacter))
                {
                    var constraintSource = new ConstraintSource()
                    {
                        sourceTransform = clientCharacter.ChildVizObject.CharacterSwap.CharacterModel.handRight.transform,
                        weight = 1
                    };
                    positionConstraint.AddSource(constraintSource);
                    positionConstraint.constraintActive = true;
                }
            }

            return true;
        }

        public override bool Update()
        {
            return ActionConclusion.Stop;
        }
    }
}
