using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Netcode;
using UnityEngine;

public interface ITrigger
{
    byte EventIndex { get; }
}

[StructLayout(LayoutKind.Explicit)]
public struct Trigger<T> where T : unmanaged, ITrigger
{
    [FieldOffset(0)]
    public T Data;

    [FieldOffset(26)]
    public short AbilityInstanceId;

    [FieldOffset(28)]
    short TypeId;

    [FieldOffset(30)]
    public byte EventIndex;

    public IAbility GetAbility(AbilityRunner runner)
    {
        return null;
    }

    public bool Is<T>() where T: unmanaged, ITrigger
    {
        return true;
    }

    public Trigger<U> As<U>() where U : unmanaged, ITrigger
    {
        return new Trigger<U>(){};
    }
}

public struct TriggerBase : ITrigger
{
}

public struct AlwaysTrigger : ITrigger
{
    public byte EventIndex { get; set; }
}

public struct TimeTrigger : ITrigger
{
    public byte EventIndex { get; set; }

    public float Time;
}

public struct TakeDamageTrigger : ITrigger
{
    public byte EventIndex { get; set; }
}

public struct TriggerRegistry
{
    public IEnumerable<Trigger<T>> GetTriggerIterator<T>() where T : unmanaged
    {
        return null;
    }

    public IEnumerable<Trigger<T>> GetTriggerIterator<V, T>() where T : unmanaged
    {
        return null;
    }

    public IEnumerable<Trigger<T>> GetTriggerIterator<T>(short abilityInstanceId) where T : unmanaged
    {
        return null;
    }

    public IEnumerable<Trigger<TriggerBase>> GetTriggerIterator(short abilityInstanceId)
    {
        return null;
    }

    public bool HasTrigger<T>() where T : unmanaged
    {
        return true;
    }
}

public interface IAbility
{
    IList<ITrigger> GetTriggers();
}

public class ExampleAbility : IAbility
{
    public IList<ITrigger> GetTriggers()
    {
        return new List<ITrigger>()
        {
            new AlwaysTrigger(){EventIndex = 0}
        };
    }

    public void Example(short abilityInstanceId, byte eventIndex)
    {
        Debug.Log($"Ability with instanceId {abilityInstanceId} triggered event with eventIndex: {eventIndex}");
    }
}

public struct AbilityContext
{

}

public partial class AbilityRunner
{
    public TriggerRegistry TriggerRegistry;

    public List<IAbility> Abilities;

    public virtual bool ActivationCheck(AbilityContext context)
    {
        return true;
    }
}

public class CustomActivationCheck : AbilityRunner
{
    public override bool ActivationCheck(AbilityContext context)
    {
        return Random.Range(0, 1f) > 0.5f;
    }
}

public class Example
{
    AbilityRunner m_Runner;

    public void Update()
    {
        // good for generic updates e.g. iterating over all time triggers
        foreach (var trigger in m_Runner.TriggerRegistry.GetTriggerIterator<AlwaysTrigger>())
        {
            var ability = trigger.GetAbility(m_Runner);
            if (ability is ExampleAbility exampleAbility)
            {
                exampleAbility.Example(trigger.AbilityInstanceId, trigger.EventIndex);
            }
        }

        // when we are only interested about a specific ability type
        foreach (var trigger in m_Runner.TriggerRegistry.GetTriggerIterator<ExampleAbility, AlwaysTrigger>())
        {
            ExampleAbility ability = (ExampleAbility)trigger.GetAbility(m_Runner);
            ability.Example(trigger.AbilityInstanceId, trigger.EventIndex);
        }
    }
}

// public void DealDamage()
// {
// // Very fast check for checking whether any trigger exists at all
// if (m_Runner.TriggerRegistry.HasTrigger<TakeDamageTrigger>() == false)
// {
//     return;
// }
// }

public struct EnemyTarget
{
    public NetworkObjectReference Target;
}
