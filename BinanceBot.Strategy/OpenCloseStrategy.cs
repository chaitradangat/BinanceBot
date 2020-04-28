using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

using PineScriptPort;

using BinanceBot.Domain;

using BinanceBot.Settings;

namespace BinanceBot.Strategy
{
    public class OpenCloseStrategy
    {
        #region -variables to calculate signal strength-
        public int BuyCounter;

        public int SellCounter;

        private StrategyOutput prevOutput;
        #endregion

        #region -variables of strategy configuration-

        private int KandleMultiplier; //3

        private int ExitSignalStrength; //15


        private bool EscapeTraps; //true
        private int EscapeTrapCandleIdx; //3
        private int EscapeTrapSignalStrength; //300


        private bool GrabMissedPosition; //true
        private int MissedPositionStartCandleIndex; //3
        private int MissedPositionEndCandleIndex; //5
        private int MissedPositionSignalStrength; //200

        private bool ExitImmediate;
        #endregion

        public OpenCloseStrategy()
        {
            BuyCounter = 0;

            SellCounter = 0;

            prevOutput = StrategyOutput.None;

            //set strategy variables
            KandleMultiplier = OpenCloseStrategySettings.settings.KandleMultiplier;

            ExitSignalStrength = OpenCloseStrategySettings.settings.ExitSignalStrength;

            ExitImmediate = OpenCloseStrategySettings.settings.ExitImmediate;

            //set escape strategy variables
            EscapeTraps = OpenCloseStrategySettings.settings.EscapeTraps;

            EscapeTrapCandleIdx = OpenCloseStrategySettings.settings.EscapeTrapCandleIdx;

            EscapeTrapSignalStrength = OpenCloseStrategySettings.settings.EscapeTrapSignalStrength;

            //set missed position strategy variables
            GrabMissedPosition = OpenCloseStrategySettings.settings.GrabMissedPosition;

            MissedPositionStartCandleIndex = OpenCloseStrategySettings.settings.MissedPositionStartCandleIndex;

            MissedPositionEndCandleIndex = OpenCloseStrategySettings.settings.MissedPositionEndCandleIndex;

            MissedPositionSignalStrength = OpenCloseStrategySettings.settings.MissedPositionSignalStrength;
        }

        public void RunStrategy(List<OHLCKandle> inputkandles, ref bool isBuy, ref bool isSell, ref string trend, ref string mood, ref string histdata, ref SimplePosition currentPosition, decimal currentClose, decimal risk, decimal reward, decimal leverage, ref decimal shortPercentage, ref decimal longPercentage, ref decimal profitFactor, int signalStrength, ref StrategyOutput stratetgyOutput, decimal decreaseOnNegative)
        {
            PineScriptFunction fn = new PineScriptFunction();

            //higher timeframe candles with smma values
            var largekandles = fn.converttohighertimeframe(inputkandles, KandleMultiplier);//3

            largekandles = fn.smma(largekandles, 8);


            //lower timeframe candles with smma values
            inputkandles = fn.smma(inputkandles, 8);

            var closeseriesmma = inputkandles.Select(x => x.Close).ToList();


            //map higher timeframe values to lower timeframe values
            var altkandles = fn.superimposekandles(largekandles, inputkandles);

            var closeSeriesAlt = altkandles.Select(x => x.Close).ToList();

            var openSeriesAlt = altkandles.Select(x => x.Open).ToList();


            //trend and mood calculation-
            trend = closeSeriesAlt.Last() > openSeriesAlt.Last() ? "BULLISH" : "BEARISH";

            mood = closeseriesmma.Last() > openSeriesAlt.Last() ? "BULLISH" : "BEARISH";


            //buy sell signal
            var xlong = fn.crossover(closeSeriesAlt, openSeriesAlt);

            var xshort = fn.crossunder(closeSeriesAlt, openSeriesAlt);

            isBuy = xlong.Last();

            isSell = xshort.Last();


            //historical data
            histdata = "";
            for (int i = 0; i < xlong.Count; i++)
            {
                if (xlong[i])
                {
                    histdata += " B" + (xlong.Count - i - 1).ToString();
                }
                else if (xshort[i])
                {
                    histdata += " S" + (xlong.Count - i - 1).ToString();
                }
                else
                {
                    // meh :\
                }
            }

            stratetgyOutput = MakeBuySellDecision(isBuy, isSell, trend, mood, currentClose, ref currentPosition, risk, reward, leverage, ref shortPercentage, ref longPercentage, ref profitFactor, signalStrength, histdata, decreaseOnNegative);

            if (stratetgyOutput != StrategyOutput.None)
            {
                ResetCounters();

                profitFactor = (decimal)1;
            }
        }

