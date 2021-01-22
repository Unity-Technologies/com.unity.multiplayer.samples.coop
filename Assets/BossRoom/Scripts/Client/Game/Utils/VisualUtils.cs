using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Repository for visualization-related utility functions. 
    /// </summary>
    public static class VisualUtils
    {
        /// <summary>
        /// Smoothly interpolates towards the parent transform. 
        /// </summary>
        /// <param name="moveTransform">The transform to interpolate</param>
        /// <param name="targetTransform">The transform to interpolate towards.  </param>
        /// <param name="timeDelta">Time in seconds that has elapsed, for purposes of interpolation.</param>
        /// <param name="maxSpeed">The max speed to allow the moveTransform to advance at, in m/s </param>
        /// <param name="maxAngularSpeed">The max angular speed to to rotate at, in degrees/s.</param>
        public static void SmoothMove(Transform moveTransform, Transform targetTransform, float timeDelta, float maxSpeed, float maxAngularSpeed)
        {
            var posDiff = targetTransform.position - moveTransform.position;
            var angleDiff = Quaternion.Angle(targetTransform.transform.rotation, moveTransform.rotation);

            float posDiffMag = posDiff.magnitude;
            if (posDiffMag > 0)
            {
                float maxMove = timeDelta * maxSpeed;
                float moveDist = Mathf.Min(maxMove, posDiffMag);
                posDiff *= (moveDist / posDiffMag);

                moveTransform.position += posDiff;
            }

            if (angleDiff > 0)
            {
                float maxAngleMove = timeDelta * maxAngularSpeed;
                float angleMove = Mathf.Min(maxAngleMove, angleDiff);
                float t = angleMove / angleDiff;
                moveTransform.rotation = Quaternion.Slerp(moveTransform.rotation, targetTransform.rotation, t);
            }
        }
    }

}

