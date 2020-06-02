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

        //common method to route all the validations
        private void ValidateDecision(RobotInput robotInput, ref StrategyData strategyData, int signalStrength)
        {
            if (IsDecisionStrong(signalStrength, strategyData.Decision, strategyData.DecisionType))
            {
                if (strategyData.Decision == StrategyDecision.Open)
                {
                    ValidateOpenPosition(robotInput, ref strategyData);
                }
                if (strategyData.Decision == StrategyDecision.OpenMissed)
                {
                    ValidateOpenMissedPosition(robotInput, ref strategyData);
                }
                if (strategyData.Decision == StrategyDecision.ExitHeavy)
                {
                    ValidateExitPositionHeavyLoss(robotInput, ref strategyData);
                }
                if (strategyData.Decision == StrategyDecision.Exit)
                {
                    ValidateExitPosition(robotInput, ref strategyData);
                }
                if (strategyData.Decision == StrategyDecision.TakeProfit)
                {
                    ValidateTakeProfit(robotInput, ref strategyData);
                }
                if (strategyData.Decision == StrategyDecision.Escape)
                {
                    ValidateEscapeTrap(robotInput, ref strategyData);
                }
            }
            else
            {
                ResetDecision(ref strategyData);
            }
        }

        //common method used throughout all the validations
        private void ValidateDecision(RobotInput roboInput, ref StrategyData strategyData, int signalStrength, bool dummyvar)//#dummyvar to be removed once refactoring is successful
        {
            if (IsDecisionStrong(signalStrength, strategyData.Decision, strategyData.DecisionType))
            {
                StrategyDecision decision;

                StrategyDecision skipdecision;

                StrategyDecision decisiontype;

                switch (strategyData.Decision)
                {
                    case StrategyDecision.Open:
                        decision = StrategyDecision.Open;
                        skipdecision = StrategyDecision.SkipOpen;
                        break;

                    case StrategyDecision.OpenMissed:
                        decision = StrategyDecision.OpenMissed;
                        skipdecision = StrategyDecision.SkipMissedOpen;
                        break;

                    case StrategyDecision.ExitHeavy:
                        decision = StrategyDecision.ExitHeavy;
                        skipdecision = StrategyDecision.SkipExitHeavy;
                        break;

                    case StrategyDecision.Exit:
                        decision = StrategyDecision.Exit;
                        skipdecision = StrategyDecision.SkipExit;
                        break;

                    case StrategyDecision.TakeProfit:
                        decision = StrategyDecision.TakeProfit;
                        skipdecision = StrategyDecision.SkipTakeProfit;
                        break;

                    case StrategyDecision.Escape:
                        decision = StrategyDecision.Escape;
                        skipdecision = StrategyDecision.SkipEscape;
                        break;

                    default:
                        decision = StrategyDecision.None;
                        skipdecision = StrategyDecision.None;
                        break;
                }

                switch (strategyData.DecisionType)
                {
                    case StrategyDecision.Buy:
                        decisiontype = StrategyDecision.Buy;
                        break;

                    case StrategyDecision.Sell:
                        decisiontype = StrategyDecision.Sell;
                        break;

                    default:
                        decisiontype = StrategyDecision.None;
                        break;
                }

                if (!validator.IsTradeOnRightKandle(strategyData, decisiontype, decision, decision.ToString()))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    if (decisiontype == StrategyDecision.Buy)
                    {
                        strategyData.SkipReasons.Add(SkipReason.RedKandle);
                    }
                    if (decisiontype == StrategyDecision.Sell)
                    {
                        strategyData.SkipReasons.Add(SkipReason.GreenKandle);
                    }
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, decisiontype, BollingerFactor, roboInput.reward, decision.ToString()))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.InvalidBollinger);
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap, decision.ToString()))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.LowSignalGap);
                }

                if (!validator.IsSignalGoodQuality(strategyData, decisiontype, RequiredSignalQuality, decision.ToString()))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.LowSignalQuality);
                }

                if (!validator.IsKandleConsistent(strategyData, decisiontype, ConsistentKandlesLookBack, decision.ToString()))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.InconsistentKandles);
                }

                if (!validator.IsTradeMatchTrend(strategyData, decisiontype, decision.ToString()))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.AgainstTrend);
                }
            }
            else
            {
                ResetDecision(ref strategyData);
            }
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

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    if (decisiontype == StrategyDecision.Buy)
                    {
                        strategyData.SkipReasons.Add(SkipReason.RedKandle);
                    }
                    if (decisiontype == StrategyDecision.Sell)
                    {
                        strategyData.SkipReasons.Add(SkipReason.GreenKandle);
                    }
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, decisiontype, BollingerFactor, roboInput.reward))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.InvalidBollinger);
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.LowSignalGap);
                }

                if (!validator.IsSignalGoodQuality(strategyData, decisiontype, RequiredSignalQuality))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.LowSignalQuality);
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

                if (!validator.IsTradeOnRightKandle(strategyData, decisiontype, decision))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    if (decisiontype == StrategyDecision.Buy)
                    {
                        strategyData.SkipReasons.Add(SkipReason.RedKandle);
                    }
                    if (decisiontype == StrategyDecision.Sell)
                    {
                        strategyData.SkipReasons.Add(SkipReason.GreenKandle);
                    }
                }

                if (!validator.IsTradeValidOnBollinger(strategyData, decisiontype, BollingerFactor, roboInput.reward))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.InvalidBollinger);
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.LowSignalGap);
                }

                if (!validator.IsSignalGoodQuality(strategyData, decisiontype, RequiredSignalQuality))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.LowSignalQuality);
                }

                if (!validator.IsKandleConsistent(strategyData, decisiontype, ConsistentKandlesLookBack))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.InconsistentKandles);
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

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.LowSignalGap);
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

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    if (decisiontype == StrategyDecision.Buy)
                    {
                        strategyData.SkipReasons.Add(SkipReason.RedKandle);
                    }
                    if (decisiontype == StrategyDecision.Sell)
                    {
                        strategyData.SkipReasons.Add(SkipReason.GreenKandle);
                    }
                }

                if (!validator.IsSignalGapValid(strategyData, RequiredSignalGap))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.LowSignalGap);
                }

                if (!validator.IsTradeMatchTrend(strategyData, decisiontype))
                {
                    strategyData.Decision = skipdecision;

                    strategyData.SkipReasons.Add((SkipReason)(int)decisiontype);

                    strategyData.SkipReasons.Add(SkipReason.AgainstTrend);
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
                ValidateDecision(roboInput, ref strategyData, OpenPositionSignalStrength);
            }

            else if (OpenMissedPosition(position, ref strategyData) && GrabMissedPosition)
            {
                ValidateDecision(roboInput, ref strategyData, MissedPositionSignalStrength, true);
            }

            else if (ExitPositionHeavyLoss(position, ref strategyData))
            {
                ValidateDecision(roboInput, ref strategyData, ExitPositionHeavyLossSignalStrength, true);
            }

            else if (ExitPosition(position, ref strategyData, roboInput.risk))
            {
                ValidateDecision(roboInput, ref strategyData, ExitSignalStrength, true);
            }

            else if (TakeProfit(position, ref strategyData, roboInput.reward))
            {
                ValidateDecision(roboInput, ref strategyData, TakeProfitSignalStrength, true);
            }

            else if (EscapeTrap(position, ref strategyData) && EscapeTraps)
            {
                ValidateDecision(roboInput, ref strategyData, EscapeTrapSignalStrength, true);
            }

            else
            {
                ResetDecision(ref strategyData);
            }

            //signal strength data
            strategyData.LatestSignalStrength = LatestSignalStrength;//improve this code laterz

            strategyData.BuyCounter = BuyCounter;

            strategyData.SellCounter = SellCounter;

            strategyData.PrevDecision = prevDecision;

            strategyData.PrevDecisionType = prevDecisionType;

        }
    }
}
