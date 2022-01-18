using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TradeBotLibrary.Models
{
    public class Request
    {
        public Func<Task> Task { get; private set; }
        public bool Done { get; set; }

        public Request(Func<Task> task)
        {
            Task = task;
        }
    }
}
