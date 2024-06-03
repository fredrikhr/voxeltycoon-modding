using System.Collections;

using UnityEngine;

using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace FredrikHr.VoxelTycoonModding.VehicleScheduleStatistics;

public class RootTaskMonitorManager : LazyManager<RootTaskMonitorManager>
{
    private readonly Action<Vehicle, VehicleManagerChangedEventType> onVehicleChanged;
    private readonly Dictionary<Vehicle, List<RootTaskMonitorSharedData>> monitoredVehicles = [];

    public event Action<RootTask>? RootTaskStarted;
    public event Action<RootTask>? RootTaskCompleted;
    public event Action<RootTask>? RootTaskCancelled;

    public void StartMonitor(RootTask rootTask)
    {
        var vehicle = rootTask.Vehicle;
        RootTaskMonitorSharedData monitor;
        lock (monitoredVehicles)
        {
            if (monitoredVehicles.TryGetValue(vehicle, out var monitors))
            {
                if (monitors.FirstOrDefault(m => m.RootTask == rootTask) is not null)
                    return;
                monitor = new(rootTask);
                monitors.Add(monitor);
            }
            else
            {
                monitor = new(rootTask);
                monitoredVehicles[vehicle] = [monitor];
            }
            monitor.Coroutine = vehicle.StartCoroutine(RunMonitor(monitor));
        }
    }

    public void StopMonitor(RootTask rootTask)
    {
        var vehicle = rootTask.Vehicle;
        RootTaskMonitorSharedData? monitor = null;
        lock (monitoredVehicles)
        {
            if (monitoredVehicles.TryGetValue(vehicle, out var monitors))
            {
                for (int i = 0; i < monitors.Count; i++)
                {
                    monitor = monitors[i];
                    if (monitor.RootTask == rootTask)
                    {
                        monitors.RemoveAt(i);
                        break;
                    }
                    else
                        monitor = null;
                }
                if (monitors.Count == 0) monitoredVehicles.Remove(vehicle);
            }
        }
        var coroutine = monitor?.Coroutine;
        if (coroutine is not null) vehicle.StopCoroutine(coroutine);
    }

    private IEnumerator RunMonitor(RootTaskMonitorSharedData monitorData)
    {
        var rootTask = monitorData.RootTask;
        var vehicle = monitorData.Vehicle;
        try
        {
            var waitUntilYield = new RootTaskMonitorWaitUntilChanged(monitorData);
            do
            {
                yield return waitUntilYield;
                var (isStarted, isCompleted, isCancelled) =
                    (monitorData.IsStarted, monitorData.IsCompleted, monitorData.IsCancelled);
                if (!isStarted && rootTask.IsStarted)
                    RootTaskStarted?.Invoke(rootTask);
                if (!isCompleted && rootTask.IsCompleted)
                    RootTaskCompleted?.Invoke(rootTask);
                if (!isCancelled && rootTask.IsCancelled)
                    RootTaskCancelled?.Invoke(rootTask);
            } while (ContinueUpdatedMonitor(vehicle, monitorData));
        }
        finally
        {
            lock (monitoredVehicles)
            {
                if (monitoredVehicles.TryGetValue(vehicle, out var monitors))
                {
                    monitors.Remove(monitorData);
                    if (monitors.Count == 0) monitoredVehicles.Remove(vehicle);
                }
            }
        }

        static bool ContinueUpdatedMonitor(Vehicle vehicle, RootTaskMonitorSharedData monitorData)
        {
            if (monitorData.Coroutine is null) return false;
            VehicleSchedule schedule = vehicle.Schedule;
            if (vehicle.Schedule != schedule) return false;
            RootTask rootTask = monitorData.RootTask;
            if (!schedule.GetTasks().Contains(rootTask)) return false;
            (monitorData.IsStarted, monitorData.IsCompleted, monitorData.IsCancelled) =
                (rootTask.IsStarted, rootTask.IsCompleted, rootTask.IsCancelled);
            return true;
        }
    }

    public RootTaskMonitorManager() : base()
    {
        onVehicleChanged = (vehicle, change) =>
        {
            switch (change)
            {
                case VehicleManagerChangedEventType.Removed:
                    OnVehicleRemoved(vehicle);
                    break;
            }
        };
    }

    protected override void OnInitialize()
    {
        VehicleManager.Current.Changed += onVehicleChanged;
    }

    protected override void OnDeinitialize()
    {
        VehicleManager.Current.Changed -= onVehicleChanged;
    }

    private void OnVehicleRemoved(Vehicle vehicle)
    {
        List<RootTaskMonitorSharedData>? monitors;
        lock (monitoredVehicles)
        {
            if (monitoredVehicles.TryGetValue(vehicle, out monitors))
            {
                monitoredVehicles.Remove(vehicle);
            }
        }
        foreach (var monitor in monitors ?? [])
        {
            var coroutine = monitor.Coroutine;
            if (coroutine is not null)
            {
                vehicle.StopCoroutine(coroutine);
                monitor.Coroutine = null;
            }
        }
    }
}
