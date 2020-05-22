using System;
using System.Collections.Generic;

using System.Linq;

using System.Text;

using BinanceBot.Domain;

using BinanceBot.Validator;

using BinanceBot.Settings;

namespace BinanceBot.Strategy
{
    public class OpenCloseStrategyDecision
    {
        TradeValidator validator;

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

        #region -variables to calculate signal strength-
        private int BuyCounter;

        private int SellCounter;

        private StrategyDecision prevOutput;

        private int LatestSignalStrength;
        #endregion

        #region -Decision Methods for Buy Sell Opinion-
        private bool IsValidSignal(bool isBuy, bool isSell, int signalStrength, StrategyDecision currentState, ref StrategyDecision prevState)
        {
            LatestSignalStrength = signalStrength;

            if (prevState != currentState)
            {
                BuyCounter = 0;

                SellCounter = 0;

                prevState = currentState;
            }

            //simple logic of current and previous states match
            if (prevState == currentState && prevState != StrategyDecision.None)
            {
                if (isBuy && (currentState == StrategyDecision.OpenPositionWithBuy || currentState == StrategyDecision.BookProfitWithBuy))
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (isSell && (currentState == StrategyDecision.OpenPositionWithSell || currentState == StrategyDecision.BookProfitWithSell))
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (isBuy && currentState == StrategyDecision.ExitPositionWithBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (isSell && currentState == StrategyDecision.ExitPositionWithSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (ExitImmediate && currentState == StrategyDecision.ExitPositionWithBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (ExitImmediate && currentState == StrategyDecision.ExitPositionWithSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyDecision.EscapeTrapWithBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyDecision.EscapeTrapWithSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyDecision.MissedPositionBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyDecision.MissedPositionSell)
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

            if (strategyData.isBuy && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength, StrategyDecision.OpenPositionWithBuy, ref prevOutput))
            {
                return true;
            }

            if (strategyData.isSell && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength, StrategyDecision.OpenPositionWithSell, ref prevOutput))
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
            StrategyDecision signaldecision = StrategyDecision.None;

            int signalperiod = -1;

            GetSignalData(strategyData, ref signaldecision, ref signalperiod);


            if (signaldecision == StrategyDecision.None || signalperiod == -1)
            {
                return false;
            }

            //missed buy position
            if (signaldecision == StrategyDecision.Buy && signalperiod >= MissedPositionStartCandleIndex && signalperiod <= MissedPositionEndCandleIndex && IsValidSignal(false, false, MissedPositionSignalStrength, StrategyDecision.MissedPositionBuy, ref prevOutput))//3,5,200
            {
                return true;
            }

            //missed sell position
            if (signaldecision == StrategyDecision.Sell && signalperiod >= MissedPositionStartCandleIndex && signalperiod <= MissedPositionEndCandleIndex && IsValidSignal(false, false, MissedPositionSignalStrength, StrategyDecision.MissedPositionSell, ref prevOutput))//3,5,200
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
            else if (position.PositionType == "BUY" && strategyData.longPercentage <= risk && IsValidSignal(strategyData.isBuy, strategyData.isSell, ExitSignalStrength, StrategyDecision.ExitPositionWithSell, ref prevOutput))//15
            {
                return true;
            }
            else if (position.PositionType == "SELL" && strategyData.shortPercentage <= risk && IsValidSignal(strategyData.isBuy, strategyData.isSell, ExitSignalStrength, StrategyDecision.ExitPositionWithBuy, ref prevOutput))//15
            {
                return true;
            }
            else if (position.PositionType == "BUY" && strategyData.isSell && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength / 2, StrategyDecision.ExitPositionWithSell, ref prevOutput))
            {
                return true;
            }
            else if (position.PositionType == "SELL" && strategyData.isBuy && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength / 2, StrategyDecision.ExitPositionWithBuy, ref prevOutput))
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
            StrategyDecision signaldecision = StrategyDecision.None;

            int signalperiod = -1;

            GetSignalData(strategyData, ref signaldecision, ref signalperiod);

            if (signaldecision == StrategyDecision.None || signalperiod == -1)
            {
                return false;
            }

            //the bot is trapped with sell position!!
            if (position.PositionType == "SELL" && signaldecision == StrategyDecision.Buy && signalperiod >= EscapeTrapCandleIdx && IsValidSignal(false, false, EscapeTrapSignalStrength, StrategyDecision.EscapeTrapWithBuy, ref prevOutput))//3,300
            {
                return true;
            }

            //the bot is trapped with buy position
            if (position.PositionType == "BUY" && signaldecision == StrategyDecision.Sell && signalperiod >= EscapeTrapCandleIdx && IsValidSignal(false, false, EscapeTrapSignalStrength, StrategyDecision.EscapeTrapWithSell, ref prevOutput))//3,300
            {
                return true;
            }

            return false;
        }

