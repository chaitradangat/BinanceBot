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
                        strategyData = new StrategyData(strategyData.profitFactor);//tracking updated check for more elegant ways to write this..

                        Stopwatch sw = new Stopwatch();

                        sw.Start();

                        List<OHLCKandle> ohlckandles = new List<OHLCKandle>();

                        var currentClose = default(decimal);

                        var currentPosition = new SimplePosition(robotInput.quantity);

                        Thread.Sleep(pingtime);
                        #endregion

                        if (isLive)
                        {
                            webCall.GetCurrentPosition(ref currentPosition, robotInput.quantity, ref strategyData);
                        }

                        webCall.GetKLinesDataCached(robotInput.timeframe, robotInput.candleCount, ref currentClose, ref ohlckandles);

                        robotInput.currentClose = currentClose;

                        openclosestrategy.RunStrategy(ohlckandles, robotInput, ref strategyData, ref currentPosition);

                        if (isLive && strategyData.Output != StrategyOutput.None)
                        {
                            PlaceOrders(robotInput.quantity, currentClose, strategyData.Output, strategyData);
                        }

                        sw.Stop();

                        DumpToConsole(strategyData, currentPosition, robotInput, currentClose, sw.ElapsedMilliseconds);

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

        public void PlaceOrders(decimal quantity, decimal currrentClose, StrategyOutput strategyOutput, StrategyData strategyData)
        {
            BinancePlacedOrder placedOrder = null;

            if (strategyOutput == StrategyOutput.OpenPositionWithBuy || strategyOutput == StrategyOutput.ExitPositionWithBuy ||
                strategyOutput == StrategyOutput.BookProfitWithBuy || strategyOutput == StrategyOutput.MissedPositionBuy ||
                strategyOutput == StrategyOutput.ExitPositionHeavyLossWithBuy)
            {
                placedOrder = webCall.PlaceBuyOrder(quantity, -1, true);
            }

            else if (strategyOutput == StrategyOutput.OpenPositionWithSell || strategyOutput == StrategyOutput.ExitPositionWithSell ||
                     strategyOutput == StrategyOutput.BookProfitWithSell || strategyOutput == StrategyOutput.MissedPositionSell ||
                     strategyOutput == StrategyOutput.ExitPositionHeavyLossWithSell)
            {
                placedOrder = webCall.PlaceSellOrder(quantity, -1, true);
            }

            else if (strategyOutput == StrategyOutput.EscapeTrapWithBuy)
            {
                if (BinanceBotSettings.settings.ReOpenOnEscape)
                {
                    placedOrder = webCall.PlaceBuyOrder(quantity * 2, -1, true);
                }
                else
                {
                    placedOrder = webCall.PlaceBuyOrder(quantity, -1, true);
                }
            }

            else if (strategyOutput == StrategyOutput.EscapeTrapWithSell)
            {
                if (BinanceBotSettings.settings.ReOpenOnEscape)
                {
                    placedOrder = webCall.PlaceSellOrder(quantity * 2, -1, true);
                }
                else
                {
                    placedOrder = webCall.PlaceSellOrder(quantity, -1, true);
                }
            }
            else
            {
                //no action
            }

            if (placedOrder != null || strategyOutput == StrategyOutput.AvoidOpenWithSell || strategyOutput == StrategyOutput.AvoidOpenWithBuy)
            {
                DumpToLog(currrentClose, strategyOutput.ToString(), strategyData);
            }

        }

        private void DumpToConsole(StrategyData strategyData, SimplePosition order, RobotInput sInput, decimal currentClose, long cycleTime)
        {
            Console.Clear();

            Console.WriteLine();

            Console.WriteLine("\n--------------------------------------------------------------------------");

            Console.WriteLine("\nMARKET DETAILS: \n");

            //latest price
            Console.WriteLine("{0} : {1}\n", sInput.symbol, currentClose);

            //mood
            if (strategyData.mood == "BULLISH")
            {//\u02C4
                Console.WriteLine("MOOD    : {0}\n", "UP");
            }
            else if (strategyData.mood == "BEARISH")
            {
                Console.WriteLine("MOOD    : {0}\n", "DOWN");
            }
            else
            {
                Console.WriteLine("MOOD : {0}\n", "");
            }

            //trend
            if (strategyData.trend == "BULLISH")
            {
                Console.WriteLine("TREND   : {0}\n", "UP");
            }
            else if (strategyData.trend == "BEARISH")
            {
                Console.WriteLine("TREND   : {0}\n", "DOWN");
            }
            else
            {
                Console.WriteLine("TREND : {0}\n", "");
            }

            if (strategyData.prevOutput.ToString().ToLower().Contains("buy") && strategyData.LatestSignalStrength != 0)
            {
                //signal
                Console.WriteLine("DECISION : {0}  {1}%  @STRENGTH OF {2}\n", strategyData.prevOutput.ToString(),
                    100 * strategyData.BuyCounter / strategyData.LatestSignalStrength, strategyData.LatestSignalStrength
                    );
            }
            else if (strategyData.prevOutput.ToString().ToLower().Contains("sell") && strategyData.LatestSignalStrength != 0)
            {
                Console.WriteLine("DECISION : {0}  {1}%  @STRENGTH OF {2}\n", strategyData.prevOutput.ToString(),
                    100 * strategyData.SellCounter / strategyData.LatestSignalStrength, strategyData.LatestSignalStrength
                    );
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

            Console.WriteLine("ADJUSTED PROFIT LIMIT {0}% \n", sInput.reward * strategyData.profitFactor);

            Console.WriteLine("CURRENT PROFIT LIMIT {0}% \n", sInput.reward);

            Console.WriteLine("CURRENT LOSS LIMIT {0}% \n", sInput.risk);

            Console.WriteLine("CURRENT LEVERAGE {0}x\n", sInput.leverage);

            Console.WriteLine("--------------------------------------------------------------------------\n");

            Console.WriteLine("Refresh Rate {0} milliseconds\n", cycleTime);
        }

        private void DumpToLog(decimal currentClose, string decision, StrategyData strategyData)
        {
            string timeutc530 = DateTime.Now.ToUniversalTime().AddMinutes(330).ToString();

            decimal percentage;

            if (decision.ToLower().Contains("buy"))
            {
                percentage = Math.Round(strategyData.shortPercentage, 3);
            }
            else if (decision.ToLower().Contains("sell"))
            {
                percentage = Math.Round(strategyData.longPercentage, 3);
            }
            else
            {
                percentage = 0;
            }

            string debuginfo = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", timeutc530, decision, currentClose, percentage, strategyData.histdata,strategyData.BollingerUpper,strategyData.BollingerMiddle,strategyData.BollingerLower);

            File.AppendAllLines("debug.logs", new[] { debuginfo });

            File.AppendAllLines("Logs\\debug.txt", new[] { debuginfo });
        }

    }
}