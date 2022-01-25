using BossRoom.Scripts.Shared.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// An entry point to the application, where we bind all the common dependencies to the root DI scope
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private UpdateRunner m_UpdateRunner;

        private void Awake()
        {
            DontDestroyOnLoad(m_UpdateRunner.gameObject);

            var scope = DIScope.RootScope;

            scope.BindInstanceAsSingle(m_UpdateRunner);

            scope.FinalizeScopeConstruction();

            SceneManager.LoadScene("MainMenu");
        }
    }
}

