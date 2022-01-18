using System;
using System.Collections.Generic;
using System.Text;

namespace TradeBotLibrary.Models
{
    public class ShortItemComparer : IEqualityComparer<ShortItem>
    {
        public bool Equals(ShortItem x, ShortItem y)
        {
            if (x.Name == y.Name && x.Price == y.Price) return true;
            return false;
        }

        public int GetHashCode(ShortItem obj)
        {
            return obj.GetHashCode();
        }
    }
}
