using BossRoom.Scripts.Shared.Infrastructure;
using UnityEngine;

namespace BossRoom.Scripts.Client.UI
{
    public class GameObjectFactory
    {
        private readonly IInstanceResolver m_Scope;

        [Inject]
        public GameObjectFactory(IInstanceResolver scope)
        {
            m_Scope = scope;
        }

        public GameObject InstantiateActive(GameObject source, Transform parent)
        {
            var copy = Object.Instantiate(source, parent);
            copy.SetActive(true);
            m_Scope.InjectIn(copy);
            return copy;
        }
    }
}
