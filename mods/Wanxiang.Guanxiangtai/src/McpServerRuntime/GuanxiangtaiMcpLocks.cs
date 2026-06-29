using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Wanxiang.Guanxiangtai.McpServerRuntime;

public static class GuanxiangtaiMcpLocks
{
    private static readonly TimeSpan EndpointFileLockTimeout = TimeSpan.FromSeconds(1);

    public static IDisposable? TryAcquireServerInstance(string ownerDirectory)
    {
        string mutexName = BuildMutexName("ServerInstance", ownerDirectory);
        return TryAcquireLongLived(mutexName);
    }

    internal static IDisposable AcquireEndpointFile(string endpointFilePath)
    {
        string mutexName = BuildMutexName("EndpointFile", endpointFilePath);
        return TryAcquire(mutexName, EndpointFileLockTimeout)
            ?? throw new IOException("Timed out waiting for Wanxiang.Guanxiangtai MCP server runtime endpoint file lock.");
    }

    private static MutexLease? TryAcquire(
        string mutexName,
        TimeSpan timeout)
    {
        Mutex? mutex = new(false, mutexName);

        try
        {
            bool acquired;

            try
            {
                acquired = mutex.WaitOne(timeout);
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            if (!acquired)
            {
                return null;
            }

            MutexLease lease = new(mutex);
            mutex = null;
            return lease;
        }
        finally
        {
            mutex?.Dispose();
        }
    }

    private static LongLivedMutexLease? TryAcquireLongLived(string mutexName)
    {
        LongLivedMutexLease lease = new(mutexName);

        try
        {
            lease.Start();
        }
        catch
        {
            lease.Dispose();
            throw;
        }

        if (lease.Acquired)
        {
            return lease;
        }

        lease.Dispose();
        return null;
    }

    private static string BuildMutexName(
        string purpose,
        string scope)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(Path.GetFullPath(scope));
#if NET6_0_OR_GREATER
        byte[] hash = SHA256.HashData(bytes);
#else
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(bytes);
#endif

        return GuanxiangtaiMcp.ModId + "." + purpose + "." + ToHex(hash);
    }

    private static string ToHex(byte[] bytes)
    {
        StringBuilder builder = new(bytes.Length * 2);

        foreach (byte value in bytes)
        {
            _ = builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private sealed class MutexLease(Mutex mutex) : IDisposable
    {
        private readonly Mutex _mutex = mutex;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }

    private sealed class LongLivedMutexLease : IDisposable
    {
        private static readonly TimeSpan ReleaseJoinTimeout = TimeSpan.FromSeconds(5);

        private readonly string _mutexName;
        private readonly ManualResetEventSlim _ready = new();
        private readonly ManualResetEventSlim _release = new();
        private readonly Thread _ownerThread;
        private bool _disposed;

        public LongLivedMutexLease(string mutexName)
        {
            _mutexName = mutexName;
            _ownerThread = new Thread(HoldMutex)
            {
                IsBackground = true,
                Name = "Wanxiang.Guanxiangtai MCP server mutex",
            };
        }

        public bool Acquired { get; private set; }

        private Exception? Failure { get; set; }

        public void Start()
        {
            _ownerThread.Start();
            _ready.Wait();

            if (Failure is not null)
            {
                throw new IOException(
                    "Could not acquire Wanxiang.Guanxiangtai MCP server mutex.",
                    Failure);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _release.Set();

            if (_ownerThread.IsAlive)
            {
                _ = _ownerThread.Join(ReleaseJoinTimeout);
            }

            _ready.Dispose();
            _release.Dispose();
        }

        private void HoldMutex()
        {
            try
            {
                using Mutex mutex = new(false, _mutexName);

                try
                {
                    Acquired = mutex.WaitOne(TimeSpan.Zero);
                }
                catch (AbandonedMutexException)
                {
                    Acquired = true;
                }

                _ready.Set();

                if (!Acquired)
                {
                    return;
                }

                _release.Wait();
                mutex.ReleaseMutex();
            }
            catch (Exception ex) when (ex is ApplicationException
                or IOException
                or InvalidOperationException
                or NotSupportedException
                or ObjectDisposedException
                or ThreadInterruptedException
                or UnauthorizedAccessException
                or WaitHandleCannotBeOpenedException)
            {
                Failure = ex;
                _ready.Set();
            }
        }
    }
}
