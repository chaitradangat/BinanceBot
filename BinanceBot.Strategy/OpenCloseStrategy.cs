using System;

using System.Collections.Generic;

using System.Linq;

using PineScriptPort;

using BinanceBot.Domain;

using BinanceBot.Settings;

namespace BinanceBot.Strategy
{
    public class OpenCloseStrategy
    {
        //strategy variables 
        OpenCloseStrategyDecision strategyDecision;

        private int KandleMultiplier; //3

        private string Smoothing;//DEMA

        //ctor
        public OpenCloseStrategy()
        {
            //set strategy variables
            strategyDecision = new OpenCloseStrategyDecision();

            KandleMultiplier = OpenCloseStrategySettings.settings.KandleMultiplier;

            Smoothing = OpenCloseStrategySettings.settings.Smoothing;
        }

        //utility functions
        private void UpdateSignalData(ref StrategyData strategyData, List<bool> xlong, List<bool> xshort)
        {
            //historical data
            strategyData.histdata = "";
            for (int i = 0; i < xlong.Count; i++)
            {
                if (xlong[i])
                {
                    strategyData.histdata += " B" + (xlong.Count - i - 1).ToString();
                }
                else if (xshort[i])
                {
                    strategyData.histdata += " S" + (xlong.Count - i - 1).ToString();
                }
                else
                {
                    // meh :\
                }
            }

            var histDataSplit = Convert.ToString(strategyData.histdata).Split(' ');

            var histDataInt = histDataSplit.Skip(histDataSplit.Length - 3).Take(3);

            var val0 = Convert.ToInt32(histDataInt.ElementAt(0).Replace("B", "").Replace("S", ""));

            var val1 = Convert.ToInt32(histDataInt.ElementAt(1).Replace("B", "").Replace("S", ""));

            var val2 = Convert.ToInt32(histDataInt.ElementAt(2).Replace("B", "").Replace("S", ""));

            strategyData.SignalGap0 = val0 - val1;

            strategyData.SignalGap1 = val1 - val2;
        }

        private List<OHLCKandle> GetSmoothData(List<OHLCKandle> kandles, int lookback, string smoothing)
        {
            PineScriptFunction fn = new PineScriptFunction();

            if (smoothing.ToUpper() == "DEMA")
            {
                kandles = fn.dema(kandles, lookback);
            }
            else
            {
                kandles = fn.smma(kandles, lookback);
            }

            return kandles;
        }

        private void UpdateBollingerData(ref StrategyData strategyData)
        {
            PineScriptFunction fn = new PineScriptFunction();

            //make a copy of original data
            var kcopy = strategyData.kandles.Select(x => new OHLCKandle
            {
                Close = x.Close,
                CloseTime = x.CloseTime,
                Open = x.Open,
                OpenTime = x.OpenTime,
                High = x.High,
                Low = x.Low
            }).ToList();

            var kcopyopenseries = kcopy.Select(x => (decimal)x.Open).ToList();

            var kcopycloseseries = kcopy.Select(x => (decimal)x.Close).ToList();

            ////start bollinger bands data
            var bollingerData = fn.bollinger(kcopy, 20);

            strategyData.BollingerUpper = bollingerData.Last().High;

            strategyData.BollingerMiddle = bollingerData.Last().Close;

            strategyData.BollingerLower = bollingerData.Last().Low;

            var pricecrossunder = fn.crossunder(kcopyopenseries, bollingerData.Select(x => x.High).ToList());

            var pricecrossover = fn.crossover(kcopycloseseries, bollingerData.Select(x => x.Low).ToList());

            strategyData.BollTopCrossed = pricecrossunder.Skip(pricecrossunder.Count - 7).Take(7).Contains(true);

            strategyData.BollBottomCrossed = pricecrossover.Skip(pricecrossover.Count - 7).Take(7).Contains(true);
            //end bollinger bands data
        }

        public void RunStrategy(List<OHLCKandle> inputkandles, RobotInput robotInput, SimplePosition currentPosition, ref StrategyData strategyData)
        {
            PineScriptFunction fn = new PineScriptFunction();

            //convert to higher timeframe
            var largekandles = fn.converttohighertimeframe(inputkandles, KandleMultiplier);//3

            //higher timeframe candles with smoothened values
            largekandles = GetSmoothData(largekandles, 8, Smoothing);

            //lower timeframe candles with smoothened values
            inputkandles = GetSmoothData(inputkandles, 8, Smoothing);

            var inputcloseseriesmoothed = inputkandles.Select(x => x.Close).ToList();

            //map higher timeframe values to lower timeframe values
            var altkandles = fn.superimposekandles(largekandles, inputkandles);

            var closeSeriesAlt = altkandles.Select(x => x.Close).ToList();

            var openSeriesAlt = altkandles.Select(x => x.Open).ToList();
            //end map higher timeframe values to lower timeframe values

            //trend and mood calculation-
            strategyData.trend = closeSeriesAlt.Last() > openSeriesAlt.Last() ? "BULLISH" : "BEARISH";

            strategyData.mood = inputcloseseriesmoothed.Last() > openSeriesAlt.Last() ? "BULLISH" : "BEARISH";

            //start buy sell signal
            var xlong = fn.crossover(closeSeriesAlt, openSeriesAlt);

            var xshort = fn.crossunder(closeSeriesAlt, openSeriesAlt);

            strategyData.isBuy = xlong.Last();

            strategyData.isSell = xshort.Last();
            //end buy sell signal

            UpdateBollingerData(ref strategyData);

            UpdateSignalData(ref strategyData, xlong, xshort);

            strategyDecision.Decide(ref strategyData, currentPosition, robotInput);

            if (strategyData.Output != StrategyOutput.None)
            {
                strategyDecision.ResetCounters();

                strategyData.profitFactor = 1m;
            }
        }
    }
}