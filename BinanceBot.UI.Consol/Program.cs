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

            BinanceCommand bcmd = new BinanceCommand(ApiKey, ApiSecret);

            bcmd.StartRoBot(strategyInput, isLive);

            Console.ReadLine();
        }
    }
}