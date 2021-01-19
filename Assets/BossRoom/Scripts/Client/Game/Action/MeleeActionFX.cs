using MLAPI;
using MLAPI.Spawning;

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

        // Start is called before the first frame update
        public override bool Start()
        {
            m_Parent.OurAnimator.SetTrigger(Description.Anim);
            return true;
        }

        // Update is called once per frame
        public override bool Update()
        {
            return true;
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
        }

        private void PlayHitReact()
        {
            if (m_ImpactPlayed) { return; }
            m_ImpactPlayed = true;

            //Is my original target still in range? Then definitely get him!
            if (Data.TargetIds != null && Data.TargetIds.Length > 0 && SpawnManager.SpawnedObjects.ContainsKey(Data.TargetIds[0]))
            {
                NetworkedObject originalTarget = SpawnManager.SpawnedObjects[Data.TargetIds[0]];
                float padRange = Description.Range + k_RangePadding;

                if ((m_Parent.transform.position - originalTarget.transform.position).sqrMagnitude < (padRange * padRange))
                {
                    ClientCharacterVisualization targetViz = originalTarget.GetComponent<BossRoom.Client.ClientCharacter>().ChildVizObject;
                    targetViz.OurAnimator.SetTrigger("HitReact1");
                }
            }

            //in the future we may do another physics check to handle the case where a target "ran under our weapon".
            //But for now, if the original target is no longer present, then we just don't play our hit react on anything. 
        }
    }
}
