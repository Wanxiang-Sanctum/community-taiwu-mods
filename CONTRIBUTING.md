# 贡献入口

本文面向准备向本仓库提交 issue、讨论或 PR 的人，帮助贡献者把变更放到正确入口，并完成提交前检查。完整开发维护手册见
[`docs/development/README.md`](docs/development/README.md)。

安装或使用 Mod 的入口是根 [`README.md`](README.md) 与具体 Mod 的 `README.md`。

## 先判断变更类型

| 变更 | 先读 |
| --- | --- |
| 构建、打包、发布、新增或移除项目 | [`docs/development/README.md`](docs/development/README.md) |
| 文档结构、受众分层、模板文档同步 | [`docs/development/documentation.md`](docs/development/documentation.md) |
| 所有 Mod 共同的组包、插件入口、依赖部署规则 | [`mods/README.md`](mods/README.md) |
| 某个 Mod 的源码模块、发布内容或内部设计 | `mods/<ModName>/DEVELOPMENT.md` |
| 某个 Mod 的玩家说明、安装和运行边界 | `mods/<ModName>/README.md` |
| 内部共享项目 | [`shared/README.md`](shared/README.md) |
| 创建/移除命令实现、模板变量和渲染规则 | [`tools/README.md`](tools/README.md)、[`templates/README.md`](templates/README.md) |

## 提交前检查

- 提交文档变更时，按主要受众拆分玩家说明、贡献入口和维护手册。
- 修改 `PackageReference`、`Directory.Packages.props` 或新增项目后，运行 restore，并提交对应项目的
  `packages.lock.json`。
- 修改文档、配置或项目文件后，运行 `dotnet msbuild repo.proj -t:Check`。
- 修改 C# 源码后，按影响范围运行 `dotnet build Taiwu.Mods.slnx` 或对应项目构建。
- 修改组包入口、发布目录或插件依赖部署后，运行受影响 Mod 的 `pack-mod` 命令。

需要更细的命令、环境变量、发布 tag 和模板同步规则时，回到 [`docs/development/README.md`](docs/development/README.md)。
