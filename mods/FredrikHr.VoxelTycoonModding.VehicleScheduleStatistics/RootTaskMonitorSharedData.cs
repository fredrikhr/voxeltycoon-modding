using UnityEngine;

using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace FredrikHr.VoxelTycoonModding.VehicleScheduleStatistics;

public class RootTaskMonitorSharedData(RootTask rootTask)
{
    public Vehicle Vehicle { get; init; } = rootTask.Vehicle;
    public VehicleSchedule Schedule { get; init; } = rootTask.Schedule;
    public RootTask RootTask { get; init; } = rootTask;
    public bool IsStarted { get; set; } = rootTask.IsStarted;
    public bool IsCompleted { get; set; } = rootTask.IsCompleted;
    public bool IsCancelled { get; set; } = rootTask.IsCancelled;
    public Coroutine? Coroutine { get; set; }
}
