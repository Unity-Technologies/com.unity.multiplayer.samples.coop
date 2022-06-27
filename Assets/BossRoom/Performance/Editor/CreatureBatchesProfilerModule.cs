using Unity.Profiling;
using Unity.Profiling.Editor;

[System.Serializable]
[ProfilerModuleMetadata("Creature Batches")]
public class CreatureBatchesProfilerModule : ProfilerModule
{
    static readonly ProfilerCounterDescriptor[] k_Counters =
    {
        new("Creature Count", ProfilerCategory.Scripts),
        new("Batches Count", ProfilerCategory.Render),
    };

    public CreatureBatchesProfilerModule() : base(k_Counters) { }
}
