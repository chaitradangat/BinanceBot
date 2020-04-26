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

        //method retired
        /*static void ReadConfig(ref string symbol, ref decimal quantity, ref string ApiKey, ref string ApiSecret, ref decimal riskPercentage, ref decimal rewardPercentage, ref decimal leverge, ref int signalStrength, ref string timeframe, ref int candleCount, ref bool isLive, ref decimal decreaseOnNegative)
        {
            symbol = ConfigurationManager.AppSettings["SYMBOL"];

            quantity = decimal.Parse(ConfigurationManager.AppSettings["QUANTITY"]);

            ApiKey = ConfigurationManager.AppSettings["APIKEY"];

            ApiSecret = ConfigurationManager.AppSettings["APISECRET"];

            riskPercentage = decimal.Parse(ConfigurationManager.AppSettings["RISKPERCENTAGE"]);

            rewardPercentage = decimal.Parse(ConfigurationManager.AppSettings["REWARDPERCENTAGE"]);

            leverge = decimal.Parse(ConfigurationManager.AppSettings["LEVERAGE"]);

            signalStrength = int.Parse(ConfigurationManager.AppSettings["SIGNALSTRENGTH"]);

            timeframe = ConfigurationManager.AppSettings["TIMEFRAME"];

            candleCount = int.Parse(ConfigurationManager.AppSettings["CANDLECOUNT"]);

            isLive = bool.Parse(ConfigurationManager.AppSettings["ISLIVE"]);

            decreaseOnNegative = decimal.Parse(ConfigurationManager.AppSettings["DECREASEONNEGATIVE"]);
        }*/
    }
}