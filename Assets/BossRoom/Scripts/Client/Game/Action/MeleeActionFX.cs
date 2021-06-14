using MLAPI;
using MLAPI.Spawning;
using System.Collections.Generic;

namespace BossRoom.Visual
{

    /// <summary>
    /// The visual part of a MeleeAction. See MeleeAction.cs for more about this action type.
    /// </summary>
    public class MeleeActionFX : ActionFX
    {
        public MeleeActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

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


        public override bool Start()
        {
            if( !Anticipated)
            {
                PlayAnim();
            }

            base.Start();

            // we can optionally have special particles that should play on the target. If so, add them now.
            // (don't wait until impact, because the particles need to start sooner!)
            if (Data.TargetIds != null
                && Data.TargetIds.Length > 0
                && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out var targetNetworkObj)
                && targetNetworkObj != null)
            {
                float padRange = Description.Range + k_RangePadding;

                if ((m_Parent.transform.position - targetNetworkObj.transform.position).sqrMagnitude < (padRange * padRange))
                {
                    // target is in range! Play the graphics
                    m_SpawnedGraphics = InstantiateSpecialFXGraphics(targetNetworkObj.transform, true);
                }
            }
            return true;
        }

        public override bool Update()
        {
            return ActionConclusion.Continue;
        }

        public override void OnAnimEvent(string id)
        {
            if (id == "impact" && !m_ImpactPlayed)
            {
                PlayHitReact();
            }
        }

        public override void End()
        {
            //if this didn't already happen, make sure it gets a chance to run. This could have failed to run because
            //our animationclip didn't have the "impact" event properly configured (as one possibility).
            PlayHitReact();
            base.End();
        }

        public override void Cancel()
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

        private void PlayAnim()
        {
            m_Parent.OurAnimator.SetTrigger(Description.Anim);
        }

        private void PlayHitReact()
        {
            if (m_ImpactPlayed) { return; }
            m_ImpactPlayed = true;

            //Is my original target still in range? Then definitely get him!
            if (Data.TargetIds != null && Data.TargetIds.Length > 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(Data.TargetIds[0]))
            {
                NetworkObject originalTarget = NetworkManager.Singleton.SpawnManager.SpawnedObjects[Data.TargetIds[0]];
                float padRange = Description.Range + k_RangePadding;

                if ((m_Parent.transform.position - originalTarget.transform.position).sqrMagnitude < (padRange * padRange))
                {
                    if( originalTarget.NetworkObjectId != m_Parent.NetworkObjectId )
                    {
                        string hitAnim = Description.ReactAnim;
                        if(string.IsNullOrEmpty(hitAnim)) { hitAnim = k_DefaultHitReact; }
                        var clientChar = originalTarget.GetComponent<Client.ClientCharacter>();
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

        public override void AnticipateAction()
        {
            base.AnticipateAction();

            //note: because the hit-react is driven from the animation, this means we can anticipatively trigger a hit-react too. That
            //will make combat feel responsive, but of course the actual damage won't be applied until the server tells us about it.
            PlayAnim();
        }
    }
}
