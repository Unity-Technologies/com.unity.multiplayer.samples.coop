using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{

    /// <summary>
    /// The visual part of a MeleeAction. See MeleeAction.cs for more about this action type.
    /// </summary>
    public class MeleeActionFX : ActionFX
    {
        public MeleeActionFX(ref ActionRequestData data, ClientCharacterVisualization clientParent) : base(ref data, clientParent) { }

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


        public override bool OnStartClient()
        {
            base.OnStartClient();

            // we can optionally have special particles that should play on the target. If so, add them now.
            // (don't wait until impact, because the particles need to start sooner!)
            if (c_Data.TargetIds != null
                && c_Data.TargetIds.Length > 0
                && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(c_Data.TargetIds[0], out var targetNetworkObj)
                && targetNetworkObj != null)
            {
                float padRange = c_Description.Range + k_RangePadding;

                Vector3 targetPosition;
                if (PhysicsWrapper.TryGetPhysicsWrapper(c_Data.TargetIds[0], out var physicsWrapper))
                {
                    targetPosition = physicsWrapper.Transform.position;
                }
                else
                {
                    targetPosition = targetNetworkObj.transform.position;
                }

                if ((m_ClientParent.transform.position - targetPosition).sqrMagnitude < (padRange * padRange))
                {
                    // target is in range! Play the graphics
                    m_SpawnedGraphics = InstantiateSpecialFXGraphicsClient(physicsWrapper ? physicsWrapper.Transform : targetNetworkObj.transform, true);
                }
            }
            return true;
        }

        public override bool OnUpdateClient()
        {
            return ActionConclusion.Continue;
        }

        public override void OnAnimEventClient(string id)
        {
            if (id == "impact" && !m_ImpactPlayed)
            {
                PlayHitReact();
            }
        }

        public override void EndClient()
        {
            //if this didn't already happen, make sure it gets a chance to run. This could have failed to run because
            //our animationclip didn't have the "impact" event properly configured (as one possibility).
            PlayHitReact();
            base.EndClient();
        }

        public override void CancelClient()
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

        private void PlayHitReact()
        {
            if (m_ImpactPlayed) { return; }
            m_ImpactPlayed = true;

            if (NetworkManager.Singleton.IsServer)
            {
                return;
            }

            //Is my original target still in range? Then definitely get him!
            if (c_Data.TargetIds != null &&
                c_Data.TargetIds.Length > 0 &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(c_Data.TargetIds[0], out var targetNetworkObj)
                && targetNetworkObj != null)
            {
                float padRange = c_Description.Range + k_RangePadding;

                Vector3 targetPosition;
                if (PhysicsWrapper.TryGetPhysicsWrapper(c_Data.TargetIds[0], out var movementContainer))
                {
                    targetPosition = movementContainer.Transform.position;
                }
                else
                {
                    targetPosition = targetNetworkObj.transform.position;
                }

                if ((m_ClientParent.transform.position - targetPosition).sqrMagnitude < (padRange * padRange))
                {
                    if (targetNetworkObj.NetworkObjectId != m_ClientParent.NetworkObjectId)
                    {
                        string hitAnim = c_Description.ReactAnim;
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
