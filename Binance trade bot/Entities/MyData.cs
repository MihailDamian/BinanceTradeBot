using Binance.Net.Objects.Spot.MarketStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binance_trade_bot.Entities
{
    public class MyData
    {
      

        public void AddPairs(List<Pair> pairs)
        {
            foreach (var pair in pairs)
            {
                string key = String.Concat(pair.First.ToUpper(), pair.Second.ToUpper());
                if (!PairsData.ContainsKey(key))
                {
                    PairsData.Add(key, pair);
                }
            }
        }

        internal Pair GetPairWithoutCurrency(string currency)
        {
            return PairsData.Where(p => p.Value.First != currency && p.Value.Second != currency).FirstOrDefault().Value;
        }

        internal Pair GetComparationPair(Pair firstPair)
        {
            return PairsData.Where(p => (p.Value.First != firstPair.First || p.Value.Second != firstPair.First) && p.Value.Second != firstPair.Second).FirstOrDefault().Value;
        }

        
    }
}
