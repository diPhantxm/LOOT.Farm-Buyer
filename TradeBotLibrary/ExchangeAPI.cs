using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TradeBotLibrary.Interfaces;
using TradeBotLibrary.Models;

namespace TradeBotLibrary
{
    public class ExchangeAPI : APIBase, IExchanger
    {
        /// <summary>
        /// Constructor for Exchange service API
        /// </summary>
        /// <param name="reqsInSec">Amount of requests to API per second</param>
        public ExchangeAPI(string name, int reqsInSec = 5) : base(name, Type.Exchange, reqsInSec)
        {
            
        }




        public async Task<Tuple<float, string>> GetBalance(string secretKey = "")
        {
            var req = new Request(async () =>
            {
                var uri = @"https://loot.farm/en";

                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(uri);
                    var response = (HttpWebResponse)request.GetResponse();

                    var stream = response.GetResponseStream();
                    var buffer = new byte[16384];
                    var count = 0;
                    var ret = new StringBuilder();

                    do
                    {
                        count = stream.Read(buffer, 0, buffer.Length);
                        if (count != 0)
                        {
                            ret.Append(Encoding.Default.GetString(buffer, 0, count));
                        }
                    }
                    while (count > 0);

                    var html = new HtmlDocument();
                    html.LoadHtml(ret.ToString());

                    var divBlanace = html.GetElementbyId("myBalance");
                    var balance = divBlanace.InnerText;
                    Balance = Convert.ToDouble(balance);
                }
                catch (Exception)
                {
                    Balance = null;
                }

            });

            await Call(req); 

            if (Balance == null) return new Tuple<float, string>(.0f, "USD");
            return new Tuple<float, string>((float)Balance, "USD");
        }

        public async Task<IEnumerable<ShortItem>> GetAllItemsAverage()
        {
            var req = new Request(async () =>
            {
                var url = $"https://loot.farm/fullprice.json";

                using (var response = api.GetAsync(url).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        json = Regex.Replace(json, "\"\\d+_\\d+\":", "").Replace("\"items\":{", "\"items\":[").Replace("}}}", "}]}");
                        var deserializedObject = JsonConvert.DeserializeObject<ExchangeItem[]>(json);

                        Items = deserializedObject
                            .Where(x => x.Have > 0)
                            .Select(g => new ShortItem()
                            {
                                Name = g.Name,
                                Price = g.Price / 100
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
    }
}