        private void ResetCounters()
        {
            BuyCounter = 0;

            SellCounter = 0;

            prevOutput = StrategyOutput.None;
        }

        private bool IsValidSignal(bool isBuy, bool isSell, int signalStrength, StrategyOutput currentState, ref StrategyOutput prevState)
        {
            if (prevState != currentState)
            {
                BuyCounter = 0;

                SellCounter = 0;

                prevState = currentState;
            }

            //simple logic of current and previous states match
            if (prevState == currentState && prevState != StrategyOutput.None)
            {
                if (isBuy && (currentState == StrategyOutput.OpenPositionWithBuy || currentState == StrategyOutput.BookProfitWithBuy))
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (isSell && (currentState == StrategyOutput.OpenPositionWithSell || currentState == StrategyOutput.BookProfitWithSell))
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (isBuy && currentState == StrategyOutput.ExitPositionWithBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (isSell && currentState == StrategyOutput.ExitPositionWithSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (ExitImmediate && currentState == StrategyOutput.ExitPositionWithBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (ExitImmediate && currentState == StrategyOutput.ExitPositionWithSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyOutput.EscapeTrapWithBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyOutput.EscapeTrapWithSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyOutput.MissedPositionBuy)
                {
                    ++BuyCounter;

                    return BuyCounter >= signalStrength;
                }
                if (!isBuy && !isSell && currentState == StrategyOutput.MissedPositionSell)
                {
                    ++SellCounter;

                    return SellCounter >= signalStrength;
                }
            }

            return false;
        }

        private bool ExitPosition(SimplePosition order, bool isBuy, bool isSell, decimal longPercentage, decimal shortPercentage, decimal risk, int signalStrength)
        {
            if (order.OrderID == -1)
            {
                //no positions to exit from
                return false;
            }
            else if (order.OrderType == "BUY" && longPercentage <= risk && IsValidSignal(isBuy, isSell, ExitSignalStrength, StrategyOutput.ExitPositionWithSell, ref prevOutput))//15
            {
                return true;
            }
            else if (order.OrderType == "SELL" && shortPercentage <= risk && IsValidSignal(isBuy, isSell, ExitSignalStrength, StrategyOutput.ExitPositionWithBuy, ref prevOutput))//15
            {
                return true;
            }
            else if (order.OrderType == "BUY" && isSell && IsValidSignal(isBuy, isSell, signalStrength / 2, StrategyOutput.ExitPositionWithSell, ref prevOutput))
            {
                return true;
            }
            else if (order.OrderType == "SELL" && isBuy && IsValidSignal(isBuy, isSell, signalStrength / 2, StrategyOutput.ExitPositionWithBuy, ref prevOutput))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool OpenPosition(SimplePosition order, bool isBuy, bool isSell, int signalStrength, string mood, string trend)
        {
            if (order.OrderID != -1)
            {
                return false;
            }

            if (isBuy && IsValidSignal(isBuy, isSell, signalStrength, StrategyOutput.OpenPositionWithBuy, ref prevOutput))
            {
                return true;
            }

            if (isSell && IsValidSignal(isBuy, isSell, signalStrength, StrategyOutput.OpenPositionWithSell, ref prevOutput))
            {
                return true;
            }

            return false;
        }

        private bool BookProfit(SimplePosition order, bool isBuy, bool isSell, decimal profitFactor, decimal shortPercentage, decimal longPercentage, decimal reward, int signalStrength)
        {
            if (order.OrderID == -1)
            {
                return false;
            }

            if (order.OrderType == "BUY" && longPercentage >= profitFactor * reward)
            {
                return true;
            }

            if (order.OrderType == "SELL" && shortPercentage >= profitFactor * reward)
            {
                return true;
            }

            if (order.OrderType == "BUY" && isSell && longPercentage >= reward / 2)
            {
                return true;
            }

            if (order.OrderType == "SELL" && isBuy && shortPercentage >= reward / 2)
            {
                return true;
            }

            return false;
        }

        private bool EscapeTrap(SimplePosition position, bool isBuy, bool isSell, string histdata)
        {
            //no open positions
            if (position.OrderID == -1)
            {
                return false;
            }

            //no historical decisions available
            if (string.IsNullOrEmpty(histdata))
            {
                return false;
            }

            //in middle of decision
            if (isBuy || isSell)
            {
                return false;
            }

            //invalid historical data
            string decisiontype = "";
            int decisionperiod = -1;
            GetLatestDecision(histdata, ref decisiontype, ref decisionperiod);
            if (string.IsNullOrEmpty(decisiontype) || decisionperiod == -1)
            {
                return false;
            }

            //the bot is trapped with sell position!!
            if (position.OrderType == "SELL" && decisiontype == "B" && decisionperiod >= EscapeTrapCandleIdx && IsValidSignal(false, false, EscapeTrapSignalStrength, StrategyOutput.EscapeTrapWithBuy, ref prevOutput))//3,300
            {
                return true;
            }

            //the bot is trapped with buy position
            if (position.OrderType == "BUY" && decisiontype == "S" && decisionperiod >= EscapeTrapCandleIdx && IsValidSignal(false, false, EscapeTrapSignalStrength, StrategyOutput.EscapeTrapWithSell, ref prevOutput))//3,300
            {
                return true;
            }

            return false;
        }

        private bool OpenMissedPosition(SimplePosition position, bool isBuy, bool isSell, string histdata)
        {
            //position already exists
            if (position.OrderID != -1)
            {
                return false;
            }

            //in middle of a decision
            if (isBuy || isSell)
            {
                return false;
            }

            //no historical data available
            if (string.IsNullOrEmpty(histdata))
            {
                return false;
            }

            //invalid historical data
            string decisiontype = "";
            int decisionperiod = -1;
            GetLatestDecision(histdata, ref decisiontype, ref decisionperiod);
            if (string.IsNullOrEmpty(decisiontype) || decisionperiod == -1)
            {
                return false;
            }

            //missed buy position
            if (decisiontype == "B" && decisionperiod >= MissedPositionStartCandleIndex && decisionperiod <= MissedPositionEndCandleIndex && IsValidSignal(false, false, MissedPositionSignalStrength, StrategyOutput.MissedPositionBuy, ref prevOutput))//3,5,200
            {
                return true;
            }

            //missed sell position
            if (decisiontype == "S" && decisionperiod >= MissedPositionStartCandleIndex && decisionperiod <= MissedPositionEndCandleIndex && IsValidSignal(false, false, MissedPositionSignalStrength, StrategyOutput.MissedPositionSell, ref prevOutput))//3,5,200
            {
                return true;
            }

            return false;
        }

        private void CalculatePercentageChange(SimplePosition order, decimal currentClose, decimal leverage, ref decimal longPercentage, ref decimal shortPercentage, ref decimal profitFactor, decimal decreaseOnNegative)
        {
            if (order.OrderID != -1)
            {
                shortPercentage = leverage * ((order.EntryPrice - currentClose) / order.EntryPrice) * 100;

                longPercentage = leverage * ((currentClose - order.EntryPrice) / order.EntryPrice) * 100;

                if (shortPercentage < 0 && order.OrderType == "SELL")
                {
                    profitFactor = decreaseOnNegative;
                }
                else if (longPercentage < 0 && order.OrderType == "BUY")
                {
                    profitFactor = decreaseOnNegative;
                }
                else
                {
                    //meh :\
                }
            }
        }

        private StrategyOutput MakeBuySellDecision(bool isBuy, bool isSell, string trend, string mood, decimal currentClose, ref SimplePosition order, decimal risk, decimal reward, decimal leverage, ref decimal shortPercentage, ref decimal longPercentage, ref decimal profitFactor, int signalStrength, string histData, decimal decreaseOnNegative)
        {
            var sOutput = StrategyOutput.None;

            CalculatePercentageChange(order, currentClose, leverage, ref longPercentage, ref shortPercentage, ref profitFactor, decreaseOnNegative);

            if (OpenPosition(order, isBuy, isSell, signalStrength, mood, trend))
            {
                if (isBuy)
                {
                    sOutput = StrategyOutput.OpenPositionWithBuy;
                }
                if (isSell)
                {
                    sOutput = StrategyOutput.OpenPositionWithSell;
                }
            }
            else if (ExitPosition(order, isBuy, isSell, longPercentage, shortPercentage, risk, signalStrength))
            {
                if (order.OrderType == "BUY")
                {
                    sOutput = StrategyOutput.ExitPositionWithSell;
                }
                if (order.OrderType == "SELL")
                {
                    sOutput = StrategyOutput.ExitPositionWithBuy;
                }
            }
            else if (BookProfit(order, isBuy, isSell, profitFactor, shortPercentage, longPercentage, reward, signalStrength))
            {
                if (order.OrderType == "BUY")
                {
                    sOutput = StrategyOutput.BookProfitWithSell;
                }
                if (order.OrderType == "SELL")
                {
                    sOutput = StrategyOutput.BookProfitWithBuy;
                }
            }
            else if (EscapeTrap(order, isBuy, isSell, histData) && EscapeTraps)
            {
                if (order.OrderType == "BUY")
                {
                    sOutput = StrategyOutput.EscapeTrapWithSell;
                }
                if (order.OrderType == "SELL")
                {
                    sOutput = StrategyOutput.EscapeTrapWithBuy;
                }
            }
            else if (OpenMissedPosition(order, isBuy, isSell, histData) && GrabMissedPosition)
            {
                string decisiontype = "";

                int decisionperiod = 0;

                GetLatestDecision(histData, ref decisiontype, ref decisionperiod);

                if (decisiontype == "B")
                {
                    sOutput = StrategyOutput.MissedPositionBuy;
                }
                if (decisiontype == "S")
                {
                    sOutput = StrategyOutput.MissedPositionSell;
                }
            }
            else
            {
                sOutput = StrategyOutput.None;
            }

            return sOutput;
        }

        private void GetLatestDecision(string histdata, ref string decisiontype, ref int decisionperiod)
        {
            decisiontype = "";

            decisionperiod = -1;

            if (string.IsNullOrEmpty(histdata))
            {
                //no historical decisions available
                return;
            }

            var latestdecision = histdata.Split(' ').Last();

            if (!string.IsNullOrEmpty(latestdecision))
            {
                if (latestdecision.Contains("B"))
                {
                    decisiontype = "B";
                }
                else if (latestdecision.Contains("S"))
                {
                    decisiontype = "S";
                }
                else
                {
                    decisiontype = "";
                    decisionperiod = -1;
                }

                decisionperiod = Convert.ToInt32(latestdecision.Replace(decisiontype, ""));
            }
            else
            {
                decisiontype = "";
                decisionperiod = -1;
            }
        }
    }
}