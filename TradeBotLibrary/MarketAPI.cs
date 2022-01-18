using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using TradeBotLibrary.Interfaces;
using TradeBotLibrary.Models;
using TradeBotLibrary.Models.Responses;

namespace TradeBotLibrary
{
    public class MarketAPI : APIBase, IMarket
    {
        private bool PutOnSaleSuccess;
        private bool BuyItemSuccess;
        private bool UpdateInventorySuccess;
        private List<ShortItem> GetInventoryItems;
        private bool InsertOrderSuccess;
        private RemoveFromSaleResponse RemoveFromSaleResponse;

        public MarketAPI(string name, int reqsInSec = 5) : base(name, Type.Market, reqsInSec)
        {
            api.Timeout = TimeSpan.FromMinutes(60);
        }

        // Interface
        public async Task<IEnumerable<ShortItem>> GetAllItemsAverage()
        {
            var req = new Request(async () =>
            {
                var url = $"https://market.csgo.com/api/v2/prices/class_instance/USD.json";

                using (var response = await api.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        json = Regex.Replace(json, "\"\\d+_\\d+\":", "").Replace("\"items\":{", "\"items\":[").Replace("}}}", "}]}");
                        var deserializedObject = JsonConvert.DeserializeObject<MarketPricesResponse>(json);

                        Items = deserializedObject.Items.GroupBy(x => x.Market_Hash_Name)
                            .Select(g => new ShortItem()
                            {
                                Name = g.Key,
                                Price = g.OrderByDescending(x => x.Popularity_7d)
                                    .First()
                                    .Price
                            })
                            .ToList();

                    }
                    else
                    {
                        throw new HttpRequestException("Error occured while processing api request");
                    }
                }
            });

            await Call(req);
            return Items;
        }

        public async Task<IEnumerable<ShortItem>> GetAllItemsOnSale()
        {
            var req = new Request(async () =>
            {
                var url = $"https://market.csgo.com/api/v2/prices/class_instance/USD.json";

                using (var response = await api.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        json = Regex.Replace(json, "\"\\d+_\\d+\":", "").Replace("\"items\":{", "\"items\":[").Replace("}}}", "}]}");
                        var deserializedObject = JsonConvert.DeserializeObject<MarketPricesResponse>(json);

                        Items = deserializedObject.Items
                            .Select(x => new ShortItem()
                            {
                                Name = x.Market_Hash_Name,
                                Price = x.Price
                            })
                            .ToList();
                    }
                    else
                    {
                        throw new HttpRequestException("Error occured while processing api request");
                    }
                }
            });

