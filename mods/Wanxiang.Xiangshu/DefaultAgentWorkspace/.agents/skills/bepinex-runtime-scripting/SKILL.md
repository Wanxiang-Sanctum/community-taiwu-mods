---
name: bepinex-runtime-scripting
description: "Use when drafting or revising Xiangshu runtime C# scripts that inspect or mutate live frontend or backend game state, especially scripts that need BepInEx helper namespaces for reflection, detours, IL manipulation, or metadata inspection. Do not use for ordinary conversation, deciding whether to call an MCP tool, static mod source edits, or broad game API cataloging."
---

# BepInEx Runtime Scripting

## Scope

Use this skill only after the current task has already selected Xiangshu runtime C# scripting as the implementation path. Produce a complete compilation unit, choose the frontend or backend side, use BepInEx helper namespaces when low-level access is needed, and keep live-state changes narrow and verifiable.

## Script Entry Contract

The MCP tool receives a complete C# compilation unit, not a statements snippet. Declare required `using` directives and namespaces explicitly; the runner supplies no implicit `using` list. Use this as the minimum entry contract shell only; choose the body from the current task and relevant guide fragments.

```csharp
using Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    public static object? Execute(XiangshuScriptGlobals globals)
    {
        return null;
    }
}
```

The entry type may be inside a namespace, but its simple name must be `XiangshuScript`, and the script must define exactly one public static non-generic class with that simple name. Define exactly one public static `Execute` or `ExecuteAsync` method that takes one `XiangshuScriptGlobals` parameter; synchronous values, `Task`, and `Task<T>` are accepted. Use `globals.Arguments` for MCP arguments and `globals.CancellationToken` for cancellable work.

## Entry Thread Selection

Choose the `xiangshu_run_csharp_script` `entryThread` together with the target side before drafting the body:

- Use `entryThread: "current"` only for pure computation, reference checks, type/member discovery, and other probes that do not touch live game objects, Unity state, backend domains, or live game state.
- Use `entryThread: "mainThread"` for Unity objects, frontend UI, EventSystem state, backend `DomainManager` access, game entities, live game state, and any mutation of live game or mod state.
- `entryThread` controls only the entry invocation thread. If the script deliberately schedules later work with `ExecuteAsync`, callbacks, or target-side async APIs, handle that later work according to that API's threading rules and keep it separate from entry-thread selection.

## BepInEx Helper Namespaces

The script runner compiles against the target plugin side's deployment directory, trusted platform assemblies, and already loaded assemblies. For BepInEx-style low-level work, orient around these namespace families rather than looking for a broad `BepInEx.*` API surface:

- `HarmonyLib`: `Harmony`, `HarmonyPatch`, `AccessTools`, `Traverse`, `CodeInstruction`, and patch/transpiler helpers. Prefer `AccessTools` for private or inherited members when direct typed access is not available.
- `MonoMod.RuntimeDetour`: runtime `Hook`, `ILHook`, detour config, hook collections, and detour inspection. Use it only when the request needs an active runtime hook or detour.
- `MonoMod.Cil`: `ILContext`, `ILCursor`, and IL editing helpers used with `ILHook` or transpiler-style work.
- `MonoMod.Utils`: dynamic data, reflection helpers, and low-level utility types. Use it as a helper layer after a concrete type or member target is known.
- `Mono.Cecil`, `Mono.Cecil.Cil`, and on backend also `Mono.Cecil.Rocks` plus symbol namespaces: assembly metadata and IL model inspection. Treat file rewrites as a separate explicit file-mutation task, not the normal runtime-script path.

Frontend scripts align with the Unity/netstandard side and should assume the common subset: Harmony, Mono.Cecil, MonoMod.RuntimeDetour, MonoMod.Cil, and MonoMod.Utils. Backend scripts align with the .NET backend side and can also use MonoMod.Core, MonoMod.ILHelpers, Cecil symbol support, and Cecil Rocks helpers when a task specifically needs them.

## Runtime Game API Orientation

Use this orientation as a compact map of the loaded game API surface when a script needs concrete game state or UI types. For game knowledge queries, config lookup, localization, template/display helpers, Baixiaoce data, or namespace routing before a script, read `tool-guides/GAME_KNOWLEDGE.md` first.

- Prefer backend for authoritative runtime game state: Taiwu identity, inventory, characters, map/world data, organizations, items, information, combat state, monthly events, adventure state, and state mutations.
- Backend domain APIs usually live under `GameData.Domains.*`. `GameData.Domains.Taiwu`, `GameData.Domains.Character`, `GameData.Domains.Item`, `GameData.Domains.Map`, `GameData.Domains.World`, `GameData.Domains.Organization`, `GameData.Domains.Building`, `GameData.Domains.CombatSkill`, `GameData.Domains.Combat`, `GameData.Domains.Adventure`, `GameData.Domains.TaiwuEvent`, `GameData.Domains.LifeRecord`, `GameData.Domains.Information`, `GameData.Domains.LegendaryBook`, `GameData.Domains.Merchant`, `GameData.Domains.Mod`, `GameData.Domains.Global`, `GameData.Domains.Extra`, `GameData.Domains.Story`, `GameData.Domains.TutorialChapter`, and `GameData.Domains.SpecialEffect` are common anchors.
- `DomainManager.*` is the usual backend entry shape for domain access. For Taiwu-specific state, look for `DomainManager.Taiwu`; for characters, use `DomainManager.Character`; for event or monthly flow, use `DomainManager.TaiwuEvent` or related domain managers.
- Shared value shapes, display data, config cells, and enum names usually come from `GameData.Shared`, `Config`, `Config.Common`, `Config.ConfigCells`, and `GameData.Domains.*` enum namespaces. Prefer reachable enum and config names over raw numeric constants.
- Prefer frontend for visible UI state, selected controls, active windows, Unity objects, local resources, hotkeys, and display-only data.
- Frontend UI roots usually live under `Game.Views.*`; reusable widgets and list/sort/filter components under `Game.Components.*` and `CommonSortAndFilterLegacy.*`; UI lifecycle, resources, commands, and localization under `FrameWork.UISystem`, `FrameWork.ResManager`, `FrameWork.CommandSystem`, and `FrameWork.Localization`.
- `Config.*`, `GameDataExtensions`, and `UICommon` are often better first targets than broad reflection when resolving display text, template facts, localization, or frontend bridge helpers.
- Resolve text, config IDs, event GUIDs, and player-facing names from the player request, current tool results, packaged lightweight context, or live game APIs.

## Drafting Order

When drafting the script body:

- Decide the target side and `entryThread` from the requested state or action.
- Use the entry contract above as the outer shape, then add only the current task's direct API or helper calls.
- Prefer direct public game APIs. Choose helper namespaces from the BepInEx map above only when reflection, private access, hooks, IL work, or metadata inspection is part of the task.
- Use the runtime game API orientation when the task needs a concrete game API, config ID, UI type, or state owner.
- Bind or inspect live game objects only after the target side and helper approach are known.
- Keep the body focused on the requested runtime state change.

## Runtime Discipline

- When a candidate helper or game member is uncertain, use a narrow read-only probe or compile diagnostics for that specific namespace, type, member, or overload. For unresolved candidates, return the unresolved assembly, namespace, type, or member names.
- Read before writing, and return enough before/after data to verify the change.
- Prefer the narrowest member write, method call, hook, or metadata read that represents the requested state change.
- Return original values when they help verify the requested change.
