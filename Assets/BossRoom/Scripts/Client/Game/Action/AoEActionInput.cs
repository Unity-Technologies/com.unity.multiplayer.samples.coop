using System.Collections;
using System.Collections.Generic;
using BossRoom;
using UnityEngine;

/*
 * First step in AoE ability. Will update the initial input visuals and will be in charge of tracking the user inputs. Once the ability
 * is confirmed and the mouse is clicked, it'll send the appropriate RPC to the server, triggering the AoE serer side gameplay logic.
 * The server side gameplay action will then trigger the client side resulting FX.
 * This action's flow is this: (Client) AoEActionInput --> (Server) AoEAction --> (Client) AoEActionFX
 */
public class AoEActionInput : BaseActionInput
{
    void Start()
    {
        var radius = GameDataSource.Instance.ActionDataByType[m_ActionType].Radius;
        this.transform.localScale = new Vector3(radius, radius, radius);
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            var data = new ActionRequestData()
            {
                Position = this.transform.position,
                ActionTypeEnum = m_ActionType,
                ShouldQueue = false,
                TargetIds = null
            };
            m_PlayerOwner.ClientSendActionRequest(ref data);
            Destroy(this.gameObject);
            return;
        }

        int layerMask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, float.PositiveInfinity, layerMask))
        {
            this.transform.position = hit.point;
        }
    }
}
