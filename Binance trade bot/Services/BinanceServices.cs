using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketStream;
using Binance.Net.Objects.Spot.SpotData;
using Binance.Net.Objects.Spot.WalletData;
using Binance_trade_bot.Entities;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public static decimal fees = 0.00075M; // 0.075%
        public static decimal startBalanceUSDT = 12;
        public static BinanceSocketClient BinanceSocketClient { get; set; }
        public static BinanceClient BinanceClient { get; set; }
        public static MySettings Settings { get; set; }
        private static readonly object _cacheLock = new object();
        public static bool Login()
        {
            LogServices.Info("Setup application");
            Settings = GetSettings();

            LogServices.Info("Start Login");
            SetupClient();
            LogServices.Success("Login succeeded");
            return true;
        }


        internal static void GetData()
        {
            var accountInfo = Services.BinanceServices.BinanceClient.General.GetAccountInfo().Data;
            if (!Settings.FirstPair.UpdatBalanceData(accountInfo))
            {
                LogServices.Error($"Update balance not found for {Settings.SecondPair.GetPairKey()}");
            }
            if (!Settings.SecondPair.UpdatBalanceData(accountInfo))
            {
                LogServices.Error($"Update balance not found for {Settings.SecondPair.GetPairKey()}");
            }
            if (!Settings.ComparationPair.UpdatBalanceData(accountInfo))
            {
                LogServices.Error($"Update balance not found for {Settings.SecondPair.GetPairKey()}");
            }

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

        private static JObject GetSecrets()
        {
            using (StreamReader r = new StreamReader("_config/secrets.json"))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<JObject>(json);
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

        private static void SetupClient()
        {
            BinanceSocketClient = new BinanceSocketClient();


            var secrets = GetSecrets();
            string apiKey = secrets["ApiKey"].ToString();
            string apiSecret = secrets["SecretKey"].ToString();
            BinanceClient = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials(apiKey, apiSecret)
            });
            var startResult = BinanceClient.Spot.UserStream.StartUserStream();

            if (!startResult.Success)
                throw new Exception($"Failed to start user stream: {startResult.Error}");


            BinanceSocketClient.Spot.SubscribeToUserDataUpdates(startResult.Data,
                orderUpdate =>
                { // Handle order update
                    //LogServices.Success(orderUpdate.ToString());
                },

                ocoUpdate =>
                { // Handle oco order update
                    //LogServices.Success(ocoUpdate.ToString());
                },

                positionUpdate =>
                { // Handle account position update
                    //LogServices.Success(positionUpdate.ToString());
                },

                balanceUpdate =>
                { // Handle balance update
                    //LogServices.Success(balanceUpdate.ToString());
                });
        }

        private static void ExecutePattern(Pair activePair, BinanceStreamBookPrice data)
        {
            lock (_cacheLock)
            {
                activePair.LastData = data;
                // checking if process is executed successfully by another thread
                if (Settings.LastDataIsNotNull())
                {
                    if (SellFirstPair())
                    {
                        //buy first pair, change, sell second pair
                        WebCallResult<BinancePlacedOrder> transferResult, secondResult;
                        LogServices.Info("Make first trade");
                        var result = BinanceClient.Spot.Order.PlaceOrder(Settings.FirstPair.GetPairKey(), OrderSide.Buy, OrderType.Limit, timeInForce: TimeInForce.FillOrKill, quantity: Math.Round(startBalanceUSDT / Settings.FirstPair.LastData.BestAskPrice, 0), price: Settings.FirstPair.LastData.BestAskPrice);
                        LogServices.Success($"End first trade  order id {result.Data.OrderId},{Settings.FirstPair.Second}:{result.Data.QuoteQuantityFilled}  filled {result.Data.QuantityFilled}");
                        if (result.Data != null && result.Data.QuoteQuantityFilled != 0)
                        {
                            LogServices.Info("Make transfer trade");
                            transferResult = BinanceClient.Spot.Order.PlaceOrder(Settings.ComparationPair.GetPairKey(), OrderSide.Sell, OrderType.Market,/* timeInForce: TimeInForce.FillOrKill,*/
                                quantity: result.Data.QuantityFilled
                               /* ,price: Settings.ComparationPair.LastData.BestBidPrice*/);
                            LogServices.Success($"End transfer trade  order id {transferResult.Data.OrderId},sell {transferResult.Data.QuantityFilled} filled {transferResult.Data.QuoteQuantityFilled}");
                            if (transferResult.Data.QuoteQuantityFilled != 0)
                            {
                                LogServices.Info("Make second trade");
                                secondResult = BinanceClient.Spot.Order.PlaceOrder(Settings.SecondPair.GetPairKey(), OrderSide.Sell, OrderType.Market, /*timeInForce: TimeInForce.FillOrKill,*/ quantity: Math.Round(transferResult.Data.QuoteQuantityFilled, 6, MidpointRounding.ToNegativeInfinity)/*, price: Settings.SecondPair.LastData.BestBidPrice*/);
                                LogServices.Success($"End second trade  order id {secondResult.Data.OrderId}, sell {secondResult.Data.QuantityFilled } filled {secondResult.Data.QuoteQuantityFilled}");
                            }
                        }

                        Settings.ClearData();
                    }
                    else if (SellSecondPair())
                    {
                        //buy second pair, exchange, sell first pair
                        WebCallResult<BinancePlacedOrder> transferResult, secondResult;
                        LogServices.Info("Make first trade");
                        var result = BinanceClient.Spot.Order.PlaceOrder(Settings.SecondPair.GetPairKey(), OrderSide.Buy, OrderType.Limit, timeInForce: TimeInForce.FillOrKill,
                            quantity: Math.Round(startBalanceUSDT / Settings.SecondPair.LastData.BestAskPrice, 6)
                            , price: Settings.SecondPair.LastData.BestAskPrice);

                        LogServices.Success($"End first trade  order id {result.Data.OrderId},{Settings.SecondPair.Second}:{result.Data.QuoteQuantityFilled}  filled {result.Data.QuantityFilled}");

                        //var orderStatus = BinanceClient.Spot.Order.GetOrder(Settings.FirstPair.GetPairKey(), orderId: result.Data.OrderId);
                        if (result.Data != null && result.Data.QuoteQuantityFilled != 0)
                        {
                            LogServices.Info("Make transfer trade");

                            transferResult = BinanceClient.Spot.Order.PlaceOrder(Settings.ComparationPair.GetPairKey(), OrderSide.Buy, OrderType.Market, /*timeInForce: TimeInForce.FillOrKill,*/ quantity: Math.Round(result.Data.QuantityFilled / Settings.ComparationPair.LastData.BestAskPrice, 0, MidpointRounding.ToNegativeInfinity)
                                /*,price: Settings.ComparationPair.LastData.BestAskPrice*/);

                            LogServices.Success($"End transfer trade  order id {transferResult.Data.OrderId},sell {transferResult.Data.QuoteQuantityFilled} filled {transferResult.Data.QuantityFilled}");
                            if (transferResult.Data.QuoteQuantityFilled != 0)
                            {
                                LogServices.Info("Make second trade");
                                secondResult = BinanceClient.Spot.Order.PlaceOrder(Settings.FirstPair.GetPairKey(), OrderSide.Sell, OrderType.Market, /*timeInForce: TimeInForce.FillOrKill,*/ quantity: transferResult.Data.QuantityFilled/*, price: Settings.FirstPair.LastData.BestBidPrice*/);
                                LogServices.Success($"End second trade  order id {secondResult.Data.OrderId}, sell {secondResult.Data.QuantityFilled } filled {secondResult.Data.QuoteQuantityFilled}");
                            }
                        }

                        Settings.ClearData();
                    }
                }
            }
        }

        private static bool SellFirstPair()
        {
            decimal usdValue = 100;
            decimal dogeValue = (usdValue / Settings.FirstPair.LastData.BestAskPrice) * (1 - fees);//buy
            decimal btcValue = (dogeValue * Settings.ComparationPair.LastData.BestBidPrice) * (1 - fees);//sell
            decimal finalUsdValue = (btcValue * Settings.SecondPair.LastData.BestBidPrice) * (1 - fees);//sell
            if (finalUsdValue > usdValue + 0.01m)
            {
                LogServices.Warrning("Try Sell first pair " + finalUsdValue);
                Console.Beep();

                return true;
            }
            else
                return false;
        }

        private static bool SellSecondPair()
        {
            decimal usdValue = 100;
            decimal btcValue = (usdValue / Settings.SecondPair.LastData.BestAskPrice) * (1 - fees); //buy
            decimal dogeValue = (btcValue / Settings.ComparationPair.LastData.BestAskPrice) * (1 - fees);//buy
            decimal finalUsdValue = (dogeValue * Settings.FirstPair.LastData.BestBidPrice) * (1 - fees);//sell
            if (finalUsdValue > usdValue + 0.01m)
            {
                LogServices.Warrning("Try Sell second pair " + finalUsdValue);
                Console.Beep();

                return true;
            }
            else
                return false;
        }


    }
}
