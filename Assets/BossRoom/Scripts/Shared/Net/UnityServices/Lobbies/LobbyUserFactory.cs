using BossRoom.Scripts.Shared.Infrastructure;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    public class LobbyUserFactory
    {
        private IInstanceResolver m_diScope;

        [Inject]
        private void InjectDependencies(IInstanceResolver scope)
        {
            m_diScope = scope;
        }

        public LobbyUserFactory()
        {

        }

        public LobbyUser Create()
        {
            var user = new LobbyUser();
            m_diScope.Inject(user);
            return user;
        }
    }
}
