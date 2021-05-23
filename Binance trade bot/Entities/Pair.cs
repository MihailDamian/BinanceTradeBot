using Binance.Net.Objects.Spot.MarketStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binance_trade_bot.Entities
{
    public class Pair
    {
        public string First { get; set; }
        public string Second { get; set; }
        public BinanceStreamBookPrice LastData { get; set; }

        public string GetPairKey()
        {
            return string.Concat(First, Second);
        }
    }
}
