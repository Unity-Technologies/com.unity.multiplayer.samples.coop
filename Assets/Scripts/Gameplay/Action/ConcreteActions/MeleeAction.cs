using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// Action that represents a swing of a melee weapon. It is not explicitly targeted, but rather detects the foe that was hit with a physics check.
    /// </summary>
    /// <remarks>
    /// Q: Why do we DetectFoe twice, once in Start, once when we actually connect?
    /// A: The weapon swing doesn't happen instantaneously. We want to broadcast the action to other clients as fast as possible to minimize latency,
    ///    but this poses a conundrum. At the moment the swing starts, you don't know for sure if you've hit anybody yet. There are a few possible resolutions to this:
    ///      1. Do the DetectFoe operation once--in Start.
    ///         Pros: Simple! Only one physics cast per swing--saves on perf.
    ///         Cons: Is unfair. You can step out of the swing of an attack, but no matter how far you go, you'll still be hit. The reverse is also true--you can
    ///               "step into an attack", and it won't affect you. This will feel terrible to the attacker.
    ///      2. Do the DetectFoe operation once--in Update. Send a separate RPC to the targeted entity telling it to play its hit react.
    ///         Pros: Always shows the correct behavior. The entity that gets hit plays its hit react (if any).
    ///         Cons: You need another RPC. Adds code complexity and bandwidth. You also don't have enough information when you start visualizing the swing on
    ///               the client to do any intelligent animation handshaking. If your server->client latency is even a little uneven, your "attack" animation
    ///               won't line up correctly with the hit react, making combat look floaty and disjointed.
    ///      3. Do the DetectFoe operation twice, once in Start and once in Update.
    ///         Pros: Is fair--you do the hit-detect at the moment of the swing striking home. And will generally play the hit react on the right target.
    ///         Cons: Requires more complicated visualization logic. The initial broadcast foe can only ever be treated as a "hint". The graphics logic
    ///               needs to do its own range checking to pick the best candidate to play the hit react on.
    ///
    /// As so often happens in networked games (and games in general), there's no perfect solution--just sets of tradeoffs. For our example, we're showing option "3".
    /// </remarks>
    [CreateAssetMenu()]
    public class MeleeAction : Action
    {
        private bool m_ExecutionFired;
        private ulong m_ProvisionalTarget;

        //have we actually played an impact? This won't necessarily happen for all swings. Sometimes you're just swinging at space.
        private bool m_ImpactPlayed;

        /// <summary>
        /// When we detect if our original target is still around, we use a bit of padding on the range check.
        /// </summary>
        private const float k_RangePadding = 3f;

        /// <summary>
        /// List of active special graphics playing on the target.
        /// </summary>
        private List<SpecialFXGraphic> m_SpawnedGraphics = null;

        public override bool OnStart(ServerCharacter parent)
        {
            ulong target = (Data.TargetIds != null && Data.TargetIds.Length > 0) ? Data.TargetIds[0] : parent.NetState.TargetId.Value;
            IDamageable foe = DetectFoe(parent, target);
            if (foe != null)
            {
                m_ProvisionalTarget = foe.NetworkObjectId;
                Data.TargetIds = new ulong[] { foe.NetworkObjectId };
            }

            // snap to face the right direction
            if (Data.Direction != Vector3.zero)
            {
                parent.physicsWrapper.Transform.forward = Data.Direction;
            }

            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
            parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            if (!m_ExecutionFired && (Time.time - TimeStarted) >= Config.ExecTimeSeconds)
            {
                m_ExecutionFired = true;
                var foe = DetectFoe(parent, m_ProvisionalTarget);
                if (foe != null)
                {
                    foe.ReceiveHP(parent, -Config.Amount);
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the ServerCharacter of the foe we hit, or null if none found.
        /// </summary>
        /// <returns></returns>
        private IDamageable DetectFoe(ServerCharacter parent, ulong foeHint = 0)
        {
            return GetIdealMeleeFoe(Config.IsFriendly ^ parent.IsNpc, parent.physicsWrapper.DamageCollider, Config.Range, foeHint);
        }

        /// <summary>
        /// Utility used by Actions to perform Melee attacks. Performs a melee hit-test
        /// and then looks through the results to find an alive target, preferring the provided
        /// enemy.
        /// </summary>
        /// <param name="isNPC">true if the attacker is an NPC (and therefore should hit PCs). False for the reverse.</param>
        /// <param name="ourCollider">The collider of the attacking GameObject.</param>
        /// <param name="meleeRange">The range in meters to check for foes.</param>
        /// <param name="preferredTargetNetworkId">The NetworkObjectId of our preferred foe, or 0 if no preference</param>
        /// <returns>ideal target's IDamageable, or null if no valid target found</returns>
        public static IDamageable GetIdealMeleeFoe(bool isNPC, Collider ourCollider, float meleeRange, ulong preferredTargetNetworkId)
        {
            RaycastHit[] results;
            int numResults = ActionUtils.DetectMeleeFoe(isNPC, ourCollider, meleeRange, out results);

            IDamageable foundFoe = null;

            //everything that got hit by the raycast should have an IDamageable component, so we can retrieve that and see if they're appropriate targets.
            //we always prefer the hinted foe. If he's still in range, he should take the damage, because he's who the client visualization
            //system will play the hit-react on (in case there's any ambiguity).
            for (int i = 0; i < numResults; i++)
            {
                var damageable = results[i].collider.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsDamageable() &&
                    (damageable.NetworkObjectId == preferredTargetNetworkId || foundFoe == null))
                {
                    foundFoe = damageable;
                }
            }

            return foundFoe;
        }

        public override bool OnStartClient(ClientCharacterVisualization parent)
        {
            base.OnStartClient(parent);

            // we can optionally have special particles that should play on the target. If so, add them now.
            // (don't wait until impact, because the particles need to start sooner!)
            if (Data.TargetIds != null
                && Data.TargetIds.Length > 0
                && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out var targetNetworkObj)
                && targetNetworkObj != null)
            {
                float padRange = Config.Range + k_RangePadding;

                Vector3 targetPosition;
                if (PhysicsWrapper.TryGetPhysicsWrapper(Data.TargetIds[0], out var physicsWrapper))
                {
                    targetPosition = physicsWrapper.Transform.position;
                }
                else
                {
                    targetPosition = targetNetworkObj.transform.position;
                }

                if ((parent.transform.position - targetPosition).sqrMagnitude < (padRange * padRange))
                {
                    // target is in range! Play the graphics
                    m_SpawnedGraphics = InstantiateSpecialFXGraphics(physicsWrapper ? physicsWrapper.Transform : targetNetworkObj.transform, true);
                }
            }
            return true;
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            return ActionConclusion.Continue;
        }

        public override void OnAnimEventClient(ClientCharacterVisualization parent, string id)
        {
            if (id == "impact" && !m_ImpactPlayed)
            {
                PlayHitReact(parent);
            }
        }

        public override void EndClient(ClientCharacterVisualization parent)
        {
            //if this didn't already happen, make sure it gets a chance to run. This could have failed to run because
            //our animationclip didn't have the "impact" event properly configured (as one possibility).
            PlayHitReact(parent);
            base.EndClient(parent);
        }

        public override void CancelClient(ClientCharacterVisualization parent)
        {
            // if we had any special target graphics, tell them we're done
            if (m_SpawnedGraphics != null)
            {
                foreach (var spawnedGraphic in m_SpawnedGraphics)
                {
                    if (spawnedGraphic)
                    {
                        spawnedGraphic.Shutdown();
                    }
                }
            }
        }

        private void PlayHitReact(ClientCharacterVisualization parent)
        {
            if (m_ImpactPlayed) { return; }
            m_ImpactPlayed = true;

            if (NetworkManager.Singleton.IsServer)
            {
                return;
            }

            //Is my original target still in range? Then definitely get him!
            if (Data.TargetIds != null &&
                Data.TargetIds.Length > 0 &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out var targetNetworkObj)
                && targetNetworkObj != null)
            {
                float padRange = Config.Range + k_RangePadding;

                Vector3 targetPosition;
                if (PhysicsWrapper.TryGetPhysicsWrapper(Data.TargetIds[0], out var movementContainer))
                {
                    targetPosition = movementContainer.Transform.position;
                }
                else
                {
                    targetPosition = targetNetworkObj.transform.position;
                }

                if ((parent.transform.position - targetPosition).sqrMagnitude < (padRange * padRange))
                {
                    if (targetNetworkObj.NetworkObjectId != parent.NetworkObjectId)
                    {
                        string hitAnim = Config.ReactAnim;
                        if (string.IsNullOrEmpty(hitAnim)) { hitAnim = k_DefaultHitReact; }
                        var clientChar = targetNetworkObj.GetComponent<Client.ClientCharacter>();
                        if (clientChar && clientChar.ChildVizObject && clientChar.ChildVizObject.OurAnimator)
                        {
                            clientChar.ChildVizObject.OurAnimator.SetTrigger(hitAnim);
                        }
                    }
                }
            }

            //in the future we may do another physics check to handle the case where a target "ran under our weapon".
            //But for now, if the original target is no longer present, then we just don't play our hit react on anything.
        }

    }
}
