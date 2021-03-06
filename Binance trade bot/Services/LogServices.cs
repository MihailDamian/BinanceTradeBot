using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binance_trade_bot.Services
{
    public static class LogServices
    {
        private static void ResetConsoleSettings()
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void Warrning(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"{DateTime.Now.TimeOfDay} {message}");
            ResetConsoleSettings();
        }

        public static void Info(string message)
        {
            Console.WriteLine($"{DateTime.Now.TimeOfDay} {message}");
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"{DateTime.Now.TimeOfDay} {message}");
            ResetConsoleSettings();
        }

        public static void Success(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{DateTime.Now.TimeOfDay} {message}");
            ResetConsoleSettings();
        }
    }
}
