# Community Taiwu Mods

[![Ask Zread](https://img.shields.io/badge/Ask_Zread-_.svg?style=flat&color=00b0aa&labelColor=000000&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTQuOTYxNTYgMS42MDAxSDIuMjQxNTZDMS44ODgxIDEuNjAwMSAxLjYwMTU2IDEuODg2NjQgMS42MDE1NiAyLjI0MDFWNC45NjAxQzEuNjAxNTYgNS4zMTM1NiAxLjg4ODEgNS42MDAxIDIuMjQxNTYgNS42MDAxSDQuOTYxNTZDNS4zMTUwMiA1LjYwMDEgNS42MDE1NiA1LjMxMzU2IDUuNjAxNTYgNC45NjAxVjIuMjQwMUM1LjYwMTU2IDEuODg2NjQgNS4zMTUwMiAxLjYwMDEgNC45NjE1NiAxLjYwMDFaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00Ljk2MTU2IDEwLjM5OTlIMi4yNDE1NkMxLjg4ODEgMTAuMzk5OSAxLjYwMTU2IDEwLjY4NjQgMS42MDE1NiAxMS4wMzk5VjEzLjc1OTlDMS42MDE1NiAxNC4xMTM0IDEuODg4MSAxNC4zOTk5IDIuMjQxNTYgMTQuMzk5OUg0Ljk2MTU2QzUuMzE1MDIgMTQuMzk5OSA1LjYwMTU2IDE0LjExMzQgNS42MDE1NiAxMy43NTk5VjExLjAzOTlDNS42MDE1NiAxMC42ODY0IDUuMzE1MDIgMTAuMzk5OSA0Ljk2MTU2IDEwLjM5OTlaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik0xMy43NTg0IDEuNjAwMUgxMS4wMzg0QzEwLjY4NSAxLjYwMDEgMTAuMzk4NCAxLjg4NjY0IDEwLjM5ODQgMi4yNDAxVjQuOTYwMUMxMC4zOTg0IDUuMzEzNTYgMTAuNjg1IDUuNjAwMSAxMS4wMzg0IDUuNjAwMUgxMy43NTg0QzE0LjExMTkgNS42MDAxIDE0LjM5ODQgNS4zMTM1NiAxNC4zOTg0IDQuOTYwMVYyLjI0MDFDMTQuMzk4NCAxLjg4NjY0IDE0LjExMTkgMS42MDAxIDEzLjc1ODQgMS42MDAxWiIgZmlsbD0iI2ZmZiIvPgo8cGF0aCBkPSJNNCAxMkwxMiA0TDQgMTJaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00IDEyTDEyIDQiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIxLjUiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPgo8L3N2Zz4K&logoColor=ffffff)](https://zread.ai/Wanxiang-Sanctum/community-taiwu-mods)

Community Taiwu Mods 是 Wanxiang-Sanctum 维护的太吾绘卷 mod 集合，面向愿意了解安装边界、运行要求和源码来源的
技术玩家。

## 当前 Mod

| Mod | 适合谁 | 继续阅读 |
| --- | --- | --- |
| 相枢 | 想在太吾绘卷内接入本机 CLI Agent，并理解受信脚本运行风险的玩家。当前仍处于实验阶段。 | [相枢说明](mods/Wanxiang.Xiangshu/README.md) |
| 万象引 | 订阅相枢或其它明确要求万象引的 Mod 时需要的前置依赖。它本身不提供单独玩法入口。 | [万象引说明](mods/Wanxiang.Prelude/README.md) |

## 获取与安装

优先通过 Steam Workshop 订阅需要的 Mod。相枢会声明万象引作为前置依赖，订阅时按 Steam 的依赖提示处理即可。

GitHub Release 中的 zip 包用于手动安装或排查发布内容。zip 内包含可直接放入太吾本地 Mod 目录的同名目录；手动安装时仍要
自己确认前置依赖和版本匹配。

## 使用边界

相枢不是内置模型，也不会替玩家配置第三方 Agent、账号、模型或网络环境。它会把玩家的游戏内对话投递给本机 CLI Agent，
并允许受信 Agent 通过相枢工具链在游戏前端或后端插件进程中运行脚本。请只把它交给你信任的 Agent、工作目录和本机环境。

万象引是前置依赖 Mod。只有你订阅的其它 Mod 明确要求万象引，或 Steam Workshop 自动提示需要依赖时，才需要订阅它。

## 源码

这个仓库公开承载这些 Mod 的源码。维护者入口见
[开发维护文档](docs/development/README.md)；普通使用和配置优先阅读对应 Mod 的 README。

## License

本仓库使用 [Mulan PSL v2](LICENSE)。
