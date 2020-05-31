using System;

using System.Linq;

using BinanceBot.Domain;

using BinanceBot.Validator;

using BinanceBot.Settings;

namespace BinanceBot.Strategy
{
    public class OpenCloseStrategyDecision
    {
        #region -variables of strategy configuration-
        //master validator class
        TradeValidator validator;

        //ruleset
        private string ValidationRuleSet;

        //exit vars
        private bool ExitImmediate;//false
        private decimal HeavyRiskPercentage;//15

        //escape vars
        private bool EscapeTraps; //true
        private int EscapeTrapCandleIdx; //3
        private int EscapeTrapSignalStrength; //300

        //missed position vars
        private bool GrabMissedPosition; //true
        private int MissedPositionStartCandleIndex; //3
        private int MissedPositionEndCandleIndex; //5

        private int ConsistentKandlesLookBack; //3

        //validation vars
        private decimal BollingerFactor;//1.1
        private int RequiredSignalGap;//4
        private int RequiredSignalQuality;//3


        private int OpenPositionSignalStrength;//50
        private int MissedPositionSignalStrength;//200
        private int ExitPositionHeavyLossSignalStrength;//0
        private int ExitSignalStrength;
        private int TakeProfitSignalStrength;
        #endregion

        #region -variables to calculate signal strength-
        private int BuyCounter;

        private int SellCounter;

        private StrategyDecision prevDecision;

        private StrategyDecision prevDecisionType;

        private int LatestSignalStrength;
        #endregion

        #region -Decision Methods-
        private bool OpenPosition(SimplePosition position, ref StrategyData strategyData)
        {
            if (position.PositionType != PositionType.None)
            {
                return false;
            }

            if (strategyData.isBuy)
            {
                strategyData.Decision = StrategyDecision.Open;
                strategyData.DecisionType = StrategyDecision.Buy;
                return true;
            }

            if (strategyData.isSell)
            {
                strategyData.Decision = StrategyDecision.Open;
                strategyData.DecisionType = StrategyDecision.Sell;
                return true;
            }

            return false;
        }

        private bool OpenMissedPosition(SimplePosition position, ref StrategyData strategyData)
        {
            //position already exists
            if (position.PositionType != PositionType.None)
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
            if (signaldecision == StrategyDecision.Buy && signalperiod >= MissedPositionStartCandleIndex && signalperiod <= MissedPositionEndCandleIndex)//3,5,200
            {
                strategyData.Decision = StrategyDecision.OpenMissed;
                strategyData.DecisionType = StrategyDecision.Buy;
                return true;
            }

            //missed sell position
            if (signaldecision == StrategyDecision.Sell && signalperiod >= MissedPositionStartCandleIndex && signalperiod <= MissedPositionEndCandleIndex)//3,5,200
            {
                strategyData.Decision = StrategyDecision.OpenMissed;
                strategyData.DecisionType = StrategyDecision.Sell;
                return true;
            }

            return false;
        }

        private bool ExitPositionHeavyLoss(SimplePosition position, ref StrategyData strategyData)
        {
            //no position to exit from
            if (position.PositionType == PositionType.None)
            {
                return false;
            }

            if (position.PositionType == PositionType.Buy && strategyData.Percentage <= HeavyRiskPercentage)
            {
                strategyData.Decision = StrategyDecision.ExitHeavy;
                strategyData.DecisionType = StrategyDecision.Sell;
                return true;
            }

            if (position.PositionType == PositionType.Sell && strategyData.Percentage <= HeavyRiskPercentage)
            {
                strategyData.Decision = StrategyDecision.ExitHeavy;
                strategyData.DecisionType = StrategyDecision.Buy;
                return true;
            }

            return false;
        }

        private bool ExitPosition(SimplePosition position, ref StrategyData strategyData, decimal risk)
        {
            if (position.PositionType == PositionType.None)
            {
                return false;
            }
            if (position.PositionType == PositionType.Buy && strategyData.Percentage <= risk)//15
            {
                strategyData.Decision = StrategyDecision.Exit;
                strategyData.DecisionType = StrategyDecision.Sell;
                return true;
            }
            if (position.PositionType == PositionType.Sell && strategyData.Percentage <= risk)//15
            {
                strategyData.Decision = StrategyDecision.Exit;
                strategyData.DecisionType = StrategyDecision.Buy;
                return true;
            }
            if (position.PositionType == PositionType.Buy && strategyData.isSell)
            {
                strategyData.Decision = StrategyDecision.Exit;
                strategyData.DecisionType = StrategyDecision.Sell;
                return true;
            }
            if (position.PositionType == PositionType.Sell && strategyData.isBuy)
            {
                strategyData.Decision = StrategyDecision.Exit;
                strategyData.DecisionType = StrategyDecision.Buy;
                return true;
            }
            return false;
        }

