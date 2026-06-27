return {
  Title = "法不加身：一力破万法",
  Source = 1,
  FileId = 3752091543,
  Version = "0.1.0",
  GameVersion = "1.0.36",
  Author = "Wanxiang Sanctum",
  Description = [=[[h1]法不加身[/h1]
[h2]一力破万法[/h2]
万法纷至，皆不加身。

太吾行走江湖，难免遇上内劲暗伤、毒素缠身、惑心乱神、封穴破绽与诸般战斗异状。本 Mod 为太吾套上一层只在运行时生效的“法不加身”规则：这些阴损路数不再落到太吾身上，太吾也不会借它们反加于人；但普通外伤仍是普通外伤，刀剑拳脚照样见真章。

[h2]主要效果[/h2]
[list]
[*]太吾免受新增内伤、内伤进度、心神伤害和心神标记影响。
[*]太吾在战斗中视为内伤、心神、破绽、封穴和毒素免疫；新增毒素与新增战斗状态会被挡下。
[*]太吾造成的内伤、心神、新增毒素和战斗状态不会生效。
[*]涉及太吾的施展、入战类功法特殊效果会跳过；装备、突破被动、真气、功法配置和普通外伤等基础流程仍按原版计算。
[*]直接施加的致命伤、致命标记和标记转移不会作用于太吾或由太吾造成；普通外伤积重至深，仍可能转成致命伤。
[/list]

[h2]作用方式[/h2]
[list]
[*]规则只在运行时模拟，不向存档写入真实人物特性，也不保存额外人物数据；关闭或移除 Mod 后，不会留下额外特性。
[*]人物特性列表中的“法不加身”只是提示标记，且只显示在太吾身上。
[*]已经存在的伤势、毒素、破绽、封穴、致命伤进度或战斗状态，不会被主动清空。
[*]无需设置，启用后即对太吾生效。
[/list]

[h2]开源地址[/h2]
[url=https://github.com/Wanxiang-Sanctum/community-taiwu-mods]https://github.com/Wanxiang-Sanctum/community-taiwu-mods[/url]
]=],
  Cover = "Cover.png",
  WorkshopCover = "Cover.png",
  TagList = {
    "Modifications",
  },
  BackendPlugins = {
    "Wanxiang.Fabujiashen.Backend.dll",
  },
  FrontendPlugins = {
    "Wanxiang.Fabujiashen.Frontend.dll",
  },
}
