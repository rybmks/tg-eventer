using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bot.Interfaces
{
    public interface ILogger
    {
        public void Info(string mes);
        public void Error(string mes);
        public void Warning(string mes);
    }
}
