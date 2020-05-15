using System;

using System.Collections.Generic;

using System.Linq;

using PineScriptPort;

using BinanceBot.Domain;

using BinanceBot.Settings;

namespace BinanceBot.Strategy
{
    public class OpenCloseStrategy
    {
        #region -variables to calculate signal strength-
        private int BuyCounter;

        private int SellCounter;

        private StrategyOutput prevOutput;

        private int LatestSignalStrength;
        #endregion

        #region -variables of strategy configuration-

        private int KandleMultiplier; //3

        private int ExitSignalStrength; //15


        private bool EscapeTraps; //true
        private int EscapeTrapCandleIdx; //3
        private int EscapeTrapSignalStrength; //300


        private bool GrabMissedPosition; //true
        private int MissedPositionStartCandleIndex; //3
        private int MissedPositionEndCandleIndex; //5
        private int MissedPositionSignalStrength; //200

        private bool ExitImmediate;//false

        private decimal HeavyRiskPercentage;//15

        private string Smoothing;//DEMA

        #endregion

        #region -Validator Methods for Buy Sell Decision-
        private bool IsValidSignal(bool isBuy, bool isSell, int signalStrength, StrategyOutput currentState, ref StrategyOutput prevState)
        {
            LatestSignalStrength = signalStrength;

            if (prevState != currentState)
            {
                BuyCounter = 0;

                SellCounter = 0;

                prevState = currentState;
            }

            //simple logic of current and previous states match
            if (prevState == currentState && prevState != StrategyOutput.None)
            {
                if (isBuy && (currentState == StrategyOutput.OpenPositionWithBuy || currentState == StrategyOutput.BookProfitWithBuy))
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (isSell && (currentState == StrategyOutput.OpenPositionWithSell || currentState == StrategyOutput.BookProfitWithSell))
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (isBuy && currentState == StrategyOutput.ExitPositionWithBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (isSell && currentState == StrategyOutput.ExitPositionWithSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (ExitImmediate && currentState == StrategyOutput.ExitPositionWithBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (ExitImmediate && currentState == StrategyOutput.ExitPositionWithSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyOutput.EscapeTrapWithBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyOutput.EscapeTrapWithSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyOutput.MissedPositionBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyOutput.MissedPositionSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
            }

            return false;
        }

        private bool ExitPosition(SimplePosition position, StrategyData strategyData, decimal risk, int signalStrength)
        {
            if (position.PositionID == -1)
            {
                //no positions to exit from
                return false;
            }
            else if (position.PositionType == "BUY" && strategyData.longPercentage <= risk && IsValidSignal(strategyData.isBuy, strategyData.isSell, ExitSignalStrength, StrategyOutput.ExitPositionWithSell, ref prevOutput))//15
            {
                return true;
            }
            else if (position.PositionType == "SELL" && strategyData.shortPercentage <= risk && IsValidSignal(strategyData.isBuy, strategyData.isSell, ExitSignalStrength, StrategyOutput.ExitPositionWithBuy, ref prevOutput))//15
            {
                return true;
            }
            else if (position.PositionType == "BUY" && strategyData.isSell && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength / 2, StrategyOutput.ExitPositionWithSell, ref prevOutput))
            {
                return true;
            }
            else if (position.PositionType == "SELL" && strategyData.isBuy && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength / 2, StrategyOutput.ExitPositionWithBuy, ref prevOutput))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool OpenPosition(SimplePosition position, StrategyData strategyData, int signalStrength)
        {
            if (position.PositionID != -1)
            {
                return false;
            }

            if (strategyData.isBuy && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength, StrategyOutput.OpenPositionWithBuy, ref prevOutput))
            {



                return true;
            }

            if (strategyData.isSell && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength, StrategyOutput.OpenPositionWithSell, ref prevOutput))
            {
                return true;
            }

            return false;
        }

        private bool BookProfit(SimplePosition position, StrategyData strategyData, decimal reward)
        {
            if (position.PositionID == -1)
            {
                return false;
            }

            if (position.PositionType == "BUY" && strategyData.longPercentage >= strategyData.profitFactor * reward)
            {
                return true;
            }

            if (position.PositionType == "SELL" && strategyData.shortPercentage >= strategyData.profitFactor * reward)
            {
                return true;
            }

            if (position.PositionType == "BUY" && strategyData.isSell && strategyData.longPercentage >= reward / 2)
            {
                return true;
            }

            if (position.PositionType == "SELL" && strategyData.isBuy && strategyData.shortPercentage >= reward / 2)
            {
                return true;
            }

            return false;
        }

        private bool EscapeTrap(SimplePosition position, StrategyData strategyData)
        {
            //no open positions
            if (position.PositionID == -1)
            {
                return false;
            }

            //no historical decisions available
            if (string.IsNullOrEmpty(strategyData.histdata))
            {
                return false;
            }

            //in middle of decision
            if (strategyData.isBuy || strategyData.isSell)
            {
                return false;
            }

            //invalid historical data
            string decisiontype = "";
            int decisionperiod = -1;
            GetLatestDecision(strategyData.histdata, ref decisiontype, ref decisionperiod);
            if (string.IsNullOrEmpty(decisiontype) || decisionperiod == -1)
            {
                return false;
            }

            //the bot is trapped with sell position!!
            if (position.PositionType == "SELL" && decisiontype == "B" && decisionperiod >= EscapeTrapCandleIdx && IsValidSignal(false, false, EscapeTrapSignalStrength, StrategyOutput.EscapeTrapWithBuy, ref prevOutput))//3,300
            {
                return true;
            }

            //the bot is trapped with buy position
            if (position.PositionType == "BUY" && decisiontype == "S" && decisionperiod >= EscapeTrapCandleIdx && IsValidSignal(false, false, EscapeTrapSignalStrength, StrategyOutput.EscapeTrapWithSell, ref prevOutput))//3,300
            {
                return true;
            }

            return false;
        }

        private bool OpenMissedPosition(SimplePosition position, StrategyData strategyData)
        {
            //position already exists
            if (position.PositionID != -1)
            {
                return false;
            }

            //in middle of a decision
            if (strategyData.isBuy || strategyData.isSell)
            {
                return false;
            }

            //no historical data available
            if (string.IsNullOrEmpty(strategyData.histdata))
            {
                return false;
            }

            //invalid historical data
            string decisiontype = "";
            int decisionperiod = -1;
            GetLatestDecision(strategyData.histdata, ref decisiontype, ref decisionperiod);
            if (string.IsNullOrEmpty(decisiontype) || decisionperiod == -1)
            {
                return false;
            }

            //missed buy position
            if (decisiontype == "B" && decisionperiod >= MissedPositionStartCandleIndex && decisionperiod <= MissedPositionEndCandleIndex && IsValidSignal(false, false, MissedPositionSignalStrength, StrategyOutput.MissedPositionBuy, ref prevOutput))//3,5,200
            {
                return true;
            }

            //missed sell position
            if (decisiontype == "S" && decisionperiod >= MissedPositionStartCandleIndex && decisionperiod <= MissedPositionEndCandleIndex && IsValidSignal(false, false, MissedPositionSignalStrength, StrategyOutput.MissedPositionSell, ref prevOutput))//3,5,200
            {
                return true;
            }

            return false;
        }

        private bool ExitPositionHeavyLoss(SimplePosition position, StrategyData strategyData, decimal HeavyRiskPercentage)
        {
            //no position to exit from
            if (position.PositionID == -1)
            {
                return false;
            }

            if (position.PositionType == "BUY" && strategyData.longPercentage <= HeavyRiskPercentage)
            {
                return true;
            }

            if (position.PositionType == "SELL" && strategyData.shortPercentage <= HeavyRiskPercentage)
            {
                return true;
            }

            return false;
        }

        private void CalculatePercentageChange(SimplePosition order, decimal currentClose, decimal leverage, decimal decreaseOnNegative, ref StrategyData strategyData)
        {
            if (order.PositionID != -1)
            {
                strategyData.shortPercentage = leverage * ((order.EntryPrice - currentClose) / order.EntryPrice) * 100;

                strategyData.longPercentage = leverage * ((currentClose - order.EntryPrice) / order.EntryPrice) * 100;

                if (strategyData.shortPercentage < 0 && order.PositionType == "SELL")
                {
                    strategyData.profitFactor = decreaseOnNegative;
                }
                else if (strategyData.longPercentage < 0 && order.PositionType == "BUY")
                {
                    strategyData.profitFactor = decreaseOnNegative;
                }
                else
                {
                    //meh :\
                }
            }

        }

        /// <summary>
        /// Function to add bollinger bias to Buy Sell Decision while opening position
        /// </summary>
        /// <param name="tradingPrice"></param>
        /// <param name="bollingerUpper"></param>
        /// <param name="bollingerMiddle"></param>
        /// <param name="bollingerLower"></param>
        /// <returns></returns>
        private bool IsBollingerBuy(StrategyData strategyData, decimal tradingPrice, decimal profitPercentage)
        {
            //calculate percetage scope for buy with respect to upper bollinger Band
            var buyPercentageScope = ((strategyData.BollingerUpper - tradingPrice) / strategyData.BollingerUpper) * 100;

            if (buyPercentageScope >= (profitPercentage * strategyData.profitFactor))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Function to add bollinger bias to Buy Sell Decision while opening position
        /// </summary>
        /// <param name="tradingPrice"></param>
        /// <param name="bollingerUpper"></param>
        /// <param name="bollingerMiddle"></param>
        /// <param name="bollingerLower"></param>
        private bool IsBollingerSell(StrategyData strategyData, decimal tradingPrice, decimal profitPercentage)
        {
            //calculate percetage scope for buy with respect to upper bollinger Band
            var sellPercentageScope = ((tradingPrice - strategyData.BollingerLower) / tradingPrice) * 100;

            if (sellPercentageScope >= (profitPercentage * strategyData.profitFactor))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region -utility functions-
        private void ResetCounters()
        {
            BuyCounter = 0;

            SellCounter = 0;

            prevOutput = StrategyOutput.None;

            LatestSignalStrength = 0;
        }

        private void GetLatestDecision(string histdata, ref string decisiontype, ref int decisionperiod)
        {
            decisiontype = "";

            decisionperiod = -1;

            if (string.IsNullOrEmpty(histdata))
            {
                //no historical decisions available
                return;
            }

            var latestdecision = histdata.Split(' ').Last();

            if (!string.IsNullOrEmpty(latestdecision))
            {
                if (latestdecision.Contains("B"))
                {
                    decisiontype = "B";
                }
                else if (latestdecision.Contains("S"))
                {
                    decisiontype = "S";
                }
                else
                {
                    decisiontype = "";
                    decisionperiod = -1;
                }

                decisionperiod = Convert.ToInt32(latestdecision.Replace(decisiontype, ""));
            }
            else
            {
                decisiontype = "";
                decisionperiod = -1;
            }
        }
        #endregion

        public OpenCloseStrategy()
        {
            BuyCounter = 0;

            SellCounter = 0;

            prevOutput = StrategyOutput.None;

            //set strategy variables
            KandleMultiplier = OpenCloseStrategySettings.settings.KandleMultiplier;

            ExitSignalStrength = OpenCloseStrategySettings.settings.ExitSignalStrength;

            ExitImmediate = OpenCloseStrategySettings.settings.ExitImmediate;

            Smoothing = OpenCloseStrategySettings.settings.Smoothing;

            //set escape strategy variables
            EscapeTraps = OpenCloseStrategySettings.settings.EscapeTraps;

            EscapeTrapCandleIdx = OpenCloseStrategySettings.settings.EscapeTrapCandleIdx;

            EscapeTrapSignalStrength = OpenCloseStrategySettings.settings.EscapeTrapSignalStrength;

            //set missed position strategy variables
            GrabMissedPosition = OpenCloseStrategySettings.settings.GrabMissedPosition;

            MissedPositionStartCandleIndex = OpenCloseStrategySettings.settings.MissedPositionStartCandleIndex;

            MissedPositionEndCandleIndex = OpenCloseStrategySettings.settings.MissedPositionEndCandleIndex;

            MissedPositionSignalStrength = OpenCloseStrategySettings.settings.MissedPositionSignalStrength;

            HeavyRiskPercentage = OpenCloseStrategySettings.settings.HeavyRiskPercentage;
        }

        public void RunStrategy(List<OHLCKandle> inputkandles, RobotInput robotInput, ref StrategyData strategyData, ref SimplePosition currentPosition)
        {
            PineScriptFunction fn = new PineScriptFunction();

            //make a copy of original data
            var kcopy = inputkandles.Select(x => new OHLCKandle
            {
                Close = x.Close,
                CloseTime = x.CloseTime,
                Open = x.Open,
                OpenTime = x.OpenTime,
                High = x.High,
                Low = x.Low
            }).ToList();

            //convert to higher timeframe
            var largekandles = fn.converttohighertimeframe(inputkandles, KandleMultiplier);//3

            if (Smoothing.ToUpper() == "DEMA")
            {
                //higher timeframe candles with dema values
                largekandles = fn.dema(largekandles, 8);

                //lower timeframe candles with dema values
                inputkandles = fn.dema(inputkandles, 8);
            }
            else
            {
                //higher timeframe candles with smma values
                largekandles = fn.smma(largekandles, 8);

                //lower timeframe candles with smma values
                inputkandles = fn.smma(inputkandles, 8);
            }


            var closeseriesmma = inputkandles.Select(x => x.Close).ToList();


            //map higher timeframe values to lower timeframe values
            var altkandles = fn.superimposekandles(largekandles, inputkandles);

            var closeSeriesAlt = altkandles.Select(x => x.Close).ToList();

            var openSeriesAlt = altkandles.Select(x => x.Open).ToList();


            //trend and mood calculation-
            strategyData.trend = closeSeriesAlt.Last() > openSeriesAlt.Last() ? "BULLISH" : "BEARISH";

            strategyData.mood = closeseriesmma.Last() > openSeriesAlt.Last() ? "BULLISH" : "BEARISH";


            //buy sell signal
            var xlong = fn.crossover(closeSeriesAlt, openSeriesAlt);

            var xshort = fn.crossunder(closeSeriesAlt, openSeriesAlt);

            strategyData.isBuy = xlong.Last();

            strategyData.isSell = xshort.Last();


            //bollinger bands data
            var bollingerData = fn.bollinger(kcopy, 20);

            strategyData.BollingerUpper = bollingerData.Last().High;

            strategyData.BollingerMiddle = bollingerData.Last().Close;

            strategyData.BollingerLower = bollingerData.Last().Low;

            //historical data
            strategyData.histdata = "";
            for (int i = 0; i < xlong.Count; i++)
            {
                if (xlong[i])
                {
                    strategyData.histdata += " B" + (xlong.Count - i - 1).ToString();
                }
                else if (xshort[i])
                {
                    strategyData.histdata += " S" + (xlong.Count - i - 1).ToString();
                }
                else
                {
                    // meh :\
                }
            }

            MakeBuySellDecision(ref strategyData, ref currentPosition, robotInput);

            if (strategyData.Output != StrategyOutput.None)
            {
                ResetCounters();

                strategyData.profitFactor = 1m;
            }
        }

        private void MakeBuySellDecision(ref StrategyData strategyData, ref SimplePosition position, RobotInput roboInput)
        {
            var sOutput = StrategyOutput.None;

            CalculatePercentageChange(position, roboInput.currentClose, roboInput.leverage, roboInput.decreaseOnNegative, ref strategyData);

            if (OpenPosition(position, strategyData, roboInput.signalStrength))
            {
                if (strategyData.isBuy)
                {
                    if (IsBollingerBuy(strategyData, roboInput.currentClose, roboInput.reward))
                    {
                        sOutput = StrategyOutput.OpenPositionWithBuy;
                    }
                    else
                    {
                        sOutput = StrategyOutput.AvoidOpenWithBuy;
                    }
                }
                if (strategyData.isSell)
                {
                    if (IsBollingerSell(strategyData, roboInput.currentClose, roboInput.reward))
                    {
                        sOutput = StrategyOutput.OpenPositionWithSell;
                    }
                    else
                    {
                        sOutput = StrategyOutput.AvoidOpenWithSell;
                    }
                }
            }
            else if (ExitPositionHeavyLoss(position, strategyData, HeavyRiskPercentage))
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyOutput.ExitPositionHeavyLossWithSell;
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyOutput.ExitPositionHeavyLossWithBuy;
                }
            }
            else if (ExitPosition(position, strategyData, roboInput.risk, roboInput.signalStrength))
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyOutput.ExitPositionWithSell;
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyOutput.ExitPositionWithBuy;
                }
            }
            else if (BookProfit(position, strategyData, roboInput.reward))
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyOutput.BookProfitWithSell;
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyOutput.BookProfitWithBuy;
                }
            }
            else if (EscapeTrap(position, strategyData) && EscapeTraps)
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyOutput.EscapeTrapWithSell;
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyOutput.EscapeTrapWithBuy;
                }
            }
            else if (OpenMissedPosition(position, strategyData) && GrabMissedPosition)
            {
                string decisiontype = "";

                int decisionperiod = 0;

                GetLatestDecision(strategyData.histdata, ref decisiontype, ref decisionperiod);

                if (decisiontype == "B")
                {
                    if (IsBollingerBuy(strategyData, roboInput.currentClose, roboInput.reward))
                    {
                        sOutput = StrategyOutput.MissedPositionBuy;
                    }
                    else
                    {
                        sOutput = StrategyOutput.AvoidOpenWithBuy;
                    }
                }
                if (decisiontype == "S")
                {
                    if (IsBollingerSell(strategyData, roboInput.currentClose, roboInput.reward))
                    {
                        sOutput = StrategyOutput.MissedPositionSell;
                    }
                    else
                    {
                        sOutput = StrategyOutput.AvoidOpenWithSell;
                    }
                }
            }
            else
            {
                sOutput = StrategyOutput.None;
            }

            strategyData.prevOutput = prevOutput;

            strategyData.LatestSignalStrength = LatestSignalStrength;//improve this code laterz

            strategyData.BuyCounter = BuyCounter;

            strategyData.SellCounter = SellCounter;

            strategyData.Output = sOutput;
        }
    }
}