using Binance.Net;
using Binance.Net.Objects.Spot.MarketStream;
using Binance_trade_bot.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binance_trade_bot.Services
{
    public static class BinanceServices
    {
        public static BinanceSocketClient BinanceSocketClient { get; set; }
        public static MySettings Settings { get; set; }
        private static readonly object _cacheLock = new object();
        public static bool Login()
        {
            LogServices.Info("Setup application");
            Settings = GetSettings();

            LogServices.Info("Start Login");
            BinanceSocketClient = new BinanceSocketClient();
            LogServices.Success("Login succeeded");
            return true;
        }


        internal static void GetData()
        {
            GetData(Settings.FirstPair.GetPairKey());
            GetData(Settings.SecondPair.GetPairKey());
            GetData(Settings.ComparationPair.GetPairKey());
        }
        public static void GetData(string key)
        {
            BinanceSocketClient.Spot.SubscribeToBookTickerUpdates(key, data =>
             {
                 UpdatePairsINFO(data);
             });
        }

        private static MySettings GetSettings()
        {
            using (StreamReader r = new StreamReader("_config/settings.json"))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<MySettings>(json);
            }
        }

        private static void UpdatePairsINFO(BinanceStreamBookPrice data)
        {
            try
            {
                bool executePattern = false;
                Pair activePair = null;
                if (data.Symbol == Settings.FirstPair.GetPairKey())
                {
                    activePair = Settings.FirstPair;
                }
                else if (data.Symbol == Settings.SecondPair.GetPairKey())
                {
                    activePair = Settings.SecondPair;
                }
                else if (data.Symbol == Settings.ComparationPair.GetPairKey())
                {
                    activePair = Settings.ComparationPair;
                }
                ExecutePattern(activePair, data);
            }
            catch (Exception ex)
            {
                LogServices.Error(ex.ToString());
            }
        }

        private static void ExecutePattern(Pair activePair, BinanceStreamBookPrice data)
        {
            lock (_cacheLock)
            {
                decimal fees = 1.0007m;
                activePair.LastData = data;
                // checking if process is executed successfully by another thread
                if (Settings.LastDataIsNotNull())
                {
                    decimal value = Settings.FirstPair.LastData.BestAskPrice / Settings.SecondPair.LastData.BestBidPrice;
                    decimal valueRound = Math.Round(value, 8, MidpointRounding.ToNegativeInfinity);// Math.Round(value, Settings.MathRoundPairValue, MidpointRounding.ToNegativeInfinity);
                    if (valueRound > Settings.ComparationPair.LastData.BestAskPrice * fees)
                    {
                        //buy first pair, change, sell second pair


                        Settings.ClearData();
                    }
                    else
                    {
                        value = Settings.FirstPair.LastData.BestBidPrice / Settings.SecondPair.LastData.BestAskPrice;
                        valueRound = Math.Round(value, 8, MidpointRounding.ToNegativeInfinity);//Math.Round(value, Settings.MathRoundPairValue, MidpointRounding.ToNegativeInfinity);
                        if (valueRound > Settings.ComparationPair.LastData.BestBidPrice * fees)
                        {
                            //buy second pair, exchange, sell first pair


                            Settings.ClearData();
                        }
                    }
                }
            }
        }
        // 1

    }
}
