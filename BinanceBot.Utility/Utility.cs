using System;

using System.IO;

namespace BinanceBot.Utility
{
    public static class Utility
    {
        /// <summary>
        /// Function to enable logging and write initial schema
        /// </summary>
        public static void EnableLogging()
        {
            if (!File.Exists("debug.logs"))
            {
                File.AppendAllLines("debug.logs", new[] { "Date	Signal\tPrice\t%\tSignalHistory\tBU\tBM\tBL\tS0\tS1\tA1\tA2\tA3\tA4" });
            }
        }
    }
}
