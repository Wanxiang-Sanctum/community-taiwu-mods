using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Scripting;

public sealed class XiangshuScriptRunner
{
    private const string EntryTypeSimpleName = "XiangshuScript";
    private const string AsyncEntryMethodName = "ExecuteAsync";
    private const string SyncEntryMethodName = "Execute";
    private const string ScriptGlobalsFullName =
        "Wanxiang.Xiangshu.Scripting.XiangshuScriptGlobals";

    private readonly string _targetSide;
    private readonly ScriptReferenceResolver _referenceResolver;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.Indented,
    };

    public XiangshuScriptRunner(
        string targetSide,
        IEnumerable<string>? referenceDirectories = null)
    {
#if NET6_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSide);
#else
        if (string.IsNullOrWhiteSpace(targetSide))
        {
            throw new ArgumentException("Target side is required.", nameof(targetSide));
        }
#endif

        _targetSide = targetSide;
        _referenceResolver = new ScriptReferenceResolver(referenceDirectories);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Script errors are returned to the MCP caller as tool data instead of escaping the plugin IPC handler.")]
    public async Task<IpcRunScriptResponse> ExecuteAsync(
        IpcRunScriptRequest request,
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

        IReadOnlyList<string> diagnostics = [];

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            ScriptCompilationResult compilationResult = Compile(request.Script, cancellationToken);
            diagnostics = compilationResult.Diagnostics;
            if (!compilationResult.Succeeded || compilationResult.AssemblyBytes is null)
            {
                return CreateErrorResponse(
                    "Script compilation failed.",
                    diagnostics);
            }

            object? value = await InvokeAsync(
                compilationResult.AssemblyBytes,
                new XiangshuScriptGlobals(
                    _targetSide,
                    request.Arguments,
                    cancellationToken),
                cancellationToken);

            return new IpcRunScriptResponse(
                SerializeReturnValueJson(value),
                error: string.Empty,
                diagnostics);
        }
        catch (OperationCanceledException)
        {
            return CreateErrorResponse(
                "Script execution was canceled.",
                diagnostics);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(
                UnwrapInvocationException(ex).ToString(),
                diagnostics);
        }
    }

    private ScriptCompilationResult Compile(string source, CancellationToken cancellationToken)
    {
        CompilationReferences references =
            _referenceResolver.CollectReferences(
                typeof(XiangshuScriptGlobals).Assembly,
                ScriptGlobalsFullName);
        if (!references.HasRequiredReferences)
        {
            return new ScriptCompilationResult(
                false,
                assemblyBytes: null,
                references.Diagnostics);
        }

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Latest),
            cancellationToken: cancellationToken);
        CSharpCompilation compilation = CSharpCompilation.Create(
            $"Wanxiang.Xiangshu.DynamicScript.{Guid.NewGuid():N}",
            [syntaxTree],
            references.References,
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
            .. references.Diagnostics,
            .. emitResult.Diagnostics
                .Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
                .Select(static diagnostic => diagnostic.ToString()),
        ];

        if (!emitResult.Success)
        {
            return new ScriptCompilationResult(false, assemblyBytes: null, diagnostics);
        }

        return new ScriptCompilationResult(true, assemblyStream.ToArray(), diagnostics);
    }

    private async Task<object?> InvokeAsync(
        byte[] assemblyBytes,
        XiangshuScriptGlobals globals,
        CancellationToken cancellationToken)
    {
        using AssemblyResolutionScope resolutionScope =
            _referenceResolver.CreateAssemblyResolutionScope();
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
                $"Compiled script has multiple public static {EntryTypeSimpleName} entry types. Keep only one entry type.");
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
                    && HasScriptGlobalsParameter(method)),
        ];

        if (candidates.Length == 1)
        {
            return candidates[0];
        }

        if (candidates.Length > 1)
        {
            throw new InvalidOperationException(
                $"Compiled script has multiple matching entry methods. Keep only one {AsyncEntryMethodName}"
                + $" or {SyncEntryMethodName} overload with one parameter whose type resolves to {ScriptGlobalsFullName}.");
        }

        throw new InvalidOperationException(GetEntryMethodContractMessage());
    }

    private static bool IsValidEntryType(Type type)
    {
        return type.Name == EntryTypeSimpleName
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

    private static bool HasScriptGlobalsParameter(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Length == 1
            && parameters[0].ParameterType == typeof(XiangshuScriptGlobals);
    }

    private static string GetEntryTypeContractMessage()
    {
        return "Compiled script entry type contract was not satisfied. Define exactly one public static "
            + $"non-generic class named {EntryTypeSimpleName}.";
    }

    private static string GetEntryMethodContractMessage()
    {
        return "Compiled script entry method contract was not satisfied. Define exactly one public static "
            + $"{EntryTypeSimpleName}.{AsyncEntryMethodName} or {EntryTypeSimpleName}.{SyncEntryMethodName} method with one "
            + $"parameter whose type resolves to {ScriptGlobalsFullName}.";
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

    private static IpcRunScriptResponse CreateErrorResponse(
        string error,
        IReadOnlyList<string> diagnostics)
    {
        return new IpcRunScriptResponse(
            returnValueJson: string.Empty,
            error,
            diagnostics);
    }

    private static Exception UnwrapInvocationException(Exception exception)
    {
        return exception is TargetInvocationException { InnerException: not null }
            ? exception.InnerException
            : exception;
    }

    private sealed class ScriptCompilationResult(
        bool succeeded,
        byte[]? assemblyBytes,
        IReadOnlyList<string> diagnostics)
    {
        public bool Succeeded { get; } = succeeded;

        public byte[]? AssemblyBytes { get; } = assemblyBytes;

        public IReadOnlyList<string> Diagnostics { get; } = diagnostics;
    }

}
