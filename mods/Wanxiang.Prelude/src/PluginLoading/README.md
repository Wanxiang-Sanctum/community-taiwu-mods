# PluginLoading

万象引前后端共用的插件加载桥接项目。

这个项目只维护太吾插件加载策略：安装 `PluginHelper.LoadPlugin` 补丁，按入口 DLL 所在目录优先解析依赖，
在入口加载时注册本地依赖图，并为后续 `AssemblyResolve` 使用请求方所属目录继续解析。它不声明万象引提供
哪些运行时 DLL；运行时部署清单归前后端入口项目的 `Taiwu.Mod.props` 维护。

前端和后端入口项目引用本项目，并在组包时把 `Wanxiang.Prelude.PluginLoading.dll` 合并进各自入口 DLL。

需要访问 `TaiwuModdingLib` 的非 public API 时，在本项目文件里维护 `Publicize` 项；入口项目不再用反射绕过
访问限制。
