using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

namespace BossRoom.Server
{
    [DefaultExecutionOrder(10000)] // The enable/disable triggers have to be called after the triggers from NavMeshObstacle which update the nav mesh.
    [RequireComponent(typeof(NavMeshObstacle))]
    public sealed class DynamicNavObstacle : MonoBehaviour
    {
        NavigationSystem m_NavigationSystem;

        void Awake()
        {
            m_NavigationSystem = GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag).GetComponent<NavigationSystem>();
        }

        void OnValidate()
        {
            if (gameObject.scene.rootCount > 1) // Hacky way for checking if this is a scene object or a prefab instance and not a prefab definition.
            {
                Assert.IsNotNull(
                    GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag)?.GetComponent<NavigationSystem>(),
                    $"NavigationSystem not found. Is there a NavigationSystem Behaviour in the Scene and does its GameObject have the {NavigationSystem.NavigationSystemTag} tag?"
                );
            }
        }

        void OnEnable()
        {
            m_NavigationSystem.OnDynamicObstacleEnabled();
        }

        void OnDisable()
        {
            m_NavigationSystem.OnDynamicObstacleDisabled();
        }
    }
}
