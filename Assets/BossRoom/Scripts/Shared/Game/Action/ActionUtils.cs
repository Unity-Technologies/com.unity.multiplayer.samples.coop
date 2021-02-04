using System;
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
        /// <param name="favoreddir">optional param that when set, will sort the results from the collision to be clos</param>
        /// <remarks>
        /// This method does not alloc. It returns a maximum of 4 results. Consume the results immediately, as the array will be overwritten with
        /// the next similar query. 
        /// </remarks>
        /// <returns>Total number of foes encountered. </returns>
        public static int DetectMeleeFoe(bool isNPC, Collider attacker, ActionDescription description, out RaycastHit[] results, Vector3? favorPoint)
        {
            //this simple detect just does a boxcast out from our position in the direction we're facing, out to the range of the attack. 

            var myBounds = attacker.bounds;

            //NPCs (monsters) can hit PCs, and vice versa. No friendly fire allowed on either side. 
            int mask = LayerMask.GetMask(isNPC ? "PCs" : "NPCs");

            int numResults = Physics.BoxCastNonAlloc(attacker.transform.position, myBounds.extents,
                attacker.transform.forward, s_Hits, Quaternion.identity, description.Range, mask);

            results = s_Hits;


            if (numResults > 0)
            {
                if (favorPoint.HasValue)
                {
                    //The sort here gets a tad tricky because we don't allocate a new array, meaning there can be empty entries here.
                    //To fix this in the sort we just need to push all the nulled entries to the back of the list.
                    Array.Sort(results, (x, y) =>
                    {
                        if (x.collider == null)
                        {
                            return 1;
                        }
                        else if (y.collider == null)
                        {
                            return -1;
                        }
                        else
                        {
                            Debug.Log(Vector3.Distance(x.transform.position, favorPoint.Value) - Vector3.Distance(y.transform.position, favorPoint.Value));
                            return Vector3.Distance(x.transform.position, favorPoint.Value).CompareTo(Vector3.Distance(y.transform.position, favorPoint.Value));
                        }
                    });
                }
            }


            return numResults;
        }

        /// <summary>
        /// Calculates the relative point for the collider based on the given hit direction.  This essentially gets the desired face of the collider in world coordinates
        /// </summary>
        /// <param name="col"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Vector3 ComputeDirectionPoint(Collider col, HitDirection dir)
        {
            var position = col.transform.position;
            switch (dir)
            {
                //This really shouldn't be called if given no direction, but we'll return the collider's transform.
                case HitDirection.NoDirection:
                    {
                        return position;
                    }
                case HitDirection.Left:
                    {
                        return position - col.transform.right * col.bounds.extents.x;
                    }
                case HitDirection.Right:
                    {
                        return position + col.transform.right * col.bounds.extents.x;
                    }
                default:
                    {
                        return position;
                    }
            }
        }
    }


}
