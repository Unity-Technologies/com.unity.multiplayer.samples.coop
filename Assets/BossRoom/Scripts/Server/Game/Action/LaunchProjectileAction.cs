using MLAPI;
using UnityEngine;


namespace BossRoom.Server
{
    /// <summary>
    /// Action responsible for creating a projectile object.
    /// </summary>
    public class LaunchProjectileAction : Action
    {
        private bool m_Launched = false;

        public LaunchProjectileAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            //snap to face the direction we're firing, and then broadcast the animation, which we do immediately.
            m_Parent.transform.forward = Data.Direction;
            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool Update()
        {
            if (TimeRunning >= Description.ExecTimeSeconds && !m_Launched)
            {
                LaunchProjectile();
            }

            return true;
        }

        /// <summary>
        /// Looks through the ProjectileInfo list and finds the appropriate one to instantiate.
        /// For the base class, this is always just the first entry with a valid prefab in it!
        /// </summary>
        /// <exception cref="System.Exception">thrown if no Projectiles are valid</exception>
        protected virtual ActionDescription.ProjectileInfo GetProjectileInfo()
        {
            foreach (var projectileInfo in Description.Projectiles)
            {
                if (projectileInfo.ProjectilePrefab && projectileInfo.ProjectilePrefab.GetComponent<NetworkProjectileState>())
                    return projectileInfo;
            }
            throw new System.Exception($"Action {Description.ActionTypeEnum} has no usable Projectiles!");
        }

        /// <summary>
        /// Instantiates and configures the arrow. Repeatedly calling this does nothing.
        /// </summary>
        /// <remarks>
        /// This calls GetProjectilePrefab() to find the prefab it should instantiate.
        /// </remarks>
        protected void LaunchProjectile()
        {
            if (!m_Launched)
            {
                m_Launched = true;

                var projectileInfo = GetProjectileInfo();
                GameObject projectile = Object.Instantiate(projectileInfo.ProjectilePrefab);

                // point the projectile the same way we're facing
                projectile.transform.forward = m_Parent.transform.forward;

                //this way, you just need to "place" the arrow by moving it in the prefab, and that will control
                //where it appears next to the player.
                projectile.transform.position = m_Parent.transform.localToWorldMatrix.MultiplyPoint(projectile.transform.position);
                projectile.GetComponent<ServerProjectileLogic>().Initialize(m_Parent.NetworkObjectId, in projectileInfo);

                projectile.GetComponent<NetworkObject>().Spawn();
            }
        }

        public override void End()
        {
            //make sure this happens.
            LaunchProjectile();
        }
    }
}
