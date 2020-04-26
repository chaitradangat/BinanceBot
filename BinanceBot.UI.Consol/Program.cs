using System;

using System.Configuration;

using BinanceBot.Application;

using BinanceBot.Settings;

namespace BinanceBot.UI.Consol
{
    class Program
    {
        static void Main(string[] args)
        {
            #region -config variables-
            string symbol = BinanceBotSettings.settings.Symbol;

            decimal quantity = BinanceBotSettings.settings.Quantity;

            string ApiKey = BinanceBotSettings.settings.ApiKey;

            string ApiSecret = BinanceBotSettings.settings.ApiSecret;

            decimal riskPercentage = BinanceBotSettings.settings.RiskPercentage;

            decimal rewardPercentage = BinanceBotSettings.settings.RewardPercentage;

            decimal decreaseOnNegative = BinanceBotSettings.settings.DecreaseOnNegative;

            decimal leverage = BinanceBotSettings.settings.Leverage;

            int signalStrength = BinanceBotSettings.settings.SignalStrength;

            string timeframe = BinanceBotSettings.settings.TimeFrame;

            int candleCount = BinanceBotSettings.settings.CandleCount;

            bool isLive = BinanceBotSettings.settings.IsLive;
            #endregion

            BinanceCommand bcmd = new BinanceCommand();

            bcmd.ConnectFuturesBot(symbol, quantity, ApiKey, ApiSecret, riskPercentage, rewardPercentage, leverage, signalStrength, timeframe, candleCount, isLive, decreaseOnNegative);

            Console.ReadLine();
        }
    }
}