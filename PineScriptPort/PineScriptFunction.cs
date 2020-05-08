using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BinanceBot.Domain;

namespace PineScriptPort
{
    public class PineScriptFunction
    {
        /// <summary>
        /// calculates sam for the given series - #tested OK
        /// </summary>
        /// <param name="series"></param>
        /// <param name="lookback"></param>
        /// <returns>simple moving average series</returns>
        public List<decimal> sma(List<decimal> series, int lookback)
        {
            var results = new List<decimal>();

            for (int i = series.Count - 1; i >= lookback - 1; i--)
            {
                decimal sum = 0;

                for (int j = 0; j < lookback; j++)
                {
                    sum += series[i - j];
                }

                results.Add(sum / lookback);
            }

            results.Reverse();

            return results;
        }













        /// <summary>
        /// calculates ema for a given series
        /// </summary>
        /// <param name="series"></param>
        /// <param name="lookback"></param>
        /// <returns>exponential moving average #tested OK</returns>
        public List<decimal> ema(List<decimal> series, int lookback)
        {
            if (series.Count < lookback)
            {
                return null;
            }

            int diff = series.Count - lookback;

            decimal[] newdata = series.Take(lookback).ToArray();

            decimal factor = CalculateFactor(lookback);

            decimal sma = Average(newdata);

            List<decimal> result = new List<decimal>();
            result.Add(sma);

            for (int i = 0; i < diff; i++)
            {
                decimal prev = result[result.Count - 1];

                decimal price = series[lookback + i];

                decimal next = factor * (price - prev) + prev;
                result.Add(next);
            }

            return result;
        }

        public bool cross(List<decimal> closeseries, List<decimal> series2)
        {
            return closeseries.Last() != series2.Last();
        }

        /// <summary>
        /// Returns average of two series
        /// </summary>
        /// <param name="series1"></param>
        /// <param name="series2"></param>
        /// <returns></returns>
        public List<decimal> avgseries(List<decimal> series1, List<decimal> series2)
        {
            var result = default(List<decimal>);

            if (series1.Count >= series2.Count)
            {
                var diff = series1.Count - series2.Count;

                result = new List<decimal>(new decimal[series1.Count]);

                for (int i = series1.Count - 1; i >= 0; i--)
                {
                    if (i - diff >= 0)
                    {
                        result[i] = (series1[i] + series2[i - diff]) / 2;
                    }
                    else
                    {
                        result[i] = series1[i];
                    }
                }
            }
            else if (series1.Count < series2.Count)
            {
                var diff = series2.Count - series1.Count;

                result = new List<decimal>(new decimal[series2.Count]);

                for (int i = series2.Count - 1; i >= 0; i--)
                {
                    if (i - diff >= 0)
                    {
                        result[i] = (series2[i] + series1[i - diff]) / 2;
                    }
                    else
                    {
                        result[i] = series2[i];
                    }
                }
            }
            else
            {
                //meh :/
            }
            return result;
        }

        /// <summary>
        /// recursive highest with lookback
        /// </summary>
        /// <param name="series"></param>
        /// <param name="lookback"></param>
        /// <returns></returns>
        public List<decimal> highest(List<decimal> series, int lookback)
        {
            var result = new List<decimal>(new decimal[series.Count - lookback + 1]);

            int k = 0;

            for (int i = result.Count - 1; i >= 0; --i)
            {
                var temp = new List<decimal>();

                for (int j = series.Count - 1 - k; j >= series.Count - lookback - k; --j)
                {
                    temp.Add(series[j]);
                }

                result[i] = temp.Max();
                ++k;
            }

            return result;
        }

        /// <summary>
        /// recursive lowest with lookback
        /// </summary>
        /// <param name="series"></param>
        /// <param name="lookback"></param>
        /// <returns></returns>
        public List<decimal> lowest(List<decimal> series, int lookback)
        {
            var result = new List<decimal>(new decimal[series.Count - lookback + 1]);

            int k = 0;

            for (int i = result.Count - 1; i >= 0; --i)
            {
                var temp = new List<decimal>();

                for (int j = series.Count - 1 - k; j >= series.Count - lookback - k; --j)
                {
                    temp.Add(series[j]);
                }

                result[i] = temp.Min();
                ++k;
            }

            return result;
        }

