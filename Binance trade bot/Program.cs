using Binance_trade_bot.Services;
using System;

namespace Binance_trade_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            BinanceServices.Login();
            BinanceServices.GetData();

            LogServices.Warrning("Press enter to exit");
            Console.ReadLine();
        }
    }
}
