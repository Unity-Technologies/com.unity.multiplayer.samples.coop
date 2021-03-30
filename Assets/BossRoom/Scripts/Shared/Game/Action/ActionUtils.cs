using UnityEngine;
using MLAPI.Spawning;

namespace BossRoom
{
    public static class ActionUtils
    {
        //cache Physics Cast hits, to minimize allocs.
        private static RaycastHit[] s_Hits = new RaycastHit[4];
        // cache layer IDs (after first use). -1 is a sentinel value meaning "uninitialized"
        private static int s_layer_PCs = -1;
        private static int s_layer_NPCs = -1;

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
            return DetectNearbyEntities(isNPC, !isNPC, attacker, description.Range, out results);
        }

        /// <summary>
        /// Detects friends and/or foes near us.
        /// </summary>
        /// <param name="wantPcs">true if we should detect PCs</param>
        /// <param name="wantNpcs">true if we should detect NPCs</param>
        /// <param name="attacker">The collider of the attacking GameObject.</param>
        /// <param name="range">The range in meters to check.</param>
        /// <param name="results">Place an uninitialized RayCastHit[] ref in here. It will be set to the results array. </param>
        /// <returns></returns>
        public static int DetectNearbyEntities(bool wantPcs, bool wantNpcs, Collider attacker, float range, out RaycastHit[] results)
        {
            //this simple detect just does a boxcast out from our position in the direction we're facing, out to the range of the attack.

            var myBounds = attacker.bounds;

            if (s_layer_PCs == -1)
                s_layer_PCs = LayerMask.NameToLayer("PCs");
            if (s_layer_NPCs == -1)
                s_layer_NPCs = LayerMask.NameToLayer("NPCs");

            int mask = 0;
            if (wantPcs)
                mask |= (1 << s_layer_PCs);
            if (wantNpcs)
                mask |= (1 << s_layer_NPCs);

            int numResults = Physics.BoxCastNonAlloc(attacker.transform.position, myBounds.extents,
                attacker.transform.forward, s_Hits, Quaternion.identity, range, mask);

            results = s_Hits;
            return numResults;
        }

        /// <summary>
        /// Does this NetId represent a valid target? Used by Target Action. The target needs to exist, be a
        /// NetworkCharacterState, and be alive. In the future, it will be any non-dead IDamageable.
        /// </summary>
        /// <param name="targetId">the NetId of the target to investigate</param>
        /// <returns>true if this is a valid target</returns>
        public static bool IsValidTarget(ulong targetId)
        {
            //note that we DON'T check if you're an ally. It's perfectly valid to target friends,
            //because there are friendly skills, such as Heal.

            if (!NetworkSpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetChar))
            {
                return false;
            }

            var targetable = targetChar.GetComponent<ITargetable>();
            return targetable != null && targetable.IsValidTarget;
        }

    }

    /// <summary>
    /// Small utility to better understand action start and stop conclusion
    /// </summary>
    public static class ActionConclusion
    {
        public const bool Stop = false;
        public const bool Continue = true;
    }
}
