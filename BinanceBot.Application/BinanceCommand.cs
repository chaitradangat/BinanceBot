﻿using System;
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
        private BinanceWebCall webCall;

        private int pingtime;

        public BinanceCommand(string ApiKey, string ApiSecret)
        {
            webCall = new BinanceWebCall();

            webCall.AddAuthenticationInformation(ApiKey, ApiSecret);

            pingtime = BinanceBotSettings.settings.PingTimer;
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
                        #region -variables refreshed every cycle-
                        var currentPosition = new SimplePosition(robotInput.quantity);

                        strategyData = new StrategyData(strategyData.profitFactor);//tracking updated check for more elegant ways to write this..

                        List<OHLCKandle> ohlckandles = new List<OHLCKandle>();

                        Stopwatch sw = new Stopwatch();

                        sw.Start();

                        Thread.Sleep(pingtime);
                        #endregion

                        if (isLive)
                        {
                            webCall.GetCurrentPosition(ref currentPosition, robotInput.quantity, ref strategyData);
                        }

                        webCall.GetKLinesDataCached(robotInput.timeframe, robotInput.candleCount, ref strategyData, ref ohlckandles);

                        openclosestrategy.RunStrategy(ohlckandles, robotInput, currentPosition, ref strategyData);

                        if (isLive && strategyData.Output != StrategyOutput.None)
                        {
                            PlaceOrders(robotInput, strategyData);
                        }

                        sw.Stop();

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

        public void PlaceOrders(RobotInput robotInput, StrategyData strategyData)
        {
            BinancePlacedOrder placedOrder = null;

            if (strategyData.Output == StrategyOutput.OpenPositionWithBuy || strategyData.Output == StrategyOutput.ExitPositionWithBuy ||
                strategyData.Output == StrategyOutput.BookProfitWithBuy || strategyData.Output == StrategyOutput.MissedPositionBuy ||
                strategyData.Output == StrategyOutput.ExitPositionHeavyLossWithBuy)
            {
                placedOrder = webCall.PlaceBuyOrder(robotInput.quantity, -1, true);
            }

            else if (strategyData.Output == StrategyOutput.OpenPositionWithSell || strategyData.Output == StrategyOutput.ExitPositionWithSell ||
                     strategyData.Output == StrategyOutput.BookProfitWithSell || strategyData.Output == StrategyOutput.MissedPositionSell ||
                     strategyData.Output == StrategyOutput.ExitPositionHeavyLossWithSell)
            {
                placedOrder = webCall.PlaceSellOrder(robotInput.quantity, -1, true);
            }

            else if (strategyData.Output == StrategyOutput.EscapeTrapWithBuy)
            {
                if (BinanceBotSettings.settings.ReOpenOnEscape)
                {
                    placedOrder = webCall.PlaceBuyOrder(robotInput.quantity * 2, -1, true);
                }
                else
                {
                    placedOrder = webCall.PlaceBuyOrder(robotInput.quantity, -1, true);
                }
            }

            else if (strategyData.Output == StrategyOutput.EscapeTrapWithSell)
            {
                if (BinanceBotSettings.settings.ReOpenOnEscape)
                {
                    placedOrder = webCall.PlaceSellOrder(robotInput.quantity * 2, -1, true);
                }
                else
                {
                    placedOrder = webCall.PlaceSellOrder(robotInput.quantity, -1, true);
                }
            }
            else
            {
                //no action
            }

            if (placedOrder != null 
                || strategyData.Output == StrategyOutput.AvoidOpenWithSell || strategyData.Output == StrategyOutput.AvoidOpenWithBuy 
                || strategyData.Output == StrategyOutput.AvoidLowSignalGapBuy || strategyData.Output == StrategyOutput.AvoidLowSignalGapSell
                || strategyData.Output == StrategyOutput.AvoidOpenWithBuyOnRedKandle || strategyData.Output == StrategyOutput.AvoidOpenWithSellOnGreenKandle
                || strategyData.Output == StrategyOutput.AvoidEscapeWithSell || strategyData.Output == StrategyOutput.AvoidEscapeWithBuy
                )
            {
                DumpToLog(robotInput, strategyData);
            }

        }

        private void DumpToConsole(StrategyData strategyData, SimplePosition order, RobotInput robotInput, long cycleTime)
        {
            Console.Clear();

            var bu_percentage = Math.Round((100 * (strategyData.BollingerUpper - strategyData.currentClose) / strategyData.BollingerUpper), 3);

            var bm_percentage = Math.Round((100 * (strategyData.BollingerMiddle - strategyData.currentClose) / strategyData.BollingerMiddle), 3);

            var bd_percentage = Math.Round((100 * (strategyData.currentClose - strategyData.BollingerLower) / strategyData.currentClose), 3);

            Console.WriteLine("\n\n--------------------------------------------------------------------------");

            Console.WriteLine("\nMARKET DETAILS: \n");

            //latest price
            Console.WriteLine("{0} : {1} \n", robotInput.symbol, strategyData.currentClose);

            Console.WriteLine("BBAND   : {0}%   {1}%   {2}%   {3}{4}\n", bu_percentage, bm_percentage, bd_percentage, strategyData.BollTopCrossed ? "*TOPCROSSED*" : "", strategyData.BollBottomCrossed ? "*BOTTOMCROSSED*" : "");

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

            Console.WriteLine("HISTORY : {0}", strategyData.histdata);

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

            Console.WriteLine("ADJUSTED PROFIT LIMIT {0}% \n", robotInput.reward * strategyData.profitFactor);

            Console.WriteLine("CURRENT PROFIT LIMIT {0}% \n", robotInput.reward);

            Console.WriteLine("CURRENT LOSS LIMIT {0}% \n", robotInput.risk);

            Console.WriteLine("CURRENT LEVERAGE {0}x\n", robotInput.leverage);

            Console.WriteLine("--------------------------------------------------------------------------\n");

            Console.WriteLine("Refresh Rate {0} milliseconds\n", cycleTime);
        }

        private void DumpToLog(RobotInput robotInput, StrategyData strategyData)
        {
            var bu_percentage = Math.Round((100 * (strategyData.BollingerUpper - strategyData.currentClose) / strategyData.BollingerUpper), 3);

            var bm_percentage = Math.Round((100 * (strategyData.BollingerMiddle - strategyData.currentClose) / strategyData.BollingerMiddle), 3);

            var bd_percentage = Math.Round((100 * (strategyData.currentClose - strategyData.BollingerLower) / strategyData.currentClose), 3);

            string timeutc530 = DateTime.Now.ToUniversalTime().AddMinutes(330).ToString();

            decimal percentage;

            if (strategyData.Output.ToString().ToLower().Contains("buy"))
            {
                percentage = Math.Round(strategyData.shortPercentage, 3);
            }
            else if (strategyData.Output.ToString().ToLower().Contains("sell"))
            {
                percentage = Math.Round(strategyData.longPercentage, 3);
            }
            else
            {
                percentage = 0;
            }

            if (strategyData.Output == StrategyOutput.AvoidOpenWithBuy)//to log only close encounters
            {
                if (bu_percentage < robotInput.reward * 0.90m || strategyData.BollTopCrossed)
                {
                    return;
                }
            }
            if (strategyData.Output == StrategyOutput.AvoidOpenWithSell)//to log only close encounters
            {
                if (bd_percentage < robotInput.reward * 0.90m || strategyData.BollBottomCrossed)
                {
                    return;
                }
            }

            string debuginfo = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",

            timeutc530, strategyData.Output.ToString(), strategyData.currentClose, percentage, strategyData.histdata,

            bu_percentage, bm_percentage, bd_percentage, strategyData.SignalGap0, strategyData.SignalGap1);

            File.AppendAllLines("debug.logs", new[] { debuginfo });

            File.AppendAllLines("Logs\\debug.txt", new[] { debuginfo });
        }

    }
}