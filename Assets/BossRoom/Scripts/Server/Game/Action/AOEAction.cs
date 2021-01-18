using BossRoom;
using BossRoom.Server;

public class AOEAction : Action
{
    public AOEAction(ServerCharacter parent, ref ActionRequestData data, int level) : base(parent, ref data, level)
    {

    }

    public override bool Start()
    {
        return true;
    }

    public override bool Update()
    {
        return false;
    }
}
