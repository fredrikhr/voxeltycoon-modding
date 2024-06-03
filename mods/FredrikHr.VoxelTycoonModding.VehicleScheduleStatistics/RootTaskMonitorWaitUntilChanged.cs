using UnityEngine;

namespace FredrikHr.VoxelTycoonModding.VehicleScheduleStatistics;

public class RootTaskMonitorWaitUntilChanged(
    RootTaskMonitorSharedData monitorData
    ) : CustomYieldInstruction()
{
    public bool IsChanged()
    {
        return (monitorData.RootTask.IsStarted != monitorData.IsStarted)
            || (monitorData.RootTask.IsCompleted != monitorData.IsCompleted)
            || (monitorData.RootTask.IsCancelled != monitorData.IsCancelled);
    }

    public override bool keepWaiting => !IsChanged();
}
