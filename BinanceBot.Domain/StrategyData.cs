using System;

using System.Collections.Generic;

using System.Text;

namespace BinanceBot.Domain
{
    /// <summary>
    /// Class to hold all data related to a strategy
    /// </summary>
    public class StrategyData
    {
        public StrategyData()
        {
            this.profitFactor = (decimal)1;

            this.Decision = StrategyDecision.None;

            this.DecisionType = StrategyDecision.None;

            this.PrevDecision = StrategyDecision.None;

            this.PrevDecisionType = StrategyDecision.None;

            this.SkipReasons = new HashSet<SkipReason>();

            this.MacdData = new MacdData();
        }

        public StrategyData(decimal profitFactor)
        {
            this.profitFactor = profitFactor;

            this.Decision = StrategyDecision.None;

            this.DecisionType = StrategyDecision.None;

            this.PrevDecision = StrategyDecision.None;

            this.PrevDecisionType = StrategyDecision.None;

            this.SkipReasons = new HashSet<SkipReason>();

            this.MacdData = new MacdData();
        }

        //this is a copy of kandles which will be pristine or if read should not be modified in any part of kode
        public List<OHLCKandle> kandles { get; set; }

        public decimal PrevOpen { get; set; }

        public decimal PrevClose { get; set; }

        public decimal currentOpen { get; set; }

        public decimal currentClose { get; set; }

        public bool isBuy { get; set; }

        public bool isSell { get; set; }

        public string trend { get; set; }

        public string mood { get; set; }

        public string histdata { get; set; }

        public decimal Percentage { get; set; }

        public decimal profitFactor { get; set; }

        public StrategyDecision PrevDecision { get; set; }

        public StrategyDecision PrevDecisionType { get; set; }

        public StrategyDecision Decision { get; set; }

        public StrategyDecision DecisionType { get; set; }

        public int LatestSignalStrength { get; set; }

        public int BuyCounter { get; set; }

        public int SellCounter { get; set; }

        //boll values will move to seperate indicator using this variable meanwhile
        public decimal BollingerUpper { get; set; }

        public decimal BollingerMiddle { get; set; }

        public decimal BollingerLower { get; set; }

        public decimal BollingerUpperPercentage { get; set; }

        public decimal BollingerMiddlePercentage { get; set; }

        public decimal BollingerLowerPercentage { get; set; }

        public bool BollTopCrossed { get; set; }

        public bool BollBottomCrossed { get; set; }

        public bool BollMiddleCrossed { get; set; }
        //boll values end

        //signal related variables
        public int SignalGap0 { get; set; }

        public int SignalGap1 { get; set; }

        public int SignalQuality { get; set; }
        //
        public HashSet<SkipReason> SkipReasons { get; set; }

        public MacdData MacdData { get; set; }
    }


    public class MacdData
    {
        public MacdData()
        {

        }

        public List<decimal> ema26 { get; set; }

        public List<decimal> ema12 { get; set; }

        public List<decimal> macd { get; set; }

        public List<decimal> signal { get; set; }

        public bool IsBullish { get; set; }

        public bool IsBearish { get; set; }

        public List<bool> IsBullishCross { get; set; }

        public List<bool> IsBearishCross { get; set; }
    }
}
