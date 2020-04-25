using System;

using System.Configuration;

using BinanceBot.Application;



namespace BinanceBot.UI.Consol
{
    class Program
    {
        static void Main(string[] args)
        {
            #region -config variables-
            string symbol = "";

            decimal quantity = 0;

            string ApiKey = "";

            string ApiSecret = "";

            decimal riskPercentage = 0;

            decimal rewardPercentage = 0;

            decimal leverage = 0;

            int signalStrength = 0;

            string timeframe = "";

            int candleCount = 15;

            bool isLive = true;

            decimal decreaseOnNegative = (decimal)0.5;
            #endregion

            ReadConfig(ref symbol, ref quantity, ref ApiKey, ref ApiSecret, ref riskPercentage, ref rewardPercentage, ref leverage, ref signalStrength, ref timeframe, ref candleCount, ref isLive, ref decreaseOnNegative);

            BinanceCommand bcmd = new BinanceCommand();

            bcmd.ConnectFuturesBot(symbol, quantity, ApiKey, ApiSecret, riskPercentage, rewardPercentage, leverage, signalStrength, timeframe, candleCount, isLive, decreaseOnNegative);

            Console.ReadLine();
        }

        static void ReadConfig(ref string symbol, ref decimal quantity, ref string ApiKey, ref string ApiSecret, ref decimal riskPercentage, ref decimal rewardPercentage, ref decimal leverge, ref int signalStrength, ref string timeframe, ref int candleCount, ref bool isLive, ref decimal decreaseOnNegative)
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
        }
    }
}