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
            //this.isBuy = default(bool);

            //this.isSell = default(bool);

            //this.mood = default(string);

            //this.trend = default(string);

            //this.shortPercentage = default(decimal);

            //this.longPercentage = default(decimal);

            //this.histdata = default(string);

            this.profitFactor = (decimal)1;
        }

        public StrategyData(decimal profitFactor)
        {
            this.profitFactor = profitFactor;
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
    }
}
