# tools

仓库维护工具目录。

`tools/Taiwu.Mods.Cli/` 实现本仓库使用的命令行维护工具：创建 mod、创建内部共享项目、从解决方案取消注册项目，
以及组装可部署 mod 目录。

## 文档边界

本目录 README 说明工具代码在文档树中的位置，不复制每个命令的完整用法。常用命令入口由仓库根
`README.md` 维护；mod 组包 item、插件入口、依赖部署和发布目录项目的语义由 `mods/README.md` 维护；
模板变量、模板目录和渲染规则由 `templates/README.md` 维护。

工具实现不承担具体 mod 的例外规则。某个 mod 需要额外文件、目录、发布目录或依赖部署动作时，写在该 mod 的
`Taiwu.Mod.Pack.proj`、项目文件或项目旁 `Taiwu.Mod.props` 中。

## 维护入口

- `tools/Taiwu.Mods.Cli/CommandLineOptions.cs`：命令、参数和帮助文本入口。
- `tools/Taiwu.Mods.Cli/TemplateRenderer.cs`：模板变量和严格渲染规则。
- `tools/Taiwu.Mods.Cli/TemplateDirectory.cs`：模板目录复制和路径渲染。
- `tools/Taiwu.Mods.Cli/ModPacker.cs`：调用 MSBuild 组包目标并组装可部署目录。

新增或调整命令时，同步更新 `CommandLineOptions.cs`、仓库根 `README.md` 中的常用命令说明，以及受影响目录自己的
README。只有模板上下文变量或模板选择规则变化时，才更新 `templates/README.md`。
