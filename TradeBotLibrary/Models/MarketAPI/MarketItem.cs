using System;
using System.Collections.Generic;
using System.Text;

namespace TradeBotLibrary.Models
{
    public class MarketItem
    {
        public string Id;
        public string ClassId;
        public string InstanceId;
        public string Market_Hash_Name;
        public float Market_Price;
        public bool Tradable;
    }
}
