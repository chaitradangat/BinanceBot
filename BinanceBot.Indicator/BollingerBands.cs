using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using BinanceBot.Domain;

using PineScriptPort;


namespace BinanceBot.Indicator
{
    /// <summary>
    /// The BollingerBand Data Indicator
    /// </summary>
    public class BollingerBands
    {
        private List<OHLCKandle> BollingerData;

        public decimal BollingerUpper { get; set; }

        public decimal BollingerMiddle { get; set; }

        public decimal BollingerLower { get; set; }

        public decimal BollingerUpperPercentage { get; set; }

        public decimal BollingerMiddlePercentage { get; set; }

        public decimal BollingerLowerPercentage { get; set; }

        public bool BollTopCrossed { get; set; }

        public bool BollBottomCrossed { get; set; }

        public bool BollMiddleCrossed { get; set; }

        public BollingerBands(List<OHLCKandle> kandles, int bollCrossDistance = 7)
        {
            //make a copy to avoid spoiling the inputdata
            var kcopy = kandles.Select(x => new OHLCKandle
            {
                Close = x.Close,
                CloseTime = x.CloseTime,
                Open = x.Open,
                OpenTime = x.OpenTime,
                High = x.High,
                Low = x.Low
            }).ToList();

            Calculate(kcopy, bollCrossDistance);
        }

        /// <summary>
        /// Computes Bollinger Bands
        /// </summary>
        /// <param name="kandles">The input kandles over which bollinger bands will be calculated</param>
        /// <param name="bollCrossDistance">The maximum distance till which cross with lower and upper sma is valid</param>
        public void Calculate(List<OHLCKandle> kandles, int bollCrossDistance)
        {
            PineScriptFunction fn = new PineScriptFunction();

            var close = kandles.Last().Close;

            var openseries = kandles.Select(x => (decimal)x.Open).ToList();

            var closeseries = kandles.Select(x => (decimal)x.Close).ToList();

            this.BollingerData = fn.bollinger(kandles, 20);

            this.BollingerUpper = BollingerData.Last().High;

            this.BollingerMiddle = BollingerData.Last().Close;

            this.BollingerLower = BollingerData.Last().Low;

            this.BollingerUpperPercentage = Math.Round((100 * (this.BollingerUpper - close) / this.BollingerUpper), 3);

            this.BollingerMiddlePercentage = Math.Round((100 * (close - this.BollingerMiddle) / this.BollingerMiddle), 3);

            this.BollingerLowerPercentage = Math.Round((100 * (close - this.BollingerLower) / close), 3);

            var pricecrosstopband = fn.crossunder(openseries, BollingerData.Select(x => x.High).ToList());

            var pricecrossbottomband = fn.crossover(closeseries, BollingerData.Select(x => x.Low).ToList());

            var pricecrossmiddleband1 = fn.crossunder(openseries, BollingerData.Select(x => x.Close).ToList());

            var pricecrossmiddleband2 = fn.crossover(closeseries, BollingerData.Select(x => x.Close).ToList());

            this.BollTopCrossed = pricecrosstopband.Skip(pricecrosstopband.Count - bollCrossDistance).Take(bollCrossDistance).Contains(true);

            this.BollBottomCrossed = pricecrossbottomband.Skip(pricecrossbottomband.Count - bollCrossDistance).Take(bollCrossDistance).Contains(true);

            this.BollMiddleCrossed = pricecrossmiddleband1.Skip(pricecrossmiddleband1.Count - 2).Take(2).Contains(true)||
                                     pricecrossmiddleband2.Skip(pricecrossmiddleband2.Count - 2).Take(2).Contains(true);
        }




    }




}
