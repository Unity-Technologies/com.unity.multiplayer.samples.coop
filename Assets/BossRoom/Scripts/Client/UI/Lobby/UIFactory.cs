using BossRoom.Scripts.Shared.Infrastructure;
using UnityEngine;

namespace BossRoom.Scripts.Client.UI
{
    public class UIFactory
    {
        private readonly IInstanceResolver m_Scope;

        [Inject]
        public UIFactory(IInstanceResolver scope)
        {
            m_Scope = scope;
        }

        public GameObject InstantiateActive(GameObject source, Transform parent)
        {
            var copy = Object.Instantiate(source, parent);
            copy.SetActive(true);
            m_Scope.Inject(copy);
            return copy;
        }
    }
}
