using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Component to simply play a raising/descending animation when this NetworkObject's parent NetworkObject changes.
    /// </summary>
    /// <remarks>
    /// Currently, Netcode for GameObjects' (Netcode) NetworkAnimator component does not support animations that apply
    /// Root Motion. This script is a workaround and will be refactored when Root Motion-based animations are supported.
    /// </remarks>
    public class ServerDisplacerOnParentChange : NetworkBehaviour
    {
        const float k_PickUpAnimationLength = 1f;

        const float k_DropAnimationLength = 0.7f;

        [SerializeField]
        NetworkTransform m_NetworkTransform;

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            if (!IsServer)
            {
                return;
            }

            if (parentNetworkObject)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothPositionLerpY(k_PickUpAnimationLength, 1f, true));
            }
            else
            {
                StopAllCoroutines();

                m_NetworkTransform.enabled = true;

                StartCoroutine(SmoothPositionLerpY(k_DropAnimationLength, 0));
            }
        }

        IEnumerator SmoothPositionLerpY(float length, float targetHeight, bool local = false)
        {
            var start = local ? transform.localPosition.y : transform.position.y;

            var progress = 0f;
            var duration = 0f;

            while (progress < 1f)
            {
                duration += Time.deltaTime;
                progress = Mathf.Clamp(duration / length, 0f, 1f);
                var progressY = Mathf.Lerp(start, targetHeight, progress);

                if (local)
                {
                    transform.localPosition = new Vector3(transform.localPosition.x,
                        progressY,
                        transform.localPosition.z);
                }
                else
                {
                    transform.position = new Vector3(transform.position.x, progressY, transform.position.z);
                }

                yield return null;
            }
        }
    }
}
