using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public static class ActionUtils
    {
        //cache Physics Cast hits, to minimize allocs.
        private static RaycastHit[] s_Hits = new RaycastHit[4];
        // cache layer IDs (after first use). -1 is a sentinel value meaning "uninitialized"
        private static int s_layer_PCs = -1;
        private static int s_layer_NPCs = -1;
        private static int s_layer_Ground = -1;

        /// <summary>
        /// When doing line-of-sight checks we assume the characters' "eyes" are at this height above their transform
        /// </summary>
        private static readonly Vector3 k_CharacterEyelineOffset = new Vector3(0, 1, 0);

        /// <summary>
        /// When teleporting to a destination, this is how far away from the destination spot to arrive
        /// </summary>
        private const float k_CloseDistanceOffset = 1;

        /// <summary>
        /// When checking if a teleport-destination is "too close" to the starting spot, anything less than this is too close
        /// </summary>
        private const float k_VeryCloseTeleportRange = k_CloseDistanceOffset + 1;

        /// <summary>
        /// Does a melee foe hit detect.
        /// </summary>
        /// <param name="isNPC">true if the attacker is an NPC (and therefore should hit PCs). False for the reverse.</param>
        /// <param name="attacker">The collider of the attacking GameObject.</param>
        /// <param name="range">The range in meters to check for foes.</param>
        /// <param name="results">Place an uninitialized RayCastHit[] ref in here. It will be set to the results array. </param>
        /// <remarks>
        /// This method does not alloc. It returns a maximum of 4 results. Consume the results immediately, as the array will be overwritten with
        /// the next similar query.
        /// </remarks>
        /// <returns>Total number of foes encountered. </returns>
        public static int DetectMeleeFoe(bool isNPC, Collider attacker, float range, out RaycastHit[] results)
        {
            return DetectNearbyEntities(isNPC, !isNPC, attacker, range, out results);
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

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetChar))
            {
                return false;
            }

            var targetable = targetChar.GetComponent<ITargetable>();
            return targetable != null && targetable.IsValidTarget;
        }


        /// <summary>
        /// Given the coordinates of two entities, checks to see if there is an obstacle between them.
        /// (Since character coordinates are beneath the feet of the visual avatar, we add a small amount of height to
        /// these coordinates to simulate their eye-line.)
        /// </summary>
        /// <param name="character1Pos">first character's position</param>
        /// <param name="character2Pos">second character's position</param>
        /// <param name="missPos">the point where an obstruction occurred (or if no obstruction, this is just character2Pos)</param>
        /// <returns>true if no obstructions, false if there is a Ground-layer object in the way</returns>
        public static bool HasLineOfSight(Vector3 character1Pos, Vector3 character2Pos, out Vector3 missPos)
        {
            if (s_layer_Ground == -1)
                s_layer_Ground = LayerMask.NameToLayer("Ground");
            int mask = 1 << s_layer_Ground;

            character1Pos += k_CharacterEyelineOffset;
            character2Pos += k_CharacterEyelineOffset;
            var rayDirection = character2Pos - character1Pos;
            var distance = rayDirection.magnitude;

            var numHits = Physics.RaycastNonAlloc(new Ray(character1Pos, rayDirection), s_Hits, distance, mask);
            if (numHits == 0)
            {
                missPos = character2Pos;
                return true;
            }
            else
            {
                missPos = s_Hits[0].point;
                return false;
            }
        }

        /// <summary>
        /// Helper method that calculates the percent a charge-up action is charged, based on how long it has run, returning a value
        /// from 0-1.
        /// </summary>
        /// <param name="stoppedChargingUpTime">The time when we finished charging up, or 0 if we're still charging.</param>
        /// <param name="timeRunning">How long the action has been running. </param>
        /// <param name="timeStarted">when the action started. </param>
        /// <param name="execTime">the total execution time of the action (usually not its duration). </param>
        /// <returns>Percent charge-up, from 0 to 1. </returns>
        public static float GetPercentChargedUp(float stoppedChargingUpTime, float timeRunning, float timeStarted, float execTime)
        {
            float timeSpentChargingUp;
            if (stoppedChargingUpTime == 0)
            {
                timeSpentChargingUp = timeRunning; // we're still charging up, so all of our runtime has been charge-up time
            }
            else
            {
                timeSpentChargingUp = stoppedChargingUpTime - timeStarted;
            }
            return Mathf.Clamp01(timeSpentChargingUp / execTime);
        }

        /// <summary>
        /// Determines a spot very near a chosen location, so that we can teleport next to the target (rather
        /// than teleporting literally on top of the target). Can optionally perform a bunch of additional checks:
        /// - can do a line-of-sight check and stop at the first obstruction.
        /// - can make sure that the chosen spot is a meaningful distance away from the starting spot.
        /// - can make sure that the chosen spot is no further than a specified distance away.
        /// </summary>
        /// <param name="characterTransform">character's transform</param>
        /// <param name="targetSpot">location we want to be next to</param>
        /// <param name="stopAtObstructions">true if we should be blocked by obstructions such as walls</param>
        /// <param name="distanceToUseIfVeryClose">if we should fix up very short teleport destinations, the new location will be this far away (in meters). -1 = don't check for short teleports</param>
        /// <param name="maxDistance">returned location will be no further away from characterTransform than this. -1 = no max distance</param>
        /// <returns>new coordinates that are near the destination (or near the first obstruction)</returns>
        public static Vector3 GetDashDestination(Transform characterTransform, Vector3 targetSpot, bool stopAtObstructions, float distanceToUseIfVeryClose = -1, float maxDistance = -1)
        {
            Vector3 destinationSpot = targetSpot;

            if (distanceToUseIfVeryClose != -1)
            {
                // make sure our stopping point is a meaningful distance away!
                if (destinationSpot == Vector3.zero || Vector3.Distance(characterTransform.position, destinationSpot) <= k_VeryCloseTeleportRange)
                {
                    // we don't have a meaningful stopping spot. Find a new one based on the character's current direction
                    destinationSpot = characterTransform.position + characterTransform.forward * distanceToUseIfVeryClose;
                }
            }

            if (maxDistance != -1)
            {
                // make sure our stopping point isn't too far away!
                float distance = Vector3.Distance(characterTransform.position, destinationSpot);
                if (distance > maxDistance)
                {
                    destinationSpot = Vector3.MoveTowards(destinationSpot, characterTransform.position, distance - maxDistance);
                }
            }

            if (stopAtObstructions)
            {
                // if we're going to hit an obstruction, stop at the obstruction
                if (!HasLineOfSight(characterTransform.position, destinationSpot, out Vector3 collidePos))
                {
                    destinationSpot = collidePos;
                }
            }

            // now get a spot "near" the end point
            destinationSpot = Vector3.MoveTowards(destinationSpot, characterTransform.position, k_CloseDistanceOffset);

            return destinationSpot;
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
