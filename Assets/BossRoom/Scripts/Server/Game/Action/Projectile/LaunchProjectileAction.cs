using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BossRoom.Server
{
    public class LaunchProjectileAction : Action
    {
        private bool m_Launched = false;

        public LaunchProjectileAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            if( Description.Spawns == null || Description.Spawns.Length == 0 )
            {
                Debug.LogWarning("Misconfigured Action: " + Description.ActionTypeEnum + ", it uses LaunchProjectileAction, but has no spawns defined");
            }

            //TODO, create the projectile.
            return false;
        }

        public override bool Update()
        {
            //if( (Time.time-TimeStarted))

            return false;
        }

        private void LaunchProjectile()
        {
            if(!m_Launched)
            {
                m_Launched = true;

                //TODO: use object pooling for arrows. 
                GameObject projectile = UnityEngine.Object.Instantiate(Description.Spawns[0]);
                projectile.transform.forward = m_Parent.transform.forward;

                //this way, you just need to "place" the arrow by moving it in the prefab, and that will control
                //where it appears next to the player. 
                projectile.transform.position = m_Parent.transform.localToWorldMatrix.MultiplyPoint(projectile.transform.position);

                projectile.GetComponent<MLAPI.NetworkedObject>().Spawn(null, true);
            }
        }

        public override void End()
        {
            //make sure this happens. 
            LaunchProjectile();
        }
    }
}
