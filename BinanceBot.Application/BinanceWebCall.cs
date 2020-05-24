using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.IO;


using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;

using BinanceBot.Domain;

using BinanceBot.Settings;

namespace BinanceBot.Application
{
    public class BinanceWebCall
    {
        //shared variables
        public BinanceClient client;
        private string symbol;
        private TimeSpan? TimeZoneDiff;
        private List<OHLCKandle> kandleCache;
        private string timeframeCache;

        /// <summary>
        /// constructor
        /// </summary>
        public BinanceWebCall()
        {

        }

        /// <summary>
        /// Assign variables common to this class to all functions
        /// </summary>
        /// <param name="client"></param>
        /// <param name="symbol"></param>
        public void AssignBinanceWebCallFeatures(BinanceClient client, string symbol)
        {
            this.client = client;
            this.symbol = symbol;
        }
        /// <summary>
        /// Assign variables common to this class to all functions
        /// </summary>
        /// <param name="client"></param>
        /// <param name="symbol"></param>
        public void AssignBinanceWebCallFeatures(string symbol)
        {
            this.symbol = symbol;
        }
        /// <summary>
        /// Authentication Information to access Binance API
        /// </summary>
        /// <param name="ApiKey"></param>
        /// <param name="ApiSecret"></param>
        public void AddAuthenticationInformation(string ApiKey, string ApiSecret)
        {
            BinanceClient.SetDefaultOptions(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(ApiKey, ApiSecret),
            });

            BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials(ApiKey, ApiSecret),
            });
        }
        /// <summary>
        /// Place Sell Order of Given Quantity
        /// </summary>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        /// <param name="futures"></param>
        /// <returns></returns>
        public BinancePlacedOrder PlaceSellOrder(decimal quantity, decimal price, bool futures)
        {
            var sellOrder = client.PlaceOrder(symbol, OrderSide.Sell, OrderType.Market, quantity: quantity, futures: true);

            if (sellOrder.Success)
            {
                return sellOrder.Data;
            }
            else
            {
                return null;
            }

        }
        /// <summary>
        /// Place Buy Order with given Quantity
        /// </summary>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        /// <param name="futures"></param>
        /// <returns></returns>
        public BinancePlacedOrder PlaceBuyOrder(decimal quantity, decimal price, bool futures)
        {
            var buyOrder = client.PlaceOrder(symbol, OrderSide.Buy, OrderType.Market, quantity: quantity, futures: true);

            if (buyOrder.Success)
            {
                return buyOrder.Data;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Get All Positions for given Symbol
        /// </summary>
        /// <param name="position"></param>
        /// <param name="quantity"></param>
        /// <param name="profitFactor"></param>
        public void GetCurrentPosition(ref SimplePosition position, decimal quantity, ref decimal profitFactor)
        {
        getpositions:

            position = new SimplePosition
            {
                PositionID = -1,

                PositionType = "",

                EntryPrice = -1,

                Quantity = quantity,

                Trend = "",

                Mood = ""
            };

            var positions = client.GetPositions(default, true);

            var currentPosition = positions.Data.Where(x => x.symbol == symbol).Single();

            if (currentPosition.entryPrice != 0)
            {
                if (currentPosition.entryPrice > currentPosition.markPrice && currentPosition.unRealizedProfit > 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "SELL";
                    position.Quantity = quantity;
                }
                else if (currentPosition.entryPrice < currentPosition.markPrice && currentPosition.unRealizedProfit < 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "SELL";
                    position.Quantity = quantity;
                }
                else if (currentPosition.entryPrice < currentPosition.markPrice && currentPosition.unRealizedProfit > 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "BUY";
                    position.Quantity = quantity;
                }
                else if (currentPosition.entryPrice > currentPosition.markPrice && currentPosition.unRealizedProfit < 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "BUY";
                    position.Quantity = quantity;
                }
                else
                {
                    Thread.Sleep(1000);
                    goto getpositions;
                }
            }
            else
            {

                profitFactor = 1;

                position.PositionID = -1;

                position.PositionType = "";

                position.EntryPrice = -1;

                position.Quantity = quantity;
            }
        }
        /// <summary>
        /// Get All Positions for given Symbol
        /// </summary>
        /// <param name="position"></param>
        /// <param name="quantity"></param>
        /// <param name="profitFactor"></param>
        public void GetCurrentPosition(ref SimplePosition position, decimal quantity, ref StrategyData strategyData)
        {
        getpositions:

            position = new SimplePosition
            {
                PositionID = -1,

                PositionType = "",

                EntryPrice = -1,

                Quantity = quantity,

                Trend = "",

                Mood = ""
            };

            var positions = client.GetPositions(default, true);

            var currentPosition = positions.Data.Where(x => x.symbol == symbol && x.positionSide == "BOTH").Single();

            if (currentPosition.entryPrice != 0)
            {
                if (currentPosition.entryPrice > currentPosition.markPrice && currentPosition.unRealizedProfit > 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "SELL";
                    position.Quantity = quantity;
                }
                else if (currentPosition.entryPrice < currentPosition.markPrice && currentPosition.unRealizedProfit < 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "SELL";
                    position.Quantity = quantity;
                }
                else if (currentPosition.entryPrice < currentPosition.markPrice && currentPosition.unRealizedProfit > 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "BUY";
                    position.Quantity = quantity;
                }
                else if (currentPosition.entryPrice > currentPosition.markPrice && currentPosition.unRealizedProfit < 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "BUY";
                    position.Quantity = quantity;
                }
                else
                {
                    Thread.Sleep(1000);
                    goto getpositions;
                }
            }
            else
            {

                strategyData.profitFactor = 1;

                position.PositionID = -1;

                position.PositionType = "";

                position.EntryPrice = -1;

                position.Quantity = quantity;
            }
        }
        /// <summary>
        /// Get all Positions for the given symbol 
        /// </summary>
        /// <param name="robotInput"></param>
        /// <param name="strategyData"></param>
        /// <param name="position"></param>
        /// <param name="isLive"></param>
        public void GetCurrentPosition(RobotInput robotInput, ref StrategyData strategyData, ref SimplePosition position, bool isLive)
        {
            if (!isLive)
            {
                return;
            }
        getpositions:

            position = new SimplePosition
            {
                PositionID = -1,

                PositionType = "",

                EntryPrice = -1,

                Quantity = robotInput.quantity,

                Trend = "",

                Mood = ""
            };

            var positions = client.GetPositions(default, true);

            var currentPosition = positions.Data.Where(x => x.symbol == symbol && x.positionSide == "BOTH").Single();

            if (currentPosition.entryPrice != 0)
            {
                if (currentPosition.entryPrice > currentPosition.markPrice && currentPosition.unRealizedProfit > 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "SELL";
                    position.Quantity = robotInput.quantity;
                }
                else if (currentPosition.entryPrice < currentPosition.markPrice && currentPosition.unRealizedProfit < 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "SELL";
                    position.Quantity = robotInput.quantity;
                }
                else if (currentPosition.entryPrice < currentPosition.markPrice && currentPosition.unRealizedProfit > 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "BUY";
                    position.Quantity = robotInput.quantity;
                }
                else if (currentPosition.entryPrice > currentPosition.markPrice && currentPosition.unRealizedProfit < 0)
                {
                    position.EntryPrice = currentPosition.entryPrice;
                    position.PositionID = 999;
                    position.PositionType = "BUY";
                    position.Quantity = robotInput.quantity;
                }
                else
                {
                    Thread.Sleep(1000);
                    goto getpositions;
                }
            }
            else
            {

                strategyData.profitFactor = 1;

                position.PositionID = -1;

                position.PositionType = "";

                position.EntryPrice = -1;

                position.Quantity = robotInput.quantity;
            }
        }

        /// <summary>
        /// Get Candle Data From the Servers
        /// </summary>
        /// <param name="timeframe"></param>
        /// <param name="candleCount"></param>
        /// <param name="currentClose"></param>
        /// <param name="ohlckandles"></param>
        public void GetKLinesData(string timeframe, int candleCount, ref List<OHLCKandle> ohlckandles)
        {
            #region -candle interval for the request-

            var timeframe_ = KlineInterval.ThirtyMinutes;

            if (timeframe == "1m")
            {
                timeframe_ = KlineInterval.OneMinute;
            }
            else if (timeframe == "5m")
            {
                timeframe_ = KlineInterval.FiveMinutes;
            }
            else if (timeframe == "15m")
            {
                timeframe_ = KlineInterval.FifteenMinutes;
            }
            else if (timeframe == "30m")
            {
                timeframe_ = KlineInterval.ThirtyMinutes;
            }
            else if (timeframe == "1h")
            {
                timeframe_ = KlineInterval.OneHour;
            }
            else if (timeframe == "2h")
            {
                timeframe_ = KlineInterval.TwoHour;
            }
            else if (timeframe == "4h")
            {
                timeframe_ = KlineInterval.FourHour;
            }
            else if (timeframe == "6h")
            {
                timeframe_ = KlineInterval.SixHour;
            }
            else if (timeframe == "1d")
            {
                timeframe_ = KlineInterval.OneDay;
            }
            else
            {
                timeframe_ = KlineInterval.ThirtyMinutes;
            }

            #endregion

            var startTime = GetStartTime(candleCount, timeframe);

            var klines = client.GetKlines(symbol, timeframe_, startTime: startTime, ct: default, futures: true);

            ohlckandles = ConvertToOHLCKandles(klines.Data.ToList());
        }
        /// <summary>
        /// Convert Binance Klines to OHLC Kandles
        /// </summary>
        /// <param name="klines"></param>
        /// <returns></returns>
        private List<OHLCKandle> ConvertToOHLCKandles(List<BinanceKline> klines)
        {
            List<OHLCKandle> candles = new List<OHLCKandle>();

            foreach (var item in klines)
            {
                candles.Add(
                    new OHLCKandle
                    {
                        Open = item.Open,
                        Close = item.Close,
                        High = item.High,
                        Low = item.Low,
                        OpenTime = item.OpenTime,
                        CloseTime = item.CloseTime
                    });
            }


            return candles;
        }
        /// <summary>
        /// Get Candle Data From the Servers with local caching (faster performance)
        /// </summary>
        /// <param name="timeframe"></param>
        /// <param name="candleCount"></param>
        /// <param name="currentClose"></param>
        /// <param name="ohlckandles"></param>
        public void GetKLinesDataCached(string timeframe, int candleCount, ref StrategyData strategyData, ref List<OHLCKandle> ohlckandles)
        {
            //insert data in cache
            if (string.IsNullOrEmpty(timeframeCache) || timeframe != timeframeCache || kandleCache == null || kandleCache.Count == 0)
            {
                //get all the kandles from server
                GetKLinesData(timeframe, candleCount, ref ohlckandles);

                //synchronize cache
                kandleCache = ohlckandles.Select(x => new OHLCKandle
                {
                    Open = x.Open,

                    Close = x.Close,

                    High = x.High,

                    Low = x.Low,

                    OpenTime = x.OpenTime,

                    CloseTime = x.CloseTime

                }).ToList();

                //synchronize timeframe
                timeframeCache = timeframe.ToString();
            }
            //update existing cache
            else
            {
                #region -candle interval for the request-

                KlineInterval timeframe_;

                if (timeframe == "1m")
                {
                    timeframe_ = KlineInterval.OneMinute;
                }
                else if (timeframe == "5m")
                {
                    timeframe_ = KlineInterval.FiveMinutes;
                }
                else if (timeframe == "15m")
                {
                    timeframe_ = KlineInterval.FifteenMinutes;
                }
                else if (timeframe == "30m")
                {
                    timeframe_ = KlineInterval.ThirtyMinutes;
                }
                else if (timeframe == "1h")
                {
                    timeframe_ = KlineInterval.OneHour;
                }
                else if (timeframe == "2h")
                {
                    timeframe_ = KlineInterval.TwoHour;
                }
                else if (timeframe == "4h")
                {
                    timeframe_ = KlineInterval.FourHour;
                }
                else if (timeframe == "6h")
                {
                    timeframe_ = KlineInterval.SixHour;
                }
                else if (timeframe == "1d")
                {
                    timeframe_ = KlineInterval.OneDay;
                }
                else
                {
                    timeframe_ = KlineInterval.ThirtyMinutes;
                }

                #endregion

                //get 2 latest kandles from server
                var klines = client.GetKlines(symbol, timeframe_, limit: 2, futures: true);

                //convert 2 latest kandles to ohlc
                ohlckandles = ConvertToOHLCKandles(klines.Data.ToList());

                //cache count
                int countToMaintain = kandleCache.Count;

                //synchronize cache with latest values
                foreach (var kandle in ohlckandles)
                {
                    var k = kandleCache.Where(x => x.OpenTime == kandle.OpenTime).SingleOrDefault();

                    if (k == null)
                    {
                        #region -Add to Cache-
                        kandleCache.Add(
                            new OHLCKandle
                            {
                                Open = kandle.Open,

                                Close = kandle.Close,

                                High = kandle.High,

                                Low = kandle.Low,

                                OpenTime = kandle.OpenTime,

                                CloseTime = kandle.CloseTime
                            });
                        #endregion
                    }
                    else
                    {
                        #region -Update Cache-
                        k.Close = kandle.Close;
                        k.Open = kandle.Open;
                        k.High = kandle.High;
                        k.Low = kandle.Low;
                        #endregion
                    }
                }

                while (kandleCache.Count != countToMaintain)
                {
                    kandleCache.RemoveAt(0);
                }

                //output values
                ohlckandles = kandleCache.Select(x => new OHLCKandle
                {
                    Open = x.Open,

                    Close = x.Close,

                    High = x.High,

                    Low = x.Low,

                    OpenTime = x.OpenTime,

                    CloseTime = x.CloseTime

                }).ToList();
            }


            //save values in strategydata variable
            strategyData.currentOpen = ohlckandles[ohlckandles.Count - 1].Open;

            strategyData.currentClose = ohlckandles[ohlckandles.Count - 1].Close;

            strategyData.PrevOpen = ohlckandles[ohlckandles.Count - 2].Open;

            strategyData.PrevClose = ohlckandles[ohlckandles.Count - 2].Close;

            strategyData.kandles =

            ohlckandles.Select(x => new OHLCKandle
            {
                Close = x.Close,
                CloseTime = x.CloseTime,
                High = x.High,
                Low = x.Low,
                Open = x.Open,
                OpenTime = x.OpenTime

            }).ToList();
        }

        #region -timezone and server time functions-
        /// <summary>
        /// Get live server time of Binance Server
        /// </summary>
        /// <returns></returns>
        private DateTime GetServerTime()
        {
            DateTime dtServer = client.GetServerTime().Data;

            return dtServer;
        }
        /// <summary>
        /// Get cached/calculated server time of Binance Server
        /// </summary>
        /// <returns></returns>
        private DateTime GetServerTimeCached()
        {
            if (TimeZoneDiff == null)
            {
                TimeZoneDiff = GetTimeZoneDifference();
            }

            return DateTime.Now.Add((TimeSpan)TimeZoneDiff);
        }
        /// <summary>
        /// Get timezone difference between local machine and Binance Server
        /// </summary>
        /// <returns></returns>
        private TimeSpan GetTimeZoneDifference()
        {
            var serverTime = GetServerTime();

            var dtLocal = DateTime.Now;

            var tdiff = Math.Round((serverTime - dtLocal).TotalMinutes, 0, MidpointRounding.AwayFromZero);

            return TimeSpan.FromMinutes(tdiff);
        }
        /// <summary>
        /// Calculate max start-time of candle to request from Binance Server
        /// </summary>
        /// <param name="candleCount"></param>
        /// <param name="timeframe"></param>
        /// <returns></returns>
        public DateTime GetStartTime(int candleCount, string timeframe)
        {
            DateTime serverDateTime = GetServerTimeCached();

            int minutesRequested = 0;

            #region -set multiplier-
            if (timeframe == "1m")
            {
                minutesRequested = 1;
            }
            else if (timeframe == "5m")
            {
                minutesRequested = 5;
            }
            else if (timeframe == "15m")
            {
                minutesRequested = 15;
            }
            else if (timeframe == "30m")
            {
                minutesRequested = 30;
            }
            else if (timeframe == "1h")
            {
                minutesRequested = 60;
            }
            else if (timeframe == "2h")
            {
                minutesRequested = 120;
            }
            else if (timeframe == "4h")
            {
                minutesRequested = 240;
            }
            else if (timeframe == "6h")
            {
                minutesRequested = 360;
            }
            else if (timeframe == "1d")
            {
                minutesRequested = 24 * 60;
            }
            else
            {
                minutesRequested = 30;
            }
            #endregion

            int totalRequestedminutes = minutesRequested * candleCount;

            var dtTemp = serverDateTime.AddMinutes(-1 * totalRequestedminutes);

            if (dtTemp.Minute > 30)
            {
                dtTemp = dtTemp.AddMinutes((dtTemp.Minute - 30) * -1);
            }
            if (dtTemp.Minute < 30)
            {
                dtTemp = dtTemp.AddMinutes((dtTemp.Minute + 30) * -1);
            }

            return new DateTime(dtTemp.Year, dtTemp.Month, dtTemp.Day, dtTemp.Hour, dtTemp.Minute, 0);
        }
        #endregion

        //method to place orders
        public void PlaceOrders(RobotInput robotInput, StrategyData strategyData, bool isLive)
        {
            if (!isLive || strategyData.Decision == StrategyDecision.None)
            {
                return;
            }

            BinancePlacedOrder placedOrder = null;

            if (strategyData.Decision == StrategyDecision.OpenPositionWithBuy || strategyData.Decision == StrategyDecision.ExitPositionWithBuy ||
                strategyData.Decision == StrategyDecision.BookProfitWithBuy || strategyData.Decision == StrategyDecision.MissedPositionBuy ||
                strategyData.Decision == StrategyDecision.ExitPositionHeavyLossWithBuy)
            {
                placedOrder = PlaceBuyOrder(robotInput.quantity, -1, true);
            }

            else if (strategyData.Decision == StrategyDecision.OpenPositionWithSell || strategyData.Decision == StrategyDecision.ExitPositionWithSell ||
                     strategyData.Decision == StrategyDecision.BookProfitWithSell || strategyData.Decision == StrategyDecision.MissedPositionSell ||
                     strategyData.Decision == StrategyDecision.ExitPositionHeavyLossWithSell)
            {
                placedOrder = PlaceSellOrder(robotInput.quantity, -1, true);
            }

            else if (strategyData.Decision == StrategyDecision.EscapeTrapWithBuy)
            {
                if (BinanceBotSettings.settings.ReOpenOnEscape)
                {
                    placedOrder = PlaceBuyOrder(robotInput.quantity * 2, -1, true);
                }
                else
                {
                    placedOrder = PlaceBuyOrder(robotInput.quantity, -1, true);
                }
            }

            else if (strategyData.Decision == StrategyDecision.EscapeTrapWithSell)
            {
                if (BinanceBotSettings.settings.ReOpenOnEscape)
                {
                    placedOrder = PlaceSellOrder(robotInput.quantity * 2, -1, true);
                }
                else
                {
                    placedOrder = PlaceSellOrder(robotInput.quantity, -1, true);
                }
            }
            else
            {
                //no action
            }

            if (placedOrder != null
                || strategyData.Decision == StrategyDecision.AvoidOpenWithSell || strategyData.Decision == StrategyDecision.AvoidOpenWithBuy
                || strategyData.Decision == StrategyDecision.AvoidLowSignalGapBuy || strategyData.Decision == StrategyDecision.AvoidLowSignalGapSell
                || strategyData.Decision == StrategyDecision.AvoidOpenWithBuyOnRedKandle || strategyData.Decision == StrategyDecision.AvoidOpenWithSellOnGreenKandle
                || strategyData.Decision == StrategyDecision.AvoidEscapeWithSell || strategyData.Decision == StrategyDecision.AvoidEscapeWithBuy
                || strategyData.Decision == StrategyDecision.AvoidInvalidBollingerOpenBuy || strategyData.Decision == StrategyDecision.AvoidInvalidBollingerOpenSell
                )
            {
                DumpToLog(robotInput, strategyData);
            }

        }

        private void DumpToLog(RobotInput robotInput, StrategyData strategyData)
        {
            string timeutc530 = DateTime.Now.ToUniversalTime().AddMinutes(330).ToString();

            decimal percentage;

            var avoidReasons = "";

            if (strategyData.Decision.ToString().ToLower().Contains("buy"))
            {
                percentage = Math.Round(strategyData.shortPercentage, 3);
            }
            else if (strategyData.Decision.ToString().ToLower().Contains("sell"))
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
                    avoidReasons += AvoidReason.ToString() + "\t";
                }
            }

            //strategyData.AvoidReasons.Clear();//this is un-necessary please verify later

            string debuginfo = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}",

            timeutc530, strategyData.Decision.ToString(), strategyData.currentClose, percentage, strategyData.histdata,

            strategyData.BollingerUpperPercentage, strategyData.BollingerMiddlePercentage, strategyData.BollingerLowerPercentage,

            strategyData.SignalGap0, strategyData.SignalGap1, avoidReasons);

            File.AppendAllLines("debug.logs", new[] { debuginfo });

            File.AppendAllLines("Logs\\debug.txt", new[] { debuginfo });
        }
    }
}
