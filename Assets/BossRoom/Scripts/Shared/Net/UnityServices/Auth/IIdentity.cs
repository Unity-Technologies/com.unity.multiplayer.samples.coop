namespace BossRoom.Scripts.Shared.Net.UnityServices.Auth
{
    public interface IIdentity
    {
        SubIdentity GetSubIdentity(IIdentityType identityType);
    }
}