            await Call(req);
            return Items;
        }

        public async Task<IEnumerable<ShortItem>> GetInventory(string secretKey)
        {
            var req = new Request(async () =>
            {
                GetInventoryItems = new List<ShortItem>();

                //await UpdateInventory(secretKey);

                var url = $"https://market.csgo.com/api/v2/my-inventory/?key={ secretKey }";

                using (var response = await api.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var deserializedResponse = JsonConvert.DeserializeObject<MarketInventoryResponse>(json);

                        var items = deserializedResponse.Items.Select(x => new ShortItem
                        {
                            Id = x.Id,
                            ClassId = x.ClassId,
                            InstanceId = x.InstanceId,
                            Price = x.Market_Price,
                            Name = x.Market_Hash_Name
                        });

                        GetInventoryItems = items.ToList();
                    }
                    else
                    {
                        GetInventoryItems = null;
                    }
                }
            });

            await Call(req);
            return GetInventoryItems;
        }

        public async Task<bool> PutOnSale(string secretKey, ShortItem item)
        {
            var req = new Request(async () =>
            {
                var id = item.Id;
                var price = item.Price;

                var url = $"https://market.csgo.com/api/v2/add-to-sale?key={ secretKey }&id={ id }&price={ price.ToString() }&cur=USD";

                using (var response = await api.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var deserializedResponse = JsonConvert.DeserializeObject<PutOnSaleResponse>(json);

                        PutOnSaleSuccess = Boolean.Parse(deserializedResponse.Success);
                        return;
                    }
                    else
                    {
                        PutOnSaleSuccess = false;
                        return;
                    }
                }
                
            });

            await Call(req);

            return PutOnSaleSuccess;
        }

        public async Task<IEnumerable<string>> InstaSell(string key)
        {
            var req = new Request(async () =>
            {
                var invItems = (await GetInventory(key)).ToList();
                var prices = await GetAllItemsOnSale();

                var invPrice = .0;

                for (int i = 0; i < invItems.Count / RequestsInSec; i++)
                {
                    for (int j = 0; j < RequestsInSec; j++)
                    {
                        var index = i * RequestsInSec + j;

                        var price = prices.Where(x => x.Name == invItems[index].Name).Select(x => x.Price).Min() * 1000 - 0.001;
                        if (price == null) continue;

                        invItems[index].Price = (float?)price;
                        await PutOnSale(key, invItems[index]);

                        invPrice += (double)price / 1000;

                        //yield return String.Format("{0} has been placed for ${1}", invItems[i * RequestsInSec + j].Name, price / 1000);
                    }

                    Thread.Sleep(1000);
                }

                //yield return String.Format($"Placed items cost ${ invPrice }");
            });

            await Call(req);

            return new List<string>();
        }

        public async Task<Tuple<float, string>> GetBalance(string key)
        {
            var req = new Request(async () =>
            {
                var url = $"https://market.csgo.com/api/v2/get-money?key={ key }";

                using (var response = await api.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var deserializedResponse = JsonConvert.DeserializeObject<GetMoneyResponse>(json);

                        Balance = deserializedResponse.money;
                    }
                    else
                    {
                        Balance = null;
                    }
                }
            });

            await Call(req);

            return new Tuple<float, string>((float)Balance, "USD");
        }

        public async Task<bool> BuyItem(ShortItem item, string key)
        {
            var req = new Request(async () =>
            {
                var url = $"https://market.csgo.com/api/v2/buy?key={key}&hash_name={item.Name}&price={item.Price * 1000}";

                using (var response = await api.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var deserializedResponse = JsonConvert.DeserializeObject<BuyItemResponse>(json);

                        BuyItemSuccess = bool.Parse(deserializedResponse.Success);
                        return;
                    }
                    else
                    {
                        BuyItemSuccess = false;
                        return;
                    }
                }
            });

            await Call(req);
            return BuyItemSuccess;
        }



        // Additional functions
        public async Task Sell(string key, List<ShortItem> prices)
        {
            var req = new Request(async () =>
            {
                var invItems = (await GetInventory(key)).ToList();

                var invPrice = .0;

                for (int i = 0; i < invItems.Count() / RequestsInSec; i++)
                {
                    for (int j = 0; j < RequestsInSec; j++)
                    {
                        var price = .0f;

                        var priceMatch = prices.Find(x => x.Name == invItems[i * RequestsInSec + j].Name);
                        if (priceMatch == null) continue;

                        price = (float)priceMatch.Price * 1000;
                        invPrice += price / 1000;
                        invItems[i * RequestsInSec + j].Price = price;
                        await PutOnSale(key, invItems[i * RequestsInSec + j]);
                    }
                }
            });

            await Call(req);
        }

        public async Task<bool> InsertOrder(string key, string classid, string instanceid, float price)
        {
            var req = new Request(async () =>
            {
                var url = $"https://market.csgo.com/api/InsertOrder/{classid}/{instanceid}/{price.ToString()}//?key={key}";

                using (var response = await api.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();

                        var pattern = "(\"success\"):\\s(\\w+)";
                        var match = Regex.Match(json, pattern);

                        if (match.Groups[2].Value == "true") InsertOrderSuccess = true;
                        else InsertOrderSuccess = false;
                    }
                    else
                    {
                        throw new HttpRequestException("Error occured while processing api request");
                    }
                }
            });

            await Call(req);
            return InsertOrderSuccess;
        }

        public async Task<RemoveFromSaleResponse> RemoveAllFromSale(string secretKey)
        {
            var req = new Request(async () =>
            {
                var url = $"https://market.csgo.com/api/v2/remove-all-from-sale?key={secretKey}";
                using (var response = await api.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        RemoveFromSaleResponse = JsonConvert.DeserializeObject<RemoveFromSaleResponse>(json);
                    }
                    else
                    {
                        RemoveFromSaleResponse = null;
                    }
                }
            });

            await Call(req);
            return RemoveFromSaleResponse;
        }

        public async Task<bool> UpdateInventory(string key)
        {
            var req = new Request(async () =>
            {
                UpdateInventorySuccess = false;

                var url = $"https://market.csgo.com/api/UpdateInventory/?key={key}";

                using (var response = await api.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        UpdateInventorySuccess = true;
                    }
                }
            });

            await Call(req);
            return UpdateInventorySuccess;
        }
    }
}
