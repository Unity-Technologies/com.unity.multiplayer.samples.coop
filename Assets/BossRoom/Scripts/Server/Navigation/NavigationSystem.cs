using NUnit.Framework;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// This system exists to coordinate path finding and navigation functionality in a scene.
    /// The Unity NavMesh is only used to calculate navigation paths. Moving along those paths is done by this system.
    /// </summary>
    public class NavigationSystem : MonoBehaviour
    {
        public const string NavigationSytemTag = "NavigationSystem";

        /// <summary>
        /// Event that gets invoked when the navigation mesh changed. This happens when dynamic obstacles move or get active
        /// </summary>
        public event System.Action OnNavigationMeshChanged = delegate{};

        /// <summary>
        /// Whether all paths need to be recalculated in the next fixed update.
        /// </summary>
        private bool navMeshChanged;

        public void OnDynamicObstacleDisabled()
        {
            navMeshChanged = true;
        }

        public void OnDynamicObstacleEnabled()
        {
            navMeshChanged = true;
        }

        private void FixedUpdate()
        {
            // This is done in fixed update to make sure that only one expensive global recalculation happens per fixed update.
            if (navMeshChanged)
            {
                OnNavigationMeshChanged.Invoke();
                navMeshChanged = false;
            }
        }

        private void OnValidate()
        {
            Assert.AreEqual(NavigationSytemTag, tag, $"The GameObject of the {nameof(NavigationSystem)} component has to use the {NavigationSystem.NavigationSytemTag} tag!");
        }
    }
}