        private void CalculatePercentageChange(SimplePosition position, RobotInput robotInput, ref StrategyData strategyData)
        {
            if (position.PositionID != -1)
            {
                strategyData.shortPercentage = robotInput.leverage * ((position.EntryPrice - strategyData.currentClose) / position.EntryPrice) * 100;

                strategyData.longPercentage = robotInput.leverage * ((strategyData.currentClose - position.EntryPrice) / position.EntryPrice) * 100;

                if (strategyData.shortPercentage < 0 && position.PositionType == "SELL")
                {
                    strategyData.profitFactor = robotInput.decreaseOnNegative;
                }
                else if (strategyData.longPercentage < 0 && position.PositionType == "BUY")
                {
                    strategyData.profitFactor = robotInput.decreaseOnNegative;
                }
                else
                {
                    //meh :\
                }
            }
        }

        //validations for the decisions made 
        private void ValidateOpenPosition(StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            if (decision == StrategyDecision.Buy)
            {
                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyDecision.Buy, StrategyDecision.Open))
                {
                    sOutput = StrategyDecision.AvoidOpenWithBuyOnRedKandle;
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, StrategyDecision.Buy))
                {
                    sOutput = StrategyDecision.AvoidOpenWithBuy;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyDecision.AvoidLowSignalGapBuy;
                }
            }
            if (decision == StrategyDecision.Sell)
            {
                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyDecision.Sell, StrategyDecision.Open))
                {
                    sOutput = StrategyDecision.AvoidOpenWithSellOnGreenKandle;
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, StrategyDecision.Sell))
                {
                    sOutput = StrategyDecision.AvoidOpenWithSell;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyDecision.AvoidLowSignalGapSell;
                }
            }
        }

        private void ValidateOpenMissedPosition(StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            if (decision == StrategyDecision.Buy)
            {
                if (!validator.KandlesAreConsistent(strategyData, StrategyDecision.Buy, 3))
                {
                    sOutput = StrategyDecision.AvoidBuyNoEntryPoint;
                }

                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyDecision.Buy, StrategyDecision.Open))
                {
                    sOutput = StrategyDecision.AvoidOpenWithBuyOnRedKandle;
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, StrategyDecision.Buy))
                {
                    sOutput = StrategyDecision.AvoidOpenWithBuy;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyDecision.AvoidLowSignalGapBuy;
                }
            }
            if (decision == StrategyDecision.Sell)
            {
                //validators

                if (!validator.KandlesAreConsistent(strategyData, StrategyDecision.Sell, 3))
                {
                    sOutput = StrategyDecision.AvoidSellNoEntryPoint;
                }

                if (!validator.IsTradeOnRightKandle(strategyData, StrategyDecision.Sell, StrategyDecision.Open))
                {
                    sOutput = StrategyDecision.AvoidOpenWithSellOnGreenKandle;
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, StrategyDecision.Sell))
                {
                    sOutput = StrategyDecision.AvoidOpenWithSell;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyDecision.AvoidLowSignalGapSell;
                }
            }
        }

        private void ValidateExitPositionHeavyLoss(StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            //this logic will be done later.
            return;
        }

        private void ValidateExitPosition(StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            if (decision == StrategyDecision.Sell)
            {
                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyDecision.AvoidLowSignalGapSell;
                }
            }
            if (decision == StrategyDecision.Buy)
            {
                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyDecision.AvoidLowSignalGapBuy;
                }
            }
        }

        private void ValidateBookProfit(StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            //this logic will be done later.
            return;
        }

        private void ValidateEscapeTrap(StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            if (decision == StrategyDecision.Sell)
            {
                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyDecision.Sell, StrategyDecision.Exit))
                {
                    sOutput = StrategyDecision.AvoidEscapeWithSell;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyDecision.AvoidLowSignalGapSell;
                }
            }
            if (decision == StrategyDecision.Buy)
            {
                //validators
                if (!validator.IsTradeOnRightKandle(strategyData, StrategyDecision.Buy, StrategyDecision.Exit))
                {
                    sOutput = StrategyDecision.AvoidEscapeWithBuy;
                }

                if (!validator.IsSignalGapValid(strategyData))
                {
                    sOutput = StrategyDecision.AvoidLowSignalGapBuy;
                }
            }
        }



        #endregion

        #region -Utility Functions-
        private void GetSignalData(StrategyData strategyData, ref StrategyDecision decision, ref int period)
        {
            var signaldata = Convert.ToString(strategyData.histdata.Split(' ').Last());

            if (signaldata.Contains("B"))
            {
                decision = StrategyDecision.Buy;
            }

            if (signaldata.Contains("S"))
            {
                decision = StrategyDecision.Sell;
            }

            period = Convert.ToInt32(signaldata.Replace("B", "").Replace("S", ""));
        }

        public void ResetCounters()
        {
            BuyCounter = 0;

            SellCounter = 0;

            prevOutput = StrategyDecision.None;

            LatestSignalStrength = 0;
        }
        #endregion

        public OpenCloseStrategyDecision()
        {
            BuyCounter = 0;

            SellCounter = 0;

            prevOutput = StrategyDecision.None;

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

        //method to take decision
        public void Decide(ref StrategyData strategyData, SimplePosition position, RobotInput roboInput)
        {
            var sOutput = StrategyDecision.None;

            CalculatePercentageChange(position, roboInput, ref strategyData);

            if (OpenPosition(position, strategyData, roboInput.signalStrength))
            {
                if (strategyData.isBuy)
                {
                    sOutput = StrategyDecision.OpenPositionWithBuy;

                    ValidateOpenPosition(strategyData, StrategyDecision.Buy, ref sOutput);
                }
                if (strategyData.isSell)
                {
                    sOutput = StrategyDecision.OpenPositionWithSell;

                    ValidateOpenPosition(strategyData, StrategyDecision.Sell, ref sOutput);
                }
            }

            else if (OpenMissedPosition(position, strategyData) && GrabMissedPosition)
            {
                StrategyDecision signaldecision = StrategyDecision.None;

                int signalperiod = 0;

                GetSignalData(strategyData, ref signaldecision, ref signalperiod);

                if (signaldecision == StrategyDecision.Buy)
                {
                    sOutput = StrategyDecision.MissedPositionBuy;

                    //validators
                    ValidateOpenMissedPosition(strategyData, StrategyDecision.Buy, ref sOutput);
                }
                if (signaldecision == StrategyDecision.Sell)
                {
                    sOutput = StrategyDecision.MissedPositionSell;

                    //validators
                    ValidateOpenMissedPosition(strategyData, StrategyDecision.Sell, ref sOutput);
                }
            }

            else if (ExitPositionHeavyLoss(position, strategyData, HeavyRiskPercentage))
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyDecision.ExitPositionHeavyLossWithSell;

                    ValidateExitPositionHeavyLoss(strategyData, StrategyDecision.Sell, ref sOutput);
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyDecision.ExitPositionHeavyLossWithBuy;

                    ValidateExitPositionHeavyLoss(strategyData, StrategyDecision.Buy, ref sOutput);
                }
            }

            else if (ExitPosition(position, strategyData, roboInput.risk, roboInput.signalStrength))
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyDecision.ExitPositionWithSell;

                    ValidateExitPosition(strategyData, StrategyDecision.Sell, ref sOutput);
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyDecision.ExitPositionWithBuy;

                    ValidateExitPosition(strategyData, StrategyDecision.Buy, ref sOutput);
                }
            }

            else if (BookProfit(position, strategyData, roboInput.reward))
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyDecision.BookProfitWithSell;

                    ValidateBookProfit(strategyData, StrategyDecision.Sell, ref sOutput);
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyDecision.BookProfitWithBuy;

                    ValidateBookProfit(strategyData, StrategyDecision.Buy, ref sOutput);
                }
            }

            else if (EscapeTrap(position, strategyData) && EscapeTraps)
            {
                if (position.PositionType == "BUY")
                {
                    sOutput = StrategyDecision.EscapeTrapWithSell;

                    //validators
                    ValidateEscapeTrap(strategyData, StrategyDecision.Sell, ref sOutput);
                }
                if (position.PositionType == "SELL")
                {
                    sOutput = StrategyDecision.EscapeTrapWithBuy;

                    ValidateEscapeTrap(strategyData, StrategyDecision.Buy, ref sOutput);
                }
            }

            else
            {
                sOutput = StrategyDecision.None;
            }

            strategyData.prevOutput = prevOutput;

            strategyData.LatestSignalStrength = LatestSignalStrength;//improve this code laterz

            strategyData.BuyCounter = BuyCounter;

            strategyData.SellCounter = SellCounter;

            strategyData.Decision = sOutput;
        }
    }
}
