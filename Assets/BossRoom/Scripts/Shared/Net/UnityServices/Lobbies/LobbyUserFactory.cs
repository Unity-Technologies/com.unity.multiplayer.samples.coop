using BossRoom.Scripts.Shared.Infrastructure;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    public class LobbyUserFactory
    {
        private readonly IInstanceResolver m_diScope;

        public LobbyUserFactory(IInstanceResolver scope)
        {
            m_diScope = scope;
        }
        
        public LobbyUser Create()
        {
            var user = new LobbyUser();
            m_diScope.Inject(user);
            return user;
        }
    }
}
