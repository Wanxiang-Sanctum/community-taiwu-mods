---
name: unity-use
description: "Use when Xiangshu needs to observe or act through the player's live Unity frontend view, including screenshots, screen-coordinate targeting, UI gesture replay, selected-control state, or player-visible verification. Use with live Xiangshu frontend MCP/runtime tools. Do not use for backend state edits, static game knowledge, ordinary conversation, or source-code maintenance."
---

# Unity Use

## Scope

Use this skill to give Xiangshu player-view eyes and hands inside the running Unity Game view. The primary responsibility is to observe the visible frontend, choose player-like actions, perform them through the frontend surface, and verify the visible result.

Target `frontend` for Unity Game view, screenshots, UI, screen coordinates, EventSystem state, selected controls, and visual verification. Use `backend` only when the player-view action needs an additional authoritative game-state check; do not use backend mutation to impersonate a player gesture.

## Branch Strategy

Do not try to enumerate every UI action. Treat screenshotting, clicking, typing, scrolling, dragging, submitting, and hotkeys as examples of a broader pattern:

1. Identify the player's intent: observe, target, gesture, or verify.
2. Choose the best observation channel available in this turn.
3. Convert evidence into Unity screen coordinates or selected objects.
4. Apply the narrowest frontend action that matches the player gesture.
5. Verify through a fresh visual or frontend-state observation.

Infer missing details from the visible state, current tool results, and the player's wording. Use read-only observation when it can
resolve ambiguity. Ask the player only when the intended target or consequence remains ambiguous after that, or when the next action
may be irreversible and the player has not already accepted that consequence.

Closed sets are worth naming only when the runtime makes them closed: actual tool names exposed this turn, MCP content block types, target side names such as `frontend`/`backend`, and fixed Unity API event handlers. For open sets, write and follow decision rules instead of expanding lists.

## Tool Selection

Prefer tools in this order:

1. A dedicated Xiangshu frontend MCP tool for the exact player-view operation; use `xiangshu_capture_player_view` for screenshots.
2. `xiangshu_run_csharp_script` on `frontend` with a complete C# compilation unit.
3. A narrow frontend method call or reflection probe when EventSystem replay cannot express the action.
4. Backend read-only verification after the frontend action, if persisted state matters.

Use `tool-guides/RUNTIME_SCRIPTING.md` before drafting runtime scripts if it has not already been loaded this turn. Check the actual tools exposed in the current turn; do not assume that a dedicated screenshot or gesture tool exists just because this skill describes the preferred shape. Actual tool descriptions override this skill.

## Observation Policy

Screenshots are visual context, so a screenshot tool should return MCP `image` content with `mimeType: image/png` as the primary result. Use `xiangshu_capture_player_view` as the zero-configuration full-screen native PNG capture; do not ask for crop, size, scale, or quality before the first visual observation. A saved file path is a cache or fallback, not proof that the model has seen the image.

Treat Xiangshu's own chat window as agent chrome, not ordinary game UI. Its root GameObject is named `Wanxiang.Xiangshu.ChatWindow`. Exclude that root from screenshots, hit probes, and pointer target selection unless the player is explicitly operating the Xiangshu chat window or diagnosing chat UI behavior. Runtime scripts must not hide, deactivate, or refocus it merely to make ordinary game observation possible.

Use this fallback ladder:

1. `xiangshu_capture_player_view`, returning full-screen native PNG image content.
2. Another dedicated screenshot MCP tool returning image content, a resource, or a resource link that the client can load.
3. Structured frontend probes, such as raycast hits or selected object state, when pixels cannot be inspected.
4. Runtime script image capture only as a last resort; do not hide, deactivate, or refocus Xiangshu chat UI to make that script work.

Because the dedicated screenshot tool captures full-screen native PNG, the PNG's intrinsic pixel dimensions are the Unity screen coordinate bounds for that observation.

## Coordinate Rules

Unity screen coordinates use bottom-left origin: `(0, 0)` at bottom-left and the screenshot image dimensions at top-right for `xiangshu_capture_player_view` results. Many image viewers report top-left image coordinates. Convert before acting:

```csharp
float unityX = imageX;
float unityY = imageHeight - imageY;
```

Prefer target centers over edges. Return the point, origin, screen size, and hit/selection evidence from probes so the next action uses the same coordinate frame.

## Runtime Script Contract

The Xiangshu runner waits for `Task` and `Task<T>`, not `UniTask`. Unity frontend APIs must run on the Unity main thread:

```csharp
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    public static async Task<object?> ExecuteAsync(XiangshuScriptGlobals globals)
    {
        await UniTask.SwitchToMainThread(globals.CancellationToken);
        return new { side = globals.Side };
    }
}
```

Use `globals.Arguments` for coordinates, text, path options, and mode flags. Return compact structured data. Do not return image bytes or large base64 payloads through script JSON; use the dedicated screenshot tool for visual observation, or fall back to structured frontend probes.

## Target Probe Pattern

