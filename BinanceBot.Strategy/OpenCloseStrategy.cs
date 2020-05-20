using System;

using System.Collections.Generic;

using System.Linq;

using PineScriptPort;

using BinanceBot.Domain;

using BinanceBot.Settings;

using BinanceBot.Validator;

namespace BinanceBot.Strategy
{
    public class OpenCloseStrategy
    {
        TradeValidator validator;

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

        private decimal BollingerFactor;//1.1

        private int RequiredSignalGap;//4

        #endregion

        #region -Decision Methods for Buy Sell Opinion-
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

        private void CalculatePercentageChange(SimplePosition order, RobotInput robotInput, ref StrategyData strategyData)
        {
            if (order.PositionID != -1)
            {
                strategyData.shortPercentage = robotInput.leverage * ((order.EntryPrice - strategyData.currentClose) / order.EntryPrice) * 100;

                strategyData.longPercentage = robotInput.leverage * ((strategyData.currentClose - order.EntryPrice) / order.EntryPrice) * 100;

                if (strategyData.shortPercentage < 0 && order.PositionType == "SELL")
                {
                    strategyData.profitFactor = robotInput.decreaseOnNegative;
                }
                else if (strategyData.longPercentage < 0 && order.PositionType == "BUY")
                {
                    strategyData.profitFactor = robotInput.decreaseOnNegative;
                }
                else
                {
                    //meh :\
                }
            }
        }


