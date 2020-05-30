using System;

using System.IO;

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

        public static void DumpToConsole(StrategyData strategyData, SimplePosition order, RobotInput robotInput, decimal BollingerFactor, string LastAvoidReason, long cycleTime)
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

            if (strategyData.PrevDecision.ToString().ToLower().Contains("buy") && strategyData.LatestSignalStrength != 0)
            {
                //signal
                Console.WriteLine("DECISION : {0}  {1}%  @STRENGTH OF {2}\n", strategyData.PrevDecision.ToString(), 100 * strategyData.BuyCounter / strategyData.LatestSignalStrength, strategyData.LatestSignalStrength);
            }
            else if (strategyData.PrevDecision.ToString().ToLower().Contains("sell") && strategyData.LatestSignalStrength != 0)
            {
                Console.WriteLine("DECISION : {0}  {1}%  @STRENGTH OF {2}\n", strategyData.PrevDecision.ToString(), 100 * strategyData.SellCounter / strategyData.LatestSignalStrength, strategyData.LatestSignalStrength);
            }
            else
            {
                Console.WriteLine("DECISION : {0}\n", "NO DECISION");
            }

            Console.WriteLine("SGNLHISTORY : {0}\n", strategyData.histdata);

            if (strategyData.AvoidReasons != null && strategyData.AvoidReasons.Count > 0)
            {
                LastAvoidReason = "";
                foreach (var AvoidReason in strategyData.AvoidReasons)
                {
                    LastAvoidReason += AvoidReason.ToString() + "\t";
                }
            }

            Console.WriteLine("SKIPHISTORY : {0}", LastAvoidReason);

            Console.WriteLine("\n--------------------------------------------------------------------------");

            Console.WriteLine("\nORDER DETAILS: \n");

            Console.WriteLine("ID {0}\n", order?.PositionID);

            Console.WriteLine("TYPE {0} \n", order?.PositionType);

            Console.WriteLine("ENTRY PRICE {0} \n", order?.EntryPrice);

            if (order?.PositionID != -1 && order?.PositionType == "SELL")
            {
                Console.WriteLine("PERCENTAGE {0} \n", Math.Round(strategyData.shortPercentage, 3));
            }
            if (order?.PositionID != -1 && order?.PositionType == "BUY")
            {
                Console.WriteLine("PERCENTAGE {0} \n", Math.Round(strategyData.longPercentage, 3));
            }



            Console.WriteLine("LIMITS > ADJPROFIT *{0}%*  PROFIT *{1}%*  LOSS *{2}%* BOLL *{3}%*\n",
            Math.Round(robotInput.reward * strategyData.profitFactor, 3),
            robotInput.reward,
            robotInput.risk,
            Math.Round(robotInput.reward * BollingerFactor, 3));

            Console.WriteLine("LEVERAGE {0}x\n", robotInput.leverage);

            Console.WriteLine("--------------------------------------------------------------------------\n");

            Console.WriteLine("Refresh Rate {0} milliseconds\n", cycleTime);




        }

    }
}
