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

        //public List<decimal> diff { get; set; }

        public List<decimal> signal { get; set; }

        public List<decimal> macd { get; set; }

        public List<decimal> histogram { get; set; }

        public bool IsBullish { get; set; }

        public bool IsBearish { get; set; }

        public List<bool> IsBullishCross { get; set; }

        public List<bool> IsBearishCross { get; set; }

        //public decimal diffvalue { get; set; }

        public decimal signalvalue { get; set; }

        public decimal macdvalue { get; set; }

        public decimal histogramvalue { get; set; }

        public string signalhistory { get; set; }

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

            macd = fn.diff(emafast, emaslow);

            signal = fn.ema(macd, 9);

            histogram = fn.diff(macd, signal);

            IsBullishCross = fn.crossover(macd, signal);

            IsBearishCross = fn.crossunder(macd, signal);

            IsBullish = IsBullishCross.Last();

            IsBearish = IsBearishCross.Last();

            //diffvalue = diff.Last();

            signalvalue = signal.Last();

            macdvalue = macd.Last();

            histogramvalue = histogram.Last();

            signalhistory = "";
            for (int i = 0; i < IsBullishCross.Count; i++)
            {
                if (IsBullishCross[i])
                {
                    signalhistory += " B" + (IsBullishCross.Count - i - 1).ToString();
                }
                else if (IsBearishCross[i])
                {
                    signalhistory += " S" + (IsBullishCross.Count - i - 1).ToString();
                }
                else
                {
                    // meh :\
                }
            }
        }
    }
}