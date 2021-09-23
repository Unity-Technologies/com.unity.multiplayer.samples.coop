using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// This system exists to coordinate path finding and navigation functionality in a scene.
    /// The Unity NavMesh is only used to calculate navigation paths. Moving along those paths is done by this system.
    /// </summary>
    public class NavigationSystem : MonoBehaviour
    {
        public const string NavigationSystemTag = "NavigationSystem";

        /// <summary>
        /// Event that gets invoked when the navigation mesh changed. This happens when dynamic obstacles move or get active
        /// </summary>
        public event System.Action OnNavigationMeshChanged = delegate{};

        /// <summary>
        /// Whether all paths need to be recalculated in the next fixed update.
        /// </summary>
        private bool m_NavMeshChanged;

        public void OnDynamicObstacleDisabled()
        {
            m_NavMeshChanged = true;
        }

        public void OnDynamicObstacleEnabled()
        {
            m_NavMeshChanged = true;
        }

        private void FixedUpdate()
        {
            // This is done in fixed update to make sure that only one expensive global recalculation happens per fixed update.
            if (m_NavMeshChanged)
            {
                OnNavigationMeshChanged.Invoke();
                m_NavMeshChanged = false;
            }
        }

        private void OnValidate()
        {
            Assert.AreEqual(NavigationSystemTag, tag, $"The GameObject of the {nameof(NavigationSystem)} component has to use the {NavigationSystem.NavigationSystemTag} tag!");
        }
    }
}
