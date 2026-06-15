using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Scripting;

public sealed class XiangshuScriptRunner(string side)
{
    private const string EntryTypeName = "XiangshuScript";
    private const string AsyncEntryMethodName = "ExecuteAsync";
    private const string SyncEntryMethodName = "Execute";

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.Indented,
    };

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Script errors are returned to the MCP caller as tool data instead of escaping the plugin IPC handler.")]
    public async Task<IpcExecuteScriptResponse> ExecuteAsync(
        IpcExecuteScriptRequest request,
        CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(request);
#else
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }
#endif

        IpcExecuteScriptResponse response = new();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            EmitResultData emit = Compile(request.Script, cancellationToken);
            response.Diagnostics.AddRange(emit.Diagnostics);
            if (!emit.Succeeded || emit.AssemblyBytes is null)
            {
                response.Error = "Script compilation failed.";
                return response;
            }

            object? value = await InvokeAsync(
                emit.AssemblyBytes,
                new XiangshuScriptGlobals(
                    side,
                    request.Arguments,
                    cancellationToken),
                cancellationToken);

            response.ReturnValueJson = SerializeReturnValueJson(value);
        }
        catch (OperationCanceledException)
        {
            response.Error = "Script execution was canceled.";
        }
        catch (Exception ex)
        {
            response.Error = UnwrapInvocationException(ex).ToString();
        }

        return response;
    }

    private static EmitResultData Compile(string source, CancellationToken cancellationToken)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Latest),
            cancellationToken: cancellationToken);
        CSharpCompilation compilation = CSharpCompilation.Create(
            $"Wanxiang.Xiangshu.DynamicScript.{Guid.NewGuid():N}",
            [syntaxTree],
            CollectReferences(),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Enable));

        using MemoryStream assemblyStream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emitResult = compilation.Emit(
            assemblyStream,
            cancellationToken: cancellationToken);
        string[] diagnostics =
        [
            .. emitResult.Diagnostics
                .Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
                .Select(static diagnostic => diagnostic.ToString()),
        ];

        if (!emitResult.Success)
        {
            return new EmitResultData(false, assemblyBytes: null, diagnostics);
        }

        return new EmitResultData(true, assemblyStream.ToArray(), diagnostics);
    }

    private static IEnumerable<MetadataReference> CollectReferences()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(static assembly => !assembly.IsDynamic)
            .Select(static assembly => assembly.Location)
            .Where(static location => !string.IsNullOrWhiteSpace(location) && File.Exists(location))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(static location => MetadataReference.CreateFromFile(location));
    }

    private static async Task<object?> InvokeAsync(
        byte[] assemblyBytes,
        XiangshuScriptGlobals globals,
        CancellationToken cancellationToken)
    {
        Assembly assembly = Assembly.Load(assemblyBytes);
        Type scriptType = FindEntryType(assembly);
        MethodInfo executeMethod = FindEntryMethod(scriptType);

        object? result = executeMethod.Invoke(null, [globals]);
        return await ResolveInvocationResultAsync(result, cancellationToken);
    }

    private static Type FindEntryType(Assembly assembly)
    {
        Type[] candidates =
        [
            .. assembly.GetTypes().Where(IsValidEntryType),
        ];

        if (candidates.Length == 1)
        {
            return candidates[0];
        }

        if (candidates.Length > 1)
        {
            throw new InvalidOperationException(
                $"Compiled script has multiple public static {EntryTypeName} entry types. Keep only one entry type.");
        }

        throw new InvalidOperationException(GetEntryTypeContractMessage());
    }

    private static MethodInfo FindEntryMethod(Type scriptType)
    {
        const BindingFlags EntryMethodSearchFlags =
            BindingFlags.Public
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        MethodInfo[] candidates =
        [
            .. scriptType
                .GetMethods(EntryMethodSearchFlags)
                .Where(static method =>
                    IsEntryMethodName(method.Name)
                    && HasGlobalsParameter(method)),
        ];

        if (candidates.Length == 1)
        {
            return candidates[0];
        }

        if (candidates.Length > 1)
        {
            throw new InvalidOperationException(
                $"Compiled script has multiple matching entry methods. Keep only one {AsyncEntryMethodName}"
                + $" or {SyncEntryMethodName} overload that takes XiangshuScriptGlobals.");
        }

        throw new InvalidOperationException(GetEntryMethodContractMessage());
    }

    private static bool IsValidEntryType(Type type)
    {
        return type.Name == EntryTypeName
            && type.IsClass
            && type.IsAbstract
            && type.IsSealed
            && type.IsPublic
            && !type.ContainsGenericParameters;
    }

    private static bool IsEntryMethodName(string name)
    {
        return name is AsyncEntryMethodName or SyncEntryMethodName;
    }

    private static bool HasGlobalsParameter(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Length == 1
            && parameters[0].ParameterType == typeof(XiangshuScriptGlobals);
    }

    private static string GetEntryTypeContractMessage()
    {
        return "Compiled script entry type contract was not satisfied. Define exactly one public static "
            + $"non-generic class named {EntryTypeName}.";
    }

    private static string GetEntryMethodContractMessage()
    {
        return "Compiled script entry method contract was not satisfied. Define exactly one public static "
            + $"{EntryTypeName}.{AsyncEntryMethodName} or {EntryTypeName}.{SyncEntryMethodName} method with one "
            + "XiangshuScriptGlobals parameter.";
    }

    private static async Task<object?> ResolveInvocationResultAsync(
        object? result,
        CancellationToken cancellationToken)
    {
        if (result is null)
        {
            return null;
        }

        if (result is Task task)
        {
            await WaitForTaskAsync(task, cancellationToken);
            return ReadTaskResult(task);
        }

        return result;
    }

    private static async Task WaitForTaskAsync(
        Task task,
        CancellationToken cancellationToken)
    {
        if (task.IsCompleted)
        {
            await task;
            return;
        }

        TaskCompletionSource<bool> cancellationSource = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using CancellationTokenRegistration registration = cancellationToken.Register(
            static state =>
            {
                if (state is TaskCompletionSource<bool> source)
                {
                    _ = source.TrySetResult(true);
                }
            },
            cancellationSource);

        Task completedTask = await Task.WhenAny(task, cancellationSource.Task);
        if (!ReferenceEquals(completedTask, task))
        {
            throw new OperationCanceledException(cancellationToken);
        }

        await task;
    }

    private static object? ReadTaskResult(Task task)
    {
        Type taskType = task.GetType();
        if (!taskType.IsGenericType)
        {
            return null;
        }

        PropertyInfo resultProperty = taskType
            .GetProperty(nameof(Task<>.Result), BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Generic Task result does not expose Result.");
        return resultProperty.GetValue(task);
    }

    private static string SerializeReturnValueJson(object? value)
    {
        return JsonConvert.SerializeObject(value, JsonSettings) ?? "null";
    }

    private static Exception UnwrapInvocationException(Exception exception)
    {
        return exception is TargetInvocationException { InnerException: not null }
            ? exception.InnerException
            : exception;
    }

    private sealed class EmitResultData(
        bool succeeded,
        byte[]? assemblyBytes,
        IReadOnlyList<string> diagnostics)
    {
        public bool Succeeded { get; } = succeeded;

        public byte[]? AssemblyBytes { get; } = assemblyBytes;

        public IReadOnlyList<string> Diagnostics { get; } = diagnostics;
    }

}
