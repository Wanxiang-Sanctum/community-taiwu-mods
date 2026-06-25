return {
  Title = "飞剑术自动连发",
  Source = 1,
  FileId = 0,
  Version = "1.0.20.0",
  GameVersion = "1.0.24",
  Author = "Duma",
  Description = [=[飞剑术熟练度达到一定值后，施展飞剑术将自动消耗式并连续释放多次，无需手动重复施放。
熟练度 >= 300：连发 1 次
熟练度 >= 900：连发 2 次
熟练度 >= 1800：连发 3 次
]=],
  BackendPlugins = {
    "Backend/AdvancedFlySword.Backend.dll",
  },
}
