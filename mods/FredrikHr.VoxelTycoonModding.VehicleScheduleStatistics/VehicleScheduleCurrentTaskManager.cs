using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace FredrikHr.VoxelTycoonModding.VehicleScheduleStatistics;

public class VehicleScheduleCurrentTaskManager : LazyManager<VehicleScheduleCurrentTaskManager>
{
    private readonly HashSet<int> monitoredVehicles = [];

    private class VehicleScheduleWaitWhileCurrentTask : CustomYieldInstruction
    {
        public VehicleSchedule Schedule { get; set; }
        public VehicleTask CurrentTask { get; set; }

        public override bool keepWaiting => ReferenceEquals(CurrentTask, Schedule.CurrentTask);
    }

    public event PropertyChangedEventHandler<VehicleSchedule, VehicleTask> CurrentTaskChanged;

    public void Monitor(Vehicle vehicle)
    {
        if (monitoredVehicles.Add(vehicle.Id))
        {
            vehicle.StartCoroutine(MontorCoroutine(vehicle));
        }
    }

    private IEnumerator MontorCoroutine(Vehicle vehicle)
    {
        var coroutineWait = new VehicleScheduleWaitWhileCurrentTask()
        {
            Schedule = vehicle.Schedule,
            CurrentTask = vehicle.CurrentTask,
        };

        while (vehicle.IsEnabled)
        {
            yield return coroutineWait;
            CurrentTaskChanged?.Invoke(coroutineWait.Schedule, coroutineWait.CurrentTask, coroutineWait.Schedule.CurrentTask);
            coroutineWait.CurrentTask = coroutineWait.Schedule.CurrentTask;
        }
        monitoredVehicles.Remove(vehicle.Id);
    }
}
