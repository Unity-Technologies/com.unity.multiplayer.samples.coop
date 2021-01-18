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

        /// <summary>
        /// Convenience getter that returns our CharacterData
        /// </summary>
        private CharacterClass CharacterData
        {
            get { return GameDataSource.s_Instance.CharacterDataByType[m_NetworkCharacter.CharacterType.Value]; }
        }

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
            m_NpcLayerMask = LayerMask.NameToLayer("NPCs");

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

                    ActionRequestData playerAction;
                    bool doAction = GetActionRequestForTarget(ref hit, out playerAction);

                    if (doAction)
                    {
                        float range = GameDataSource.s_Instance.ActionDataByType[playerAction.ActionTypeEnum].Range;
                        var chaseData = new ActionRequestData();
                        chaseData.ActionTypeEnum = ActionType.GENERAL_CHASE;
                        chaseData.Amount = range;
                        chaseData.TargetIds = new ulong[] { GetTargetObject(ref hit) };
                        m_NetworkCharacter.ClientSendActionRequest(ref chaseData);
                        m_NetworkCharacter.ClientSendActionRequest(ref playerAction);
                    }
                }
                else
                {
                    var data = new ActionRequestData();
                    data.ActionTypeEnum = CharacterData.Skill1;
                    m_NetworkCharacter.ClientSendActionRequest(ref data);
                }

                m_ClickRequest = null;
            }
        }

        /// <summary>
        /// When you right-click on something you will want to do contextually different things. For example you might attack an enemy,
        /// but revive a friend. You might also decide to do nothing (e.g. right-clicking on a friend who hasn't FAINTED). 
        /// </summary>
        /// <param name="hit">The RaycastHit of the entity we clicked on.</param>
        /// <param name="resultData">Out parameter that will be filled with the resulting action, if any.</param>
        /// <returns>true if we should play an action, false otherwise. </returns>
        private bool GetActionRequestForTarget(ref RaycastHit hit, out ActionRequestData resultData)
        {
            resultData = new ActionRequestData();
            var targetNetState = hit.transform.GetComponent<NetworkCharacterState>();
            if (targetNetState == null)
            {
                //Not a Character. In the future this could represent interacting with some other interactable, but for
                //now, it implies we just do nothing.
                return false;
            }

            if (targetNetState.IsNPC)
            {
                resultData.ShouldQueue = true; //wait your turn--don't clobber the chase action.
                ActionType skill1 = CharacterData.Skill1;
                resultData.ActionTypeEnum = skill1;
                return true;
            }
            else if (targetNetState.NetworkLifeState.Value == LifeState.FAINTED)
            {
                resultData = new ActionRequestData();
                resultData.ShouldQueue = true;
                resultData.ActionTypeEnum = ActionType.GENERAL_REVIVE;
                resultData.TargetIds = new[] { targetNetState.NetworkId };
                return true;
            }

            return false;
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
        private ulong GetTargetObject(ref RaycastHit hit)
        {
            if (hit.collider == null) { return 0; }
            var targetObj = hit.collider.GetComponent<NetworkedObject>();
            if (targetObj == null) { return 0; }

            return targetObj.NetworkId;
        }
    }
}
