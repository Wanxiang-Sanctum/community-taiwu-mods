using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Compiles and invokes trusted C# scripts inside the current plugin process.
/// </summary>
public sealed class DynamicScriptRunner
{
    private const string CompilationFailureReason =
        "Compilation could not produce an assembly.";
    private const string AsyncEntryMethodName = "ExecuteAsync";
    private const string SyncEntryMethodName = "Execute";

    private readonly DynamicScriptEntryContract _contract;
    private readonly ScriptReferenceResolver _referenceResolver;
    private readonly IDynamicScriptEntryDispatcher _entryDispatcher;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.None,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicScriptRunner"/> class.
    /// </summary>
    /// <param name="contract">The mod-specific script entry contract.</param>
    /// <param name="referenceOptions">The explicit assembly reference inputs.</param>
    /// <param name="entryDispatcher">An optional dispatcher for host thread selection.</param>
    public DynamicScriptRunner(
        DynamicScriptEntryContract contract,
        DynamicScriptReferenceOptions referenceOptions,
        IDynamicScriptEntryDispatcher? entryDispatcher = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(referenceOptions);
#else
        if (contract is null)
        {
            throw new ArgumentNullException(nameof(contract));
        }

        if (referenceOptions is null)
        {
            throw new ArgumentNullException(nameof(referenceOptions));
        }
#endif

        _contract = contract;
        _referenceResolver = new ScriptReferenceResolver(referenceOptions);
        _entryDispatcher = entryDispatcher ?? CurrentThreadEntryDispatcher.Instance;
    }

    /// <summary>
    /// Compiles the requested script, invokes its entry method when possible, and returns invocation facts.
    /// </summary>
    /// <param name="request">The script execution request.</param>
    /// <param name="globals">The caller-owned globals object passed to the script entry method.</param>
    /// <param name="cancellationToken">The cancellation token for the compile and invocation operation.</param>
    /// <returns>The script run result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="globals"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="globals"/> does not satisfy the configured script contract.</exception>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Script exceptions and pre-invocation rejection reasons are returned through the result union.")]
    public async Task<DynamicScriptRunResult> ExecuteAsync(
        DynamicScriptRunRequest request,
        object globals,
        CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(globals);
#else
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (globals is null)
        {
            throw new ArgumentNullException(nameof(globals));
        }
#endif
        ValidateGlobals(globals);

        bool entryInvoked = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            ScriptCompilationResult compilationResult = Compile(request.Script, cancellationToken);
            if (compilationResult is ScriptCompilationResult.Rejected rejected)
            {
                return DynamicScriptRunResult.NotInvoked(
                    rejected.Reason,
                    rejected.Details);
            }

            ScriptCompilationResult.Compiled compiled = (ScriptCompilationResult.Compiled)compilationResult;
            object? value = await InvokeEntryAsync(
                compiled.AssemblyBytes,
                globals,
                request.EntryThread,
                () => entryInvoked = true,
                cancellationToken);

            return DynamicScriptRunResult.InvokedWithReturnValue(SerializeReturnValueJson(value));
        }
        catch (OperationCanceledException)
        {
            const string message = "Script execution was canceled.";
            return entryInvoked
                ? DynamicScriptRunResult.InvokedWithException(message)
                : DynamicScriptRunResult.NotInvoked(message);
        }
        catch (Exception ex)
        {
            string message = UnwrapInvocationException(ex).ToString();
            return entryInvoked
                ? DynamicScriptRunResult.InvokedWithException(message)
                : DynamicScriptRunResult.NotInvoked(message);
        }
    }

    private ScriptCompilationResult Compile(string source, CancellationToken cancellationToken)
    {
        CompilationReferences references =
            _referenceResolver.CollectReferences(
                _contract.ScriptGlobalsType.Assembly,
                GetScriptGlobalsTypeName());
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
            CreateScriptAssemblyName(),
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

    private void ValidateGlobals(object globals)
    {
        if (!_contract.ScriptGlobalsType.IsInstanceOfType(globals))
        {
            throw new ArgumentException(
                $"Script globals object has type {globals.GetType().FullName}, "
                + $"but the script contract requires {GetScriptGlobalsTypeName()}.",
                nameof(globals));
        }
    }

    private async Task<object?> InvokeEntryAsync(
        byte[] assemblyBytes,
        object globals,
        DynamicScriptEntryThread entryThread,
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

    private Type FindEntryType(Assembly assembly)
    {
        return assembly.GetTypes().FirstOrDefault(IsValidEntryType)
            ?? throw new InvalidOperationException(GetEntryTypeContractMessage());
    }

    private MethodInfo FindEntryMethod(Type scriptType)
    {
        const BindingFlags EntryMethodSearchFlags =
            BindingFlags.Public
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        MethodInfo[] candidates =
        [
            .. scriptType
                .GetMethods(EntryMethodSearchFlags)
                .Where(method =>
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
                + $" or {SyncEntryMethodName} overload with one parameter whose type resolves to {GetScriptGlobalsTypeName()}.");
        }

        throw new InvalidOperationException(GetEntryMethodContractMessage());
    }

    private bool IsValidEntryType(Type type)
    {
        return type.FullName == _contract.EntryTypeFullName
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

    private bool HasScriptGlobalsParameter(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Length == 1
            && parameters[0].ParameterType == _contract.ScriptGlobalsType;
    }

    private string GetEntryTypeContractMessage()
    {
        return "Entry type contract was not satisfied. Define exactly one public static "
            + $"non-generic class named {_contract.EntryTypeFullName}.";
    }

    private string GetEntryMethodContractMessage()
    {
        return "Entry method contract was not satisfied. Define exactly one public static "
            + $"{_contract.EntryTypeFullName}.{AsyncEntryMethodName} or "
            + $"{_contract.EntryTypeFullName}.{SyncEntryMethodName} method with one "
            + $"parameter whose type resolves to {GetScriptGlobalsTypeName()}.";
    }

    private string GetScriptGlobalsTypeName()
    {
        return _contract.ScriptGlobalsType.FullName ?? _contract.ScriptGlobalsType.Name;
    }

    private string CreateScriptAssemblyName()
    {
        string? globalsAssemblyName = _contract.ScriptGlobalsType.Assembly.GetName().Name;
        string assemblyNamePrefix = string.IsNullOrWhiteSpace(globalsAssemblyName)
            ? _contract.EntryTypeFullName
            : globalsAssemblyName;
        return $"{assemblyNamePrefix}.DynamicScript.{Guid.NewGuid():N}";
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

    private static DynamicScriptNotInvokedDetails CreateCompilationFailureDetails(
        IReadOnlyList<string> referenceDiagnostics,
        string[] compilationDiagnostics)
    {
        return new DynamicScriptNotInvokedDetails(
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
            DynamicScriptNotInvokedDetails? details = null) : ScriptCompilationResult
        {
            public string Reason { get; } =
                reason ?? throw new ArgumentNullException(nameof(reason));

            public DynamicScriptNotInvokedDetails? Details { get; } = details;
        }
    }
}
