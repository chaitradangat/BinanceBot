using System;

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

            this.Output = StrategyOutput.None;

            this.prevOutput = StrategyOutput.None;
        }

        public StrategyData(decimal profitFactor)
        {
            this.profitFactor = profitFactor;

            this.Output = StrategyOutput.None;

            this.prevOutput = StrategyOutput.None;
        }


        public bool isBuy { get; set; }

        public bool isSell { get; set; }

        public string trend { get; set; }

        public string mood { get; set; }

        public string histdata { get; set; }

        public decimal shortPercentage { get; set; }

        public decimal longPercentage { get; set; }

        public decimal profitFactor { get; set; }

        public StrategyOutput prevOutput { get; set; }

        public StrategyOutput Output { get; set; }

        public int LatestSignalStrength { get; set; }

        public int BuyCounter { get; set; }

        public int SellCounter { get; set; }

        //boll values will move to seperate indicator using this variable meanwhile
        public decimal BollingerUpper { get; set; }

        public decimal BollingerMiddle { get; set; }

        public decimal BollingerLower { get; set; }

        public bool BollTopCrossed { get; set; } 

        public bool BollBottomCrossed { get; set; }

        public int SignalGap1 { get; set; }

        public int SignalGap2 { get; set; }
    }
}
