using System;
using System.Collections.Generic;
using System.Text;

namespace TradeBotLibrary.Models
{
    public class MarketSaleItem
    {
        public string Market_Hash_Name;
        public int? Volume;
        public float? Price;
        public float? Buy_Order;
        public float? Avg_Price;
        public int? Popularity_7d;
        public string Ru_name;
        public string Ru_rarity;
        public string Ru_quality;
        public string Text_color;
        public string Bg_color;
    }
}
