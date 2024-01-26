using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// Component to simply play a descending animation when this NetworkObject's parent NetworkObject changes.
    /// </summary>
    public class ServerDisplacerOnParentChange : NetworkBehaviour
    {
        [SerializeField]
        NetworkTransform m_NetworkTransform;

        [SerializeField]
        PositionConstraint m_PositionConstraint;

        const float k_DropAnimationLength = 0.1f;

        void Awake()
        {
            m_PositionConstraint.enabled = false;
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            m_PositionConstraint.enabled = IsServer;
            enabled = IsServer;
        }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            if (!IsServer)
            {
                return;
            }

            RemoveParentConstraintSources();

            if (parentNetworkObject == null)
            {
                StopAllCoroutines();

                m_NetworkTransform.InLocalSpace = false;

                // when Netcode detects that a NetworkObject's parent has been destroyed, it assigns no parent for that
                // object
                // when this happens, NetworkTransform and PositionConstraint are disabled; here they are re-enabled
                m_NetworkTransform.enabled = true;
                m_PositionConstraint.enabled = true;

                // this NetworkObject has been dropped, move it slowly back to the ground
                StartCoroutine(SmoothPositionLerpY(k_DropAnimationLength, 0));
            }
            else
            {
                m_NetworkTransform.InLocalSpace = true;
            }
        }

        void RemoveParentConstraintSources()
        {
            if (m_PositionConstraint)
            {
                for (int i = m_PositionConstraint.sourceCount - 1; i >= 0; i--)
                {
                    m_PositionConstraint.RemoveSource(i);
                }
            }
        }

        IEnumerator SmoothPositionLerpY(float length, float targetHeight)
        {
            var start = transform.position.y;

            var progress = 0f;
            var duration = 0f;

            while (progress < 1f)
            {
                duration += Time.deltaTime;
                progress = Mathf.Clamp(duration / length, 0f, 1f);
                var progressY = Mathf.Lerp(start, targetHeight, progress);

                transform.position = new Vector3(transform.position.x, progressY, transform.position.z);

                yield return null;
            }
        }
    }
}
