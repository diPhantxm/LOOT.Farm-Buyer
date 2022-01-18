using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TradeBotLibrary.Models;

namespace TradeBotLibrary.Interfaces
{
    public interface IMarket : IAPIBase
    {
        Task<IEnumerable<ShortItem>> GetAllItemsOnSale();
        Task<IEnumerable<ShortItem>> GetInventory(string secretKey);
        Task<bool> PutOnSale(string secreyKey, ShortItem item);
        Task<IEnumerable<string>> InstaSell(string secreyKey);
    }
}
