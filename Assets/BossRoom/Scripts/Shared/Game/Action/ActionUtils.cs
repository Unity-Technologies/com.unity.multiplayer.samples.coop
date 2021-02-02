using UnityEngine;

namespace BossRoom
{
    public static class ActionUtils
    {
        //cache Physics Cast hits, to minimize allocs. 
        private static RaycastHit[] s_Hits = new RaycastHit[4];

        /// <summary>
        /// Does a melee foe hit detect. 
        /// </summary>
        /// <param name="isNPC">true if the attacker is an NPC (and therefore should hit PCs). False for the reverse.</param>
        /// <param name="attacker">The collider of the attacking GameObject.</param>
        /// <param name="description">The Description of the Action being played (containing things like Range that control the physics query.</param>
        /// <param name="results">Place an uninitialized RayCastHit[] ref in here. It will be set to the results array. </param>
        /// <remarks>
        /// This method does not alloc. It returns a maximum of 4 results. Consume the results immediately, as the array will be overwritten with
        /// the next similar query. 
        /// </remarks>
        /// <returns>Total number of foes encountered. </returns>
        public static int DetectMeleeFoe(bool isNPC, Collider attacker, ActionDescription description, out RaycastHit[] results)
        {
            return DetectNearbyFoe(isNPC, attacker, description.Range, out results);
        }

        /// <summary>
        /// Detects foes near us. (This is like DetectMeleeFoe() but doesn't need an ActionDescription, just a range.
        /// We'll probably want to completely phase out DetectMeleeFoe()... but both functions can exist for now until it's
        /// more obvious if DetectMeleeFoe() will need to be more sophisticated.)
        /// </summary>
        /// <param name="isNPC">true if the attacker is an NPC (and therefore should hit PCs). False for the reverse.</param>
        /// <param name="attacker">The collider of the attacking GameObject.</param>
        /// <param name="range">The range in meters to check.</param>
        /// <param name="results">Place an uninitialized RayCastHit[] ref in here. It will be set to the results array. </param>
        /// <returns></returns>
        public static int DetectNearbyFoe(bool isNPC, Collider attacker, float range, out RaycastHit[] results)
        {
            //this simple detect just does a boxcast out from our position in the direction we're facing, out to the range of the attack. 

            var myBounds = attacker.bounds;

            //NPCs (monsters) can hit PCs, and vice versa. No friendly fire allowed on either side. 
            int mask = LayerMask.GetMask(isNPC ? "PCs" : "NPCs");

            int numResults = Physics.BoxCastNonAlloc(attacker.transform.position, myBounds.extents,
                attacker.transform.forward, s_Hits, Quaternion.identity, range, mask);

            results = s_Hits;
            return numResults;
        }

    }


}
