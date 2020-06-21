using System;
using System.IO;
using System.Linq;

using BinanceBot.Domain;
using BinanceBot.Settings;

namespace BinanceBot.Common
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
                File.AppendAllLines(PrimaryLogPath, new[] { "Date\tSignal\tSignalType\tPrice\t%\tSignalHistory\tBU\tBM\tBL\tS0\tS1\tTrend\tMood\tMmacd\tMsignal\tMhistogram\tMbullcross\tMbearcross\tA1\tA2\tA3\tA4" });
            }
            if (!File.Exists(SecondaryLogPath))
            {
                File.AppendAllLines(SecondaryLogPath, new[] { "Date\tSignal\tSignalType\tPrice\t%\tSignalHistory\tBU\tBM\tBL\tS0\tS1\tTrend\tMood\tMmacd\tMsignal\tMhistogram\tMbullcross\tMbearcross\tA1\tA2\tA3\tA4" });
            }
        }
        /// <summary>
        /// This function dumps decision logs
        /// </summary>
        /// <param name="robotInput"></param>
        /// <param name="strategyData"></param>
        public static void DumpToLog(RobotInput robotInput, StrategyData strategyData)
        {
            string timeutc530 = DateTime.Now.ToUniversalTime().AddMinutes(330).ToString();

            var skipReasons = "";

            if (strategyData.SkipReasons != null && strategyData.SkipReasons.Count > 0)
            {
                foreach (var AvoidReason in strategyData.SkipReasons)
                {
                    skipReasons += AvoidReason.ToString() + "\t";
                }
            }

            string debuginfo = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}",

            timeutc530, strategyData.Decision.ToString(), strategyData.DecisionType.ToString(), strategyData.currentClose,

            strategyData.DecisionType != StrategyDecision.None ? strategyData.Percentage : 0,

            strategyData.histdata, strategyData.BollingerUpperPercentage, strategyData.BollingerMiddlePercentage, strategyData.BollingerLowerPercentage,

            strategyData.SignalGap0, strategyData.SignalGap1, strategyData.trend, strategyData.mood,

            strategyData.MacdData.macdvalue, strategyData.MacdData.signalvalue, strategyData.MacdData.histogramvalue,

            strategyData.MacdData.IsBullish, strategyData.MacdData.IsBearish,

            skipReasons);

            File.AppendAllLines(PrimaryLogPath, new[] { debuginfo });

            File.AppendAllLines(SecondaryLogPath, new[] { debuginfo });
        }
        /// <summary>
        /// This function dumps to display
        /// </summary>
        /// <param name="strategyData"></param>
        /// <param name="order"></param>
        /// <param name="robotInput"></param>
        /// <param name="BollingerFactor"></param>
        /// <param name="LastAvoidReason"></param>
        /// <param name="cycleTime"></param>
        public static void DumpToConsole(StrategyData strategyData, SimplePosition order, RobotInput robotInput, decimal BollingerFactor, ref string LastAvoidReason, long cycleTime)
        {
            Console.Clear();

            Console.WriteLine("\n\n--------------------------------------------------------------------------");

            Console.WriteLine("\nMARKET DETAILS: \n");

            //latest price
            Console.WriteLine("{0} : {1} \n", robotInput.symbol, strategyData.currentClose);

            Console.WriteLine("BBAND   : {0}%   {1}%   {2}%  {3}{4}{5}\n",
                strategyData.BollingerUpperPercentage,
                strategyData.BollingerMiddlePercentage,
                strategyData.BollingerLowerPercentage,
                strategyData.BollTopCrossed ? "*TOPCROSSED*" : "",
                strategyData.BollBottomCrossed ? "*BOTTOMCROSSED*" : "",
                strategyData.BollMiddleCrossed ? "*MIDDLECROSSED*" : ""
                );

            //mood
            if (strategyData.mood == "BULLISH")
            {
                Console.Write("MOOD    : [{0}] ", "UP");
            }
            else if (strategyData.mood == "BEARISH")
            {
                Console.Write("MOOD    : [{0}] ", "DOWN");
            }
            else
            {
                Console.Write("MOOD : [{0}] ", "");
            }

            //trend
            if (strategyData.trend == "BULLISH")
            {
                Console.WriteLine("  TREND  : [{0}]\n", "UP");
            }
            else if (strategyData.trend == "BEARISH")
            {
                Console.WriteLine("  TREND  : [{0}]\n", "DOWN");
            }
            else
            {
                Console.WriteLine("TREND : {0}\n", "");
            }

            if (strategyData.PrevDecisionType == StrategyDecision.Buy && strategyData.LatestSignalStrength != 0)
            {
                //signal
                Console.WriteLine("DECISION : {0}{1}  {2}%  @STRENGTH OF {3}\n", strategyData.PrevDecision, strategyData.PrevDecisionType, 100 * strategyData.BuyCounter / strategyData.LatestSignalStrength, strategyData.LatestSignalStrength);
            }
            else if (strategyData.PrevDecisionType == StrategyDecision.Sell && strategyData.LatestSignalStrength != 0)
            {
                Console.WriteLine("DECISION : {0}{1}  {2}%  @STRENGTH OF {3}\n", strategyData.PrevDecision, strategyData.PrevDecisionType, 100 * strategyData.SellCounter / strategyData.LatestSignalStrength, strategyData.LatestSignalStrength);
            }
            else
            {
                Console.WriteLine("DECISION : {0}\n", "NO DECISION");
            }

            Console.WriteLine("SGNLHISTORY :{0}\n", strategyData.histdata);

            if (strategyData.SkipReasons != null && strategyData.SkipReasons.Count > 0)
            {
                LastAvoidReason = "";
                foreach (var AvoidReason in strategyData.SkipReasons)
                {
                    LastAvoidReason += AvoidReason.ToString() + " ";
                }
            }

            Console.WriteLine("SKIPHISTORY : {0}", LastAvoidReason);

            Console.WriteLine("\n--------------------------------------------------------------------------");

            Console.WriteLine("\nORDER DETAILS: \n");

            Console.WriteLine("TYPE {0} \n", order?.PositionType);

            Console.WriteLine("ENTRY PRICE {0} \n", order?.EntryPrice);

            if (order?.PositionType != PositionType.None)
            {
                Console.WriteLine("PERCENTAGE {0} \n", Math.Round(strategyData.Percentage, 3));
            }

            Console.WriteLine("LIMITS > ADJPROFIT *{0}%*  PROFIT *{1}%*  LOSS *{2}%* BOLL *{3}%*\n",
            Math.Round(robotInput.reward * strategyData.profitFactor, 3),
            robotInput.reward,
            robotInput.risk,
            Math.Round(robotInput.reward * BollingerFactor, 3));

            Console.WriteLine("LEVERAGE {0}x\n", robotInput.leverage);

            Console.WriteLine("--------------------------------------------------------------------------\n");

            Console.WriteLine("Refresh Rate {0} milliseconds\n", cycleTime);

            DisplayMacdIndicator(strategyData);
        }

        public static void DisplayMacdIndicator(StrategyData strategyData)
        {
            var macdData = strategyData.MacdData;

            string macdvalues = string.Format("macd*{0}*\nsignal*{1}*\nhistogram*{2}*\nisbearishcross*{3}*\nisbullishcross*{4}*\nmacdsignal*{5}",
            Math.Round(macdData.macdvalue, 4), Math.Round(macdData.signalvalue, 4),
            Math.Round(macdData.histogramvalue, 4), macdData.IsBearish, macdData.IsBullish, macdData.signalhistory);

            Console.WriteLine(macdvalues);
        }

        public static void LogExceptions(Exception ex, string info)
        {
            string timeutc530 = DateTime.Now.ToUniversalTime().AddMinutes(330).ToString();

            string exceptionString = string.Format("{0}\t{1}\t{2}", timeutc530, ex.ToString(), info);

            File.AppendAllLines("exception.logs", new[] { exceptionString });
        }
    }
}