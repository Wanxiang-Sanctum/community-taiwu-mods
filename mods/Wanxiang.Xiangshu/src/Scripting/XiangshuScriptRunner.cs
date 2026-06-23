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
    private readonly IScriptEntryDispatcher _entryDispatcher;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.None,
    };

    public XiangshuScriptRunner(
        string targetSide,
        IEnumerable<string>? referenceDirectories = null,
        IScriptEntryDispatcher? entryDispatcher = null)
        : this(
            new ScriptHostOptions(
                targetSide,
                referenceDirectories: referenceDirectories),
            entryDispatcher)
    {
    }

    public XiangshuScriptRunner(
        ScriptHostOptions hostOptions,
        IScriptEntryDispatcher? entryDispatcher = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(hostOptions);
#else
        if (hostOptions is null)
        {
            throw new ArgumentNullException(nameof(hostOptions));
        }
#endif

        _targetSide = hostOptions.TargetSide;
        _referenceResolver = new ScriptReferenceResolver(hostOptions);
        _entryDispatcher = entryDispatcher ?? CurrentThreadEntryDispatcher.Instance;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Script exceptions and pre-invocation rejection reasons are returned through the IPC response union.")]
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
        bool entryInvoked = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            ScriptCompilationResult compilationResult = Compile(request.Script, cancellationToken);
            if (compilationResult is ScriptCompilationResult.Rejected rejected)
            {
                return IpcRunScriptResponse.NotInvoked(
                    rejected.Reason,
                    rejected.Details);
            }

            ScriptCompilationResult.Compiled compiled = (ScriptCompilationResult.Compiled)compilationResult;
            object? value = await InvokeEntryAsync(
                compiled.AssemblyBytes,
                new XiangshuScriptGlobals(
                    _targetSide,
                    request.Arguments,
                    cancellationToken),
                request.EntryThread,
                () => entryInvoked = true,
                cancellationToken);

            return IpcRunScriptResponse.InvokedWithReturnValue(SerializeReturnValueJson(value));
        }
        catch (OperationCanceledException)
        {
            const string message = "Script execution was canceled.";
            return entryInvoked
                ? IpcRunScriptResponse.InvokedWithException(message)
                : IpcRunScriptResponse.NotInvoked(message);
        }
        catch (Exception ex)
        {
            string message = UnwrapInvocationException(ex).ToString();
            return entryInvoked
                ? IpcRunScriptResponse.InvokedWithException(message)
                : IpcRunScriptResponse.NotInvoked(message);
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
            return new ScriptCompilationResult.Rejected(
                CompilationFailureReason,
                CreateCompilationFailureDetails(references.ReferenceDiagnostics, []));
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
        if (!emitResult.Success)
        {
            string[] compilationDiagnostics =
            [
                .. emitResult.Diagnostics
                    .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(static diagnostic => diagnostic.ToString()),
            ];

            return new ScriptCompilationResult.Rejected(
                CompilationFailureReason,
                CreateCompilationFailureDetails(
                    references.ReferenceDiagnostics,
                    compilationDiagnostics));
        }

        return new ScriptCompilationResult.Compiled(assemblyStream.ToArray());
    }

    private async Task<object?> InvokeEntryAsync(
        byte[] assemblyBytes,
        XiangshuScriptGlobals globals,
        IpcScriptEntryThread entryThread,
        Action markEntryInvoked,
        CancellationToken cancellationToken)
    {
        using AssemblyResolutionScope resolutionScope =
            _referenceResolver.CreateAssemblyResolutionScope();
        Assembly assembly = Assembly.Load(assemblyBytes);
        Type scriptType = FindEntryType(assembly);
        MethodInfo executeMethod = FindEntryMethod(scriptType);

        object? entryReturnValue = await _entryDispatcher.InvokeAsync(
            () =>
            {
                markEntryInvoked();
                return executeMethod.Invoke(null, [globals]);
            },
            entryThread,
            cancellationToken);
        return await ResolveReturnValueAsync(entryReturnValue, cancellationToken);
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
                "The compiled script has multiple public static "
                + $"{EntryTypeSimpleName} entry types. Keep only one entry type.");
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
                "The compiled script has multiple matching entry methods. "
                + $"Keep only one {AsyncEntryMethodName}"
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
        return "Entry type contract was not satisfied. Define exactly one public static "
            + $"non-generic class named {EntryTypeSimpleName}.";
    }

    private static string GetEntryMethodContractMessage()
    {
        return "Entry method contract was not satisfied. Define exactly one public static "
            + $"{EntryTypeSimpleName}.{AsyncEntryMethodName} or {EntryTypeSimpleName}.{SyncEntryMethodName} method with one "
            + $"parameter whose type resolves to {ScriptGlobalsFullName}.";
    }

    private static async Task<object?> ResolveReturnValueAsync(
        object? returnValue,
        CancellationToken cancellationToken)
    {
        if (returnValue is null)
        {
            return null;
        }

        if (returnValue is Task task)
        {
            await WaitForTaskAsync(task, cancellationToken);
            return ReadTaskReturnValue(task);
        }

        return returnValue;
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

    private static object? ReadTaskReturnValue(Task task)
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

    private const string CompilationFailureReason =
        "Compilation could not produce an assembly.";

    private static IpcRunScriptNotInvokedDetails CreateCompilationFailureDetails(
        IReadOnlyList<string> referenceDiagnostics,
        string[] compilationDiagnostics)
    {
        return new IpcRunScriptNotInvokedDetails(
            [.. referenceDiagnostics],
            compilationDiagnostics);
    }

    private static Exception UnwrapInvocationException(Exception exception)
    {
        return exception is TargetInvocationException { InnerException: not null }
            ? exception.InnerException
            : exception;
    }

    private abstract class ScriptCompilationResult
    {
        private ScriptCompilationResult()
        {
        }

        public sealed class Compiled(byte[] assemblyBytes) : ScriptCompilationResult
        {
            public byte[] AssemblyBytes { get; } =
                assemblyBytes ?? throw new ArgumentNullException(nameof(assemblyBytes));
        }

        public sealed class Rejected(
            string reason,
            IpcRunScriptNotInvokedDetails? details = null) : ScriptCompilationResult
        {
            public string Reason { get; } =
                reason ?? throw new ArgumentNullException(nameof(reason));

            public IpcRunScriptNotInvokedDetails? Details { get; } = details;
        }

    }

}
