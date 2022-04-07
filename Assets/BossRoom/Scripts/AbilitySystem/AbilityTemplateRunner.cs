using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityTemplateRunner : AbilityRunner
{
    float m_LastTime;

    void HandleAbilityTimeTriggers(AbilityInstance abilityInstance, float time)
    {
        AbilityTemplate ability = GetAbility(abilityInstance);
        var iterator = TriggerRegistry.GetTriggerIterator(abilityInstance.InstanceId);
        float abilityTime = time - abilityInstance.StartTime;

        foreach (var trigger in iterator)
        {
            if (trigger.Is<AlwaysTrigger>())
            {
                ability.Events[trigger.EventIndex].Invoke(this, abilityInstance);
            }

            if (trigger.Is<TimeTrigger>())
            {
                var timeTrigger = trigger.As<TimeTrigger>();
                if (timeTrigger.Data.Time >= abilityTime && timeTrigger.Data.Time < m_LastTime)
                {
                    ability.Events[trigger.EventIndex].Invoke(this, abilityInstance);
                }
            }
        }
    }
}

