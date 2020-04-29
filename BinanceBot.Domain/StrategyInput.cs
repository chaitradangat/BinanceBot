using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceBot.Domain
{
    /// <summary>
    /// Class to accomodate all inputs for the strategy
    /// </summary>
    public class StrategyInput
    {
        public decimal decreaseOnNegative { get; set; }

        public int candleCount { get; set; }

        public string timeframe { get; set; }

        public decimal reward { get; set; }

        public decimal risk { get; set; }

        public decimal leverage { get; set; }

        public string symbol { get; set; }

        public int signalStrength { get; set; }

        public decimal quantity { get; set; }

        public decimal currentClose { get; set; }
    }
}
