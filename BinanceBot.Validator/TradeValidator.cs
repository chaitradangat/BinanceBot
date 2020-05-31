using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BinanceBot.Domain;

using BinanceBot.Settings;

namespace BinanceBot.Validator
{
    /// <summary>
    /// Used to Validate Trades
    /// </summary>
    public class TradeValidator //#warning the variables passed to methods in this class are readonly and should not be modified
    {
        private List<string> ValidationRules;
        public TradeValidator(string ValidationRuleSet)
        {
            ValidationRules = new List<string>(ValidationRuleSet.Split(','));

            ValidationRules.RemoveAll(x => string.IsNullOrWhiteSpace(x));
        }

        /// <summary>
        /// Validate for consecutive dropping or rising kandles
        /// </summary>
        public bool KandlesAreConsistent(StrategyData strategyData, StrategyDecision decisiontype, int lookback, [CallerMemberName]string CallingDecision = "")
        {
            if (!ValidationRequired(CallingDecision))
            {
                return true;
            }

            if (decisiontype != StrategyDecision.Buy && decisiontype != StrategyDecision.Sell)
            {
                //invalid value for decision
                return false;
            }

            var kandleslice = strategyData.kandles.Skip(strategyData.kandles.Count - lookback).Take(lookback);

            for (int i = 0; i < kandleslice.Count() - 1; i++)
            {
                if (kandleslice.ElementAt(i).Open > kandleslice.ElementAt(i + 1).Open && decisiontype == StrategyDecision.Buy)
                {
                    return false;
                }

                if (kandleslice.ElementAt(i).Open < kandleslice.ElementAt(i + 1).Open && decisiontype == StrategyDecision.Sell)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate Signal Gap required to trade
        /// </summary>
        /// <param name="strategyData"></param>
        /// <returns></returns>
        public bool IsSignalGapValid(StrategyData strategyData, int RequiredSignalGap, [CallerMemberName]string CallingDecision = "")
        {
            if (!ValidationRequired(CallingDecision))
            {
                return true;
            }

            var lastSignalGap = Convert.ToInt32(strategyData.histdata.Split(' ').Last().Replace("B", "").Replace("S", ""));

            return strategyData.SignalGap1 > RequiredSignalGap || lastSignalGap >= RequiredSignalGap;
        }

        /// <summary>
        /// Validate Trade on the Bollinger
        /// </summary>
        /// <param name="strategyData"></param>
        /// <param name="decisiontype"></param>
        /// <returns></returns>
        public bool IsTradeValidOnBollinger(StrategyData strategyData, StrategyDecision decisiontype, decimal BollingerFactor, decimal Reward, [CallerMemberName]string CallingDecision = "")
        {
            if (!ValidationRequired(CallingDecision))
            {
                return true;
            }

            if (decisiontype != StrategyDecision.Buy && decisiontype != StrategyDecision.Sell)
            {
                //invalid decision input
                return false;
            }

            if (decisiontype == StrategyDecision.Buy)
            {
                //top crossed recently so chances of loss higher with buy
                if (strategyData.BollTopCrossed)
                {
                    return false;
                }

                //no buy trades from below the middle bollinger by 0.10% #this is a reversal zone
                if (strategyData.BollingerMiddle > strategyData.currentClose && Math.Abs(strategyData.BollingerMiddlePercentage) <= 0.10m)
                {
                    return false;
                }

                if (strategyData.BollingerUpperPercentage >= (Reward * BollingerFactor))
                {
                    return true;
                }

                return false;
            }

            if (decisiontype == StrategyDecision.Sell)
            {
                //bottom crossed recently so chances of loss higer with sell
                if (strategyData.BollBottomCrossed)
                {
                    return false;
                }

                //no sell trades from above the middle bollinger by 0.10% #this is a reversal zone
                if (strategyData.BollingerMiddle < strategyData.currentClose && Math.Abs(strategyData.BollingerMiddlePercentage) <= 0.10m)
                {
                    return false;
                }

                if (strategyData.BollingerLowerPercentage >= (Reward * BollingerFactor))
                {
                    return true;
                }

                return false;
            }


            return false;
        }

        /// <summary>
        /// Validate Buy On Green and Sell On Red
        /// </summary>
        /// <param name="strategyData"></param>
        /// <param name="decisiontype"></param>
        /// <returns></returns>
        public bool IsTradeOnRightKandle(StrategyData strategyData, StrategyDecision decisiontype, StrategyDecision positiondecision, [CallerMemberName]string CallingDecision = "")
        {
            if (!ValidationRequired(CallingDecision))
            {
                return true;
            }

            if (decisiontype != StrategyDecision.Buy && decisiontype != StrategyDecision.Sell &&
                positiondecision != StrategyDecision.Open && positiondecision != StrategyDecision.OpenMissed &&
                positiondecision != StrategyDecision.Exit && positiondecision != StrategyDecision.Escape)
            {
                return false;
            }

            if (decisiontype == StrategyDecision.Buy && (positiondecision == StrategyDecision.Open || positiondecision == StrategyDecision.OpenMissed))
            {
                //buy on green
                if (strategyData.currentClose > strategyData.currentOpen && strategyData.currentClose > strategyData.PrevOpen)
                {
                    return true;
                }
            }

            if (decisiontype == StrategyDecision.Sell && (positiondecision == StrategyDecision.Open || positiondecision == StrategyDecision.OpenMissed))
            {
                //sell on red
                if (strategyData.currentClose < strategyData.currentOpen && strategyData.currentClose < strategyData.PrevOpen)
                {
                    return true;
                }
            }

            if (decisiontype == StrategyDecision.Buy && (positiondecision == StrategyDecision.Exit || positiondecision == StrategyDecision.Escape))
            {
                if (strategyData.currentClose > strategyData.currentOpen)
                {
                    return true;
                }
            }

            if (decisiontype == StrategyDecision.Sell && (positiondecision == StrategyDecision.Exit || positiondecision == StrategyDecision.Escape))
            {
                if (strategyData.currentClose < strategyData.currentOpen)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validate Buy & Sell is following the trend
        /// </summary>
        /// <param name="strategyData"></param>
        /// <param name="decisiontype"></param>
        /// <param name="CallingDecision"></param>
        /// <returns></returns>
        public bool IsTradeMatchTrend(StrategyData strategyData, StrategyDecision decisiontype, [CallerMemberName]string CallingDecision = "")
        {
            if (!ValidationRequired(CallingDecision))
            {
                return true;
            }

            if (decisiontype != StrategyDecision.Buy && decisiontype != StrategyDecision.Sell)
            {
                //invalid decision input
                return false;
            }

            if (decisiontype == StrategyDecision.Buy)
            {
                if (strategyData.trend != "BULLISH" && strategyData.mood != "BULLISH")
                {
                    return false;
                }
            }

            if (decisiontype == StrategyDecision.Sell)
            {
                if (strategyData.trend != "BEARISH" && strategyData.mood != "BEARISH")
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate the signal quality for buy or sell decision
        /// </summary>
        /// <param name="strategyData"></param>
        /// <param name="decisiontype"></param>
        /// <param name="RequiredSignalQuality"></param>
        /// <param name="CallingDecision"></param>
        /// <returns></returns>
        public bool IsSignalGoodQuality(StrategyData strategyData, StrategyDecision decisiontype, int RequiredSignalQuality, [CallerMemberName]string CallingDecision = "")
        {
            if (!ValidationRequired(CallingDecision))
            {
                return true;
            }

            if (decisiontype != StrategyDecision.Buy && decisiontype != StrategyDecision.Sell)
            {
                //invalid decision input
                return false;
            }


            return strategyData.SignalQuality >= RequiredSignalQuality;
        }

        /// <summary>
        /// Checks if validation is required for "StrategyDecision.TradeDecision"
        /// </summary>
        /// <param name="CallingDecision"></param>
        /// <param name="TradeDecision"></param>
        /// <returns></returns>
        private bool ValidationRequired(string CallingDecision, [CallerMemberName]string TradeDecision = "")
        {
            if (ValidationRules.Contains(string.Format("{0}.{1}", CallingDecision, TradeDecision)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