        private bool TakeProfit(SimplePosition position, ref StrategyData strategyData, decimal reward)
        {
            if (position.PositionType == PositionType.None)
            {
                return false;
            }

            if (position.PositionType == PositionType.Buy && strategyData.Percentage >= strategyData.profitFactor * reward)
            {
                strategyData.Decision = StrategyDecision.TakeProfit;
                strategyData.DecisionType = StrategyDecision.Sell;
                return true;
            }

            if (position.PositionType == PositionType.Sell && strategyData.Percentage >= strategyData.profitFactor * reward)
            {
                strategyData.Decision = StrategyDecision.TakeProfit;
                strategyData.DecisionType = StrategyDecision.Buy;
                return true;
            }

            if (position.PositionType == PositionType.Buy && strategyData.isSell && strategyData.Percentage >= reward / 2)
            {
                strategyData.Decision = StrategyDecision.TakeProfit;
                strategyData.DecisionType = StrategyDecision.Sell;
                return true;
            }

            if (position.PositionType == PositionType.Sell && strategyData.isBuy && strategyData.Percentage >= reward / 2)
            {
                strategyData.Decision = StrategyDecision.TakeProfit;
                strategyData.DecisionType = StrategyDecision.Buy;
                return true;
            }

            return false;
        }

        private bool EscapeTrap(SimplePosition position, ref StrategyData strategyData)
        {
            //no open positions
            if (position.PositionType == PositionType.None)
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
            if (position.PositionType == PositionType.Sell && signaldecision == StrategyDecision.Buy && signalperiod >= EscapeTrapCandleIdx)//3,300
            {
                strategyData.Decision = StrategyDecision.Escape;
                strategyData.DecisionType = StrategyDecision.Buy;
                return true;
            }

            //the bot is trapped with buy position
            if (position.PositionType == PositionType.Buy && signaldecision == StrategyDecision.Sell && signalperiod >= EscapeTrapCandleIdx)//3,300
            {
                strategyData.Decision = StrategyDecision.Escape;
                strategyData.DecisionType = StrategyDecision.Sell;
                return true;
            }

            return false;
        }
        #endregion

        #region -Decision Validator Methods-
        //validations for the decisions made 
        private bool IsDecisionStrong(int signalStrength, StrategyDecision Decision, StrategyDecision DecisionType)
        {
            LatestSignalStrength = signalStrength;

            //simple logic of current and previous states match
            if (prevDecision != Decision || prevDecisionType != DecisionType)
            {
                BuyCounter = 0;

                SellCounter = 0;

                prevDecision = Decision;

                prevDecisionType = DecisionType;
            }

            if (Decision == prevDecision && DecisionType == prevDecisionType && Decision != StrategyDecision.None && DecisionType != StrategyDecision.None)
            {
                if (Decision == StrategyDecision.Open || Decision == StrategyDecision.OpenMissed || Decision == StrategyDecision.TakeProfit ||
                    Decision == StrategyDecision.Exit || Decision == StrategyDecision.Escape || Decision == StrategyDecision.ExitHeavy)
                {
                    if (DecisionType == StrategyDecision.Buy)
                    {
                        ++BuyCounter;

                        return BuyCounter >= signalStrength;
                    }
                    if (DecisionType == StrategyDecision.Sell)
                    {
                        ++SellCounter;

                        return SellCounter >= signalStrength;
                    }
                }
            }
            return false;
        }

