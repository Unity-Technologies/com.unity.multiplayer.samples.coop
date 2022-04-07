using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

public partial class AbilityRunner : NetworkBehaviour
{
    byte m_NextId;

    public Dictionary<Type, Object> MyComponetns = new Dictionary<Type, Object>();

    private List<AbilityInstance> m_LocalActiveAbilities = new List<AbilityInstance>();

    private NetworkList<AbilityInstance> m_SyncedAbilities = new NetworkList<AbilityInstance>();

    [SerializeField]
    private List<Object> m_Abilities = new List<Object>();

    [ServerRpc]
    private void RunAbilityServerRpc(ActivationContext context)
    {
        context.InitializeWriterFromReader();
        m_SyncedAbilities.Add(new AbilityInstance() { StartTime = Time.deltaTime, Context = context, InstanceId = m_NextId++, IsNetworked = true});
    }

    public void InvokeAbilityNetworked(ActivationContext context)
    {
        RunAbilityServerRpc(context);
    }

    // Run local do not sync to other clients
    public void InvokeAbilityLocal(ActivationContext context)
    {
        var instance = new AbilityInstance() { StartTime = Time.deltaTime, Context = context, InstanceId = m_NextId++, IsNetworked = false };
        m_LocalActiveAbilities.Add(instance);
        ((IAbilityDefinition)m_Abilities[context.AbilityId]).OnStart(instance);
    }

    public IAbility GetAbility(AbilityInstance instance)
    {
        return null;
    }


    public void EndAbility(AbilityInstance abilityInstance)
    {
        if (abilityInstance.IsNetworked)
        {
            if (IsServer == false)
            {
                throw new NotServerException("Only server can end a networked ability");
            }
            m_SyncedAbilities.Remove(abilityInstance);
        }
        else
        {
            m_LocalActiveAbilities.Remove(abilityInstance);
            ((IAbilityDefinition)m_Abilities[abilityInstance.Context.AbilityId]).OnEnd(abilityInstance);
            abilityInstance.Context.Dispose();
        }
    }

    public void Awake()
    {
        m_SyncedAbilities.OnListChanged += SyncedAbilitiesOnListChanged;
    }

    void SyncedAbilitiesOnListChanged(NetworkListEvent<AbilityInstance> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<AbilityInstance>.EventType.Add:
            case NetworkListEvent<AbilityInstance>.EventType.Insert:
                ((IAbilityDefinition)m_Abilities[changeEvent.Value.Context.AbilityId]).OnStart(changeEvent.Value);
                return;
            case NetworkListEvent<AbilityInstance>.EventType.Remove:
            case NetworkListEvent<AbilityInstance>.EventType.RemoveAt:
                ((IAbilityDefinition)m_Abilities[changeEvent.Value.Context.AbilityId]).OnEnd(changeEvent.Value);
                changeEvent.Value.Context.Dispose();
                return;
            default:
                throw new InvalidOperationException();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        m_SyncedAbilities.OnListChanged -= SyncedAbilitiesOnListChanged;
    }
}

public static class AbilityRunnerExt
{
    public static ActivationContext BeginActivate(this IAbilityDefinition abilityDefinition)
    {
        return new ActivationContext(abilityDefinition);
    }
}
