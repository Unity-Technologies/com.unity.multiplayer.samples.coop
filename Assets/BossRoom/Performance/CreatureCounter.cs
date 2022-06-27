using UnityEngine;
using Unity.Profiling;

public class CreatureCounter : MonoBehaviour
{
    static readonly ProfilerCounterValue<int> k_CreatureCounter = new(
        ProfilerCategory.Scripts,
        "Creature Count",
        ProfilerMarkerDataUnit.Count,
        ProfilerCounterOptions.FlushOnEndOfFrame);

    void OnEnable()
    {
        k_CreatureCounter.Value += 1;
    }

    void OnDisable()
    {
        k_CreatureCounter.Value -= 1;
    }
}
