---
name: unity-use
description: "Use when a Xiangshu player goal requires choosing, performing, or verifying a live Unity frontend action whose target is the visible UI surface, a selected control, screen coordinates, or a frontend-only command. Use with live Xiangshu frontend MCP/runtime tools; select this from visible-result or verification needs, not only explicit Unity/screenshot/tool wording. Do not use for visual-only inspection, ordinary game-state changes with a direct runtime/API owner, static game knowledge, ordinary conversation, or source-code maintenance."
---

# Unity Use

## Scope

Use this skill when the requested result belongs to the live Unity frontend and must be chosen, applied, or verified against current UI state. It covers visible UI targets, selected controls, screen-coordinate targeting, frontend-only commands, and verification after those actions.
Infer this path from the requested visible result, current UI dependency, or need to verify what the player will see.

Do not use this skill merely because a request may change the game. When a dedicated tool, backend domain API, or direct runtime API owns the result, use that path instead; return to frontend observation only if visible confirmation matters.

Observation is supporting evidence, not this skill's center. For visual-only questions, use the available screenshot or frontend observation tool directly and do not load this skill unless an action, target selection, or verification of a requested operation depends on that observation.

Target `frontend` for Unity Game view operations, UI objects, screen coordinates, EventSystem state, selected controls, input fields, and player-visible verification. Within this skill, use `backend` only for read-only authoritative runtime checks after a frontend operation when live game state matters.

## Operating Model

1. Identify the requested result and its consequence.
2. Decide whether the result is owned by direct runtime/API state, the visible frontend surface, a selected control, or a frontend-only command.
3. If direct runtime/API state owns the result, use that path and keep this skill only for visible verification.
4. Gather the least frontend evidence needed to choose a target or action.
5. Invoke the highest-level frontend entry that matches the request: dedicated tool, method or command, selected-control edit, or UI handler. Use coordinate or gesture replay only when the visible UI surface is the real target or no narrower path exists.
6. Verify through a fresh frontend probe, selected object state, screenshot evidence when visible state matters, or relevant read-only backend state.

Infer missing details from visible state, current tool results, and the player's wording. Use read-only observation or probes when they can resolve ambiguity. Treat ambiguity, missing target details, or an unpinned consequence as material for Xiangshu's own decision: choose a concrete action, narrower target, substitute fulfillment, visible outcome, or unfinished result in Xiangshu's voice; wishes, commands, and strong demands follow `AGENTS.md`.

## Tool Selection

Within the frontend path, prefer result paths in this order:

1. A dedicated Xiangshu frontend tool or stable public frontend/game API for the exact command.
2. A concrete selected-control edit, UI model update, or frontend command call.
3. `xiangshu_capture_player_view` only as evidence before targeting or for verification after an operation; its own tool description owns screenshot behavior.
4. `xiangshu_run_csharp_script` on `frontend` with `entryThread: "mainThread"`, using the entry contract from `tool-guides/RUNTIME_SCRIPTING.md`, when no narrower exposed tool exists.
5. EventSystem pointer, scroll, drag, submit, or cancel replay through that script only when the visible UI control itself is the target.
6. Backend read-only verification after the frontend action, if live game state matters.

Use `tool-guides/RUNTIME_SCRIPTING.md` before drafting runtime scripts if it has not already been loaded in the current request. Check the tools exposed in the current request; actual tool descriptions override this skill.

## Frontend Boundaries

Treat Xiangshu's own chat window as agent chrome, not ordinary game UI. Its root GameObject is named `Wanxiang.Xiangshu.ChatWindow`. The player-view screenshot tool excludes that window for observation, but real frontend pointer and keyboard operations do not automatically pass through it.

Use `includeXiangshuChat=true` only when the player is explicitly operating the Xiangshu chat window or diagnosing chat UI behavior. For normal game/UI targets, suppress chat-window hit testing with the guard below instead of filtering chat hits after raycast.

For non-chat operations, also treat `EventSystem.current.currentSelectedGameObject` under `Wanxiang.Xiangshu.ChatWindow` as chat chrome. Clear selection or select the intended target before submit, cancel, text input, or hotkey-equivalent events; do not infer that the chat input is the player target just because it was focused by the conversation.

