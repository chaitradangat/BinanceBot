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
        //ruleset
        private string ValidationRuleSet;

        //exit vars
        private bool ExitImmediate;//false
        private int ExitSignalStrength; //15
        private decimal HeavyRiskPercentage;//15

        //escape vars
        private bool EscapeTraps; //true
        private int EscapeTrapCandleIdx; //3
        private int EscapeTrapSignalStrength; //300

        //missed position vars
        private bool GrabMissedPosition; //true
        private int MissedPositionStartCandleIndex; //3
        private int MissedPositionEndCandleIndex; //5
        private int MissedPositionSignalStrength; //200
        private int ConsistentKandlesLookBack; //3

        //validation vars
        private decimal BollingerFactor;//1.1
        private int RequiredSignalGap;//4
        private int RequiredSignalQuality;//3


        #endregion

        #region -variables to calculate signal strength-
        private int BuyCounter;

        private int SellCounter;

        private StrategyDecision prevDecision;

        private StrategyDecision prevDecisionType;

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

            if (strategyData.isBuy && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength, StrategyDecision.OpenPositionWithBuy, ref prevDecision))
            {
                return true;
            }

            if (strategyData.isSell && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength, StrategyDecision.OpenPositionWithSell, ref prevDecision))
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
            if (signaldecision == StrategyDecision.Buy && signalperiod >= MissedPositionStartCandleIndex && signalperiod <= MissedPositionEndCandleIndex && IsValidSignal(false, false, MissedPositionSignalStrength, StrategyDecision.MissedPositionBuy, ref prevDecision))//3,5,200
            {
                return true;
            }

            //missed sell position
            if (signaldecision == StrategyDecision.Sell && signalperiod >= MissedPositionStartCandleIndex && signalperiod <= MissedPositionEndCandleIndex && IsValidSignal(false, false, MissedPositionSignalStrength, StrategyDecision.MissedPositionSell, ref prevDecision))//3,5,200
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
            else if (position.PositionType == "BUY" && strategyData.longPercentage <= risk && IsValidSignal(strategyData.isBuy, strategyData.isSell, ExitSignalStrength, StrategyDecision.ExitPositionWithSell, ref prevDecision))//15
            {
                return true;
            }
            else if (position.PositionType == "SELL" && strategyData.shortPercentage <= risk && IsValidSignal(strategyData.isBuy, strategyData.isSell, ExitSignalStrength, StrategyDecision.ExitPositionWithBuy, ref prevDecision))//15
            {
                return true;
            }
            else if (position.PositionType == "BUY" && strategyData.isSell && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength / 2, StrategyDecision.ExitPositionWithSell, ref prevDecision))
            {
                return true;
            }
            else if (position.PositionType == "SELL" && strategyData.isBuy && IsValidSignal(strategyData.isBuy, strategyData.isSell, signalStrength / 2, StrategyDecision.ExitPositionWithBuy, ref prevDecision))
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
            if (position.PositionType == "SELL" && signaldecision == StrategyDecision.Buy && signalperiod >= EscapeTrapCandleIdx && IsValidSignal(false, false, EscapeTrapSignalStrength, StrategyDecision.EscapeTrapWithBuy, ref prevDecision))//3,300
            {
                return true;
            }

            //the bot is trapped with buy position
            if (position.PositionType == "BUY" && signaldecision == StrategyDecision.Sell && signalperiod >= EscapeTrapCandleIdx && IsValidSignal(false, false, EscapeTrapSignalStrength, StrategyDecision.EscapeTrapWithSell, ref prevDecision))//3,300
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

        #endregion

        #region -Validator Methods-
        //validations for the decisions made 
        private void ValidateOpenPosition(RobotInput roboInput, ref StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            if (decision == StrategyDecision.Buy || decision == StrategyDecision.Sell)
            {
                if (!validator.IsTradeOnRightKandle(strategyData, decision, StrategyDecision.Open))
                {
                    sOutput = StrategyDecision.SkipOpen;

                    strategyData.AvoidReasons.Add(decision);

                    if (decision == StrategyDecision.Buy)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.RedKandle);
                    }
                    if (decision == StrategyDecision.Sell)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.GreenKandle);
                    }
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, decision, BollingerFactor, roboInput.reward))
                {
                    sOutput = StrategyDecision.SkipOpen;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.InvalidBollinger);
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    sOutput = StrategyDecision.SkipOpen;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalGap);
                }

                if (!validator.IsSignalGoodQuality(strategyData, decision, RequiredSignalQuality))
                {
                    sOutput = StrategyDecision.SkipOpen;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalQuality);
                }
            }
        }

        private void ValidateOpenMissedPosition(RobotInput roboInput, ref StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            if (decision == StrategyDecision.Buy || decision == StrategyDecision.Sell)
            {
                if (!validator.KandlesAreConsistent(strategyData, decision, ConsistentKandlesLookBack))
                {
                    sOutput = StrategyDecision.SkipMissedOpen;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.InconsistentKandles);
                }

                if (!validator.IsTradeOnRightKandle(strategyData, decision, StrategyDecision.Open))
                {
                    sOutput = StrategyDecision.SkipMissedOpen;

                    strategyData.AvoidReasons.Add(decision);

                    if (decision == StrategyDecision.Buy)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.RedKandle);
                    }
                    if (decision == StrategyDecision.Sell)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.GreenKandle);
                    }
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, decision, BollingerFactor, roboInput.reward))
                {
                    sOutput = StrategyDecision.SkipMissedOpen;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.InvalidBollinger);
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    sOutput = StrategyDecision.SkipMissedOpen;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalGap);
                }

                if (!validator.IsSignalGoodQuality(strategyData, decision, RequiredSignalQuality))
                {
                    sOutput = StrategyDecision.SkipMissedOpen;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalQuality);
                }
            }
        }

        private void ValidateExitPositionHeavyLoss(RobotInput roboInput, ref StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            //this logic will be done later.
            return;
        }

        private void ValidateExitPosition(RobotInput roboInput, ref StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            if (decision == StrategyDecision.Sell || decision == StrategyDecision.Buy)
            {
                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    sOutput = StrategyDecision.SkipExit;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalGap);
                }
            }
        }

        private void ValidateBookProfit(RobotInput roboInput, ref StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            //this logic will be done later.
            return;
        }

        private void ValidateEscapeTrap(RobotInput roboInput, ref StrategyData strategyData, StrategyDecision decision, ref StrategyDecision sOutput)
        {
            if (decision == StrategyDecision.Sell || decision == StrategyDecision.Buy)
            {
                if (!validator.IsTradeOnRightKandle(strategyData, decision, StrategyDecision.Open))
                {
                    sOutput = StrategyDecision.SkipEscape;

                    strategyData.AvoidReasons.Add(decision);

                    if (decision == StrategyDecision.Buy)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.RedKandle);
                    }
                    if (decision == StrategyDecision.Sell)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.GreenKandle);
                    }
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    sOutput = StrategyDecision.SkipEscape;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalGap);
                }

                if (!validator.IsTradeMatchTrend(strategyData, decision))
                {
                    sOutput = StrategyDecision.SkipEscape;

                    strategyData.AvoidReasons.Add(decision);

                    strategyData.AvoidReasons.Add(StrategyDecision.AgainstTrend);
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

        private void GetDecisionType(StrategyData strategyData, ref StrategyDecision decisionType)
        {
            if (strategyData!=null)
            {
                if (strategyData.isBuy)
                {
                    decisionType = StrategyDecision.Buy;
                }

                if (strategyData.isSell)
                {
                    decisionType = StrategyDecision.Sell;
                }
            }
        }

        private void GetDecisionType(SimplePosition position,ref StrategyDecision decisionType)
        {
            if (position != null)
            {
                if (position.PositionType == "BUY")
                {
                    decisionType = StrategyDecision.Sell;
                }

                if (position.PositionType == "SELL")
                {
                    decisionType = StrategyDecision.Buy;
                }
            }
        }

        public void ResetCounters()
        {
            BuyCounter = 0;

            SellCounter = 0;

            prevDecision = StrategyDecision.None;

            LatestSignalStrength = 0;
        }
        #endregion

        public OpenCloseStrategyDecision()
        {
            BuyCounter = 0;

            SellCounter = 0;

            prevDecision = StrategyDecision.None;

            //ruleset
            ValidationRuleSet = OpenCloseStrategySettings.settings.ValidationRuleSet;

            //set strategy variables
            ExitSignalStrength = OpenCloseStrategySettings.settings.ExitSignalStrength;

            ExitImmediate = OpenCloseStrategySettings.settings.ExitImmediate;

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

            //set variables to avoid wrong trades (trade validation)
            BollingerFactor = OpenCloseStrategySettings.settings.BollingerFactor;

            RequiredSignalGap = OpenCloseStrategySettings.settings.SignalGap;

            ConsistentKandlesLookBack = OpenCloseStrategySettings.settings.ConsistentKandlesLookBack;

            validator = new TradeValidator(ValidationRuleSet);

            RequiredSignalQuality = OpenCloseStrategySettings.settings.RequiredSignalQuality;
        }

        //method to take decision
        public void Decide(ref StrategyData strategyData, SimplePosition position, RobotInput roboInput)
        {
            var decision = StrategyDecision.None;

            var decisionType = StrategyDecision.None;

            CalculatePercentageChange(position, roboInput, ref strategyData);

            if (OpenPosition(position, strategyData, roboInput.signalStrength))
            {
                GetDecisionType(strategyData, ref decisionType);

                if (decisionType != StrategyDecision.None)
                {
                    decision = StrategyDecision.Open;

                    ValidateOpenPosition(roboInput, ref strategyData, decisionType, ref decision);
                }
            }

            else if (OpenMissedPosition(position, strategyData) && GrabMissedPosition)
            {
                int signalperiod = 0;

                GetSignalData(strategyData, ref decisionType, ref signalperiod);

                if (decisionType != StrategyDecision.None)
                {
                    decision = StrategyDecision.OpenMissed;

                    ValidateOpenMissedPosition(roboInput, ref strategyData, decisionType, ref decision);
                }
            }

            else if (ExitPositionHeavyLoss(position, strategyData, HeavyRiskPercentage))
            {
                GetDecisionType(position, ref decisionType);

                if (decisionType != StrategyDecision.None)
                {
                    decision = StrategyDecision.ExitHeavy;

                    ValidateExitPositionHeavyLoss(roboInput, ref strategyData, decisionType, ref decision);
                }
            }

            else if (ExitPosition(position, strategyData, roboInput.risk, roboInput.signalStrength))
            {
                GetDecisionType(position, ref decisionType);

                if (decisionType != StrategyDecision.None)
                {
                    decision = StrategyDecision.Exit;

                    ValidateExitPosition(roboInput, ref strategyData, decisionType, ref decision);
                }
            }

            else if (BookProfit(position, strategyData, roboInput.reward))
            {
                GetDecisionType(position, ref decisionType);

                if (decisionType != StrategyDecision.None)
                {
                    decision = StrategyDecision.TakeProfit;

                    ValidateBookProfit(roboInput, ref strategyData, decisionType, ref decision);
                }
            }

            else if (EscapeTrap(position, strategyData) && EscapeTraps)
            {
                GetDecisionType(position, ref decisionType);

                if (decisionType != StrategyDecision.None)
                {
                    decision = StrategyDecision.Escape;

                    ValidateEscapeTrap(roboInput, ref strategyData, decisionType, ref decision);
                }
            }

            else
            {
                decision = StrategyDecision.None;

                decisionType = StrategyDecision.None;
            }

            strategyData.prevOutput = prevDecision;

            strategyData.LatestSignalStrength = LatestSignalStrength;//improve this code laterz

            strategyData.BuyCounter = BuyCounter;

            strategyData.SellCounter = SellCounter;

            strategyData.Decision = decision;

            strategyData.DecisionType = decisionType;
        }
    }
}
