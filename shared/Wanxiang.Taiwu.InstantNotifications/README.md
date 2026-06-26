# Wanxiang.Taiwu.InstantNotifications

太吾绘卷前端即时通知适配项目。

本项目提供一个共享入口，把调用方已经判定需要展示的文本推送为游戏前端即时通知。调用方同时选择一个
`InstantNotification.DefKey` 模板，用它承载图标、通知类型和重要程度。

## 职责

`InstantNotificationPublisher.Push(...)` 把消息写入游戏前端的即时通知渲染列表，并触发
`UiEvents.OnNewInstantNotification`。本项目拥有这个前端展示适配；通知是否应该出现、显示什么文本、如何去重或节流，
由调用方所属业务模块决定。

## 模板与文本

`templateId` 使用游戏内置 `InstantNotification.DefKey` 模板 ID。模板决定通知外观、通知类型和重要程度；
`message` 是玩家可见文本，会替换模板原生描述。

调用方显式传入模板和文本。本项目的校验范围限定为模板存在性和文本可展示性。游戏内部通知类型范围属于游戏配置；
业务默认模板属于调用方策略。

## 调用方式

```csharp
InstantNotificationPublisher.Push(
    InstantNotification.DefKey.WalkThroughAbyss,
    "药钵中传来低语。");
```

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/Wanxiang.Taiwu.InstantNotifications/Wanxiang.Taiwu.InstantNotifications.csproj
```

本项目依赖 `Taiwu.ModKit.References.Frontend` 和 `Taiwu.ModKit.References.Shared`。当前实现使用可引用的
前端渲染模型、配置表和 UI 事件；调用方无需为它配置 Publicizer。
