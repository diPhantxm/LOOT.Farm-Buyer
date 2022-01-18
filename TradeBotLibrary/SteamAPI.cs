using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TradeBotLibrary.Models;

namespace TradeBotLibrary
{
    public class SteamAPI : APIBase
    {
        private HttpClient api;

        public SteamAPI(string name) : base(name, Type.Steam, 5)
        {
        }

        public async Task<List<ShortItem>> GetItems()
        {
            var req = new Request(async () =>
            {
                var start = 0;
                var items = new List<ShortItem>();

                while (true)
                {
                    var url = $"https://steamcommunity.com/market/search/render/?search_descriptions=0&sort_column=name&sort_dir=desc&appid=730&norender=1&count=100&start={start * 100}";

                    using (var response = await api.GetAsync(url))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();

                            if (json == "null")
                            {
                                System.Threading.Thread.Sleep(60 * 1000);

                                continue;
                            }

                            var pattern = "\"name\".+?(?=},{\"name\")";
                            var matches = Regex.Matches(json, pattern);

                            if (matches.Count == 0) Items = items;

                            for (int i = 0; i < matches.Count; i++)
                            {
                                pattern = "([\"A-Za-z0-9_\\-\\s]+):([\"A-Za-z0-9_\\-\\s★|\\(\\)\\$\\.™]+)";
                                var fields = Regex.Matches(matches[i].Groups[0].Value, pattern);

                                var name = string.Empty;
                                var price = .0;
                                var classid = string.Empty;
                                var instanceid = string.Empty;

                                for (int k = 0; k < fields.Count; k++)
                                {
                                    if (fields[k].Groups[1].Value == "\"name\"") name = fields[k].Groups[2].Value.Replace("\"", "");
                                    if (fields[k].Groups[1].Value == "\"classid\"") classid = fields[k].Groups[2].Value.Replace("\"", "");
                                    if (fields[k].Groups[1].Value == "\"instanceid\"") instanceid = fields[k].Groups[2].Value.Replace("\"", "");
                                    if (fields[k].Groups[1].Value == "\"sell_price\"") price = float.Parse(fields[k].Groups[2].Value) / 100;
                                }

                                items.Add(new ShortItem
                                {
                                    Name = name,
                                    Price = (float?)price,
                                    ClassId = classid,
                                    InstanceId = instanceid
                                });
                            }
                        }
                        else if (response.StatusCode == (System.Net.HttpStatusCode)429)
                        {
                            System.Threading.Thread.Sleep(60 * 1000);
                        }
                    }

                    start++;
                    System.Threading.Thread.Sleep(7500);
                }
            });

            await Call(req);
            return Items;
        }

        public Task<double?> GetBalance()
        {
            throw new NotImplementedException();
        }

        public Task<List<ShortItem>> BuyItems()
        {
            throw new NotImplementedException();
        }
    }
}
