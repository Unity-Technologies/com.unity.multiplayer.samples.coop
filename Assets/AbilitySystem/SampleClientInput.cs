using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SampleClientInput : MonoBehaviour
{
    AbilityRunner m_AbilityRunner;

    [SerializeField]
    AbilityTemplate MoveAbility;
    [SerializeField]
    AbilityTemplate AttackAbility;

    private void Awake()
    {
        m_AbilityRunner = GetComponent<AbilityRunner>();
    }

    private void Update()
    {
        Vector3 position = Vector3.zero;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            position = hitInfo.point;
        }

        if (Input.GetMouseButton(0))
        {
            var context = MoveAbility.BeginActivate();
            context.SetValueData(position);
            m_AbilityRunner.InvokeAbilityNetworked(context);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            var context = AttackAbility.BeginActivate();

            var targetData = new AbilityTarget()
            {
                TargetNetworkObject = default,
                TargetPosition = position,
            };
            context.SetNetworkSerializableData(targetData);

            m_AbilityRunner.InvokeAbilityNetworked(context);
        }
    }
}

struct AbilityTarget : INetworkSerializable
{
    public NetworkObjectReference TargetNetworkObject;
    public Vector3 TargetPosition;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeNetworkSerializable(ref TargetNetworkObject);
        if (TargetNetworkObject.TryGet(out _) == false)
        {
            serializer.SerializeValue(ref TargetPosition);
        }
    }
}
