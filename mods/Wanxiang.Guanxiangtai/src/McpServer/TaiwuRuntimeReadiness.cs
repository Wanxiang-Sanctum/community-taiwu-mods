using System.Diagnostics;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static class TaiwuRuntimeReadiness
{
    private static readonly TimeSpan RuntimeReadyWait = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    public static async Task<TaiwuLifecycleToolJson.RuntimeReadyWaitResult> WaitAsync(
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        PluginRuntimeAvailability availability;

        while (true)
        {
            availability = await PluginIpcProxy.GetRuntimeAvailabilityAsync(cancellationToken);
            if (availability.IsReady)
            {
                return TaiwuLifecycleToolJson.RuntimeReadyWaitResult.Ready(
                    GetElapsedMilliseconds(stopwatch),
                    availability);
            }

            if (stopwatch.Elapsed >= RuntimeReadyWait)
            {
                return TaiwuLifecycleToolJson.RuntimeReadyWaitResult.TimedOut(
                    GetElapsedMilliseconds(stopwatch),
                    availability);
            }

            await Task.Delay(GetNextDelay(stopwatch.Elapsed, RuntimeReadyWait), cancellationToken);
        }
    }

    private static int GetElapsedMilliseconds(Stopwatch stopwatch)
    {
        return (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue);
    }

    private static TimeSpan GetNextDelay(TimeSpan elapsed, TimeSpan timeout)
    {
        TimeSpan remaining = timeout - elapsed;
        return remaining < PollInterval ? remaining : PollInterval;
    }
}
