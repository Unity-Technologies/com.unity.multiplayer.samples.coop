using Unity.Netcode;
using UnityEngine;
using BossRoom.Scripts.Shared.Net.NetworkObjectPool;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// Action responsible for creating a projectile object.
    /// </summary>
    public class LaunchProjectileAction : Action
    {
        private bool m_Launched = false;


        public LaunchProjectileAction(ref ActionRequestData data)
            : base(ref data) { }

        public override bool OnStart(ServerCharacter parent)
        {
            //snap to face the direction we're firing, and then broadcast the animation, which we do immediately.
            parent.physicsWrapper.Transform.forward = Data.Direction;

            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
            parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            if (TimeRunning >= Description.ExecTimeSeconds && !m_Launched)
            {
                LaunchProjectile(parent);
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
        protected void LaunchProjectile(ServerCharacter parent)
        {
            if (!m_Launched)
            {
                m_Launched = true;

                var projectileInfo = GetProjectileInfo();

                NetworkObject no = NetworkObjectPool.Singleton.GetNetworkObject(projectileInfo.ProjectilePrefab, projectileInfo.ProjectilePrefab.transform.position, projectileInfo.ProjectilePrefab.transform.rotation);
                // point the projectile the same way we're facing
                no.transform.forward = parent.physicsWrapper.Transform.forward;

                //this way, you just need to "place" the arrow by moving it in the prefab, and that will control
                //where it appears next to the player.
                no.transform.position = parent.physicsWrapper.Transform.localToWorldMatrix.MultiplyPoint(no.transform.position);

                no.GetComponent<ServerProjectileLogic>().Initialize(parent.NetworkObjectId, projectileInfo);

                no.Spawn(true);
            }
        }

        public override void End(ServerCharacter parent)
        {
            //make sure this happens.
            LaunchProjectile(parent);
        }

        public override void Cancel(ServerCharacter parent)
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
            }
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            return ActionConclusion.Continue;
        }

    }
}
