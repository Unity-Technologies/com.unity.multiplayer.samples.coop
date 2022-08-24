using System;
using Unity.Multiplayer.Samples.BossRoom.Actions;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;
using Action = Unity.Multiplayer.Samples.BossRoom.Actions.Action;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Action for picking up "Heavy" items. For simplicity, this class will perform both the pickup (reparenting) of a
    /// NetworkObject, as well as the drop (deparenting).
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Pick Up Action")]
    public class PickUpAction : Action
    {
        RaycastHit[] m_RaycastHits = new RaycastHit[8];

        const string k_HeavyTag = "Heavy";

        const string k_NpcLayer = "NPCs";

        const string k_FailedPickupTrigger = "PickUpFailed";

        float m_ActionStartTime;

        static RaycastHitComparer s_RaycastHitComparer = new RaycastHitComparer();

        NetworkLifeState m_NetworkLifeState;

        bool m_AttemptedPickup;

        public override bool OnStart(ServerCharacter parent)
        {
            m_ActionStartTime = Time.time;

            // play pickup animation based if a heavy object is not already held
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    parent.NetState.heldNetworkObject.Value, out var heldObject))
            {
                if (!string.IsNullOrEmpty(Config.Anim))
                {
                    parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
                }
            }

            return true;
        }

        bool TryPickUp(ServerCharacter parent)
        {
            var numResults = Physics.RaycastNonAlloc(parent.physicsWrapper.Transform.position,
                parent.physicsWrapper.Transform.forward,
                m_RaycastHits,
                Config.Range,
                1 << LayerMask.NameToLayer(k_NpcLayer));

            Array.Sort(m_RaycastHits, 0, numResults, s_RaycastHitComparer);

            // collider must contain "Heavy" tag, the heavy object must not be parented to another NetworkObject, and
            // parenting attempt must be successful
            if (numResults == 0 || !m_RaycastHits[0].collider.TryGetComponent(out NetworkObject heavyNetworkObject) ||
                !m_RaycastHits[0].collider.gameObject.CompareTag(k_HeavyTag) ||
                (heavyNetworkObject.transform.parent != null &&
                    heavyNetworkObject.transform.parent.TryGetComponent(out NetworkObject parentNetworkObject)) ||
                !heavyNetworkObject.TrySetParent(parent.transform))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(k_FailedPickupTrigger);
                return false;
            }

            parent.NetState.heldNetworkObject.Value = heavyNetworkObject.NetworkObjectId;

            Data.TargetIds = new ulong[] { heavyNetworkObject.NetworkObjectId };

            // clear current target on successful parenting attempt
            parent.NetState.TargetId.Value = 0;

            // snap to face the right direction
            if (Data.Direction != Vector3.zero)
            {
                parent.transform.forward = Data.Direction;
            }

            // try to set the heavy object follow the hand bone transform, through PositionConstraint component
            var positionConstraint = heavyNetworkObject.GetComponent<PositionConstraint>();
            if (positionConstraint)
            {
                if (parent.TryGetComponent(out ClientCharacter clientCharacter))
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

        public override bool OnUpdate(ServerCharacter parent)
        {
            if (!m_AttemptedPickup && Time.time > m_ActionStartTime + Config.ExecTimeSeconds)
            {
                m_AttemptedPickup = true;
                if (!TryPickUp(parent))
                {
                    // pickup attempt unsuccessful; action can be terminated
                    return ActionConclusion.Stop;
                }
            }

            return ActionConclusion.Continue;
        }

        public override void Cancel(ServerCharacter parent)
        {
            if (m_NetworkLifeState.LifeState.Value == LifeState.Fainted)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parent.NetState.heldNetworkObject.Value, out var heavyNetworkObject))
                {
                    heavyNetworkObject.transform.SetParent(null);
                }
                parent.NetState.heldNetworkObject.Value = 0;
            }
        }
    }
}
