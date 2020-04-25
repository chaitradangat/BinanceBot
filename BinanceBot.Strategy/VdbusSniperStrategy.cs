using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PineScriptPort;

using BinanceBot.Domain;

namespace BinanceBot.Strategy
{
    public class VdbusSniperStrategy
    {
        public VdbusSniperStrategy()
        {

        }

        public void RunStrategy(List<OHLCKandle> inputkandles,ref bool isBuy, ref bool isSell, ref string trend, ref string mood,ref string histdata)
        {
            PineScriptFunction fn = new PineScriptFunction();

            var closevalues = inputkandles.Select(x => x.Close).ToList();

            var highvalues = inputkandles.Select(x => x.High).ToList();

            var lowvalues = inputkandles.Select(x => x.Low).ToList();

            //start mood and trend code
            var ema0 = fn.ema(closevalues, 13);

            trend = fn.trend(ema0);

            var _out = fn.sma(closevalues, 8);

            if (fn.bearish(closevalues, _out))
            {
                mood = "BEARISH";
            }
            else if (fn.bullish(closevalues, _out))
            {
                mood = "BULLISH";
            }
            else
            {
                //meh :\
            }
            //end mood and trend code


            //start signal code

            var vh1 = fn.ema(fn.highest(fn.avgseries(lowvalues, closevalues), 5),5);

            var vl1 = fn.ema(fn.lowest(fn.avgseries(highvalues, closevalues), 8), 8);

            var e_ema1 = fn.ema(closevalues, 1);

            var e_ema2 = fn.ema(e_ema1, 1);

            var e_ema3 = fn.ema(e_ema2, 1);

            var tema = fn.tema(e_ema1, e_ema2, e_ema3);

            var e_e1 = fn.ema(closevalues, 8);

            var e_e2 = fn.ema(e_e1, 5);

            var dema = fn.dema(e_e1, e_e2);

            var signal = fn.signal(tema, dema, vh1, vl1);

            var _isBuy = fn.and(fn.and(fn.greaterthan(tema, dema), fn.greaterthan(signal, lowvalues)), fn.signalcomparebuy(signal));

            var _isSell = fn.and(fn.and(fn.lessthan(tema, dema), fn.lessthan(signal, highvalues)), fn.signalcomparesell(signal));

            isBuy = _isBuy.Last();

            isSell = _isSell.Last();

            //end signal code

            for (int i = _isBuy.Count - 10  ; i <= _isBuy.Count - 2; ++i)
            {
                if (_isBuy[i])
                {
                    histdata += "B\t";
                }
                else if (_isSell[i])
                {
                    histdata += "S\t";
                }
                else
                {
                    histdata += "0\t";                
                }
            }
        }
    }
}