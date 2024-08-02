using bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bot
{
    public class ConsoleLogger : ILogger
    {
        public void Error(string mes)
        {
            Console.WriteLine($"ERROR: {mes}");
        }

        public void Info(string mes)
        {
            Console.WriteLine($"INFO: {mes}");
        }

        public void Warning(string mes)
        {
            Console.WriteLine($"WARN: {mes}");
        }
    }
}
