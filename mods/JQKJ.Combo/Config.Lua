return {
	Title = "界青快剑-连击版",
	Source = 0,
	FileId = 3452874337,
	Version = "2.0.0.4",
	GameVersion = "0.0.79.41-test",
	Author = "Duma",
	Description = "1.无限连击  20%保底几率\n2.初始几率 60%  每次连击-15%\n3.将福缘与初始几率关联，  初始几率= 60% + 福缘/2  \n4.连击时，释放进度，随连击数增长，每多一连，增加10%，最高至99%\v",
	TagList = {
		[1] = "Compatible Mods",
		[2] = "Modifications",
	},
	Visibility = 0,
	DefaultSettings = { },
	UpdateLogList = {
		[1] = {
			Timestamp = 1743066400,
		},
		[2] = {
			Timestamp = 1745074902,
			LogList = {
				[1] = "重构了代码，关联了福缘，增加了连击释放进度奖励",
			},
		},
		[3] = {
			Timestamp = 1746018436,
			LogList = {
				[1] = "修复了杀式没有正确获取的BUG",
			},
		},
		[4] = {
			Timestamp = 1746095863,
			LogList = {
				[1] = "修复了进入新战斗时释放进度没有初始化的bug",
			},
		},
		[5] = {
			Timestamp = 1747403662,
			LogList = {
				[1] = "修复了bug",
			},
		},
		[6] = {
			Timestamp = 1758710078,
		},
	},
	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = true,
	BackendPlugins = {
		[1] = "Backend/JQKJ.Combo.Backend.dll",
	},
	Cover = "快剑.png",
	WorkshopCover = "快剑.png",
}
