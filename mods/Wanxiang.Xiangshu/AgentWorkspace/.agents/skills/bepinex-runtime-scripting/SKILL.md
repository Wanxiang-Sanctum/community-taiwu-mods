---
name: bepinex-runtime-scripting
description: "Use when drafting or revising Xiangshu runtime C# scripts that need the checked-in BepInEx helper API XML for reflection, detours, IL manipulation, metadata inspection, or runtime state edits. Do not use for ordinary conversation, MCP tool selection, static mod code edits, or broad game API discovery."
---

# BepInEx Runtime Scripting

## Scope

Use this skill after the request already calls for a Xiangshu runtime C# script. Produce a complete compilation unit, choose helper APIs from the checked-in XML, orient game API use by runtime side, and keep mutations narrow.

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

Use the XML's `availableAssemblies`, `assembly`, `type`, and member signatures as the API directory. Ignore package id metadata when choosing runtime APIs.

## Runtime Game API Orientation

Use this orientation as a compact map of the loaded game API surface when a script needs concrete game state or UI types:

- Prefer backend for persisted or authoritative game state: Taiwu identity, inventory, characters, map/world data, organizations, items, information, combat state, monthly events, adventure state, and state mutations.
- Backend domain APIs usually live under `GameData.Domains.*`. `GameData.Domains.Taiwu`, `GameData.Domains.Character`, `GameData.Domains.Map`, `GameData.Domains.World`, `GameData.Domains.Organization`, `GameData.Domains.Item`, `GameData.Domains.Information`, `GameData.Domains.Combat`, `GameData.Domains.TaiwuEvent`, and `GameData.Domains.Adventure` are common anchors.
- `DomainManager.*` is the usual backend entry shape for domain access. For Taiwu-specific state, look for `DomainManager.Taiwu`; for characters, use `DomainManager.Character`; for event or monthly flow, use `DomainManager.TaiwuEvent` or related domain managers.
- Shared value shapes, display data, config cells, and enum names usually come from `GameData.Shared`, `Config`, `Config.Common`, `Config.ConfigCells`, and `GameData.Domains.*` enum namespaces. Prefer reachable enum and config names over raw numeric constants.
- Prefer frontend for visible UI state, selected controls, active windows, Unity objects, local resources, hotkeys, and display-only data.
- Frontend UI roots usually live under `Game.Views.*`; reusable widgets and list/sort/filter components under `Game.Components.*`; UI lifecycle, resources, commands, and localization under `FrameWork.UISystem`, `FrameWork.ResManager`, `FrameWork.CommandSystem`, and `FrameWork.Localization`.
- Resolve text, config IDs, event GUIDs, and player-facing names from the player request, current tool results, packaged lightweight context, or live game APIs.

## Drafting Order

When drafting the script body:

- Decide the target side from the requested state or action.
- Use the entry contract above as the outer shape.
- Read the matching XML before selecting helper namespaces, types, overloads, or parameter shapes.
- Use the runtime game API orientation when the task needs a concrete game API, config ID, UI type, or state owner.
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