        /// <summary>
        /// dema calculation without ema , ema has to be calculated beforehand and sent to this function
        /// </summary>
        /// <param name="series1"></param>
        /// <param name="series2"></param>
        /// <returns></returns>
        public List<decimal> dema(List<decimal> series1, List<decimal> series2)
        {
            var result = new List<decimal>();

            if (series1.Count <= series2.Count)
            {
                var diff = series2.Count - series1.Count;

                result = new List<decimal>(new decimal[series1.Count]);

                for (int i = result.Count - 1; i >= 0; --i)
                {
                    result[i] = (2 * series1[i]) - series2[i + diff];
                }
            }
            else if (series1.Count > series2.Count)
            {
                var diff = series1.Count - series2.Count;

                result = new List<decimal>(new decimal[series2.Count]);

                for (int i = result.Count - 1; i >= 0; --i)
                {
                    result[i] = (2 * series1[i + diff]) - series2[i];
                }
            }
            else
            {
                //meh :/
            }

            return result;
        }

        public List<decimal> dema(List<decimal> series, int lookback)
        {
            var ema1 = ema(series, lookback);

            var ema2 = ema(ema1, lookback);

            var _ema1 = new List<decimal>(ema1);

            var _ema2 = new List<decimal>(ema2);

            trimseries(ref _ema1, ref _ema2);

            var result = new List<decimal>();

            for (int i = _ema1.Count - 1; i >= 0; --i)
            {
                result.Add(2 * _ema1[i] - _ema2[i]);
            }

            result.Reverse();

            return result;
        }


        /// <summary>
        /// tema calculation without ema , ema has to be calculated beforehand and sent to this function
        /// </summary>
        /// <param name="series1"></param>
        /// <param name="series2"></param>
        /// <param name="series3"></param>
        /// <returns></returns>
        public List<decimal> tema(List<decimal> series1, List<decimal> series2, List<decimal> series3)
        {
            var resultLength = Math.Min(series1.Count, series2.Count);

            resultLength = Math.Min(resultLength, series3.Count);

            var result = new List<decimal>(new decimal[resultLength]);

            var diff1 = series1.Count - resultLength;

            var diff2 = series2.Count - resultLength;

            var diff3 = series3.Count - resultLength;

            for (int i = result.Count - 1; i >= 0; --i)
            {
                result[i] = (series1[i + diff1] - series2[i + diff2]) + series3[i + diff3];
            }

            return result;
        }

        public List<decimal> signal(List<decimal> tema, List<decimal> dema, List<decimal> vh1, List<decimal> vl1)
        {
            var temaidx = tema.Count - 1;

            var demaidx = dema.Count - 1;

            var vh1idx = vh1.Count - 1;

            var vl1idx = vl1.Count - 1;


            var resultlen = Math.Max(temaidx, demaidx);

            resultlen = Math.Max(resultlen, vh1idx);

            resultlen = Math.Max(resultlen, vl1idx);

            var result = new List<decimal>(new decimal[resultlen + 1]);

            for (int i = resultlen; i >= 0; --i)
            {
                var tma = temaidx < 0 ? tema.First() : tema[temaidx];

                var dma = demaidx < 0 ? dema.First() : dema[demaidx];

                var vh1_ = vh1idx < 0 ? vh1.First() : vh1[vh1idx];

                var vl1_ = vl1idx < 0 ? vl1.First() : vl1[vl1idx];

                result[i] = tma > dma ? Math.Max(vh1_, vl1_) : Math.Min(vh1_, vl1_);

                --temaidx;
                --demaidx;
                --vh1idx;
                --vl1idx;
            }


            return result;
        }

        public bool bullish(List<decimal> closeseries, List<decimal> series2)
        {
            return (closeseries.Last() != series2.Last()) && (closeseries[closeseries.Count - 2] < closeseries[closeseries.Count - 1]);
        }

        public bool bearish(List<decimal> closeseries, List<decimal> series2)
        {
            return (closeseries.Last() != series2.Last()) && (closeseries[closeseries.Count - 2] > closeseries[closeseries.Count - 1]);
        }

        public string trend(List<decimal> series1)
        {
            if (series1.Last() >= series1[series1.Count - 3])
            {
                return "BULLISH";
            }
            else if (series1.Last() < series1[series1.Count - 3])
            {
                return "BEARISH";
            }
            else
            {
                return "";
            }
        }


