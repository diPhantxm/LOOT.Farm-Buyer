using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TradeBotLibrary.Models;

namespace TradeBotLibrary
{
    public abstract class APIBase
    {
        public Type Type { get; private set; }
        public string Name { get; private set; }
        public bool Active;
        public List<ShortItem> Items { get; protected set; }
        public double? Balance { get; protected set; }

        protected HttpClient api;
        protected int RequestsInSec;
        protected BlockingCollection<Request> Line;

        public APIBase(string name, Type type, int reqsInSec = 5)
        {
            Name = name;
            Type = type;
            RequestsInSec = reqsInSec;

            api = new HttpClient();
            api.DefaultRequestHeaders.Accept.Clear();
            api.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Active = true;
            Items = new List<ShortItem>();
            Line = new BlockingCollection<Request>();
        }

        /// <summary>
        /// Starts API
        /// </summary>
        /// <returns>Task</returns>
        public async void Start()
        {
            var i = 0;

            while (true)
            {
                if (Line.Count != 0)
                {
                    await Line.ElementAt(0).Task.Invoke();
                    Line.ElementAt(0).Done = true;

                    var doneTask = Line.ElementAt(0);
                    Line.TryTake(out doneTask);
                    i++;

                    if (i % RequestsInSec == 0)
                    {
                        await Task.Delay(1000);
                        i = 0;
                    }
                }

                await Task.Delay(20);
            }
        }

        /// <summary>
        /// Calls request to API
        /// </summary>
        /// <param name="req">Request that should be called</param>
        public async Task Call(Request req)
        {
            await Task.Run(() =>
            {
                Line.Add(req);
                while (!req.Done) Task.Delay(10);
            });
        }
    }

    public enum Type
    {
        Exchange,
        Market,
        Steam
    }
}
