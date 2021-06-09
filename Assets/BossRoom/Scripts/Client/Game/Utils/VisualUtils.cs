using System;
using UnityEngine;

namespace BossRoom.Visual
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

        // [DefaultExecutionOrder(200)]
        // public class PositionInterpolation : Interpolation<Vector3>
        // {
        //     public override Func<Vector3, Vector3, float, Vector3> LerpFunction => Vector3.LerpUnclamped;
        //
        //     // public Vector3 Update(float time)
        //     // {
        //     //     var value = GetValueForTime(time);
        //     //     // if (float.IsNaN(value.x))
        //     //     // {
        //     //     //
        //     //     // }
        //     //
        //     //     return value;
        //     // }
        // }

        // public abstract class Interpolation<T>
        // {
        //     public abstract Func<T, T, float, T> LerpFunction { get;}
        //
        //     private (float, T) m_Last = (0, default);
        //     private (float, T) m_Previous = (0, default);
        //
        //     public T GetValueForTime(float time)
        //     {
        //         float timeSincePrevious = time - m_Previous.Item1;
        //         if (timeSincePrevious > 1f)
        //         {
        //             return m_Last.Item2;
        //         }
        //         float t = (timeSincePrevious / (m_Last.Item1 - m_Previous.Item1) - 1f);
        //
        //         return LerpFunction(m_Previous.Item2, m_Last.Item2, t);
        //     }
        //
        //     public void AddValue(float time, T value)
        //     {
        //         m_Previous = m_Last;
        //         m_Last = (time, value);
        //     }
        // }

        /// <summary>
        /// Smoothly interpolates towards the parent transform.
        /// </summary>
        /// <param name="moveTransform">The transform to interpolate</param>
        /// <param name="targetTransform">The transform to interpolate towards.  </param>
        /// <param name="deltaTime">Time in seconds that has elapsed, for purposes of interpolation.</param>
        /// <param name="normalSpeed">The closing speed in m/s. This is updated by SmoothMove every time it is called, and will drop to 0 whenever the moveTransform has "caught up". </param>
        /// <param name="maxAngularSpeed">The max angular speed to to rotate at, in degrees/s.</param>
        public static void SmoothMove(Transform moveTransform, Transform targetTransform, float deltaTime, ref float normalSpeed, float maxAngularSpeed)
        {
            var posDiff = targetTransform.position - moveTransform.position;
            var angleDiff = Quaternion.Angle(targetTransform.transform.rotation, moveTransform.rotation);
            float posDiffMag = posDiff.magnitude;

            if (posDiffMag < 0.05f) // todo config
            {
                moveTransform.position = targetTransform.position;
            }
            else if (posDiffMag > normalSpeed * deltaTime * 3f)
            {
                // for big corrections or for abilities (ex: boss charge, rogue dash)
                var newPosDelta = Mathf.Min(deltaTime * 8f, 1f) * posDiff;
                moveTransform.position += newPosDelta;
            }
            else
            {
                var newPosDelta = normalSpeed * deltaTime * posDiff.normalized;
                moveTransform.position += newPosDelta;
            }

            // if (posDiffMag > 0)
            // {
            //     closingSpeed = Mathf.Max(closingSpeed, Mathf.Max(k_MinSmoothSpeed, posDiffMag / k_TargetCatchupTime));
            //
            //     float maxMove = timeDelta * closingSpeed;
            //     float moveDist = Mathf.Min(maxMove, posDiffMag);
            //     posDiff *= (moveDist / posDiffMag);
            //
            //     moveTransform.position += posDiff;
            //
            //     if( moveDist == posDiffMag )
            //     {
            //         //we capped the move, meaning we exactly reached our target transform. Time to reset our velocity.
            //         closingSpeed = 0;
            //     }
            // }
            // else
            // {
            //     closingSpeed = 0;
            // }

            if (angleDiff > 0)
            {
                float maxAngleMove = deltaTime * maxAngularSpeed;
                float angleMove = Mathf.Min(maxAngleMove, angleDiff);
                float t = angleMove / angleDiff;
                moveTransform.rotation = Quaternion.Slerp(moveTransform.rotation, targetTransform.rotation, t);
            }
        }
    }

}

