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
            await LaunchAndWaitAsync(cancellationToken);
        return TaiwuLifecycleToolJson.Serialize(result);
    }

    public static async Task<string> StopAsync(
        McpTaiwuStopMethod method,
        CancellationToken cancellationToken)
    {
        TaiwuLifecycleToolJson.Response result =
            await StopByMethodAsync(method, cancellationToken);
        return TaiwuLifecycleToolJson.Serialize(result);
    }

    public static async Task<string> RestartAsync(
        McpTaiwuStopMethod stopMethod,
        CancellationToken cancellationToken)
    {
        TaiwuLifecycleToolJson.Response stopResult =
            await StopByMethodAsync(stopMethod, cancellationToken);

        TaiwuLifecycleToolJson.Response launchResult;
        string outcome;
        if (IsStopComplete(stopResult))
        {
            launchResult = await LaunchAndWaitAsync(cancellationToken);
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

    private static async Task<TaiwuLifecycleToolJson.Response> StopByMethodAsync(
        McpTaiwuStopMethod method,
        CancellationToken cancellationToken)
    {
        return method switch
        {
            McpTaiwuStopMethod.Force => await TaiwuProcesses.ForceStopAsync(cancellationToken),
            McpTaiwuStopMethod.RequestQuit => await RequestQuitAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(method),
                method,
                "Unsupported Taiwu stop method."),
        };
    }

    private static async Task<TaiwuLifecycleToolJson.Response> LaunchAndWaitAsync(
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

}
