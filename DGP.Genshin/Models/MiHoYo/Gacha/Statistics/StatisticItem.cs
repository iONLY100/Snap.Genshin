﻿using DGP.Snap.Framework.Attributes.DataModel;

namespace DGP.Genshin.Models.MiHoYo.Gacha.Statistics
{
    /// <summary>
    /// 带有个数统计的奖池统计物品
    /// </summary>
    [InterModel]
    public class StatisticItem
    {
        public string StarUrl { get; set; }
        public string Source { get; set; }
        public string Name { get; set; }
        public string Element { get; set; }
        public string WeaponIcon { get; set; }
        public int Count { get; set; }
    }
}