        private void ValidateOpenPosition(RobotInput roboInput, ref StrategyData strategyData)
        {
            if (strategyData.DecisionType == StrategyDecision.Buy || strategyData.DecisionType == StrategyDecision.Sell)
            {
                var decision = StrategyDecision.Open;

                var skipdecision = StrategyDecision.SkipOpen;

                var decisiontype = StrategyDecision.None;

                if (strategyData.DecisionType == StrategyDecision.Buy)
                {
                    decisiontype = StrategyDecision.Buy;
                }
                if (strategyData.DecisionType == StrategyDecision.Sell)
                {
                    decisiontype = StrategyDecision.Sell;
                }

                if (!validator.IsTradeOnRightKandle(strategyData, decisiontype, decision))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    if (decisiontype == StrategyDecision.Buy)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.RedKandle);
                    }
                    if (decisiontype == StrategyDecision.Sell)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.GreenKandle);
                    }
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, decisiontype, BollingerFactor, roboInput.reward))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    strategyData.AvoidReasons.Add(StrategyDecision.InvalidBollinger);
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalGap);
                }

                if (!validator.IsSignalGoodQuality(strategyData, decisiontype, RequiredSignalQuality))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalQuality);
                }
            }
        }

        private void ValidateOpenMissedPosition(RobotInput roboInput, ref StrategyData strategyData)
        {
            if (strategyData.DecisionType == StrategyDecision.Buy || strategyData.DecisionType == StrategyDecision.Sell)
            {
                var decision = StrategyDecision.OpenMissed;

                var skipdecision = StrategyDecision.SkipMissedOpen;

                var decisiontype = StrategyDecision.None;

                if (strategyData.DecisionType == StrategyDecision.Buy)
                {
                    decisiontype = StrategyDecision.Buy;
                }
                if (strategyData.DecisionType == StrategyDecision.Sell)
                {
                    decisiontype = StrategyDecision.Sell;
                }

                if (!validator.KandlesAreConsistent(strategyData, decisiontype, ConsistentKandlesLookBack))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    strategyData.AvoidReasons.Add(StrategyDecision.InconsistentKandles);
                }

                if (!validator.IsTradeOnRightKandle(strategyData, decisiontype, decision))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    if (decisiontype == StrategyDecision.Buy)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.RedKandle);
                    }
                    if (decisiontype == StrategyDecision.Sell)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.GreenKandle);
                    }
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, decisiontype, BollingerFactor, roboInput.reward))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    strategyData.AvoidReasons.Add(StrategyDecision.InvalidBollinger);
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalGap);
                }

                if (!validator.IsSignalGoodQuality(strategyData, decisiontype, RequiredSignalQuality))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalQuality);
                }
            }
        }

        private void ValidateExitPositionHeavyLoss(RobotInput roboInput, ref StrategyData strategyData)
        {
            //this logic will be done later.
            return;
        }

        private void ValidateExitPosition(RobotInput roboInput, ref StrategyData strategyData)
        {
            if (strategyData.DecisionType == StrategyDecision.Sell || strategyData.DecisionType == StrategyDecision.Buy)
            {
                var decision = StrategyDecision.Exit;

                var skipdecision = StrategyDecision.SkipExit;

                var decisiontype = StrategyDecision.None;

                if (strategyData.DecisionType == StrategyDecision.Buy)
                {
                    decisiontype = StrategyDecision.Buy;
                }
                if (strategyData.DecisionType == StrategyDecision.Sell)
                {
                    decisiontype = StrategyDecision.Sell;
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalGap);
                }
            }
        }

        private void ValidateTakeProfit(RobotInput roboInput, ref StrategyData strategyData)
        {
            //this logic will be done later.
            return;
        }

        private void ValidateEscapeTrap(RobotInput roboInput, ref StrategyData strategyData)
        {
            if (strategyData.DecisionType == StrategyDecision.Sell || strategyData.DecisionType == StrategyDecision.Buy)
            {
                var decision = StrategyDecision.Escape;

                var skipdecision = StrategyDecision.SkipEscape;

                var decisiontype = StrategyDecision.None;

                if (strategyData.DecisionType == StrategyDecision.Buy)
                {
                    decisiontype = StrategyDecision.Buy;
                }
                if (strategyData.DecisionType == StrategyDecision.Sell)
                {
                    decisiontype = StrategyDecision.Sell;
                }

                if (!validator.IsTradeOnRightKandle(strategyData, decisiontype, decision))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    if (decisiontype == StrategyDecision.Buy)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.RedKandle);
                    }
                    if (decisiontype == StrategyDecision.Sell)
                    {
                        strategyData.AvoidReasons.Add(StrategyDecision.GreenKandle);
                    }
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

                    strategyData.AvoidReasons.Add(StrategyDecision.LowSignalGap);
                }

                if (!validator.IsTradeMatchTrend(strategyData, decisiontype))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.AvoidReasons.Add(decisiontype);

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

        private void ResetDecision(ref StrategyData strategyData)
        {
            strategyData.Decision = StrategyDecision.None;

            strategyData.DecisionType = StrategyDecision.None;
        }

        public void ResetCounters()
        {
            BuyCounter = 0;

            SellCounter = 0;

            prevDecision = StrategyDecision.None;

            prevDecisionType = StrategyDecision.None;

            LatestSignalStrength = 0;
        }
        #endregion

        public OpenCloseStrategyDecision()
        {
            //decision strength variables
            BuyCounter = 0;
            SellCounter = 0;
            prevDecision = StrategyDecision.None;
            prevDecisionType = StrategyDecision.None;

            //missedposition decision variables
            GrabMissedPosition = OpenCloseStrategySettings.settings.GrabMissedPosition;
            MissedPositionStartCandleIndex = OpenCloseStrategySettings.settings.MissedPositionStartCandleIndex;
            MissedPositionEndCandleIndex = OpenCloseStrategySettings.settings.MissedPositionEndCandleIndex;

            //heavyloss decision variables
            ExitImmediate = OpenCloseStrategySettings.settings.ExitImmediate;
            HeavyRiskPercentage = OpenCloseStrategySettings.settings.HeavyRiskPercentage;

            //escape decision variables
            EscapeTraps = OpenCloseStrategySettings.settings.EscapeTraps;
            EscapeTrapCandleIdx = OpenCloseStrategySettings.settings.EscapeTrapCandleIdx;

            //set variables to avoid wrong trades (trade validation)
            RequiredSignalGap = OpenCloseStrategySettings.settings.SignalGap;
            BollingerFactor = OpenCloseStrategySettings.settings.BollingerFactor;
            RequiredSignalQuality = OpenCloseStrategySettings.settings.RequiredSignalQuality;
            ConsistentKandlesLookBack = OpenCloseStrategySettings.settings.ConsistentKandlesLookBack;
            ValidationRuleSet = OpenCloseStrategySettings.settings.ValidationRuleSet; //ruleset
            validator = new TradeValidator(ValidationRuleSet);

            //decision signal strengths
            OpenPositionSignalStrength = OpenCloseStrategySettings.settings.OpenPositionSignalStrength;
            MissedPositionSignalStrength = OpenCloseStrategySettings.settings.MissedPositionSignalStrength;
            ExitPositionHeavyLossSignalStrength = OpenCloseStrategySettings.settings.ExitPositionHeavyLossSignalStrength;
            ExitSignalStrength = OpenCloseStrategySettings.settings.ExitSignalStrength;
            TakeProfitSignalStrength = OpenCloseStrategySettings.settings.TakeProfitSignalStrength;
            EscapeTrapSignalStrength = OpenCloseStrategySettings.settings.EscapeTrapSignalStrength;
        }

        //method to take decision
        public void Decide(ref StrategyData strategyData, SimplePosition position, RobotInput roboInput)
        {
            if (OpenPosition(position, ref strategyData))
            {
                if (IsDecisionStrong(OpenPositionSignalStrength, strategyData.Decision, strategyData.DecisionType))
                {
                    ValidateOpenPosition(roboInput, ref strategyData);
                }
                else
                {
                    ResetDecision(ref strategyData);
                }
            }

            else if (OpenMissedPosition(position, ref strategyData) && GrabMissedPosition)
            {
                if (IsDecisionStrong(MissedPositionSignalStrength, strategyData.Decision, strategyData.DecisionType))
                {
                    ValidateOpenMissedPosition(roboInput, ref strategyData);
                }
                else
                {
                    ResetDecision(ref strategyData);
                }
            }

            else if (ExitPositionHeavyLoss(position, ref strategyData))
            {
                if (IsDecisionStrong(ExitPositionHeavyLossSignalStrength, strategyData.Decision, strategyData.DecisionType))
                {
                    ValidateExitPositionHeavyLoss(roboInput, ref strategyData);
                }
                else
                {
                    ResetDecision(ref strategyData);
                }
            }

            else if (ExitPosition(position, ref strategyData, roboInput.risk))
            {
                if (IsDecisionStrong(ExitSignalStrength, strategyData.Decision, strategyData.DecisionType))
                {
                    ValidateExitPosition(roboInput, ref strategyData);
                }
                else
                {
                    ResetDecision(ref strategyData);
                }
            }

            else if (TakeProfit(position, ref strategyData, roboInput.reward))
            {
                if (IsDecisionStrong(TakeProfitSignalStrength, strategyData.Decision, strategyData.DecisionType))
                {
                    ValidateTakeProfit(roboInput, ref strategyData);
                }
                else
                {
                    ResetDecision(ref strategyData);
                }
            }

            else if (EscapeTrap(position, ref strategyData) && EscapeTraps)
            {
                if (IsDecisionStrong(EscapeTrapSignalStrength, strategyData.Decision, strategyData.DecisionType))
                {
                    ValidateEscapeTrap(roboInput, ref strategyData);
                }
                else
                {
                    ResetDecision(ref strategyData);
                }
            }

            else
            {
                ResetDecision(ref strategyData);
            }

            strategyData.LatestSignalStrength = LatestSignalStrength;//improve this code laterz

            strategyData.BuyCounter = BuyCounter;

            strategyData.SellCounter = SellCounter;

            strategyData.PrevDecision = prevDecision;

            strategyData.PrevDecisionType = prevDecisionType;

        }
    }
}
