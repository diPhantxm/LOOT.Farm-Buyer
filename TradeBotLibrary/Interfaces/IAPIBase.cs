using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TradeBotLibrary.Models;

namespace TradeBotLibrary.Interfaces
{
    public interface IAPIBase
    {
        Task<IEnumerable<ShortItem>> GetAllItemsAverage();
        Task<Tuple<float, string>> GetBalance(string secretKey = "");
    }
}
