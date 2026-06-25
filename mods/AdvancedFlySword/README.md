# 飞剑术自动连发

施展飞剑术时，熟练度达到一定值后将自动消耗式并连续释放多次，无需手动重复施放。

## 自动连发条件

| 熟练度 | 连发次数 |
| --- | --- |
| < 300 | 不触发 |
| >= 300 | 1 次 |
| >= 900 | 2 次 |
| >= 1800 | 3 次 |

每次自动连发消耗一个式。首次触发条件：熟练度 >= 300 且当前至少有一个可用式。
连发过程中如果式被用完，自动连发终止。

## 安装

从 Releases 下载 `AdvancedFlySword-<version>.zip`，解压后将 `AdvancedFlySword/` 目录放入游戏 `Mods/` 目录即可。

## 开源

[GitHub](https://github.com/Wanxiang-Sanctum/community-taiwu-mods)
