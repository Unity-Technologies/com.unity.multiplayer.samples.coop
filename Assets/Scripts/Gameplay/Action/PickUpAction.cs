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

        const string k_FailedPickupTrigger = "PickUpFailed";

        float m_ActionStartTime;

        static RaycastHitComparer s_RaycastHitComparer;

        NetworkLifeState m_NetworkLifeState;

        bool m_AttemptedPickup;

        public PickUpAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
            s_RaycastHitComparer ??= new RaycastHitComparer();

            m_NetworkLifeState = m_Parent.NetState.NetworkLifeState;
        }

        public override bool Start()
        {
            m_ActionStartTime = Time.time;

            // play pickup animation based if a heavy object is not already held
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    m_Parent.NetState.heldNetworkObject.Value, out var heldObject))
            {
                if (!string.IsNullOrEmpty(Description.Anim))
                {
                    m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
                }
            }

            return true;
        }

        bool TryPickUp()
        {
            var numResults = Physics.RaycastNonAlloc(m_Parent.physicsWrapper.Transform.position,
                m_Parent.physicsWrapper.Transform.forward,
                m_RaycastHits,
                Description.Range,
                1 << LayerMask.NameToLayer(k_NpcLayer));

            Array.Sort(m_RaycastHits, 0, numResults, s_RaycastHitComparer);

            // collider must contain "Heavy" tag, the heavy object must not be parented to another NetworkObject, and
            // parenting attempt must be successful
            if (numResults == 0 || !m_RaycastHits[0].collider.TryGetComponent(out NetworkObject heavyNetworkObject) ||
                !m_RaycastHits[0].collider.gameObject.CompareTag(k_HeavyTag) ||
                (heavyNetworkObject.transform.parent != null &&
                    heavyNetworkObject.transform.parent.TryGetComponent(out NetworkObject parentNetworkObject)) ||
                !heavyNetworkObject.TrySetParent(m_Parent.transform))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(k_FailedPickupTrigger);
                return false;
            }

            m_Parent.NetState.heldNetworkObject.Value = heavyNetworkObject.NetworkObjectId;

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

            return true;
        }

        public override bool Update()
        {
            if (!m_AttemptedPickup && Time.time > m_ActionStartTime + Description.ExecTimeSeconds)
            {
                m_AttemptedPickup = true;
                if (!TryPickUp())
                {
                    // pickup attempt unsuccessful; action can be terminated
                    return ActionConclusion.Stop;
                }
            }

            return ActionConclusion.Continue;
        }

        public override void Cancel()
        {
            if (m_NetworkLifeState.LifeState.Value == LifeState.Fainted)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_Parent.NetState.heldNetworkObject.Value, out var heavyNetworkObject))
                {
                    heavyNetworkObject.transform.SetParent(null);
                }
                m_Parent.NetState.heldNetworkObject.Value = 0;
            }
        }
    }
}
