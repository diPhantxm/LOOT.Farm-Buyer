using System;
using System.Collections.Generic;
using System.Text;

namespace TradeBotLibrary.Models.Responses
{
    public class MarketPricesResponse
    {
        public bool Success;
        public int Ticks;
        public string Currency;
        public MarketSaleItem[] Items;

        //public bool Success { get; set; }
        //public int Time { get; set; }
        //public string Currency { get; set; }
        //public List<MarketShortItem> Items;
    }
}
