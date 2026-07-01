---
name: bepinex-runtime-scripting
description: "Use when a Xiangshu player goal has reached the runtime C# scripting path and the script needs low-level BepInEx-style helpers: reflection or private member access, Harmony/MonoMod hooks or detours, IL or metadata inspection, or API-surface discovery beyond the public runtime guide. Do not use for ordinary conversation, visual frontend action strategy, static mod source edits, broad game API cataloging, or simple live-state scripts that can use documented public runtime APIs and tool-guides directly."
---

# BepInEx Runtime Scripting

## Scope

Use this skill only after the current request has already reached the runtime C# scripting path and the script needs low-level helper access. This skill owns helper selection, low-level probing, and hook/IL discipline.

Do not use this skill to choose the game domain, player-visible answer, script entry contract, or ordinary public API path. `tool-guides/RUNTIME_SCRIPTING.md` owns the MCP tool call shape, `arguments` object, target side, entry thread, and entry contract. Domain guides and `tool-guides/GAME_KNOWLEDGE.md` own game-domain routing. Use those guides first, then apply this skill only where public APIs or stable guide anchors are not enough.

## Entry Thread Selection

Choose the `xiangshu_run_csharp_script` `entryThread` together with the target side before drafting low-level helper code:

- Use `entryThread: "current"` only for pure computation, reference checks, type/member discovery, and other probes that do not touch live game objects, Unity state, backend domains, or live game state.
- Use `entryThread: "mainThread"` for Unity objects, frontend UI, EventSystem state, backend `DomainManager` access, game entities, live game state, and any mutation of live game or mod state.
- `entryThread` controls only the entry invocation thread. If the script deliberately schedules later work with `ExecuteAsync`, callbacks, or target-side async APIs, handle that later work according to that API's threading rules and keep it separate from entry-thread selection.

## BepInEx Helper Namespaces

The script runner compiles against the explicit references provided by the target plugin host, trusted platform assemblies, and currently loaded assemblies that still have a usable location. It does not make every DLL in the plugin directory a script reference. For BepInEx-style low-level work, orient around these namespace families rather than looking for a broad `BepInEx.*` API surface:

- `HarmonyLib`: `Harmony`, `HarmonyPatch`, `AccessTools`, `Traverse`, `CodeInstruction`, and patch/transpiler helpers. Prefer `AccessTools` for private or inherited members when direct typed access is not available.
- `MonoMod.RuntimeDetour`: runtime `Hook`, `ILHook`, detour config, hook collections, and detour inspection. Use it only when the request needs an active runtime hook or detour.
- `MonoMod.Cil`: `ILContext`, `ILCursor`, and IL editing helpers used with `ILHook` or transpiler-style work.
- `MonoMod.Utils`: dynamic data, reflection helpers, and low-level utility types. Use it as a helper layer after a concrete type or member target is known.
- `Mono.Cecil`, `Mono.Cecil.Cil`, and on backend also `Mono.Cecil.Rocks` plus symbol namespaces: assembly metadata and IL model inspection. Treat file rewrites as a separate explicit file-mutation task, not the normal runtime-script path.

Frontend scripts align with the Unity/netstandard side and should assume the common subset: Harmony, Mono.Cecil, MonoMod.RuntimeDetour, MonoMod.Cil, and MonoMod.Utils. Backend scripts align with the .NET backend side and can also use MonoMod.Core, MonoMod.ILHelpers, Cecil symbol support, and Cecil Rocks helpers when a task specifically needs them.

## Drafting Order

When low-level helper code is needed:

- Read `tool-guides/RUNTIME_SCRIPTING.md` if the current request has not already loaded it; use that guide's entry contract and tool-call parameters.
- Use the relevant domain guide or `tool-guides/GAME_KNOWLEDGE.md` before helper probing, so reflection starts from a target namespace, type, or method family.
- Prefer direct public game APIs. Choose helper namespaces from the BepInEx map above only when reflection, private access, hooks, IL work, or metadata inspection is part of the task.
- Bind or inspect live game objects only after the target side and helper approach are known.
- Keep the body focused on the requested runtime state change or inspection. Reusable entry points, field shapes, and local processing patterns belong in the relevant tool guide rather than inside a one-off script body.

## Runtime Discipline

- When a candidate helper or game member is uncertain, use a narrow read-only probe or compile diagnostics for that specific namespace, type, member, or overload. For unresolved candidates, return the unresolved assembly, namespace, type, or member names.
- Read before writing, and return enough before/after data to verify the change.
- Prefer the narrowest member write, method call, hook, or metadata read that represents the requested state change.
- Return original values when they help verify the requested change.
