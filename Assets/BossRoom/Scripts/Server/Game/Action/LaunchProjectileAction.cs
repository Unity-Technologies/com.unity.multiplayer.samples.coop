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
                return false;
            }

            //snap to face the direction we're firing!
            m_Parent.transform.forward = Data.Direction;
            return true;
        }

        public override bool Update()
        {
            if( TimeRunning >= Description.ExecTime_s && !m_Launched )
            {
                LaunchProjectile();
            }

            return true;
        }

        private void LaunchProjectile()
        {
            if(!m_Launched)
            {
                m_Launched = true;

                //TODO: use object pooling for arrows. 
                GameObject projectile = UnityEngine.Object.Instantiate(Description.Spawns[0]);
                projectile.transform.forward = Data.Direction;

                //this way, you just need to "place" the arrow by moving it in the prefab, and that will control
                //where it appears next to the player. 
                projectile.transform.position = m_Parent.transform.localToWorldMatrix.MultiplyPoint(projectile.transform.position);
                projectile.GetComponent<NetworkProjectileState>().SourceAction.Value = Data.ActionTypeEnum;
                projectile.GetComponent<ServerProjectileLogic>().SpawnerId = m_Parent.NetworkId;

                projectile.GetComponent<MLAPI.NetworkedObject>().Spawn();
                m_Parent.NetState.ServerBroadcastAction(ref Data);
            }
        }

        public override void End()
        {
            //make sure this happens. 
            LaunchProjectile();
        }
    }
}
