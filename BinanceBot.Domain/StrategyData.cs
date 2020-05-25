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

            this.prevOutput = StrategyDecision.None;

            this.AvoidReasons = new HashSet<StrategyDecision>();
        }

        public StrategyData(decimal profitFactor)
        {
            this.profitFactor = profitFactor;

            this.Decision = StrategyDecision.None;

            this.prevOutput = StrategyDecision.None;

            this.AvoidReasons = new HashSet<StrategyDecision>();
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

        public decimal shortPercentage { get; set; }

        public decimal longPercentage { get; set; }

        public decimal profitFactor { get; set; }

        public StrategyDecision prevOutput { get; set; }

        public StrategyDecision Decision { get; set; }

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


        public HashSet<StrategyDecision> AvoidReasons { get; set; }
    }
}
