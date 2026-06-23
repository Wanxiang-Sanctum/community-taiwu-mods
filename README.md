# Community Taiwu Mods

[![Ask Zread](https://img.shields.io/badge/Ask_Zread-_.svg?style=flat&color=00b0aa&labelColor=000000&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTQuOTYxNTYgMS42MDAxSDIuMjQxNTZDMS44ODgxIDEuNjAwMSAxLjYwMTU2IDEuODg2NjQgMS42MDE1NiAyLjI0MDFWNC45NjAxQzEuNjAxNTYgNS4zMTM1NiAxLjg4ODEgNS42MDAxIDIuMjQxNTYgNS42MDAxSDQuOTYxNTZDNS4zMTUwMiA1LjYwMDEgNS42MDE1NiA1LjMxMzU2IDUuNjAxNTYgNC45NjAxVjIuMjQwMUM1LjYwMTU2IDEuODg2NjQgNS4zMTUwMiAxLjYwMDEgNC45NjE1NiAxLjYwMDFaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00Ljk2MTU2IDEwLjM5OTlIMi4yNDE1NkMxLjg4ODEgMTAuMzk5OSAxLjYwMTU2IDEwLjY4NjQgMS42MDE1NiAxMS4wMzk5VjEzLjc1OTlDMS42MDE1NiAxNC4xMTM0IDEuODg4MSAxNC4zOTk5IDIuMjQxNTYgMTQuMzk5OUg0Ljk2MTU2QzUuMzE1MDIgMTQuMzk5OSA1LjYwMTU2IDE0LjExMzQgNS42MDE1NiAxMy43NTk5VjExLjAzOTlDNS42MDE1NiAxMC42ODY0IDUuMzE1MDIgMTAuMzk5OSA0Ljk2MTU2IDEwLjM5OTlaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik0xMy43NTg0IDEuNjAwMUgxMS4wMzg0QzEwLjY4NSAxLjYwMDEgMTAuMzk4NCAxLjg4NjY0IDEwLjM5ODQgMi4yNDAxVjQuOTYwMUMxMC4zOTg0IDUuMzEzNTYgMTAuNjg1IDUuNjAwMSAxMS4wMzg0IDUuNjAwMUgxMy43NTg0QzE0LjExMTkgNS42MDAxIDE0LjM5ODQgNS4zMTM1NiAxNC4zOTg0IDQuOTYwMVYyLjI0MDFDMTQuMzk4NCAxLjg4NjY0IDE0LjExMTkgMS42MDAxIDEzLjc1ODQgMS42MDAxWiIgZmlsbD0iI2ZmZiIvPgo8cGF0aCBkPSJNNCAxMkwxMiA0TDQgMTJaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00IDEyTDEyIDQiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIxLjUiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPgo8L3N2Zz4K&logoColor=ffffff)](https://zread.ai/Wanxiang-Sanctum/community-taiwu-mods)

Community Taiwu Mods 是 Wanxiang-Sanctum 维护的太吾绘卷 mod 源码集合，面向愿意了解安装边界、运行要求和源码来源的
技术玩家。

## Mod 入口

本节是面向玩家的入口表，帮助判断先读哪个 Mod。

| Mod | 适合谁 | 继续阅读 |
| --- | --- | --- |
| 相枢 | 想在太吾绘卷内接入本机 CLI Agent，并理解受信脚本运行风险的玩家。当前仍处于实验阶段。 | [相枢说明](mods/Wanxiang.Xiangshu/README.md) |
| 万象引 | 订阅相枢或其它明确要求万象引的 Mod 时需要的前置依赖。它本身不提供单独玩法入口。 | [万象引说明](mods/Wanxiang.Prelude/README.md) |

安装或配置某个 Mod 前，先阅读对应 Mod 的 README。只作为前置依赖存在的 Mod，通常只在目标 Mod 或 Steam Workshop
依赖提示要求时订阅。

## 获取与安装

优先通过 Steam Workshop 订阅需要的 Mod。订阅时按目标 Mod 页面和 Steam 的依赖提示处理前置依赖。

GitHub Release 中的 zip 包用于手动安装或排查发布内容。zip 内包含可直接放入太吾本地 Mod 目录的同名目录；手动安装时
仍要自己确认前置依赖和版本匹配。

## 使用边界

具体功能、配置项、运行要求、存档影响和信任边界由各 Mod 的 `README.md` 说明。某个 Mod 如果接入本机进程、第三方工具、
脚本能力或额外运行时，请按该 Mod 的说明确认你信任对应工作目录、本机环境和依赖来源。

## 源码

这个仓库公开承载这些 Mod 的源码。一级 Mod 源码索引见 [`mods/`](mods/)，源码维护入口见
[开发维护文档](docs/development/README.md)；普通使用和配置优先阅读对应 Mod 的 README。

仓库骨架来自 [Taiwu.Mods](https://github.com/Wanxiang-Sanctum/taiwu-mods) 模板仓库。本仓库只保留这些实际 Mod
需要的本地模板、工具和发布配置适配；通用 monorepo 模板能力与从模板创建新仓库的说明由模板仓库维护。

## License

本仓库使用 [BSD 3-Clause License](LICENSE)。
