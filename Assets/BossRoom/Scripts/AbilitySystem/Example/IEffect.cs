using System;
using Unity.Multiplayer.Samples.BossRoom.Server;
using UnityEngine;

public interface IEffect
{
    void Invoke(AbilityRunner runner, AbilityInstance abilityInstance);
}
//
// [Serializable]
// public class AbilityChannel : IAbilityEffect
// {
//     public int Channel;
// }


[Serializable]
public class PlayAnimationEffect : IEffect
{
    public string Name;

    public void Invoke(AbilityRunner runner, AbilityInstance abilityInstance)
    {
        runner.GetComponentInChildren<Animator>().Play(Name);
    }
}

[Serializable]
public class MeeleAttackEffect : IEffect
{
    public int Damage;
    public float Range;

    public void Invoke(AbilityRunner runner, AbilityInstance abilityInstance)
    {
        Debug.Log("Running meele attack effect");
        // var foe = MeleeAction.GetIdealMeleeFoe(false, /*Collider*/null, Range, 0);
        // if (foe != null)
        // {
        //     foe.ReceiveHP(runner.GetComponent<ServerCharacter>(), -Damage);
        // }
    }

}

public class End : IEffect
{
    public void Invoke(AbilityRunner runner, AbilityInstance abilityInstance)
    {
        runner.EndAbility(abilityInstance);
    }
}

// public class MoveTowardsPosition : IAbilityEffect
// {
//     public float Speed;
//
//     public void Invoke(AbilityRunner runner)
//     {
//
//     }
// }

[Serializable]
public struct TimeTriggerOld
{
    public float Time;
    public int TriggerId;
}
//
// struct Effect
// {
//     int Type;
//     ulong Data;
//
//     public static void* GetEffect()
//     {
//
//     }
// }
//
// struct AttackEffect : INativeEffect
// {
//     int Type;
//     int Damage;
//     int Whatever;
//     GCHandle MyGameObject;
//
//     void DrawGui()
//     {
//         (GameObject)MyGameObject.Target
//         Damage = EditorGUI.IntField(Damage);
//     }
// }
