# 后端模块结构

`BackendPlugin.cs` 是太吾后端插件生命周期的组合根。后端职责集中在启动后端 MessagePipe IPC
endpoint、安装后端运行观察器，并把 endpoint 注册到 `.xiangshu-runtime/ipc-endpoints.json`。

后端侧脚本执行能力归这个模块：它承载后端进程可访问的游戏 API、数据状态和线程边界。

`Ipc/ItemGrafts/` 承接前端对当前寄身宿主的注册/注销请求，并更新后端 `ItemGrafts/HostRegistration`。

`ItemGrafts/` 负责后端可观察到的行囊和物品事实。它用 Harmony 观察太吾角色 `Character.SetInventory`
作为太吾行囊快照可读边界；宿主进出角色行囊则观察 `CharacterDomain.TransferInventoryItem`、`Character.AddInventoryItem`
和 `RemoveInventoryItem` 这些带 `ItemKey` 的角色行囊转移边界，并报告 from/to 角色行囊 id。宿主删除只以
`ItemDomain.RemoveItem` 和 `ForceRemoveItem` 为边界；角色行囊转移不会被解释为删除。
携带宿主语义的通知始终带宿主 key。后端只报告已经登记的宿主事实，相枢寄身状态和重新寄身判断归前端 `ItemGrafts/`。

启动时，后端从游戏 Mod 管理接口取得相枢 Mod 目录，并把 `Plugins/Backend` 作为本侧插件部署目录传给共享
脚本运行器。脚本编译和程序集解析规则归 `src/Scripting/`；后端负责本侧 endpoint 和后端 API 边界。
