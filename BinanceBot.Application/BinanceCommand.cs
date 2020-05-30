using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;

using Binance.Net;

using BinanceBot.Strategy;

using BinanceBot.Domain;

using BinanceBot.Settings;

using BinanceBot.Common;

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
                        #region -variables refreshed every cycle-
                        Stopwatch sw = new Stopwatch();

                        sw.Start();

                        var currentPosition = new SimplePosition(robotInput.quantity);

                        strategyData = new StrategyData(strategyData.profitFactor);//tracking updated check for more elegant ways to write this..

                        Thread.Sleep(pingtime);
                        #endregion

                        //get open positions from server
                        webCall.GetCurrentPosition(robotInput, ref strategyData, ref currentPosition, isLive);

                        //get kandles from server
                        webCall.GetKLinesDataCached(robotInput.timeframe, robotInput.candleCount, ref strategyData);

                        //run strategy over the kandles
                        openclosestrategy.RunStrategy(robotInput, currentPosition, ref strategyData);

                        //place orders based on strategy output
                        webCall.PlaceOrders(robotInput, strategyData, isLive);

                        sw.Stop();

                        //display data to UI
                        Utility.DumpToConsole(strategyData, currentPosition, robotInput, BollingerFactor, ref LastAvoidReason, sw.ElapsedMilliseconds);
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
    }
}