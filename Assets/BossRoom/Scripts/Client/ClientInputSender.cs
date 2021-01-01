using MLAPI;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Captures inputs for a character on a client and sends them to the server.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientInputSender : NetworkedBehaviour
    {
        private NetworkCharacterState m_NetworkCharacter;

        /// <summary>
        /// We detect clicks in Update (because you can miss single discrete clicks in FixedUpdate). But we need to 
        /// raycast in FixedUpdate, because raycasts done in Update won't work reliably. 
        /// This nullable vector will be set to a screen coordinate when an attack click was made. 
        /// </summary>
        private System.Nullable<Vector3> m_AttackClickRequest;

        public override void NetworkStart()
        {
            // TODO Don't use NetworkedBehaviour for just NetworkStart [GOMPS-81]
            if (!IsClient || !IsOwner)
            {
                enabled = false;
            }
        }


        void Awake()
        {
            m_NetworkCharacter = GetComponent<NetworkCharacterState>();
        }

        void FixedUpdate()
        {
            // TODO replace with new Unity Input System [GOMPS-81]

            // Is mouse button pressed (not just checking for down to allow continuous movement inputs by holding the mouse button down)
            if (Input.GetMouseButton(0))
            {
                RaycastHit hit;
                
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    // The MLAPI_INTERNAL channel is a reliable sequenced channel. Inputs should always arrive and be in order that's why this channel is used.
                    m_NetworkCharacter.InvokeServerRpc(m_NetworkCharacter.SendCharacterInputServerRpc, hit.point,
                        "MLAPI_INTERNAL");
                }
            }

            if (m_AttackClickRequest != null)
            {
                RaycastHit hit;

                if (Physics.Raycast(Camera.main.ScreenPointToRay(m_AttackClickRequest.Value), out hit) && GetTargetObject(ref hit) != 0)
                {
                    //these two actions will queue one after the other, causing us to run over to our target and take a swing. 
                    var chase_data = new ActionRequestData();
                    chase_data.ActionTypeEnum = Action.GENERAL_CHASE;
                    chase_data.Amount = 3f;
                    chase_data.TargetIds = new ulong[] { GetTargetObject(ref hit) };
                    m_NetworkCharacter.C2S_DoAction(ref chase_data);

                    var hit_data = new ActionRequestData();
                    hit_data.ShouldQueue = true; //wait your turn--don't clobber the chase action. 
                    hit_data.ActionTypeEnum = Action.TANK_BASEATTACK;
                    m_NetworkCharacter.C2S_DoAction(ref hit_data);
                }
                else
                {
                    var data = new ActionRequestData();
                    data.ActionTypeEnum = Action.TANK_BASEATTACK;
                    m_NetworkCharacter.C2S_DoAction(ref data);
                }

                m_AttackClickRequest = null;
            }
        }

        private void Update()
        {
            //we do this in "Update" rather than "FixedUpdate" because discrete clicks can be missed in FixedUpdate. 
            if (Input.GetMouseButtonDown(1))
            {
                m_AttackClickRequest = Input.mousePosition;
            }
        }

        /// <summary>
        /// Gets the Target NetworkId from the Raycast hit, or 0 if Raycast didn't contact a Networked Object. 
        /// </summary>
        private ulong GetTargetObject(ref RaycastHit hit )
        {
            if( hit.collider == null ) { return 0; }
            var targetObj = hit.collider.GetComponent<NetworkedObject>();
            if( targetObj == null ) { return 0;  }

            return targetObj.NetworkId;
        }
    }
}
