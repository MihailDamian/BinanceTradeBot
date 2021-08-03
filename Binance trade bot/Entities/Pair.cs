using Binance.Net.Objects.Spot.MarketStream;
using Binance.Net.Objects.Spot.SpotData;
using Binance.Net.Objects.Spot.WalletData;
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

        public BinanceBalance FirstBalance { get; set; }
        public BinanceBalance SecondBalance { get; set; }

        public decimal FirstMaxAmmountForTrade { get; set; }
        public decimal SecondMaxAmmountForTrade { get; set; }
        public string GetPairKey()
        {
            return string.Concat(First, Second);
        }

        public bool UpdatBalanceData(BinanceAccountInfo accountInfo = null)
        {
            if (accountInfo == null)
                accountInfo = Services.BinanceServices.BinanceClient.General.GetAccountInfo().Data;
            FirstBalance = GetDivident(accountInfo.Balances, First);
            SecondBalance = GetDivident(accountInfo.Balances, Second);

            return FirstBalance != null && SecondBalance != null;
        }

        private BinanceBalance GetDivident(IEnumerable<BinanceBalance> records, string key)
        {
            var result = records.Where(r => r.Asset == key).ToList();
            if (result.Count() > 0)
            {
                return result.First();
            }
            else
            {
                return null;
            }
        }
    }
}
