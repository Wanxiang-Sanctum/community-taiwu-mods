using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using MessagePack;
using ModelContextProtocol;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static class TaiwuLifecycle
{
    private const string SteamLaunchUri = "steam://rungameid/838350";

    public static async Task<string> LaunchAsync(CancellationToken cancellationToken)
    {
        TaiwuLifecycleToolJson.Response result =
            await LaunchCoreAsync(cancellationToken);
        return TaiwuLifecycleToolJson.Serialize(result);
    }

    public static async Task<string> StopAsync(
        string method,
        CancellationToken cancellationToken)
    {
        TaiwuStopMethod stopMethod = ParseStopMethod(method);
        TaiwuLifecycleToolJson.Response result =
            await StopCoreAsync(stopMethod, cancellationToken);
        return TaiwuLifecycleToolJson.Serialize(result);
    }

    public static async Task<string> RestartAsync(
        string stopMethod,
        CancellationToken cancellationToken)
    {
        TaiwuStopMethod parsedStopMethod = ParseStopMethod(stopMethod);
        TaiwuLifecycleToolJson.Response stopResult =
            await StopCoreAsync(parsedStopMethod, cancellationToken);

        TaiwuLifecycleToolJson.Response launchResult;
        string outcome;
        if (IsStopComplete(stopResult))
        {
            launchResult = await LaunchCoreAsync(cancellationToken);
            outcome = GetLaunchOutcome(launchResult);
        }
        else
        {
            launchResult = new TaiwuLifecycleToolJson.LaunchSkippedResult("stop_not_completed");
            outcome = "stop_not_completed";
        }

        return TaiwuLifecycleToolJson.Serialize(
            new TaiwuLifecycleToolJson.RestartAttemptResult(
                outcome,
                stopResult,
                launchResult));
    }

    private static async Task<TaiwuLifecycleToolJson.Response> StopCoreAsync(
        TaiwuStopMethod method,
        CancellationToken cancellationToken)
    {
        return method switch
        {
            TaiwuStopMethod.Force => await TaiwuProcesses.ForceStopAsync(cancellationToken),
            TaiwuStopMethod.RequestQuit => await RequestQuitAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(method),
                method,
                "Unsupported Taiwu stop method."),
        };
    }

    private static async Task<TaiwuLifecycleToolJson.Response> LaunchCoreAsync(
        CancellationToken cancellationToken)
    {
        TaiwuLifecycleToolJson.LaunchResult launchRequest = RequestSteamLaunch();
        if (launchRequest is TaiwuLifecycleToolJson.LaunchFailedResult)
        {
            return launchRequest;
        }

        TaiwuLifecycleToolJson.RuntimeReadyWaitResult runtimeReady =
            await TaiwuRuntimeReadiness.WaitAsync(cancellationToken);
        return new TaiwuLifecycleToolJson.LaunchAttemptResult(
            runtimeReady.Outcome,
            launchRequest,
            runtimeReady);
    }

    private static TaiwuLifecycleToolJson.LaunchResult RequestSteamLaunch()
    {
        try
        {
            using Process? process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = SteamLaunchUri,
                    UseShellExecute = true,
                });

            return new TaiwuLifecycleToolJson.LaunchRequestedResult(SteamLaunchUri);
        }
        catch (Exception ex) when (ex is Win32Exception
            or FileNotFoundException
            or InvalidOperationException)
        {
            return new TaiwuLifecycleToolJson.LaunchFailedResult(
                SteamLaunchUri,
                ex.GetType().Name,
                ex.Message);
        }
    }

    private static async Task<TaiwuLifecycleToolJson.Response> RequestQuitAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await PluginIpcProxy.RequestGameQuitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is McpException
            or ArgumentException
            or IOException
            or InvalidDataException
            or InvalidOperationException
            or MessagePackSerializationException
            or ObjectDisposedException
            or SocketException
            or UnauthorizedAccessException)
        {
            return new TaiwuLifecycleToolJson.RequestQuitFailedResult(
                ex.GetType().Name,
                ex.Message);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new TaiwuLifecycleToolJson.RequestQuitFailedResult(
                ex.GetType().Name,
                ex.Message);
        }

        TaiwuLifecycleToolJson.ProcessExitWaitResult exitWait =
            await TaiwuProcesses.WaitForExitAsync(cancellationToken);
        return new TaiwuLifecycleToolJson.RequestQuitStopAttemptResult(exitWait);
    }

    private static bool IsStopComplete(TaiwuLifecycleToolJson.Response stopResult)
    {
        return stopResult switch
        {
            TaiwuLifecycleToolJson.ForceStopAttemptResult forceStop =>
                forceStop.ExitWait.Outcome
                    == TaiwuLifecycleToolJson.ProcessExitWaitResult.StoppedOutcome,
            TaiwuLifecycleToolJson.RequestQuitStopAttemptResult requestQuit =>
                requestQuit.ExitWait.Outcome
                    == TaiwuLifecycleToolJson.ProcessExitWaitResult.StoppedOutcome,
            _ => false,
        };
    }

    private static string GetLaunchOutcome(TaiwuLifecycleToolJson.Response launchResult)
    {
        return launchResult switch
        {
            TaiwuLifecycleToolJson.LaunchAttemptResult launchAttempt =>
                launchAttempt.Outcome,
            TaiwuLifecycleToolJson.LaunchFailedResult =>
                "launch_failed",
            TaiwuLifecycleToolJson.LaunchSkippedResult =>
                "launch_skipped",
            _ => "unknown",
        };
    }

    private static TaiwuStopMethod ParseStopMethod(string method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new McpException("method must be either 'force' or 'requestQuit'.");
        }

        string trimmedMethod = method.Trim();

        if (string.Equals(trimmedMethod, "force", StringComparison.OrdinalIgnoreCase))
        {
            return TaiwuStopMethod.Force;
        }

        if (string.Equals(trimmedMethod, "requestQuit", StringComparison.OrdinalIgnoreCase))
        {
            return TaiwuStopMethod.RequestQuit;
        }

        throw new McpException("method must be either 'force' or 'requestQuit'.");
    }

    private enum TaiwuStopMethod
    {
        Force = 0,

        RequestQuit = 1,
    }
}