        public List<bool> lessthan(List<decimal> series1, List<decimal> series2)
        {
            var result = new List<bool>();

            var minlen = Math.Min(series1.Count, series2.Count);

            var maxlen = Math.Max(series1.Count, series2.Count);

            if (series1.Count <= series2.Count)
            {
                var diff = series2.Count - series1.Count;

                for (int i = series1.Count - 1; i >= 0; --i)
                {
                    result.Add(series1[i] < series2[i + diff]);
                }
            }
            else if (series1.Count > series2.Count)
            {
                var diff = series1.Count - series2.Count;

                for (int i = series2.Count - 1; i >= 0; --i)
                {
                    result.Add(series1[i + diff] < series2[i]);
                }
            }
            else
            {
                // meh :\
            }

            result.Reverse();

            return result;
        }

        public List<bool> greaterthan(List<decimal> series1, List<decimal> series2)
        {
            var result = new List<bool>();

            var minlen = Math.Min(series1.Count, series2.Count);

            var maxlen = Math.Max(series1.Count, series2.Count);

            if (series1.Count <= series2.Count)
            {
                var diff = series2.Count - series1.Count;

                for (int i = series1.Count - 1; i >= 0; --i)
                {
                    result.Add(series1[i] > series2[i + diff]);
                }
            }
            else if (series1.Count > series2.Count)
            {
                var diff = series1.Count - series2.Count;

                for (int i = series2.Count - 1; i >= 0; --i)
                {
                    result.Add(series1[i + diff] > series2[i]);
                }
            }
            else
            {
                // meh :\
            }

            result.Reverse();

            return result;
        }

        public List<bool> and(List<bool> series1, List<bool> series2)
        {
            var result = new List<bool>();

            if (series1.Count <= series2.Count)
            {
                var diff = series2.Count - series1.Count;

                for (int i = series1.Count - 1; i >= 0; --i)
                {
                    result.Add(series1[i] && series2[i + diff]);
                }
            }
            else if (series1.Count > series2.Count)
            {
                var diff = series1.Count - series2.Count;

                for (int i = series2.Count - 1; i >= 0; --i)
                {
                    result.Add(series1[i + diff] && series2[i]);
                }
            }
            else
            {
                // meh :\
            }

            result.Reverse();

            return result;
        }

        public List<bool> or(List<bool> series1, List<bool> series2)
        {
            var result = new List<bool>();

            if (series1.Count <= series2.Count)
            {
                var diff = series2.Count - series1.Count;

                for (int i = series1.Count - 1; i >= 0; --i)
                {
                    result.Add(series1[i] || series2[i + diff]);
                }
            }
            else if (series1.Count > series2.Count)
            {
                var diff = series1.Count - series2.Count;

                for (int i = series2.Count - 1; i >= 0; --i)
                {
                    result.Add(series1[i + diff] || series2[i]);
                }
            }
            else
            {
                // meh :\
            }

            result.Reverse();

            return result;
        }


        public List<bool> signalcomparebuy(List<decimal> signal)
        {
            var result = new List<bool>();

            for (int i = signal.Count - 1; i >= 0; i--)
            {
                if (i - 2 >= 0)
                {
                    result.Add(signal[i] - signal[i - 1] > signal[i - 1] - signal[i - 2]);
                }
            }

            result.Reverse();

            return result;
        }

        public List<bool> signalcomparesell(List<decimal> signal)
        {
            var result = new List<bool>();

            for (int i = signal.Count - 1; i >= 0; i--)
            {
                if (i - 2 >= 0)
                {
                    result.Add(signal[i - 1] - signal[i] > signal[i - 2] - signal[i - 1]);
                }
            }

            result.Reverse();

            return result;
        }


        #region -supplemental functions for calculating ema-

        /// <summary>
        /// supplemental function for ema
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        private decimal CalculateFactor(int days)
        {
            if (days < 0)
                return 0;

            return (decimal)2.0 / (days + 1);
        }

        /// <summary>
        /// supplemental function for ema
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private decimal Average(decimal[] data)
        {
            if (data.Length == 0)
                return 0;

            return Sum(data) / data.Length;
        }

