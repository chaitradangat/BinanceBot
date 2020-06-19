using System;
using System.Collections.Generic;
using System.Linq;

using BinanceBot.Domain;

using PineScriptPort;

namespace BinanceBot.Indicator
{
    public class Macd
    {
        public List<decimal> ema26 { get; set; }

        public List<decimal> ema12 { get; set; }

        public List<decimal> macd { get; set; }

        public List<decimal> signal { get; set; }

        public bool IsBullish { get; set; }

        public bool IsBearish { get; set; }

        public List<bool> IsBullishCross { get; set; }

        public List<bool> IsBearishCross { get; set; }

        public Macd(List<OHLCKandle> kandles)
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

            Calculate(kcopy);
        }

        public void Calculate(List<OHLCKandle> kandles)
        {
            PineScriptFunction fn = new PineScriptFunction();

            List<decimal> closevalues = kandles.Select(x => x.Close).ToList();

            ema26 = fn.ema(closevalues, 26);

            ema12 = fn.ema(closevalues, 12);

            macd = fn.diff(ema12,ema26);

            signal = fn.ema(macd, 9);

            IsBullishCross = fn.crossover(macd, signal);

            IsBearishCross = fn.crossunder(macd,signal);

            IsBullish = IsBullishCross.Last();

            IsBearish = IsBearishCross.Last();
        }
    }
}