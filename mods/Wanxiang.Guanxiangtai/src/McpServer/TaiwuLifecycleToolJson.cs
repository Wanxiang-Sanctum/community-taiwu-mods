using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static class TaiwuLifecycleToolJson
{
    public static string Serialize(Response response)
    {
        return JsonSerializer.Serialize(
            response,
            typeof(Response),
            TaiwuLifecycleToolJsonContext.Default);
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(LaunchAttemptResult), "launchAttempted")]
    [JsonDerivedType(typeof(LaunchRequestedResult), "launchRequested")]
    [JsonDerivedType(typeof(LaunchFailedResult), "launchFailed")]
    [JsonDerivedType(typeof(LaunchSkippedResult), "launchSkipped")]
    [JsonDerivedType(typeof(ForceStopAttemptResult), "forceStopAttempted")]
    [JsonDerivedType(typeof(RequestQuitStopAttemptResult), "requestQuitStopAttempted")]
    [JsonDerivedType(typeof(RequestQuitFailedResult), "requestQuitFailed")]
    [JsonDerivedType(typeof(RestartAttemptResult), "restartAttempted")]
    internal abstract class Response;

    internal abstract class LaunchResult : Response
    {
        private protected LaunchResult()
        {
        }
    }

    internal sealed class LaunchAttemptResult(
        string outcome,
        LaunchResult launch,
        RuntimeReadyWaitResult runtimeReady) : Response
    {
        public string Outcome { get; } =
            outcome ?? throw new ArgumentNullException(nameof(outcome));

        public Response Launch { get; } =
            launch ?? throw new ArgumentNullException(nameof(launch));

        public RuntimeReadyWaitResult RuntimeReady { get; } =
            runtimeReady ?? throw new ArgumentNullException(nameof(runtimeReady));
    }

    internal sealed class LaunchRequestedResult(string steamUri) : LaunchResult
    {
        public string SteamUri { get; } =
            steamUri ?? throw new ArgumentNullException(nameof(steamUri));
    }

    internal sealed class LaunchFailedResult(
        string steamUri,
        string errorType,
        string message) : LaunchResult
    {
        public string SteamUri { get; } =
            steamUri ?? throw new ArgumentNullException(nameof(steamUri));

        public string ErrorType { get; } =
            errorType ?? throw new ArgumentNullException(nameof(errorType));

        public string Message { get; } =
            message ?? throw new ArgumentNullException(nameof(message));
    }

    internal sealed class LaunchSkippedResult(string reason) : LaunchResult
    {
        public string Reason { get; } =
            reason ?? throw new ArgumentNullException(nameof(reason));
    }

    internal sealed class ForceStopAttemptResult(
        IReadOnlyList<ProcessStopResult> processes,
        ProcessExitWaitResult exitWait) : Response
    {
        public string Method { get; } = "force";

        public string Outcome { get; } =
            (exitWait ?? throw new ArgumentNullException(nameof(exitWait))).Outcome;

        public IReadOnlyList<ProcessStopResult> Processes { get; } =
            processes ?? throw new ArgumentNullException(nameof(processes));

        public ProcessExitWaitResult ExitWait { get; } =
            exitWait;
    }

    internal sealed class RequestQuitStopAttemptResult(
        ProcessExitWaitResult exitWait) : Response
    {
        public string Method { get; } = "requestQuit";

        public string Outcome { get; } =
            (exitWait ?? throw new ArgumentNullException(nameof(exitWait))).Outcome;

        public ProcessExitWaitResult ExitWait { get; } =
            exitWait;
    }

    internal sealed class RequestQuitFailedResult(
        string errorType,
        string message) : Response
    {
        public string Method { get; } = "requestQuit";

        public string Outcome { get; } = "request_failed";

        public string ErrorType { get; } =
            errorType ?? throw new ArgumentNullException(nameof(errorType));

        public string Message { get; } =
            message ?? throw new ArgumentNullException(nameof(message));
    }

    internal sealed class RestartAttemptResult(
        string outcome,
        Response stop,
        Response launch) : Response
    {
        public string Outcome { get; } =
            outcome ?? throw new ArgumentNullException(nameof(outcome));

        public Response Stop { get; } =
            stop ?? throw new ArgumentNullException(nameof(stop));

        public Response Launch { get; } =
            launch ?? throw new ArgumentNullException(nameof(launch));
    }

    internal sealed class RuntimeReadyWaitResult(
        string outcome,
        int elapsedMilliseconds,
        RuntimeSideReadiness frontend,
        RuntimeSideReadiness backend)
    {
        public string Outcome { get; } =
            outcome ?? throw new ArgumentNullException(nameof(outcome));

        public int ElapsedMilliseconds { get; } = elapsedMilliseconds;

        public RuntimeSideReadiness Frontend { get; } =
            frontend ?? throw new ArgumentNullException(nameof(frontend));

        public RuntimeSideReadiness Backend { get; } =
            backend ?? throw new ArgumentNullException(nameof(backend));

        public static RuntimeReadyWaitResult Ready(
            int elapsedMilliseconds,
            PluginRuntimeAvailability availability)
        {
            return new RuntimeReadyWaitResult(
                "ready",
                elapsedMilliseconds,
                RuntimeSideReadiness.From(availability.Frontend),
                RuntimeSideReadiness.From(availability.Backend));
        }

        public static RuntimeReadyWaitResult TimedOut(
            int elapsedMilliseconds,
            PluginRuntimeAvailability availability)
        {
            return new RuntimeReadyWaitResult(
                "timed_out",
                elapsedMilliseconds,
                RuntimeSideReadiness.From(availability.Frontend),
                RuntimeSideReadiness.From(availability.Backend));
        }
    }

    internal sealed class RuntimeSideReadiness(
        bool available,
        string? reason)
    {
        public bool Available { get; } = available;

        public string? Reason { get; } = reason;

        public static RuntimeSideReadiness From(PluginSideAvailability availability)
        {
            return new RuntimeSideReadiness(
                availability.Available,
                availability.Reason);
        }
    }

    internal sealed class ProcessExitWaitResult(
        string outcome,
        int elapsedMilliseconds,
        IReadOnlyList<ProcessObservation> observedProcesses,
        string? errorType,
        string? message)
    {
        internal const string StoppedOutcome = "stopped";

        public string Outcome { get; } =
            outcome ?? throw new ArgumentNullException(nameof(outcome));

        public int ElapsedMilliseconds { get; } = elapsedMilliseconds;

        public IReadOnlyList<ProcessObservation> ObservedProcesses { get; } =
            observedProcesses ?? throw new ArgumentNullException(nameof(observedProcesses));

        public string? ErrorType { get; } = errorType;

        public string? Message { get; } = message;

        public static ProcessExitWaitResult Stopped(int elapsedMilliseconds)
        {
            return new ProcessExitWaitResult(
                StoppedOutcome,
                elapsedMilliseconds,
                [],
                null,
                null);
        }

        public static ProcessExitWaitResult TimedOut(
            int elapsedMilliseconds,
            IReadOnlyList<ProcessObservation> observedProcesses)
        {
            return new ProcessExitWaitResult(
                "timed_out",
                elapsedMilliseconds,
                observedProcesses,
                null,
                null);
        }

        public static ProcessExitWaitResult InspectionFailed(
            int elapsedMilliseconds,
            string errorType,
            string message)
        {
            return new ProcessExitWaitResult(
                "inspection_failed",
                elapsedMilliseconds,
                [],
                errorType,
                message);
        }
    }

    internal sealed class ProcessObservation(
        string role,
        int? processId,
        string processName,
        string? executablePath)
    {
        public string Role { get; } =
            role ?? throw new ArgumentNullException(nameof(role));

        public int? ProcessId { get; } = processId;

        public string ProcessName { get; } =
            processName ?? throw new ArgumentNullException(nameof(processName));

        public string? ExecutablePath { get; } = executablePath;
    }

    internal sealed class ProcessStopResult(
        string role,
        int? processId,
        string processName,
        string? executablePath,
        string outcome,
        string? reason,
        string? errorType,
        string? message)
    {
        public string Role { get; } =
            role ?? throw new ArgumentNullException(nameof(role));

        public int? ProcessId { get; } = processId;

        public string ProcessName { get; } =
            processName ?? throw new ArgumentNullException(nameof(processName));

        public string? ExecutablePath { get; } = executablePath;

        public string Outcome { get; } =
            outcome ?? throw new ArgumentNullException(nameof(outcome));

        public string? Reason { get; } = reason;

        public string? ErrorType { get; } = errorType;

        public string? Message { get; } = message;

        public static ProcessStopResult Killed(
            string role,
            int? processId,
            string processName,
            string? executablePath)
        {
            return new ProcessStopResult(
                role,
                processId,
                processName,
                executablePath,
                "killed",
                null,
                null,
                null);
        }

        public static ProcessStopResult KillSignaled(
            string role,
            int? processId,
            string processName,
            string? executablePath)
        {
            return new ProcessStopResult(
                role,
                processId,
                processName,
                executablePath,
                "kill_signaled",
                null,
                null,
                null);
        }

        public static ProcessStopResult AlreadyExited(
            string role,
            int? processId,
            string processName,
            string? executablePath)
        {
            return new ProcessStopResult(
                role,
                processId,
                processName,
                executablePath,
                "already_exited",
                null,
                null,
                null);
        }

        public static ProcessStopResult Skipped(
            string role,
            int? processId,
            string processName,
            string? executablePath,
            string reason)
        {
            return new ProcessStopResult(
                role,
                processId,
                processName,
                executablePath,
                "skipped",
                reason,
                null,
                null);
        }

        public static ProcessStopResult Failed(
            string role,
            int? processId,
            string processName,
            string? executablePath,
            string errorType,
            string message)
        {
            return new ProcessStopResult(
                role,
                processId,
                processName,
                executablePath,
                "failed",
                null,
                errorType,
                message);
        }
    }
}

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TaiwuLifecycleToolJson.Response))]
internal sealed partial class TaiwuLifecycleToolJsonContext : JsonSerializerContext;
