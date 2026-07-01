using System.ComponentModel;
using System.Diagnostics;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static class TaiwuProcesses
{
    private const string FrontendProcessName = "The Scroll of Taiwu";

    private const string BackendProcessName = "GameData";

    private const string BackendExecutableSuffix = "Backend\\GameData.exe";

    private static readonly TimeSpan KillExitWait = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan StopCompletionWait = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    public static async Task<TaiwuLifecycleToolJson.ForceStopAttemptResult> ForceStopAsync(
        CancellationToken cancellationToken)
    {
        List<TaiwuLifecycleToolJson.ProcessStopResult> processResults = [];

        processResults.AddRange(
            StopProcessesByName(
                FrontendProcessName,
                "frontend",
                includeProcess: static _ => true,
                killEntireProcessTree: true));

        processResults.AddRange(
            StopProcessesByName(
                BackendProcessName,
                "backend",
                includeProcess: IsTaiwuBackendProcess,
                killEntireProcessTree: false));

        TaiwuLifecycleToolJson.ProcessExitWaitResult exitWait =
            await WaitForExitAsync(cancellationToken);
        return new TaiwuLifecycleToolJson.ForceStopAttemptResult(
            processResults,
            exitWait);
    }

    public static async Task<TaiwuLifecycleToolJson.ProcessExitWaitResult> WaitForExitAsync(
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            List<TaiwuLifecycleToolJson.ProcessObservation> processes;

            try
            {
                processes = ObserveTaiwuProcesses();
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException
                or InvalidOperationException)
            {
                return TaiwuLifecycleToolJson.ProcessExitWaitResult.InspectionFailed(
                    GetElapsedMilliseconds(stopwatch),
                    ex.GetType().Name,
                    ex.Message);
            }

            if (processes.Count == 0)
            {
                return TaiwuLifecycleToolJson.ProcessExitWaitResult.Stopped(
                    GetElapsedMilliseconds(stopwatch));
            }

            if (stopwatch.Elapsed >= StopCompletionWait)
            {
                return TaiwuLifecycleToolJson.ProcessExitWaitResult.TimedOut(
                    GetElapsedMilliseconds(stopwatch),
                    processes);
            }

            await Task.Delay(GetNextDelay(stopwatch.Elapsed, StopCompletionWait), cancellationToken);
        }
    }

    private static List<TaiwuLifecycleToolJson.ProcessStopResult> StopProcessesByName(
        string processName,
        string role,
        Func<Process, bool> includeProcess,
        bool killEntireProcessTree)
    {
        try
        {
            return StopProcesses(
                Process.GetProcessesByName(processName),
                role,
                includeProcess,
                killEntireProcessTree);
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException
            or InvalidOperationException)
        {
            return
            [
                TaiwuLifecycleToolJson.ProcessStopResult.Failed(
                    role,
                    null,
                    processName,
                    null,
                    ex.GetType().Name,
                    ex.Message),
            ];
        }
    }

    private static List<TaiwuLifecycleToolJson.ProcessStopResult> StopProcesses(
        IReadOnlyList<Process> processes,
        string role,
        Func<Process, bool> includeProcess,
        bool killEntireProcessTree)
    {
        List<TaiwuLifecycleToolJson.ProcessStopResult> results = [];

        foreach (Process process in processes)
        {
            using (process)
            {
                int? processId = TryGetProcessId(process);
                string processName = TryGetProcessName(process);
                string? executablePath = TryGetExecutablePath(process);

                if (!includeProcess(process))
                {
                    results.Add(
                        TaiwuLifecycleToolJson.ProcessStopResult.Skipped(
                            role,
                            processId,
                            processName,
                            executablePath,
                            "not_taiwu_backend"));
                    continue;
                }

                results.Add(
                    ForceStopProcess(
                        process,
                        role,
                        processId,
                        processName,
                        executablePath,
                        killEntireProcessTree));
            }
        }

        return results;
    }

    private static TaiwuLifecycleToolJson.ProcessStopResult ForceStopProcess(
        Process process,
        string role,
        int? processId,
        string processName,
        string? executablePath,
        bool killEntireProcessTree)
    {
        try
        {
            if (process.HasExited)
            {
                return TaiwuLifecycleToolJson.ProcessStopResult.AlreadyExited(
                    role,
                    processId,
                    processName,
                    executablePath);
            }

            process.Kill(killEntireProcessTree);
            bool exited = process.WaitForExit((int)KillExitWait.TotalMilliseconds);

            return exited
                ? TaiwuLifecycleToolJson.ProcessStopResult.Killed(
                    role,
                    processId,
                    processName,
                    executablePath)
                : TaiwuLifecycleToolJson.ProcessStopResult.KillSignaled(
                    role,
                    processId,
                    processName,
                    executablePath);
        }
        catch (Exception ex) when (ex is Win32Exception
            or InvalidOperationException
            or NotSupportedException)
        {
            return TaiwuLifecycleToolJson.ProcessStopResult.Failed(
                role,
                processId,
                processName,
                executablePath,
                ex.GetType().Name,
                ex.Message);
        }
    }

    private static List<TaiwuLifecycleToolJson.ProcessObservation> ObserveTaiwuProcesses()
    {
        List<TaiwuLifecycleToolJson.ProcessObservation> processes = [];
        processes.AddRange(
            ObserveProcessesByName(
                FrontendProcessName,
                "frontend",
                includeProcess: static _ => true));
        processes.AddRange(
            ObserveProcessesByName(
                BackendProcessName,
                "backend",
                includeProcess: IsTaiwuBackendProcess));
        return processes;
    }

    private static List<TaiwuLifecycleToolJson.ProcessObservation> ObserveProcessesByName(
        string processName,
        string role,
        Func<Process, bool> includeProcess)
    {
        List<TaiwuLifecycleToolJson.ProcessObservation> observations = [];

        foreach (Process process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                if (!includeProcess(process))
                {
                    continue;
                }

                observations.Add(
                    new TaiwuLifecycleToolJson.ProcessObservation(
                        role,
                        TryGetProcessId(process),
                        TryGetProcessName(process),
                        TryGetExecutablePath(process)));
            }
        }

        return observations;
    }

    private static bool IsTaiwuBackendProcess(Process process)
    {
        string? executablePath = TryGetExecutablePath(process);
        return executablePath?.EndsWith(
                BackendExecutableSuffix,
                StringComparison.OrdinalIgnoreCase)
            == true;
    }

    private static int? TryGetProcessId(Process process)
    {
        try
        {
            return process.Id;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static string TryGetProcessName(Process process)
    {
        try
        {
            return process.ProcessName;
        }
        catch (InvalidOperationException)
        {
            return string.Empty;
        }
    }

    private static string? TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (Exception ex) when (ex is Win32Exception
            or InvalidOperationException
            or NotSupportedException)
        {
            return null;
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
