using System;

using BinanceBot.Application;

using BinanceBot.Settings;

using BinanceBot.Domain;

namespace BinanceBot.UI.Consol
{
    class Program
    {
        static void Main(string[] args)
        {
            #region -config variables-

            string ApiKey = BinanceBotSettings.settings.ApiKey;

            string ApiSecret = BinanceBotSettings.settings.ApiSecret;

            bool isLive = BinanceBotSettings.settings.IsLive;

            RobotInput strategyInput = new RobotInput
            {
                candleCount = BinanceBotSettings.settings.CandleCount,
                decreaseOnNegative = BinanceBotSettings.settings.DecreaseOnNegative,
                leverage = BinanceBotSettings.settings.Leverage,
                quantity = BinanceBotSettings.settings.Quantity,
                reward = BinanceBotSettings.settings.RewardPercentage,
                risk = BinanceBotSettings.settings.RiskPercentage,
                signalStrength = BinanceBotSettings.settings.SignalStrength,
                symbol = BinanceBotSettings.settings.Symbol,
                timeframe = BinanceBotSettings.settings.TimeFrame
            };

            #endregion

            BinanceBot.Common.Utility.EnableLogging();

            BinanceCommand bcmd = new BinanceCommand(ApiKey, ApiSecret);

            bcmd.StartRoBot(strategyInput, isLive);

            Console.ReadLine();
        }
    }
}