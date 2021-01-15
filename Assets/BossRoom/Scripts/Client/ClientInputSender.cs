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
        private int m_NpcLayerMask;
        private NetworkCharacterState m_NetworkCharacter;

        /// <summary>
        /// We detect clicks in Update (because you can miss single discrete clicks in FixedUpdate). But we need to
        /// raycast in FixedUpdate, because raycasts done in Update won't work reliably.
        /// This nullable vector will be set to a screen coordinate when an attack click was made.
        /// </summary>
        private System.Nullable<Vector3> m_ClickRequest;

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
            m_NpcLayerMask = LayerMask.GetMask("NPCs");
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

            if (m_ClickRequest != null)
            {
                RaycastHit hit;

                if (Physics.Raycast(Camera.main.ScreenPointToRay(m_ClickRequest.Value), out hit) && GetTargetObject(ref hit) != 0)
                {
                    //if we have clicked on an enemy:
                    // - two actions will queue one after the other, causing us to run over to our target and take a swing. 
                    //if we have clicked on a fallen friend - we will revive him

                    var chase_data = new ActionRequestData();
                    chase_data.ActionTypeEnum = ActionType.GENERAL_CHASE;
                    chase_data.Amount = ActionData.ActionDescriptions[ActionType.TANK_BASEATTACK][0].Range;
                    chase_data.TargetIds = new ulong[] {GetTargetObject(ref hit)};
                    m_NetworkCharacter.ClientSendActionRequest(ref chase_data);

                    //TODO fixme: there needs to be a better way to check if target is a PC or an NPC
                    bool isTargetingNPC = hit.transform.gameObject.layer == m_NPCLayerMask;

                    if (isTargetingNPC)
                    {
                        var hit_data = new ActionRequestData();
                        hit_data.ShouldQueue = true; //wait your turn--don't clobber the chase action. 
                        hit_data.ActionTypeEnum = ActionType.TANK_BASEATTACK;
                        m_NetworkCharacter.ClientSendActionRequest(ref hit_data);
                    }
                    else
                    {
                        //proceed to revive the target if it's in FAINTED state
                        var targetCharacterState = hit.transform.GetComponent<NetworkCharacterState>();

                        if (targetCharacterState.NetworkLifeState.Value == LifeState.FAINTED)
                        {
                            var revive_data = new ActionRequestData();
                            revive_data.ShouldQueue = true;
                            revive_data.ActionTypeEnum = ActionType.GENERAL_REVIVE;
                            revive_data.TargetIds = new [] {GetTargetObject(ref hit)};
                            m_NetworkCharacter.ClientSendActionRequest(ref revive_data);
                        }
                    }
                }
                else
                {
                    var data = new ActionRequestData();
                    data.ActionTypeEnum = ActionType.TANK_BASEATTACK;
                    m_NetworkCharacter.ClientSendActionRequest(ref data);
                }

                m_ClickRequest = null;
            }
        }

        private void Update()
        {
            //we do this in "Update" rather than "FixedUpdate" because discrete clicks can be missed in FixedUpdate. 
            if (Input.GetMouseButtonDown(1))
            {
                m_ClickRequest = Input.mousePosition;
            }
        }

        /// <summary>
        /// Gets the Target NetworkId from the Raycast hit, or 0 if Raycast didn't contact a Networked Object. 
        /// </summary>
        private ulong GetTargetObject(ref RaycastHit hit )
        {
            if (hit.collider == null) { return 0; }
            var targetObj = hit.collider.GetComponent<NetworkedObject>();
            if (targetObj == null) { return 0;  }

            return targetObj.NetworkId;
        }
    }
}
