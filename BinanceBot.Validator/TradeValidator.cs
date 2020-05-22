using System;

using System.Linq;

using BinanceBot.Domain;

using BinanceBot.Settings;

namespace BinanceBot.Validator
{
    /// <summary>
    /// Used to Validate Trades
    /// </summary>
    public class TradeValidator
    {
        private int RequiredSignalGap;

        private decimal Reward;

        private decimal BollingerFactor;

        public TradeValidator()
        {
            RequiredSignalGap = OpenCloseStrategySettings.settings.SignalGap;

            Reward = BinanceBotSettings.settings.RewardPercentage;

            BollingerFactor = OpenCloseStrategySettings.settings.BollingerFactor;
        }

        //functions must be factorised further to provide avoid enumeration values directly


        /// <summary>
        /// Validate for consecutive dropping or rising kandles
        /// </summary>
        public bool KandlesAreConsistent(StrategyData strategyData, StrategyDecision decision, int lookback)
        {
            if (decision != StrategyDecision.Buy && decision != StrategyDecision.Sell)
            {
                //invalid value for decision
                return false;
            }

            var kandleslice = strategyData.kandles.Skip(strategyData.kandles.Count - lookback).Take(lookback);

            for (int i = 0; i < kandleslice.Count() - 1; i++)
            {
                if (kandleslice.ElementAt(i).Open > kandleslice.ElementAt(i + 1).Open && decision == StrategyDecision.Buy)
                {
                    return false;
                }

                if (kandleslice.ElementAt(i).Open < kandleslice.ElementAt(i + 1).Open && decision == StrategyDecision.Sell)
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
        public bool IsSignalGapValid(StrategyData strategyData)
        {
            var lastSignalGap =  Convert.ToInt32(strategyData.histdata.Split(' ').Last().Replace("B", "").Replace("S", ""));

            return strategyData.SignalGap1 > RequiredSignalGap || lastSignalGap >= RequiredSignalGap;
        }

        /// <summary>
        /// Validate Trade on the Bollinger
        /// </summary>
        /// <param name="strategyData"></param>
        /// <param name="decision"></param>
        /// <returns></returns>
        public bool IsTradeValidOnBollinger(StrategyData strategyData, StrategyDecision decision)
        {
            if (decision != StrategyDecision.Buy || decision != StrategyDecision.Sell)
            {
                //invalid decision input
                return false;
            }

            //calculate percetage scope for buy with respect to upper bollinger Band
            var buyPercentageScope = ((strategyData.BollingerUpper - strategyData.currentClose) / strategyData.BollingerUpper) * 100;

            //calculate percetage scope for buy with respect to upper bollinger Band
            var sellPercentageScope = ((strategyData.currentClose - strategyData.BollingerLower) / strategyData.currentClose) * 100;

            //calculate deviation of price from middle bollinger band
            var deviationFromMiddle = (Math.Abs(strategyData.currentClose - strategyData.BollingerMiddle) / strategyData.BollingerMiddle) * 100;

            if (decision == StrategyDecision.Buy)
            {
                //top crossed recently so chances of loss higher with buy
                if (strategyData.BollTopCrossed)
                {
                    return false;
                }

                //no buy trades from below the middle bollinger by 0.10%
                if (strategyData.BollingerMiddle > strategyData.currentClose && deviationFromMiddle > 0.10m)
                {
                    return false;
                }

                if (buyPercentageScope >= (Reward * BollingerFactor))
                {
                    return true;
                }

                return false;
            }

            if (decision == StrategyDecision.Sell)
            {
                //bottom crossed recently so chances of loss higer with sell
                if (strategyData.BollBottomCrossed)
                {
                    return false;
                }

                if (strategyData.BollingerMiddle < strategyData.currentClose && deviationFromMiddle > 0.10m)
                {
                    return false;
                }

                if (sellPercentageScope >= (Reward * BollingerFactor))
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
        /// <param name="decision"></param>
        /// <returns></returns>
        public bool IsTradeOnRightKandle(StrategyData strategyData, StrategyDecision decision, StrategyDecision positiondecision)
        {
            if (decision != StrategyDecision.Buy && decision != StrategyDecision.Sell && positiondecision != StrategyDecision.Open && positiondecision != StrategyDecision.Exit)
            {
                return false;
            }

            if (decision == StrategyDecision.Buy && positiondecision == StrategyDecision.Open)
            {
                //buy on green
                if (strategyData.currentClose > strategyData.currentOpen && strategyData.currentClose > strategyData.PrevOpen)
                {
                    return true;
                }
            }

            if (decision == StrategyDecision.Sell && positiondecision == StrategyDecision.Open)
            {
                //sell on red
                if (strategyData.currentClose < strategyData.currentOpen && strategyData.currentClose < strategyData.PrevOpen)
                {
                    return true;
                }
            }

            if (decision == StrategyDecision.Buy && positiondecision == StrategyDecision.Exit)
            {
                if (strategyData.currentClose > strategyData.currentOpen)
                {
                    return true;
                }
            }

            if (decision == StrategyDecision.Sell && positiondecision == StrategyDecision.Exit)
            {
                if (strategyData.currentClose < strategyData.currentOpen)
                {
                    return true;
                }
            }

            return false;
        }

        
    }
}
