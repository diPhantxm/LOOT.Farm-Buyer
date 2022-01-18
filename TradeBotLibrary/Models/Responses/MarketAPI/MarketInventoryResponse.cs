using System;
using System.Collections.Generic;
using System.Text;

namespace TradeBotLibrary.Models.Responses
{
    public class MarketInventoryResponse
    {
        public bool Success;
        public List<MarketItem> Items;
    }
}