        private void ValidateOpenPosition(StrategyData strategyData, StrategyOutput decision ,ref StrategyOutput sOutput)
        {
            if (decision == StrategyOutput.Buy)
            {
                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyOutput.Buy, StrategyOutput.Open))
                {
                    sOutput = StrategyOutput.AvoidOpenWithBuyOnRedKandle;
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, StrategyOutput.Buy))
                {
                    sOutput = StrategyOutput.AvoidOpenWithBuy;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyOutput.AvoidLowSignalGapBuy;
                }
            }
            if (decision == StrategyOutput.Sell)
            {
                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyOutput.Sell, StrategyOutput.Open))
                {
                    sOutput = StrategyOutput.AvoidOpenWithSellOnGreenKandle;
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, StrategyOutput.Sell))
                {
                    sOutput = StrategyOutput.AvoidOpenWithSell;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyOutput.AvoidLowSignalGapSell;
                }
            }
        }

        private void ValidateOpenMissedPosition(StrategyData strategyData,StrategyOutput decision,ref StrategyOutput sOutput)
        {
            if (decision == StrategyOutput.Buy)
            {
                if (!validator.KandlesAreConsistent(strategyData,StrategyOutput.Buy,3))
                {
                    sOutput = StrategyOutput.AvoidBuyNoEntryPoint;
                }

                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyOutput.Buy, StrategyOutput.Open))
                {
                    sOutput = StrategyOutput.AvoidOpenWithBuyOnRedKandle;
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, StrategyOutput.Buy))
                {
                    sOutput = StrategyOutput.AvoidOpenWithBuy;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyOutput.AvoidLowSignalGapBuy;
                }
            }
            if (decision == StrategyOutput.Sell)
            {
                //validators

                if (!validator.KandlesAreConsistent(strategyData, StrategyOutput.Sell, 3))
                {
                    sOutput = StrategyOutput.AvoidSellNoEntryPoint;
                }

                if (!validator.IsTradeOnRightKandle(strategyData, StrategyOutput.Sell, StrategyOutput.Open))
                {
                    sOutput = StrategyOutput.AvoidOpenWithSellOnGreenKandle;
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, StrategyOutput.Sell))
                {
                    sOutput = StrategyOutput.AvoidOpenWithSell;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyOutput.AvoidLowSignalGapSell;
                }
            }
        }

        private void ValidateExitPositionHeavyLoss(StrategyData strategyData, StrategyOutput decision, ref StrategyOutput sOutput)
        {
            //this logic will be done later.
            return;
        }

        private void ValidateExitPosition(StrategyData strategyData, StrategyOutput decision, ref StrategyOutput sOutput)
        {
            if (decision == StrategyOutput.Sell)
            {
                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyOutput.AvoidLowSignalGapSell;
                }
            }
            if (decision == StrategyOutput.Buy)
            {
                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyOutput.AvoidLowSignalGapBuy;
                }
            }
        }

        private void ValidateBookProfit(StrategyData strategyData, StrategyOutput decision, ref StrategyOutput sOutput)
        {

        }

        private void ValidateEscapeTrap(StrategyData strategyData, StrategyOutput decision, ref StrategyOutput sOutput)
        {
            if (decision == StrategyOutput.Sell)
            {
                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyOutput.Sell, StrategyOutput.Exit))
                {
                    sOutput = StrategyOutput.AvoidEscapeWithSell;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyOutput.AvoidLowSignalGapSell;
                }
            }
            if (decision == StrategyOutput.Buy)
            {
                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyOutput.Buy, StrategyOutput.Exit))
                {
                    sOutput = StrategyOutput.AvoidEscapeWithBuy;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyOutput.AvoidLowSignalGapBuy;
                }
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

        private void UpdateSignalData(ref StrategyData strategyData, List<bool> xlong, List<bool> xshort)
        {
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

            var histDataSplit = Convert.ToString(strategyData.histdata).Split(' ');

            var histDataInt = histDataSplit.Skip(histDataSplit.Length - 3).Take(3);

            var val0 = Convert.ToInt32(histDataInt.ElementAt(0).Replace("B", "").Replace("S", ""));

            var val1 = Convert.ToInt32(histDataInt.ElementAt(1).Replace("B", "").Replace("S", ""));

            var val2 = Convert.ToInt32(histDataInt.ElementAt(2).Replace("B", "").Replace("S", ""));

            strategyData.SignalGap0 = val0 - val1;

            strategyData.SignalGap1 = val1 - val2;
        }

        private List<OHLCKandle> GetSmoothData(List<OHLCKandle> kandles,int lookback,string smoothing)
        {
            PineScriptFunction fn = new PineScriptFunction();

            if (smoothing.ToUpper() == "DEMA")
            {
                kandles = fn.dema(kandles, 8);
            }
            else
            {
                kandles = fn.smma(kandles, 8);   
            }

            return kandles;
        }

        #endregion

        #region -indicator functions-

        private void UpdateBollingerData(ref StrategyData strategyData)
        {
            PineScriptFunction fn = new PineScriptFunction();

            //make a copy of original data
            var kcopy = strategyData.kandles.Select(x => new OHLCKandle
            {
                Close = x.Close,
                CloseTime = x.CloseTime,
                Open = x.Open,
                OpenTime = x.OpenTime,
                High = x.High,
                Low = x.Low
            }).ToList();

            var kcopyopenseries = kcopy.Select(x => (decimal)x.Open).ToList();

            var kcopycloseseries = kcopy.Select(x => (decimal)x.Close).ToList();

            ////start bollinger bands data
            var bollingerData = fn.bollinger(kcopy, 20);

            strategyData.BollingerUpper = bollingerData.Last().High;

            strategyData.BollingerMiddle = bollingerData.Last().Close;

            strategyData.BollingerLower = bollingerData.Last().Low;

            var pricecrossunder = fn.crossunder(kcopyopenseries, bollingerData.Select(x => x.High).ToList());

            var pricecrossover = fn.crossover(kcopycloseseries, bollingerData.Select(x => x.Low).ToList());

            strategyData.BollTopCrossed = pricecrossunder.Skip(pricecrossunder.Count - 7).Take(7).Contains(true);

            strategyData.BollBottomCrossed = pricecrossover.Skip(pricecrossover.Count - 7).Take(7).Contains(true);
            //end bollinger bands data
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

            //set variables to avoid wrong trades
            BollingerFactor = OpenCloseStrategySettings.settings.BollingerFactor;

            RequiredSignalGap = OpenCloseStrategySettings.settings.SignalGap;

            validator = new TradeValidator();
        }

        public void RunStrategy(List<OHLCKandle> inputkandles, RobotInput robotInput, ref StrategyData strategyData, ref SimplePosition currentPosition)
        {
            PineScriptFunction fn = new PineScriptFunction();

            //convert to higher timeframe
            var largekandles = fn.converttohighertimeframe(inputkandles, KandleMultiplier);//3

            //higher timeframe candles with smoothened values
            largekandles = GetSmoothData(largekandles, 8, Smoothing);

            //lower timeframe candles with smoothened values
            inputkandles = GetSmoothData(inputkandles, 8, Smoothing);

            var inputcloseseriesmoothed = inputkandles.Select(x => x.Close).ToList();

            //map higher timeframe values to lower timeframe values
            var altkandles = fn.superimposekandles(largekandles, inputkandles);

            var closeSeriesAlt = altkandles.Select(x => x.Close).ToList();

            var openSeriesAlt = altkandles.Select(x => x.Open).ToList();
            //end map higher timeframe values to lower timeframe values

            //trend and mood calculation-
            strategyData.trend = closeSeriesAlt.Last() > openSeriesAlt.Last() ? "BULLISH" : "BEARISH";

            strategyData.mood = inputcloseseriesmoothed.Last() > openSeriesAlt.Last() ? "BULLISH" : "BEARISH";

            //start buy sell signal
            var xlong = fn.crossover(closeSeriesAlt, openSeriesAlt);

            var xshort = fn.crossunder(closeSeriesAlt, openSeriesAlt);

            strategyData.isBuy = xlong.Last();

            strategyData.isSell = xshort.Last();
            //end buy sell signal

            UpdateBollingerData(ref strategyData);

            UpdateSignalData(ref strategyData, xlong, xshort);

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

            CalculatePercentageChange(position, roboInput, ref strategyData);

            if (OpenPosition(position, strategyData, roboInput.signalStrength))
            {
                if (strategyData.isBuy)
                {
                    sOutput = StrategyOutput.OpenPositionWithBuy;

                    ValidateOpenPosition(strategyData, StrategyOutput.Buy, ref sOutput);
                }
                if (strategyData.isSell)
                {
                    sOutput = StrategyOutput.OpenPositionWithSell;

                    ValidateOpenPosition(strategyData, StrategyOutput.Sell, ref sOutput);
                }
            }

            else if (OpenMissedPosition(position, strategyData) && GrabMissedPosition)
            {
                string decisiontype = "";

                int decisionperiod = 0;

                GetLatestDecision(strategyData.histdata, ref decisiontype, ref decisionperiod);

                if (decisiontype == "B")
                {
                    sOutput = StrategyOutput.MissedPositionBuy;

                    //validators
                    ValidateOpenMissedPosition(strategyData, StrategyOutput.Buy, ref sOutput);
                }
                if (decisiontype == "S")
                {
                    sOutput = StrategyOutput.MissedPositionSell;

                    //validators
                    ValidateOpenMissedPosition(strategyData, StrategyOutput.Sell, ref sOutput);
                }
            }

            else if (ExitPositionHeavyLoss(position, strategyData, HeavyRiskPercentage))
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyOutput.ExitPositionHeavyLossWithSell;

                    ValidateExitPositionHeavyLoss(strategyData, StrategyOutput.Sell, ref sOutput);
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyOutput.ExitPositionHeavyLossWithBuy;

                    ValidateExitPositionHeavyLoss(strategyData, StrategyOutput.Buy, ref sOutput);
                }
            }

            else if (ExitPosition(position, strategyData, roboInput.risk, roboInput.signalStrength))
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyOutput.ExitPositionWithSell;

                    ValidateExitPosition(strategyData, StrategyOutput.Sell, ref sOutput);
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyOutput.ExitPositionWithBuy;

                    ValidateExitPosition(strategyData, StrategyOutput.Buy, ref sOutput);
                }
            }

            else if (BookProfit(position, strategyData, roboInput.reward))
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyOutput.BookProfitWithSell;

                    ValidateBookProfit(strategyData, StrategyOutput.Sell, ref sOutput);
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyOutput.BookProfitWithBuy;

                    ValidateBookProfit(strategyData, StrategyOutput.Buy, ref sOutput);
                }
            }

            else if (EscapeTrap(position, strategyData) && EscapeTraps)
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyOutput.EscapeTrapWithSell;

                    //validators
                    ValidateEscapeTrap(strategyData, StrategyOutput.Sell, ref sOutput);
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyOutput.EscapeTrapWithBuy;

                    ValidateEscapeTrap(strategyData, StrategyOutput.Buy, ref sOutput);
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