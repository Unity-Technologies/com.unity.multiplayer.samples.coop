using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BossRoom.Server
{
    public class LaunchProjectileAction : Action
    {
        public LaunchProjectileAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            //TODO, create the projectile.
            return false;
        }

        public override bool Update()
        {
            return false;
        }
    }
}
