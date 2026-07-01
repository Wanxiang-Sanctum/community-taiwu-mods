using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static partial class DetachedServerLauncher
{
    private const uint CreateNewConsole = 0x00000010;

    private const uint CreateUnicodeEnvironment = 0x00000400;

    private const uint ExtendedStartupInfoPresent = 0x00080000;

    private const uint ProcessCreateProcess = 0x0080;

    private const uint ProcessQueryLimitedInformation = 0x1000;

    private const uint ProcThreadAttributeParentProcess = 0x00020000;

    private const uint StartfUseShowWindow = 0x00000001;

    private const uint TokenDuplicate = 0x0002;

    private const uint TokenQuery = 0x0008;

    private const ushort ShowNormal = 1;

    internal static void Launch(
        string executablePath,
        string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("当前平台不支持通过 Explorer 创建独立 MCP server 进程。");
        }

        LaunchWithExplorerParent(executablePath, workingDirectory);
    }

    [SupportedOSPlatform("windows")]
    private static void LaunchWithExplorerParent(
        string executablePath,
        string workingDirectory)
    {
        using SafeKernelHandle explorerProcess = OpenCurrentSessionExplorerProcess();
        using SafeKernelHandle explorerToken = OpenExplorerToken(explorerProcess);
        IntPtr environmentBlock = CreateUserEnvironmentBlock(explorerToken);

        try
        {
            using ProcessThreadAttributeList attributeList = CreateParentProcessAttributeList(explorerProcess);
            CreateServerProcess(
                executablePath,
                workingDirectory,
                environmentBlock,
                attributeList.Handle);
        }
        finally
        {
            if (environmentBlock != IntPtr.Zero)
            {
                _ = DestroyEnvironmentBlock(environmentBlock);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static SafeKernelHandle OpenCurrentSessionExplorerProcess()
    {
        using Process currentProcess = Process.GetCurrentProcess();
        int currentSessionId = currentProcess.SessionId;
        Win32Exception? lastOpenFailure = null;

        foreach (Process process in Process.GetProcessesByName("explorer"))
        {
            using (process)
            {
                if (!IsInSession(process, currentSessionId))
                {
                    continue;
                }

                IntPtr processHandle = OpenProcess(
                    ProcessCreateProcess | ProcessQueryLimitedInformation,
                    false,
                    process.Id);
                if (processHandle == IntPtr.Zero)
                {
                    lastOpenFailure = LastWin32Exception("无法打开当前桌面会话的 Explorer 进程。");
                    continue;
                }

                return new SafeKernelHandle(processHandle);
            }
        }

        if (lastOpenFailure is not null)
        {
            throw lastOpenFailure;
        }

        throw new InvalidOperationException("未找到当前桌面会话的 Explorer 进程，无法创建独立 MCP server 控制台。");
    }

    [SupportedOSPlatform("windows")]
    private static bool IsInSession(
        Process process,
        int sessionId)
    {
        try
        {
            return !process.HasExited && process.SessionId == sessionId;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static SafeKernelHandle OpenExplorerToken(SafeKernelHandle processHandle)
    {
        if (!OpenProcessToken(
                processHandle.DangerousGetHandle(),
                TokenQuery | TokenDuplicate,
                out IntPtr tokenHandle))
        {
            throw LastWin32Exception("无法打开 Explorer 用户令牌。");
        }

        return new SafeKernelHandle(tokenHandle);
    }

    private static IntPtr CreateUserEnvironmentBlock(SafeKernelHandle tokenHandle)
    {
        if (!CreateEnvironmentBlock(
                out IntPtr environmentBlock,
                tokenHandle.DangerousGetHandle(),
                false))
        {
            throw LastWin32Exception("无法创建桌面用户环境块。");
        }

        return environmentBlock;
    }

    private static ProcessThreadAttributeList CreateParentProcessAttributeList(SafeKernelHandle parentProcessHandle)
    {
        UIntPtr attributeListSize = UIntPtr.Zero;
        _ = InitializeProcThreadAttributeList(
            IntPtr.Zero,
            1,
            0,
            ref attributeListSize);
        if (attributeListSize == UIntPtr.Zero)
        {
            throw LastWin32Exception("无法计算进程属性列表大小。");
        }

        IntPtr attributeList = Marshal.AllocHGlobal(checked((nint)attributeListSize.ToUInt64()));
        IntPtr parentProcessHandleValue = IntPtr.Zero;
        bool attributeListInitialized = false;

        try
        {
            if (!InitializeProcThreadAttributeList(
                    attributeList,
                    1,
                    0,
                    ref attributeListSize))
            {
                throw LastWin32Exception("无法初始化进程属性列表。");
            }

            attributeListInitialized = true;
            // The attribute value buffer must live until the attribute list is destroyed.
            parentProcessHandleValue = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(parentProcessHandleValue, parentProcessHandle.DangerousGetHandle());
            if (!UpdateProcThreadAttribute(
                    attributeList,
                    0,
                    (IntPtr)ProcThreadAttributeParentProcess,
                    parentProcessHandleValue,
                    (UIntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero))
            {
                throw LastWin32Exception("无法设置 Explorer 父进程属性。");
            }

            return new ProcessThreadAttributeList(attributeList, parentProcessHandleValue);
        }
        catch
        {
            if (attributeListInitialized)
            {
                DeleteProcThreadAttributeList(attributeList);
            }

            Marshal.FreeHGlobal(attributeList);
            if (parentProcessHandleValue != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(parentProcessHandleValue);
            }

            throw;
        }
    }

    private static void CreateServerProcess(
        string executablePath,
        string workingDirectory,
        IntPtr environmentBlock,
        IntPtr attributeList)
    {
        STARTUPINFOEX startupInfo = new()
        {
            StartupInfo =
            {
                cb = Marshal.SizeOf<STARTUPINFOEX>(),
                dwFlags = StartfUseShowWindow,
                wShowWindow = ShowNormal,
            },
            lpAttributeList = attributeList,
        };

        if (!CreateProcess(
                executablePath,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                ExtendedStartupInfoPresent | CreateNewConsole | CreateUnicodeEnvironment,
                environmentBlock,
                workingDirectory,
                ref startupInfo,
                out PROCESS_INFORMATION processInformation))
        {
            throw LastWin32Exception("无法创建独立 MCP server 进程。");
        }

        CloseProcessInformationHandles(processInformation);
    }

    private static void CloseProcessInformationHandles(PROCESS_INFORMATION processInformation)
    {
        if (processInformation.hThread != IntPtr.Zero)
        {
            _ = CloseHandle(processInformation.hThread);
        }

        if (processInformation.hProcess != IntPtr.Zero)
        {
            _ = CloseHandle(processInformation.hProcess);
        }
    }

    private static Win32Exception LastWin32Exception(string message)
    {
        return new Win32Exception(Marshal.GetLastPInvokeError(), message);
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr handle);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "CreateProcessW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateProcess(
        string applicationName,
        IntPtr commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string currentDirectory,
        ref STARTUPINFOEX startupInfo,
        out PROCESS_INFORMATION processInformation);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("userenv.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateEnvironmentBlock(
        out IntPtr environment,
        IntPtr token,
        [MarshalAs(UnmanagedType.Bool)] bool inherit);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll")]
    private static partial void DeleteProcThreadAttributeList(IntPtr attributeList);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("userenv.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyEnvironmentBlock(IntPtr environment);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool InitializeProcThreadAttributeList(
        IntPtr attributeList,
        int attributeCount,
        int flags,
        ref UIntPtr size);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        int processId);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(
        IntPtr processHandle,
        uint desiredAccess,
        out IntPtr tokenHandle);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UpdateProcThreadAttribute(
        IntPtr attributeList,
        int flags,
        IntPtr attribute,
        IntPtr value,
        UIntPtr size,
        IntPtr previousValue,
        IntPtr returnSize);

    private sealed class ProcessThreadAttributeList : IDisposable
    {
        private IntPtr _parentProcessHandleValue;

        internal ProcessThreadAttributeList(
            IntPtr attributeList,
            IntPtr parentProcessHandleValue)
        {
            Handle = attributeList;
            _parentProcessHandleValue = parentProcessHandleValue;
        }

        internal IntPtr Handle { get; private set; }

        public void Dispose()
        {
            IntPtr attributeList = Handle;
            Handle = IntPtr.Zero;
            if (attributeList != IntPtr.Zero)
            {
                DeleteProcThreadAttributeList(attributeList);
                Marshal.FreeHGlobal(attributeList);
            }

            IntPtr parentProcessHandleValue = _parentProcessHandleValue;
            _parentProcessHandleValue = IntPtr.Zero;
            if (parentProcessHandleValue != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(parentProcessHandleValue);
            }
        }
    }

    private sealed class SafeKernelHandle : SafeHandle
    {
        private static readonly IntPtr InvalidHandleValue = new(-1);

        internal SafeKernelHandle(IntPtr handle)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero || handle == InvalidHandleValue;

        protected override bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;

        public IntPtr hThread;

        public int dwProcessId;

        public int dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;

        public IntPtr lpReserved;

        public IntPtr lpDesktop;

        public IntPtr lpTitle;

        public int dwX;

        public int dwY;

        public int dwXSize;

        public int dwYSize;

        public int dwXCountChars;

        public int dwYCountChars;

        public int dwFillAttribute;

        public uint dwFlags;

        public ushort wShowWindow;

        public ushort cbReserved2;

        public IntPtr lpReserved2;

        public IntPtr hStdInput;

        public IntPtr hStdOutput;

        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;

        public IntPtr lpAttributeList;
    }
}
