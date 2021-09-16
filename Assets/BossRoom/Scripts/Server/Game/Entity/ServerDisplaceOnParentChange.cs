using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom.Server
{
    public class ServerDisplaceOnParentChange : NetworkBehaviour
    {
        public void NetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            if (!IsServer)
            {
                return;
            }

            if (parentNetworkObject)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothPositionLerpY(1f, true));
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(SmoothPositionLerpY(0));
            }
        }

        IEnumerator SmoothPositionLerpY(float targetHeight, bool local = false)
        {
            var start = local ? transform.localPosition.y : transform.position.y;

            var progress = 0f;
            var duration = 0f;

            while (progress < 1f)
            {
                duration += Time.deltaTime;
                progress = Mathf.Clamp(duration, 0f, 1f);
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

                yield return new WaitForEndOfFrame();
            }
        }
    }
}
