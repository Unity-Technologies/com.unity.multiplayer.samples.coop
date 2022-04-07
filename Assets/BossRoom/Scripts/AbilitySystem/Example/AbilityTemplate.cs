using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "test")]
public class AbilityTemplate : ScriptableObject, IAbilityDefinition, IAbility
{
    [SerializeReference, SerializeReferenceButton]
    public List<IEffect> Events;

    [SerializeField]
    public List<TimeTriggerOld> Triggers;

    public ushort AbilityId { get; set; }

    public void OnStart(AbilityInstance abilityInstance)
    {
    }

    public void OnEnd(AbilityInstance abilityInstance)
    {
    }

    public IList<ITrigger> GetTriggers()
    {
        throw new NotImplementedException();
    }
}
