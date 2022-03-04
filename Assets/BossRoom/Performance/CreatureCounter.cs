using UnityEngine;
using Unity.Profiling;

public class CreatureCounter : MonoBehaviour
{
    static readonly ProfilerCategory k_BossRoomProfilerCategory = new("Boss Room");
    static readonly ProfilerCounterValue<int> k_CreatureCounter = new(
        k_BossRoomProfilerCategory,
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
