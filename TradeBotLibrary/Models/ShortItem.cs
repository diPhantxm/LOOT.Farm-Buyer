using System;
using System.Collections.Generic;
using System.Text;

namespace TradeBotLibrary.Models
{
    [Serializable]
    public class ShortItem
    {
        public string Name;
        public float? Price;
        public string Id;
        public string ClassId;
        public string InstanceId;

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Price.GetHashCode();
        }
    }
}
