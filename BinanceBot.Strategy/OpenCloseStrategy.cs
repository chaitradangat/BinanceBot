using System;

using System.Collections.Generic;

using System.Linq;

using PineScriptPort;

using BinanceBot.Domain;

using BinanceBot.Indicator;

using BinanceBot.Settings;

namespace BinanceBot.Strategy
{
    public class OpenCloseStrategy
    {
        //strategy variables 
        private OpenCloseStrategyDecision strategyDecision;

        private int KandleMultiplier; //3

        private string Smoothing;//DEMA

        private int BollingerCrossLookBack;


        //ctor
        public OpenCloseStrategy()
        {
            //set strategy variables
            strategyDecision = new OpenCloseStrategyDecision();

            KandleMultiplier = OpenCloseStrategySettings.settings.KandleMultiplier;

            Smoothing = OpenCloseStrategySettings.settings.Smoothing;

            BollingerCrossLookBack = OpenCloseStrategySettings.settings.BollingerCrossLookBack;
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

            strategyData.SignalQuality = val2;
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
            BollingerBands bBands = new BollingerBands(strategyData.kandles, BollingerCrossLookBack);

            strategyData.BollingerUpper = bBands.BollingerUpper;

            strategyData.BollingerMiddle = bBands.BollingerMiddle;

            strategyData.BollingerLower = bBands.BollingerLower;

            strategyData.BollingerUpperPercentage = bBands.BollingerUpperPercentage;

            strategyData.BollingerMiddlePercentage = bBands.BollingerMiddlePercentage;

            strategyData.BollingerLowerPercentage = bBands.BollingerLowerPercentage;

            strategyData.BollTopCrossed = bBands.BollTopCrossed;

            strategyData.BollBottomCrossed = bBands.BollBottomCrossed;

            strategyData.BollMiddleCrossed = bBands.BollMiddleCrossed;
        }

        private void UpdateMacdData(ref StrategyData strategyData)
        {
            Macd macd = new Macd(strategyData.kandles);

            strategyData.MacdData.emafast = macd.emafast;

            strategyData.MacdData.emaslow = macd.emaslow;

            strategyData.MacdData.diff = macd.diff;

            strategyData.MacdData.signal = macd.signal;

            strategyData.MacdData.macd = macd.macd;

            strategyData.MacdData.IsBearish = macd.IsBearish;

            strategyData.MacdData.IsBullish = macd.IsBullish;

            strategyData.MacdData.IsBearishCross = macd.IsBearishCross;

            strategyData.MacdData.IsBullishCross = macd.IsBullishCross;
        }

        public void RunStrategy(RobotInput robotInput, SimplePosition currentPosition, ref StrategyData strategyData)
        {
            List<OHLCKandle> inputkandles = strategyData.kandles.Select(x => new OHLCKandle
            {
                Close = x.Close,
                CloseTime = x.CloseTime,
                High = x.High,
                Low = x.Low,
                Open = x.Open,
                OpenTime = x.OpenTime
            }).ToList();

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

            UpdateMacdData(ref strategyData);

            UpdateSignalData(ref strategyData, xlong, xshort);

            strategyDecision.Decide(ref strategyData, currentPosition, robotInput);

            if (strategyData.Decision != StrategyDecision.None)
            {
                strategyDecision.ResetCounters();

                strategyData.profitFactor = 1m;
            }
        }
    }
}