## Coordinates

Unity screen coordinates use bottom-left origin: `(0, 0)` at bottom-left and `Screen.width`, `Screen.height` at top-right. Image viewers often report top-left pixel coordinates. Convert screenshot evidence before acting:

```csharp
float unityX = imageX;
float unityY = imageHeight - imageY;
```

Prefer target centers over edges. When probing a target, return the point, origin, screen size, whether the chat-window guard was active, and ordered hit/selection evidence so a follow-up action uses the same coordinate frame.

## Target Probes

Before acting on screen coordinates, raycast the point through Unity's current EventSystem. Use the ordered hits to confirm that the target matches the player intent and to find the relevant handler.

For normal game/UI targets, run the raycast probe inside `XiangshuChatWindowGuard(suppress: true)` so the probe sees the same surface as the player-view screenshot.

Probe results should include:

- the input point and coordinate origin;
- `Screen.width` and `Screen.height`;
- whether `XiangshuChatWindowGuard` was active;
- the first useful raycast hits;
- click, scroll, drag, submit, or selectable handlers found through `ExecuteEvents.GetEventHandler<T>`;
- the currently selected GameObject when selected-control state matters.

## Chat Window Guard

For actions derived from player-view coordinates, run the target probe and action replay inside a short chat-window guard that temporarily suppresses the `CanvasGroup` on each active `Wanxiang.Xiangshu.ChatWindow` root, then restores it in `Dispose`/`finally`. Save and restore `alpha`, `blocksRaycasts`, and `interactable`; do not disable canvases, raycasters, or parent objects. Do not span awaits, long-running work, or unrelated probes while the guard is active.

When coordinate actions need chat suppression, place a local guard helper inside the `frontend` script's `XiangshuScript` class.
The helper is a reusable utility, not an operation recipe; keep the rest of the script body task-specific. It needs `System`,
`System.Collections.Generic`, and `UnityEngine` at the top of the compilation unit. Action scripts usually also need
`UnityEngine.EventSystems`.

```csharp
private sealed class XiangshuChatWindowGuard : IDisposable
{
    private const string RootName = "Wanxiang.Xiangshu.ChatWindow";
    private readonly List<CanvasGroupState> _states = new();

    public XiangshuChatWindowGuard(bool suppress)
    {
        if (!suppress)
        {
            return;
        }

        foreach (GameObject root in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (root.name != RootName || !root.activeInHierarchy)
            {
                continue;
            }

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            if (group == null)
            {
                continue;
            }

            _states.Add(new CanvasGroupState(group, group.alpha, group.blocksRaycasts, group.interactable));
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        Canvas.ForceUpdateCanvases();
    }

    public void Dispose()
    {
        for (int i = _states.Count - 1; i >= 0; i--)
        {
            CanvasGroupState state = _states[i];
            if (state.Group != null)
            {
                state.Group.alpha = state.Alpha;
                state.Group.blocksRaycasts = state.BlocksRaycasts;
                state.Group.interactable = state.Interactable;
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private readonly struct CanvasGroupState
    {
        public CanvasGroupState(CanvasGroup group, float alpha, bool blocksRaycasts, bool interactable)
        {
            Group = group;
            Alpha = alpha;
            BlocksRaycasts = blocksRaycasts;
            Interactable = interactable;
        }

        public CanvasGroup Group { get; }

        public float Alpha { get; }

        public bool BlocksRaycasts { get; }

        public bool Interactable { get; }
    }
}
```

Wrap the coordinate probe and action replay:

```csharp
bool includeXiangshuChat = false;
using (new XiangshuChatWindowGuard(suppress: !includeXiangshuChat))
{
    // Raycast, choose the target, replay the requested UI action, and collect before/after facts here.
}
```

If the script cannot keep the guard inside a synchronous `using`/`finally` boundary, do not perform a coordinate-based operation. Use a direct UI/game-object call that does not depend on screen hit testing.

## Action Rules

