using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binance_trade_bot.Entities
{
    public class MySettings
    {
        public int MathRoundPairValue { get; set; }
        public Pair FirstPair { get; set; }
        public Pair SecondPair { get; set; }
        public Pair ComparationPair { get; set; }

        internal bool LastDataIsNotNull()
        {
            return FirstPair.LastData != null && SecondPair.LastData != null && ComparationPair.LastData != null;
        }

        internal void ClearData()
        {
            FirstPair.LastData = SecondPair.LastData = ComparationPair.LastData = null;
        }
    }
}
