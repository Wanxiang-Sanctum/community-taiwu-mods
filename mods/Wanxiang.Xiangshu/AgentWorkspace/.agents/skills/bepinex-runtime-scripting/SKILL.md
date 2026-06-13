---
name: bepinex-runtime-scripting
description: "Use when drafting or revising Xiangshu runtime C# scripts that need the checked-in BepInEx helper API XML for reflection, detours, IL manipulation, metadata inspection, or runtime state edits. Do not use for ordinary conversation, MCP tool selection, static mod code edits, or broad game API discovery."
---

# BepInEx Runtime Scripting

## Scope

Use this skill only after a Xiangshu runtime C# script is already the right implementation shape. MCP tool descriptions own whether and where a script is invoked; this skill owns the script compilation-unit contract, helper API reference selection, and runtime mutation discipline.

## Script Entry Contract

The MCP tool receives a complete C# compilation unit, not a statements snippet. Start from this entry shape and replace only the body with the requested work:

```csharp
using Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    public static object? Execute(XiangshuScriptGlobals globals)
    {
        return new
        {
            side = globals.Side,
        };
    }
}
```

The entry type may be inside a namespace, but its simple name must be `XiangshuScript`, and the script must define exactly one public static entry type with that simple name. Define exactly one public static `Execute` or `ExecuteAsync` method that takes one `XiangshuScriptGlobals` parameter; synchronous values, `Task`, and `Task<T>` are accepted. Use `globals.Arguments` for MCP arguments and `globals.CancellationToken` for cancellable work.

## API Reference Files

Before choosing helper APIs, read the XML that matches the script side:

- Frontend: `references/taiwu-plugin-helper-api-0.84.58-test.netstandard2.1.xml`
- Backend: `references/taiwu-plugin-helper-api-0.84.58-test.net8.0.xml`

Use the XML's `availableAssemblies`, `assembly`, `type`, and member signatures as the API directory. Do not maintain or infer a parallel DLL/API summary from this skill. The package id recorded inside the XML is provenance only: it identifies which pinned reference assemblies generated the signatures, not a runtime object or a search target.

## Drafting Order

When drafting the script body:

- Decide the target side from the requested state or action.
- Use the entry contract above as the outer shape.
- Read the matching XML before selecting helper namespaces, types, overloads, or parameter shapes.
- Turn to frontend/backend game APIs only when the task needs a concrete domain object or state API on that side.
- Bind or inspect live game objects only after the helper API choice is known.
- Keep the body focused on the requested runtime state change.

## Runtime Discipline

- Do not enumerate loaded assemblies to discover helper APIs already covered by the XML. Use runtime assembly lookup only to locate the actual assembly simple name for a known live type.
- If a direct `using` or member call fails, report the missing assembly, namespace, type, or member in the tool result.
- Read before writing, and return enough before/after data to verify the change.
- Prefer the narrowest member write, method call, hook, or metadata read that represents the requested state change.
- Preserve original values in the returned object when a change is reversible.
- If the helper cannot find a type or member, return a structured explanation instead of guessing nearby names.
- Do not expose helper names or reflection details in player-facing final text unless the player asked for mod development details.
