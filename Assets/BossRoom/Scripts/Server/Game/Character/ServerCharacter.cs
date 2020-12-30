using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ServerCharacter : MLAPI.NetworkedBehaviour
    {
        public NetworkCharacterState NetState { get; private set; }

        private ActionPlayer m_actionPlayer;

        // Start is called before the first frame update
        void Start()
        {
            NetState = GetComponent<NetworkCharacterState>();
            m_actionPlayer = new ActionPlayer(this);
        }

        public override void NetworkStart()
        {
            if (!IsServer) { this.enabled = false; }
            else
            {
                this.NetState.DoActionEvent += this.OnActionPlayRequest;
            }
        }

        public void PlayAction(ref ActionRequestData data )
        {
            this.m_actionPlayer.PlayAction(ref data);
        }

        private void OnActionPlayRequest( ActionRequestData data )
        {
            Debug.Log("Server receiving action request for " + data.ActionTypeEnum);
            this.PlayAction(ref data);
        }

        // Update is called once per frame
        void Update()
        {
            m_actionPlayer.Update();
        }
    }
}
