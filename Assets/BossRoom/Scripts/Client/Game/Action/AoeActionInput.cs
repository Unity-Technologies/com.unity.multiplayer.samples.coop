using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// This class is the first step in AoE ability. It will update the initial input visuals' position and will be in charge
    /// of tracking the user inputs. Once the ability
    /// is confirmed and the mouse is clicked, it'll send the appropriate RPC to the server, triggering the AoE serer side gameplay logic.
    /// The server side gameplay action will then trigger the client side resulting FX.
    /// This action's flow is this: (Client) AoEActionInput --> (Server) AoEAction --> (Client) AoEActionFX
    /// </summary>
    public class AoeActionInput : BaseActionInput
    {
        [SerializeField]
        private GameObject m_InRangeVisualization;

        [SerializeField]
        private GameObject m_OutOfRangeVisualization;

        Camera m_Camera;
        int m_GroundLayerMask;
        Vector3 m_Origin;

        //The general action system works on MouseDown events (to support Charged Actions), but that means that if we only wait for
        //a mouse up event internally, we will fire as part of the same UI click that started the action input (meaning the user would
        //have to drag her mouse from the button to the firing location). Tracking a mouse-down mouse-up cycle means that a user can
        //click on the ground separately from the mouse-click that engaged the action (which also makes the UI flow equivalent to the
        //flow from hitting a number key). 
        bool m_ReceivedMouseDownEvent;

        RaycastHit[] m_UpdateResult = new RaycastHit[1];

        void Start()
        {
            var radius = GameDataSource.Instance.ActionDataByType[m_ActionType].Radius;
            transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
            m_Camera = Camera.main;
            m_GroundLayerMask = LayerMask.GetMask("Ground");
            m_Origin = m_PlayerOwner.transform.position;
        }

        void Update()
        {
            if (Physics.RaycastNonAlloc(
                ray: m_Camera.ScreenPointToRay(Input.mousePosition),
                results: m_UpdateResult,
                maxDistance: float.PositiveInfinity,
                layerMask: m_GroundLayerMask) > 0)
            {
                transform.position = m_UpdateResult[0].point;
            }

            float range = GameDataSource.Instance.ActionDataByType[m_ActionType].Range;
            bool isInRange = (m_Origin - transform.position).sqrMagnitude <= range * range;
            m_InRangeVisualization.SetActive(isInRange);
            m_OutOfRangeVisualization.SetActive(!isInRange);

            // wait for the player to click down and then release the mouse button before actually taking the input
            if (Input.GetMouseButtonDown(0))
            {
                m_ReceivedMouseDownEvent = true;
            }

            if (Input.GetMouseButtonUp(0) && m_ReceivedMouseDownEvent)
            {
                if (isInRange)
                {
                    var data = new ActionRequestData
                    {
                        Position = transform.position,
                        ActionTypeEnum = m_ActionType,
                        ShouldQueue = false,
                        TargetIds = null
                    };
                    m_SendInput(data);
                }
                Destroy(gameObject);
                return;
            }
        }
    }
}
