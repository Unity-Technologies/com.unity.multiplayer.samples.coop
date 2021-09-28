using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom.Server
{
    public class ServerDisplacerOnParentChange : NetworkBehaviour
    {
        const float k_PickUpAnimationLength = 1f;

        const float k_DropAnimationLength = 0.7f;

        /// <summary>
        /// Since NetworkObject parenting is handled via custom script, <see cref="CustomParentingHandler"/>, this
        /// method is invoked externally when a parenting attempt is successful.
        /// </summary>
        /// <param name="parentNetworkObject"></param>
        public void NetworkObjectParentChanged(NetworkObject parentNetworkObject)
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
                StartCoroutine(SmoothPositionLerpY(k_DropAnimationLength, 0));
            }
        }

        IEnumerator SmoothPositionLerpY(float length, float targetHeight, bool local = false)
        {
            var start = local ? transform.localPosition.y : transform.position.y;

            var progress = 0f;
            float duration = 0f;

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
