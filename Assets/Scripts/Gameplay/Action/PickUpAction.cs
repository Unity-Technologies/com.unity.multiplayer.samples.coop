using System;
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
        RaycastHit[] m_RaycastHits = new RaycastHit[8];

        const string k_HeavyTag = "Heavy";

        const string k_NpcLayer = "NPCs";

        const float k_PickUpWait = 0.6f;

        const string k_FailedPickupTrigger = "PickUpFailed";

        float m_AnimationTimer;

        NetworkPickUpState m_NetworkPickUpState;

        static RaycastHitComparer s_RaycastHitComparer;

        public PickUpAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
            m_NetworkPickUpState = parent.GetComponent<NetworkPickUpState>();

            s_RaycastHitComparer ??= new RaycastHitComparer();
        }

        public override bool Start()
        {
            // play animation based on if a heavy object is already held and start timer
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_NetworkPickUpState.heldObject.Value,
                    out var heldObject))
            {
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
            }
            else
            {
                if (!string.IsNullOrEmpty(Description.Anim2))
                {
                    m_Parent.serverAnimationHandler.NetworkAnimator.ResetTrigger(Description.Anim2);
                }

                // pickup
                if (!string.IsNullOrEmpty(Description.Anim))
                {
                    m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
                }
            }

            m_AnimationTimer = k_PickUpWait;

            return true;
        }

        void PickUpOrDrop()
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_NetworkPickUpState.heldObject.Value,
                    out var heldObject))
            {
                // pickup object found inside of hierarchy; drop it
                m_NetworkPickUpState.heldObject.Value = 0;
                heldObject.transform.SetParent(null);
                return;
            }

            var numResults = Physics.RaycastNonAlloc(m_Parent.physicsWrapper.Transform.position,
                m_Parent.physicsWrapper.Transform.forward,
                m_RaycastHits,
                Description.Range,
                1 << LayerMask.NameToLayer(k_NpcLayer));

            Array.Sort(m_RaycastHits, 0, numResults, s_RaycastHitComparer);

            // collider must contain "Heavy" tag
            if (numResults == 0 || !m_RaycastHits[0].collider.TryGetComponent(out NetworkObject heavyNetworkObject) ||
                !m_RaycastHits[0].collider.gameObject.CompareTag(k_HeavyTag))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(k_FailedPickupTrigger);
                return;
            }

            // found a suitable collider; make sure it is not already held by another player
            if (heavyNetworkObject.transform.parent != null &&
                heavyNetworkObject.transform.parent.TryGetComponent(out NetworkObject parentNetworkObject))
            {
                // pot already parented; return for now
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(k_FailedPickupTrigger);
                return;
            }

            // found a suitable collider; try to child this NetworkObject
            if (!heavyNetworkObject.TrySetParent(m_Parent.transform))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(k_FailedPickupTrigger);
                return;
            }

            m_NetworkPickUpState.heldObject.Value = heavyNetworkObject.NetworkObjectId;

            Data.TargetIds = new ulong[] { heavyNetworkObject.NetworkObjectId };

            // clear current target on successful parenting attempt
            m_Parent.NetState.TargetId.Value = 0;

            // snap to face the right direction
            if (Data.Direction != Vector3.zero)
            {
                m_Parent.transform.forward = Data.Direction;
            }

            // try to set the heavy object follow the hand bone transform, through PositionConstraint component
            var positionConstraint = heavyNetworkObject.GetComponent<PositionConstraint>();
            if (positionConstraint)
            {
                if (m_Parent.TryGetComponent(out ClientCharacter clientCharacter))
                {
                    var constraintSource = new ConstraintSource()
                    {
                        sourceTransform = clientCharacter.ChildVizObject.CharacterSwap.CharacterModel.handSocket.transform,
                        weight = 1
                    };
                    positionConstraint.AddSource(constraintSource);
                    positionConstraint.constraintActive = true;
                }
            }
        }

        public override bool Update()
        {
            m_AnimationTimer -= Time.deltaTime;

            if (m_AnimationTimer <= 0f)
            {
                PickUpOrDrop();

                return ActionConclusion.Stop;
            }

            return ActionConclusion.Continue;
        }
    }
}
