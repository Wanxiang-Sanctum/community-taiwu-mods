---
name: unity-use
description: "Use when Xiangshu needs to perform, target, or verify the result of a player-like operation in the live Unity frontend, including screen-coordinate targeting, UI gesture replay, selected-control edits, submit/cancel actions, scrolling, dragging, or hotkey-equivalent actions. Use with live Xiangshu frontend MCP/runtime tools. Do not use for visual-only screenshots or inspection, backend state edits, static game knowledge, ordinary conversation, or source-code maintenance."
---

# Unity Use

## Scope

Use this skill when the task is operational: turn the player's intent and current frontend evidence into a narrow Unity frontend action, then verify the player-visible result.

Observation is supporting evidence, not this skill's center. For visual-only questions, use the available screenshot or frontend observation tool directly and do not load this skill unless an action, target selection, or verification of a requested operation depends on that observation.

Target `frontend` for Unity Game view operations, UI objects, screen coordinates, EventSystem state, selected controls, input fields, and player-visible verification. Use `backend` only for read-only authoritative checks after a frontend operation when persisted game state matters. Do not mutate backend state to impersonate a player gesture.

## Operating Model

1. Identify the requested operation and its consequence.
2. Gather the least frontend evidence needed to choose a target or action.
3. Convert the target into Unity screen coordinates, a selected object, or a specific UI handler.
4. Apply the narrowest player-like frontend action that matches the request.
5. Verify through a fresh frontend probe, selected object state, screenshot evidence when visible state matters, or relevant read-only backend state.

Infer missing details from visible state, current tool results, and the player's wording. Use read-only observation or probes when they can resolve ambiguity. Ask the player only when the intended target or consequence remains ambiguous, or when the next action may be irreversible and the player has not already accepted that consequence.

## Tool Selection

Prefer tools in this order:

1. A dedicated Xiangshu frontend MCP tool for the exact operation.
2. `xiangshu_capture_player_view` only as evidence before targeting or for verification after an operation; its own tool description owns screenshot behavior.
3. `xiangshu_run_csharp_script` on `frontend` with a complete C# compilation unit.
4. A narrow frontend method call or reflection probe when EventSystem replay cannot express the operation.
5. Backend read-only verification after the frontend action, if persisted state matters.

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

Use `System`, `System.Collections.Generic`, and `UnityEngine` at the top of the compilation unit for the helper. Action scripts usually also need `UnityEngine.EventSystems`. Place this helper type inside the complete `frontend` script's `XiangshuScript` class when acting on player-view coordinates:

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

For Unity UI pointer gestures, replay EventSystem events. Do not try to assign `Input.mousePosition` or fake `Input.GetMouseButtonDown`; those APIs report frame input state and do not perform UI interaction.

Use this decision model:

- If the target has an EventSystem handler, send the matching pointer, scroll, drag, submit, or cancel sequence to that handler.
- If the target is a selected input field, modify the concrete input component value and invoke its change or submit events when needed.
- If the player asks for a hotkey and the game implements it through frame input such as `Input.GetKeyDown`, prefer an equivalent visible UI control or a narrow frontend method call.
- If no EventSystem path represents the operation, call the smallest frontend API that performs the same player-visible command.
- If the action may be irreversible, verify that the player's wording already covers the consequence; otherwise stop before the write and ask one concrete question.

Minimal click event-replay fragment, not a complete runtime script. Use `tool-guides/RUNTIME_SCRIPTING.md` for the C# compilation-unit shape and supply the surrounding script context, target point, hit list, chat-window guard, and Unity main-thread switch from the current operation:

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

using (new XiangshuChatWindowGuard(suppress: !includeXiangshuChat))
{
    eventSystem.RaycastAll(eventData, hits);
    if (hits.Count > 0)
    {
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
    }
}
```

For scroll, drag, submit, cancel, and text input, keep the same discipline: identify the handler or selected control first, execute only the requested gesture, and return compact before/after facts. If a complete runtime script is needed, use the runtime scripting guide for the script shell and keep this fragment as the EventSystem core.

## Runtime Script Discipline

The Xiangshu runner receives a complete C# compilation unit. Follow `tool-guides/RUNTIME_SCRIPTING.md` for the entry contract, target side, async behavior, arguments, and result shape.

Use `globals.Arguments` for coordinates, text, target paths, mode flags, and confirmation flags. Return compact structured data. Do not return image bytes or large base64 payloads through script JSON.

If low-level reflection or private game members become necessary, combine this skill with `bepinex-runtime-scripting`, but keep the frontend operation goal primary.

## Verification And Reply

After every operation, verify with the narrowest reliable observation: raycast probe, selected object state, visible UI state, screenshot evidence when pixels matter, or read-only backend state. Report only the player-visible outcome in Xiangshu's voice. Keep tool names, file paths, stack traces, and implementation details out of ordinary replies.