Before acting on screen coordinates, raycast the point through Unity's current EventSystem. Use the ordered hits to decide whether the target matches the visible intent. The probe filters out Xiangshu chat-window hits by default; pass `includeXiangshuChat=true` only when the chat window is the requested target.

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    private const string XiangshuChatRootName = "Wanxiang.Xiangshu.ChatWindow";

    public static async Task<object?> ExecuteAsync(XiangshuScriptGlobals globals)
    {
        await UniTask.SwitchToMainThread(globals.CancellationToken);
        EventSystem es = EventSystem.current ?? throw new InvalidOperationException("No active Unity EventSystem.");
        Vector2 point = ReadPoint(globals);
        PointerEventData eventData = new(es)
        {
            pointerId = -1,
            position = point,
            button = PointerEventData.InputButton.Left,
        };

        List<RaycastResult> hits = new();
        es.RaycastAll(eventData, hits);
        bool includeXiangshuChat = ShouldIncludeXiangshuChat(globals);
        List<RaycastResult> visibleHits = includeXiangshuChat
            ? hits
            : hits.Where(hit => !IsUnderXiangshuChatWindow(hit.gameObject)).ToList();

        return new
        {
            point = new { x = point.x, y = point.y },
            screen = new { width = Screen.width, height = Screen.height },
            ignoredXiangshuChatHits = hits.Count - visibleHits.Count,
            hits = visibleHits.Take(12).Select((hit, index) => new
            {
                index,
                path = PathOf(hit.gameObject),
                module = hit.module ? hit.module.GetType().FullName : null,
                depth = hit.depth,
                distance = hit.distance,
                clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject) is { } click
                    ? PathOf(click)
                    : null,
                scrollTarget = ExecuteEvents.GetEventHandler<IScrollHandler>(hit.gameObject) is { } scroll
                    ? PathOf(scroll)
                    : null,
            }).ToArray(),
        };
    }

    private static Vector2 ReadPoint(XiangshuScriptGlobals globals)
    {
        float x = float.Parse(globals.Arguments["x"], CultureInfo.InvariantCulture);
        float y = float.Parse(globals.Arguments["y"], CultureInfo.InvariantCulture);
        if (globals.Arguments.TryGetValue("origin", out string? origin) && origin == "top-left")
        {
            y = Screen.height - y;
        }

        return new Vector2(x, y);
    }

    private static bool ShouldIncludeXiangshuChat(XiangshuScriptGlobals globals)
    {
        return globals.Arguments.TryGetValue("includeXiangshuChat", out string? value)
            && bool.TryParse(value, out bool parsed)
            && parsed;
    }

    private static bool IsUnderXiangshuChatWindow(GameObject gameObject)
    {
        for (Transform? current = gameObject.transform; current is not null; current = current.parent)
        {
            if (current.name == XiangshuChatRootName)
            {
                return true;
            }
        }

        return false;
    }

    private static string PathOf(GameObject gameObject)
    {
        List<string> names = new();
        for (Transform? current = gameObject.transform; current is not null; current = current.parent)
        {
            names.Add(current.name);
        }

        names.Reverse();
        return string.Join("/", names);
    }
}
```

## Action Rules

For Unity UI pointer gestures, replay EventSystem events rather than trying to assign `Input.mousePosition` or fake `Input.GetMouseButtonDown`; those APIs report frame input state.

Use this decision model:

- If the target has an EventSystem handler, send the matching pointer/scroll/submit/cancel sequence to that handler.
- If the target is a selected input field, modify the field type directly and invoke its change events when needed.
- If the player is targeting ordinary game UI, ignore raycast hits under `Wanxiang.Xiangshu.ChatWindow`; include that root only when the player is explicitly operating the Xiangshu chat window.
- If a hotkey is implemented only through `Input.GetKeyDown`, prefer an equivalent visible UI control or a narrow frontend method call; mention the limitation only when the player asked about operation details.
- If the action may be irreversible, first verify from visible state, current tool results, and the player's wording that the consequence
  is covered; ask for confirmation only when it remains uncovered.

Minimal click sequence. Reuse the `IsUnderXiangshuChatWindow` helper from the target probe pattern, and set `includeXiangshuChat` only when the requested target is the Xiangshu chat window:

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

eventSystem.RaycastAll(eventData, hits);
bool includeXiangshuChat = false;
RaycastResult hit = hits.FirstOrDefault(hit =>
    includeXiangshuChat || !IsUnderXiangshuChatWindow(hit.gameObject));
GameObject? target = hit.gameObject;
if (target is not null)
{
    eventData.pointerCurrentRaycast = hit;
    eventData.pointerPressRaycast = hit;
    eventData.rawPointerPress = target;
    GameObject? press = ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerDownHandler)
        ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
    if (press is not null)
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
```

After every action, verify with a new screenshot/image, raycast probe, selected object, or relevant frontend/backend state. Report the player-visible outcome in Xiangshu's voice; keep tool names, paths, and stack traces out of ordinary replies.

If low-level reflection or private game members become necessary, combine this skill with `bepinex-runtime-scripting`, but keep the player-view goal primary.
