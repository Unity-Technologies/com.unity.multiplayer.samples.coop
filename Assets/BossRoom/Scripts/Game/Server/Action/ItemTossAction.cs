using UnityEngine;
using BossRoom.Scripts.Shared.Net.NetworkObjectPool;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Action responsible for creating a projectile object.
    /// </summary>
    public class ItemTossAction : Action
    {
        bool m_Launched;

        public ItemTossAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

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
                Throw();
            }

            return true;
        }

        /// <summary>
        /// Looks through the ProjectileInfo list and finds the appropriate one to instantiate.
        /// For the base class, this is always just the first entry with a valid prefab in it!
        /// </summary>
        /// <exception cref="System.Exception">thrown if no Projectiles are valid</exception>
        ActionDescription.ProjectileInfo GetProjectileInfo()
        {
            foreach (var projectileInfo in Description.Projectiles)
            {
                if (projectileInfo.ProjectilePrefab)
                {
                    return projectileInfo;
                }
            }
            throw new System.Exception($"Action {Description.ActionTypeEnum} has no usable Projectiles!");
        }

        /// <summary>
        /// Instantiates and configures the arrow. Repeatedly calling this does nothing.
        /// </summary>
        /// <remarks>
        /// This calls GetProjectilePrefab() to find the prefab it should instantiate.
        /// </remarks>
        void Throw()
        {
            if (!m_Launched)
            {
                m_Launched = true;

                var projectileInfo = GetProjectileInfo();

                var no = NetworkObjectPool.Singleton.GetNetworkObject(projectileInfo.ProjectilePrefab, projectileInfo.ProjectilePrefab.transform.position, projectileInfo.ProjectilePrefab.transform.rotation);

                // point the projectile the same way we're facing
                no.transform.forward = m_Parent.physicsWrapper.Transform.forward;

                no.transform.position = m_Parent.physicsWrapper.Transform.localToWorldMatrix.MultiplyPoint(no.transform.position);
                no.transform.position += no.transform.forward;

                no.Spawn(true);

                // important to add a force AFTER a NetworkObject is spawned, since IsKinematic is enabled on the
                // Rigidbody component after it is spawned
                no.GetComponent<Rigidbody>().AddForce((no.transform.forward * 60f) + (no.transform.up * 150f), ForceMode.Impulse);
            }
        }
    }
}