        /// <summary>
        /// supplemantal function for ema
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private decimal Sum(decimal[] data)
        {
            decimal sum = 0;

            foreach (var d in data)
            {
                sum += d;
            }

            return sum;
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeframe"></param>
        /// <returns></returns>
        public int timeframeinterval(string timeframe)
        {
            if (timeframe.EndsWith("m"))
            {
                return int.Parse(timeframe.Replace("m", ""));
            }
            else if (timeframe.EndsWith("h"))
            {
                return 60 * int.Parse(timeframe.Replace("h", ""));
            }
            else if (timeframe.EndsWith("D"))
            {
                return 60 * 24 * int.Parse(timeframe.Replace("D", ""));
            }
            else
            {
                return -1;
            }
        }

        public List<decimal> smma(List<decimal> closevalues, int lookback)
        {
            var _sma = sma(closevalues, lookback);

            var smma = new List<decimal>(new decimal[_sma.Count - 1]);

            var reducedclose = new List<decimal>();

            var diff = closevalues.Count - _sma.Count;

            for (int i = diff + 1; i < closevalues.Count; i++)
            {
                reducedclose.Add(closevalues[i]);
            }

            for (int i = 0; i < smma.Count; i++)
            {
                if (i == 0)
                {
                    smma[0] = _sma[0];
                }
                else
                {
                    var prevsum = smma[i - 1] * lookback;
                    smma[i] = (prevsum - smma[i - 1] + reducedclose[i]) / lookback;
                }
            }
            return smma;
        }

        /// <summary>
        /// Resample a lower interval series to higher frame
        /// </summary>
        /// <param name="series"></param>
        /// <param name="multiplier"></param>
        /// <param name="type"></param>
        /// <returns>Resampled Series</returns>
        public List<decimal> resampleseries(List<decimal> series, int multiplier, string type)
        {

            var result = new List<decimal>();

            var temp = new List<decimal>();

            for (int i = series.Count - 1; i >= 0; i--)
            {
                if (temp.Count == multiplier)
                {
                    if (type.ToLower() == "close")
                    {
                        result.Add(temp.First());
                    }
                    else if (type.ToLower() == "open")
                    {
                        result.Add(temp.Last());
                    }
                    else if (type.ToLower() == "high")
                    {
                        result.Add(temp.Max());
                    }
                    else if (type.ToLower() == "low")
                    {
                        result.Add(temp.Min());
                    }
                    else
                    {
                        // meh :\
                    }
                    temp.Clear();
                }

                temp.Add(series[i]);
            }

            result.Reverse();

            return result;
        }


        /// <summary>
        /// check if series1 has crossed under series2 #bearishflag
        /// </summary>
        /// <param name="series1"></param>
        /// <param name="series2"></param>
        /// <returns></returns>
        public List<bool> crossunder(List<decimal> series1, List<decimal> series2)
        {
            //1 < 2
            //1[-1] > 2[-1] 

            var result = new List<bool>();

            var series1_ = new List<decimal>(series1);

            var series2_ = new List<decimal>(series2);

            trimseries(ref series1_, ref series2_);

            for (int i = series1_.Count - 1; i > 0; i--)
            {
                var r = series1_[i] < series2_[i] && series1_[i - 1] >= series2_[i - 1];

                result.Add(r);
            }

            result.Reverse();

            return result;
        }


        /// <summary>
        /// check if series1 had crossed above series2  #bullishflag
        /// </summary>
        /// <param name="series1"></param>
        /// <param name="series2"></param>
        /// <returns></returns>
        public List<bool> crossover(List<decimal> series1, List<decimal> series2)
        {
            //1 > 2
            //1[-1] < 2[-1]

            var result = new List<bool>();

            var series1_ = new List<decimal>(series1);

            var series2_ = new List<decimal>(series2);

            trimseries(ref series1_, ref series2_);

            for (int i = series1_.Count - 1; i > 0; i--)
            {
                var r = series1_[i] > series2_[i] && series1_[i - 1] <= series2_[i - 1];

                result.Add(r);
            }

            result.Reverse();

            return result;
        }

        /// <summary>
        /// trims series to match length starting with last element
        /// </summary>
        /// <param name="series1"></param>
        /// <param name="series2"></param>
        public void trimseries(ref List<decimal> series1, ref List<decimal> series2)
        {
            var diff = Math.Abs(series1.Count - series2.Count);

            for (int i = 0; i < diff; i++)
            {
                if (series1.Count > series2.Count)
                {
                    series1.RemoveAt(0);
                }
                else if (series2.Count > series1.Count)
                {
                    series2.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// function to convert kandles to higher timeframe
        /// </summary>
        /// <param name="kandles">lower timeframe kandles</param>
        /// <param name="multiplier">time multiplier</param>
        /// <returns></returns>
        public List<OHLCKandle> converttohighertimeframe(List<OHLCKandle> kandles, int multiplier)
        {
            var minDate = kandles.Select(x => x.OpenTime).Min();

            var minDay = minDate.Day;

            var minMonth = minDate.Month;

            var minYear = minDate.Year;

            var m = (int)Math.Round((kandles.First().CloseTime - kandles.First().OpenTime).TotalMinutes, 0, MidpointRounding.AwayFromZero);

            var ktemplate = getkandletemplate(minYear, minMonth, minDay, multiplier * m);

            foreach (var kandle in ktemplate)
            {
                var k = kandles.Where(x => x.OpenTime >= kandle.OpenTime && x.CloseTime <= kandle.CloseTime)?.OrderBy(x => x.OpenTime);

                if (k.Count() > 0)
                {
                    kandle.Open = k.First().Open;

                    kandle.Close = k.Last().Close;

                    kandle.High = k.Select(x => x.High).Max();

                    kandle.Low = k.Select(x => x.Low).Min();
                }
                else
                {
                    kandle.Open = -999;
                }
            }

            ktemplate.RemoveAll(x => x.Open == -999);

            ktemplate.RemoveAt(0);

            return ktemplate;
        }

        /// <summary>
        /// Gets Kandle Data Distribution Template
        /// </summary>
        /// <param name="interval">Interval of Kandle in Minute</param>
        private List<OHLCKandle> getkandletemplate(int Year, int Month, int Day, int interval)
        {
            List<OHLCKandle> ktemplate = new List<OHLCKandle>();

            var seedDate = new DateTime(Year, Month, Day);

            while (true)
            {
                if (ktemplate.Count == 0)
                {
                    ktemplate.Add(new OHLCKandle
                    {
                        OpenTime = seedDate,
                        CloseTime = seedDate.AddMinutes(interval)
                    });
                }
                else
                {
                    ktemplate.Add(new OHLCKandle
                    {
                        OpenTime = ktemplate.Last().CloseTime,
                        CloseTime = ktemplate.Last().CloseTime.AddMinutes(interval)
                    });
                }

                if (ktemplate.Last().OpenTime > DateTime.Now.AddDays(2))
                {
                    break;
                }
            }

            return ktemplate;
        }

        /// <summary>
        /// reassign ohlc values to kandles
        /// </summary>
        /// <param name="kandles"></param>
        /// <param name="closevalues"></param>
        /// <param name="openvalues"></param>
        public void reassignohlc(ref List<OHLCKandle> kandles, List<decimal> closevalues, List<decimal> openvalues)
        {
            trimseries(ref closevalues, ref openvalues);

            while (kandles.Count != closevalues.Count)
            {
                if (kandles.Count > closevalues.Count)
                {
                    kandles.RemoveAt(0);
                }
                else
                {
                    openvalues.RemoveAt(0);
                    closevalues.RemoveAt(0);
                }
            }

            for (int i = 0; i < kandles.Count; i++)
            {
                kandles[i].Close = closevalues[i];

                kandles[i].Open = openvalues[i];
            }
        }


        public List<OHLCKandle> dema(List<OHLCKandle> kandles, int lookback)
        {
            var kandles_ = new List<OHLCKandle>(kandles);

            var demaclosevalues = dema(kandles_.Select(x => x.Close).ToList(), lookback);

            var demaopenvalues = dema(kandles_.Select(x => x.Open).ToList(), lookback);

            trimseries(ref demaclosevalues, ref demaopenvalues);

            reassignohlc(ref kandles_, demaclosevalues, demaopenvalues);

            return kandles_;
        }

        public List<OHLCKandle> smma(List<OHLCKandle> kandles, int lookback)
        {
            var kandles_ = new List<OHLCKandle>(kandles);

            var smmaclosevalues = smma(kandles_.Select(x => x.Close).ToList(), lookback);

            var smmaopenvalues = smma(kandles_.Select(x => x.Open).ToList(), lookback);

            trimseries(ref smmaclosevalues, ref smmaopenvalues);

            reassignohlc(ref kandles_, smmaclosevalues, smmaopenvalues);

            return kandles_;
        }


        public List<OHLCKandle> superimposekandles(List<OHLCKandle> largekandles, List<OHLCKandle> smallkandles)
        {
            var _smallkandles = new List<OHLCKandle>(smallkandles);

            for (int i = 0; i < _smallkandles.Count; i++)
            {
                var k = largekandles.Where(l => l.OpenTime <= _smallkandles[i].OpenTime && l.CloseTime >= _smallkandles[i].CloseTime);

                if (k.Count() > 0)
                {
                    _smallkandles[i].Close = k.First().Close;

                    _smallkandles[i].Open = k.First().Open;
                }
                else
                {
                    _smallkandles[i] = null;
                }
            }

            _smallkandles.RemoveAll(x => x == null);

            return _smallkandles;
        }











    }
}
