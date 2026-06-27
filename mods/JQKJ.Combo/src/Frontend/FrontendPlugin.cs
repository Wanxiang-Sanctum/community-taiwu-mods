using Config;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;

namespace JQKJ.Combo.Frontend
{
    [PluginConfig("JQKJ.Combo.Frontend", "duma", "2.0.0.4")]
    public sealed class FrontendPlugin : TaiwuRemakePlugin
    {
        private const short DirectEffectId = 559;
        private const short ReverseEffectId = 1285;

        private const string DirectDesc =
            "正练：施展后自身获得1杀式，同时几率触发连击（初始60%，受福缘影响；每次连击后-15%，最低维持20%）。" +
            "连击释放进度逐次提升：首次50%，每次+10%，最高99%。";

        private const string ReverseDesc =
            "逆练：施展后敌人获得1杀式，同时几率触发连击（初始60%，受福缘影响；每次连击后-15%，最低维持20%）。" +
            "连击释放进度逐次提升：首次50%，每次+10%，最高99%。";

        public override void Initialize()
        {
            PatchDescriptions();
        }

        private static void PatchDescriptions()
        {
            try
            {
                var instance = SpecialEffect.Instance;
                if (instance == null)
                {
                    Debug.LogError("[JQKJ] SpecialEffect.Instance 为空");
                    return;
                }

                foreach (var item in instance)
                {
                    if (item.Desc == null || item.Desc.Length == 0)
                        continue;

                    if (item.TemplateId == DirectEffectId)
                    {
                        item.Desc[0] = DirectDesc;
                    }
                    else if (item.TemplateId == ReverseEffectId)
                    {
                        item.Desc[0] = ReverseDesc;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[JQKJ] PatchDescriptions 异常: " + ex);
            }
        }

        public override void Dispose()
        {
        }
    }
}
