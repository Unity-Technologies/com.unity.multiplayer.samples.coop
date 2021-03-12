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

            if (Input.GetMouseButtonUp(0))
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
                    m_PlayerOwner.RecvDoActionServerRPC(data);
                }
                Destroy(gameObject);
                return;
            }
        }
    }
}
