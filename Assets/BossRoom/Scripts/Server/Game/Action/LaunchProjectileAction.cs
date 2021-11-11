using Unity.Netcode;
using UnityEngine;
using BossRoom.Scripts.Shared.Net.NetworkObjectPool;

namespace Unity.Multiplayer.Samples.BossRoom.Server
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
            m_Parent.physicsWrapper.Transform.forward = Data.Direction;

            m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
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

                NetworkObject no = NetworkObjectPool.Singleton.GetNetworkObject(projectileInfo.ProjectilePrefab, projectileInfo.ProjectilePrefab.transform.position, projectileInfo.ProjectilePrefab.transform.rotation);
                // point the projectile the same way we're facing
                no.transform.forward = m_Parent.physicsWrapper.Transform.forward;

                //this way, you just need to "place" the arrow by moving it in the prefab, and that will control
                //where it appears next to the player.
                no.transform.position = m_Parent.physicsWrapper.Transform.localToWorldMatrix.MultiplyPoint(no.transform.position);

                no.GetComponent<ServerProjectileLogic>().Initialize(m_Parent.NetworkObjectId, projectileInfo);

                no.Spawn(true);
            }
        }

        public override void End()
        {
            //make sure this happens.
            LaunchProjectile();
        }

        public override void Cancel()
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
            }
        }
    }
}
