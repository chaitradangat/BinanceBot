using System;

using System.IO;

using BinanceBot.Domain;

using BinanceBot.Settings;

namespace BinanceBot.Utility
{
    public static class Utility
    {
        //log paths
        static private string PrimaryLogPath;

        static private string SecondaryLogPath;

        static Utility()
        {
            PrimaryLogPath = BinanceBotSettings.settings.PrimaryLogPath;

            SecondaryLogPath = BinanceBotSettings.settings.SecondaryLogPath;
        }


        /// <summary>
        /// Function to enable logging and write initial schema
        /// </summary>
        public static void EnableLogging()
        {
            if (!File.Exists(PrimaryLogPath))
            {
                File.AppendAllLines(PrimaryLogPath, new[] { "Date\tSignal\tSignalType\tPrice\t%\tSignalHistory\tBU\tBM\tBL\tS0\tS1\tA1\tA2\tA3\tA4" });
            }
            if (!File.Exists(SecondaryLogPath))
            {
                File.AppendAllLines(SecondaryLogPath, new[] { "Date\tSignal\tSignalType\tPrice\t%\tSignalHistory\tBU\tBM\tBL\tS0\tS1\tA1\tA2\tA3\tA4" });
            }
        }

        public static void DumpToLog(RobotInput robotInput, StrategyData strategyData)
        {
            string timeutc530 = DateTime.Now.ToUniversalTime().AddMinutes(330).ToString();

            decimal percentage;

            var skipReasons = "";

            if (strategyData.DecisionType == StrategyDecision.Buy)
            {
                percentage = Math.Round(strategyData.shortPercentage, 3);
            }
            else if (strategyData.DecisionType == StrategyDecision.Sell)
            {
                percentage = Math.Round(strategyData.longPercentage, 3);
            }
            else
            {
                percentage = 0;
            }

            if (strategyData.AvoidReasons != null && strategyData.AvoidReasons.Count > 0)
            {
                foreach (var AvoidReason in strategyData.AvoidReasons)
                {
                    skipReasons += AvoidReason.ToString() + "\t";
                }
            }

            string debuginfo = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}",

            timeutc530, strategyData.Decision.ToString(), strategyData.DecisionType.ToString(), strategyData.currentClose, percentage, strategyData.histdata,

            strategyData.BollingerUpperPercentage, strategyData.BollingerMiddlePercentage, strategyData.BollingerLowerPercentage,

            strategyData.SignalGap0, strategyData.SignalGap1, skipReasons);

            File.AppendAllLines(PrimaryLogPath, new[] { debuginfo });

            File.AppendAllLines(SecondaryLogPath, new[] { debuginfo });
        }

    }
}