For Unity UI pointer gestures, replay EventSystem events only when the visible UI control is the task target or no narrower frontend command represents the requested result. Do not try to assign `Input.mousePosition` or fake `Input.GetMouseButtonDown`; those APIs report frame input state and do not perform UI interaction.

Use this decision model:

- If the target is a selected input field, modify the concrete input component value and invoke its change or submit events when needed.
- If the player asks for a hotkey and the game implements it through frame input such as `Input.GetKeyDown`, prefer an equivalent visible UI control or a narrow frontend method call.
- If a stable frontend method or command performs the requested visible command, call that instead of replaying low-level input.
- If the target has an EventSystem handler and no narrower path exists, send the matching pointer, scroll, drag, submit, or cancel sequence to that handler.
- If a frontend operation may carry an unpinned consequence, use current visible state and tool results to choose the narrowest concrete action that still serves the request; when no concrete frontend action can be formed, return a visible outcome or unfinished result in Xiangshu's voice.

Use the following as event-replay primitives for cases where a visible control must be activated through EventSystem.
`tool-guides/RUNTIME_SCRIPTING.md` owns the C# compilation-unit shell and tool call; this section supplies only the EventSystem pieces,
target point, hit list, and chat-window guard context for the current operation.

Prepare pointer event data:

```csharp
PointerEventData eventData = new(eventSystem)
{
    pointerId = -1,
    position = point,
    pressPosition = point,
    button = PointerEventData.InputButton.Left,
    clickCount = 1,
    clickTime = Time.unscaledTime,
    eligibleForClick = true,
    useDragThreshold = true,
};
```

Keep raycast and action inside the same short guard:

```csharp
using (new XiangshuChatWindowGuard(suppress: !includeXiangshuChat))
{
    eventSystem.RaycastAll(eventData, hits);
    // Choose the target and invoke only the requested handler here.
}
```

For a plain click, use this core inside that guarded block after checking `hits.Count > 0` and choosing the accepted hit:

```csharp
RaycastResult hit = hits[0];
GameObject target = hit.gameObject;
eventData.pointerCurrentRaycast = hit;
eventData.pointerPressRaycast = hit;
eventData.rawPointerPress = target;

GameObject press = ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerDownHandler)
    ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
if (press != null)
{
    eventData.pointerPress = press;
    eventData.pointerClick = press;
    eventSystem.SetSelectedGameObject(press, eventData);
    ExecuteEvents.Execute(press, eventData, ExecuteEvents.pointerUpHandler);
    if (ReferenceEquals(ExecuteEvents.GetEventHandler<IPointerClickHandler>(target), press))
    {
        ExecuteEvents.Execute(press, eventData, ExecuteEvents.pointerClickHandler);
    }
}
```

For scroll, drag, submit, cancel, and text input, keep the same discipline: identify the handler or selected control first, execute only the requested gesture, and return compact before/after facts. When these fragments are used in a runtime script, wrap them with the runtime scripting guide's entry shell and keep this fragment as the EventSystem core.

## Runtime Script Discipline

Runtime scripts follow the compilation-unit entry contract in `tool-guides/RUNTIME_SCRIPTING.md`; this skill only chooses when frontend UI work needs that path and what EventSystem/UI core to place inside it.

For frontend UI, Unity objects, EventSystem state, selected controls, coordinates, and visible verification scripts, call `xiangshu_run_csharp_script` with `entryThread: "mainThread"`. Handle later awaited or callback work as a separate threading decision owned by that API.

Pass coordinates, text, target paths, and mode flags through the tool's required `arguments` object, then read them from `globals.Arguments`. Return compact structured data. Do not return image bytes or large base64 payloads through script JSON.

If low-level reflection or private game members become necessary, combine this skill with `bepinex-runtime-scripting`, but keep the frontend operation goal primary.

## Verification And Reply

After every operation, verify with the narrowest reliable observation: raycast probe, selected object state, visible UI state, screenshot evidence when pixels matter, or read-only backend state. Report only the player-visible outcome in Xiangshu's voice. Keep tool names, file paths, stack traces, and implementation details out of ordinary replies.
