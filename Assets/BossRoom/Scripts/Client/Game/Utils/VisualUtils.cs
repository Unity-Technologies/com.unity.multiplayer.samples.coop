using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Repository for visualization-related utility functions.
    /// </summary>
    public static class VisualUtils
    {
        /// <summary>
        /// Minimum Smooth Speed we will set closingSpeed to in SmoothMove.
        /// </summary>
        private const float k_MinSmoothSpeed = 4.0f;

        /// <summary>
        /// In SmoothMove we set a velocity proportional to our distance, to roughly approximate a spring effect.
        /// This is the constant we use for that calculation.
        /// </summary>
        private const float k_TargetCatchupTime = 0.1f;


        /// <summary>
        /// Smoothly interpolates towards the parent transform.
        /// </summary>
        /// <param name="moveTransform">The transform to interpolate</param>
        /// <param name="targetTransform">The transform to interpolate towards.  </param>
        /// <param name="timeDelta">Time in seconds that has elapsed, for purposes of interpolation.</param>
        /// <param name="closingSpeed">The closing speed in m/s. This is updated by SmoothMove every time it is called, and will drop to 0 whenever the moveTransform has "caught up". </param>
        /// <param name="maxAngularSpeed">The max angular speed to to rotate at, in degrees/s.</param>
        public static void SmoothMove(Transform moveTransform, Transform targetTransform, float timeDelta, ref float closingSpeed, float maxAngularSpeed)
        {
            var posDiff = targetTransform.position - moveTransform.position;
            var angleDiff = Quaternion.Angle(targetTransform.transform.rotation, moveTransform.rotation);
            float posDiffMag = posDiff.magnitude;

            if (posDiffMag > 0)
            {
                closingSpeed = Mathf.Max(closingSpeed, Mathf.Max(k_MinSmoothSpeed, posDiffMag / k_TargetCatchupTime));

                float maxMove = timeDelta * closingSpeed;
                float moveDist = Mathf.Min(maxMove, posDiffMag);
                posDiff *= (moveDist / posDiffMag);

                moveTransform.position += posDiff;

                if( moveDist == posDiffMag )
                {
                    //we capped the move, meaning we exactly reached our target transform. Time to reset our velocity.
                    closingSpeed = 0;
                }
            }
            else
            {
                closingSpeed = 0;
            }

            if (angleDiff > 0)
            {
                float maxAngleMove = timeDelta * maxAngularSpeed;
                float angleMove = Mathf.Min(maxAngleMove, angleDiff);
                float t = angleMove / angleDiff;
                moveTransform.rotation = Quaternion.Slerp(moveTransform.rotation, targetTransform.rotation, t);
            }
        }

        /// <summary>
        /// Linearly interpolate between two Vector3 values. Allows for flexible linear interpolations where
        /// current and target change over time.
        /// </summary>
        /// <param name="current"> Start of the interpolation. </param>
        /// <param name="target"> End of the interpolation. </param>
        /// <param name="lerpStart"> Calculated start for the most recent interpolation. </param>
        /// <param name="currentLerpTime"> Calculated time elapsed for the most recent interpolation.</param>
        /// <param name="lerpTime"> The duration of the interpolation, in seconds. </param>
        /// <returns> A Vector3 value between current and target. </returns>
        public static Vector3 LerpPosition(Vector3 current, Vector3 target,
            ref Vector3 lerpStart, ref float currentLerpTime, float lerpTime)
        {
            if (current != target)
            {
                lerpStart = current;
                currentLerpTime = 0f;
            }

            currentLerpTime += Time.deltaTime;
            if (currentLerpTime > lerpTime)
            {
                currentLerpTime = lerpTime;
            }

            var lerpPercentage = currentLerpTime / lerpTime;

            return Vector3.Lerp(lerpStart, target, lerpPercentage);
        }

        /// <summary>
        /// Linearly interpolate between two Quaternion values. Allows for flexible linear interpolations where
        /// current and target change over time.
        /// </summary>
        /// <param name="current"> Start of the interpolation. </param>
        /// <param name="target"> End of the interpolation. </param>
        /// <param name="lerpStart"> Calculated start for the most recent interpolation. </param>
        /// <param name="currentLerpTime"> Calculated time elapsed for the most recent interpolation.</param>
        /// <param name="lerpTime"> The duration of the interpolation, in seconds. </param>
        /// <returns> A Quaternion value between current and target. </returns>
        public static Quaternion LerpRotation(Quaternion current, Quaternion target,
            ref Quaternion lerpStart, ref float currentLerpTime, float lerpTime)
        {
            if (current != target)
            {
                lerpStart = current;
                currentLerpTime = 0f;
            }

            currentLerpTime += Time.deltaTime;
            if (currentLerpTime > lerpTime)
            {
                currentLerpTime = lerpTime;
            }

            var lerpPercentage = currentLerpTime / lerpTime;

            return Quaternion.Lerp(lerpStart, target, lerpPercentage);
        }
    }

}

