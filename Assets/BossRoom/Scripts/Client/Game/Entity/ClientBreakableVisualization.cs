using MLAPI;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Visualization class for Breakables. Breakables work by swapping a "broken" prefab at the moment of breakage. The broken prefab
    /// then handles the pesky details of actually falling apart.
    /// </summary>
    public class ClientBreakableVisualization : NetworkBehaviour
    {
        [SerializeField]
        private GameObject m_BrokenPrefab;

        public override void NetworkStart()
        {
            if (!IsClient)
            {
                enabled = false;
            }
            else
            {
                var netState = transform.parent.GetComponent<NetworkBreakableState>();
                netState.IsBroken.OnValueChanged += (bool oldVal, bool newVal) =>
                {
                    if (oldVal == false && newVal == true)
                    {
                        PerformBreak();
                    }
                };

                if (netState.IsBroken.Value == true)
                {
                    //todo: don't dramatically break on entry to scene, if already broken.
                    PerformBreak();
                }

            }
        }

        private void PerformBreak()
        {
            var myParent = transform.parent;
            Destroy(gameObject);
            var brokenPot = Instantiate(m_BrokenPrefab);
            brokenPot.transform.parent = myParent;
            brokenPot.transform.localPosition = Vector3.zero;
        }
    }
}
