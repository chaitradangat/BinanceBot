using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;

using Binance.Net;

using Binance.Net.Objects;

using BinanceBot.Strategy;

using BinanceBot.Domain;

using BinanceBot.Settings;
using System.Linq;

namespace BinanceBot.Application
{
    public class BinanceCommand
    {
        private readonly BinanceWebCall webCall;

        private readonly int pingtime;

        private decimal BollingerFactor;

        private string LastAvoidReason;

        public BinanceCommand(string ApiKey, string ApiSecret)
        {
            webCall = new BinanceWebCall();

            webCall.AddAuthenticationInformation(ApiKey, ApiSecret);

            pingtime = BinanceBotSettings.settings.PingTimer;

            BollingerFactor = OpenCloseStrategySettings.settings.BollingerFactor;

            LastAvoidReason = "";
        }

        public void StartRoBot(RobotInput robotInput, bool isLive)
        {
            #region -strategy and function level variables-
            var openclosestrategy = new OpenCloseStrategy();

            StrategyData strategyData = new StrategyData(1);

            var errorCount = 0;

            //improve this further later
            webCall.AssignBinanceWebCallFeatures(robotInput.symbol);
            #endregion

            using (webCall.client = new BinanceClient())
            {
                while (true)
                {
                    try
                    {
                        Stopwatch sw = new Stopwatch();

                        sw.Start();

                        #region -variables refreshed every cycle-
                        var currentPosition = new SimplePosition(robotInput.quantity);

                        strategyData = new StrategyData(strategyData.profitFactor);//tracking updated check for more elegant ways to write this..

                        List<OHLCKandle> ohlckandles = new List<OHLCKandle>();

                        Thread.Sleep(pingtime);
                        #endregion

                        //get open positions from server
                        webCall.GetCurrentPosition(robotInput, ref strategyData, ref currentPosition, isLive);

                        //get kandles from server
                        webCall.GetKLinesDataCached(robotInput.timeframe, robotInput.candleCount, ref strategyData, ref ohlckandles);

                        //run strategy over the kandles
                        openclosestrategy.RunStrategy(ohlckandles, robotInput, currentPosition, ref strategyData);

                        //place orders based on strategy output
                        webCall.PlaceOrders(robotInput, strategyData, isLive);

                        sw.Stop();

                        //display data to UI
                        DumpToConsole(strategyData, currentPosition, robotInput, sw.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(10);

                        ++errorCount;
                    }

                    if (errorCount >= 300)
                    {
                        break;
                    }
                }
            }
        }

        private void DumpToConsole(StrategyData strategyData, SimplePosition order, RobotInput robotInput, long cycleTime)
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

            if (strategyData.prevOutput.ToString().ToLower().Contains("buy") && strategyData.LatestSignalStrength != 0)
            {
                //signal
                Console.WriteLine("DECISION : {0}  {1}%  @STRENGTH OF {2}\n", strategyData.prevOutput.ToString(), 100 * strategyData.BuyCounter / strategyData.LatestSignalStrength, strategyData.LatestSignalStrength);
            }
            else if (strategyData.prevOutput.ToString().ToLower().Contains("sell") && strategyData.LatestSignalStrength != 0)
            {
                Console.WriteLine("DECISION : {0}  {1}%  @STRENGTH OF {2}\n", strategyData.prevOutput.ToString(), 100 * strategyData.SellCounter / strategyData.LatestSignalStrength, strategyData.LatestSignalStrength);
            }
            else
            {
                Console.WriteLine("DECISION : {0}\n", "NO DECISION");
            }

            Console.WriteLine("SIGNALHISTORY : {0}\n", strategyData.histdata);

            if (strategyData.AvoidReasons != null && strategyData.AvoidReasons.Count > 0)
            {
                LastAvoidReason = "";
                foreach (var AvoidReason in strategyData.AvoidReasons)
                {
                    LastAvoidReason += AvoidReason.ToString() + "\t";
                }
            }

            Console.WriteLine("AVOIDHISTORY : {0}", LastAvoidReason);

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
            robotInput.reward * strategyData.profitFactor,
            robotInput.reward,
            robotInput.risk,
            Math.Round(robotInput.reward * BollingerFactor, 3));

            Console.WriteLine("LEVERAGE {0}x\n", robotInput.leverage);

            Console.WriteLine("--------------------------------------------------------------------------\n");

            Console.WriteLine("Refresh Rate {0} milliseconds\n", cycleTime);


            

        }
    }
}