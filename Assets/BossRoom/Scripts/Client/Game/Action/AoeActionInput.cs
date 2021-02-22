using UnityEngine;

namespace BossRoom.Visual
{
    /// This class is the first step in AoE ability. It will update the initial input visuals' position and will be in charge
    /// of tracking the user inputs. Once the ability
    /// is confirmed and the mouse is clicked, it'll send the appropriate RPC to the server, triggering the AoE serer side gameplay logic.
    /// The server side gameplay action will then trigger the client side resulting FX.
    /// This action's flow is this: (Client) AoEActionInput --> (Server) AoEAction --> (Client) AoEActionFX
    public class AoeActionInput : BaseActionInput
    {
        Camera m_Camera;
        int m_GroundLayerMask;

        RaycastHit[] m_UpdateResult = new RaycastHit[1];

        void Start()
        {
            var radius = GameDataSource.Instance.ActionDataByType[m_ActionType].Radius;
            transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
            m_Camera = Camera.main;
            m_GroundLayerMask = LayerMask.GetMask("Ground");
        }

        void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                var data = new ActionRequestData
                {
                    Position = transform.position,
                    ActionTypeEnum = m_ActionType,
                    ShouldQueue = false,
                    TargetIds = null
                };
                m_PlayerOwner.RecvDoActionServerRPC(data);
                Destroy(gameObject);
                return;
            }
            if (Physics.RaycastNonAlloc(
                ray: m_Camera.ScreenPointToRay(Input.mousePosition),
                results: m_UpdateResult,
                maxDistance: float.PositiveInfinity,
                layerMask: m_GroundLayerMask) > 0)
            {
                transform.position = m_UpdateResult[0].point;
            }
        }
    }
}
