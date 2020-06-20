using System;
using System.Collections.Generic;
using System.Linq;

using BinanceBot.Domain;

using PineScriptPort;

namespace BinanceBot.Indicator
{
    public class Macd
    {
        public List<decimal> emaslow { get; set; }

        public List<decimal> emafast { get; set; }

        public List<decimal> diff { get; set; }

        public List<decimal> signal { get; set; }

        public List<decimal> macd { get; set; }

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

            emaslow = fn.ema(closevalues, 26);

            emafast = fn.ema(closevalues, 12);

            diff = fn.diff(emafast, emaslow);

            signal = fn.ema(diff, 9);

            macd = fn.diff(diff, signal);

            IsBullishCross = fn.crossover(diff, signal);

            IsBearishCross = fn.crossunder(diff, signal);

            IsBullish = IsBullishCross.Last();

            IsBearish = IsBearishCross.Last();
        }
    }
}