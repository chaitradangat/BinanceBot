using System;
using System.Collections.Generic;
using System.Text;

using BinanceBot.Domain;

namespace BinanceBot.Application
{
    public class BuySellDecision
    {
        public int BuyCounter = 0;

        public int SellCounter = 0;

        public BuySellDecision()
        {

        }

        public void ResetCounters()
        {
            this.BuyCounter = 0;

            this.SellCounter = 0;
        }

        public bool isValidSignal(bool isBuy, bool isSell, int signalStrength)
        {
            if (isBuy && SellCounter == 0)
            {
                ++BuyCounter;
                return BuyCounter >= signalStrength;
            }
            else if (isBuy && SellCounter > 0)
            {
                SellCounter = 0;

                ++BuyCounter;

                return false;
            }
            else if (isSell && BuyCounter == 0)
            {
                ++SellCounter;
                return SellCounter >= signalStrength;
            }
            else if (isSell && BuyCounter > 0)
            {
                BuyCounter = 0;

                ++SellCounter;

                return false;
            }
            else
            {
                return false;
            }
        }

        public bool ExitPosition(SimplePosition order, bool isBuy, bool isSell, decimal longPercentage, decimal shortPercentage, decimal risk, decimal reward, decimal ProfitFactor, int signalStrength)
        {
            if (order.OrderID == -1)
            {
                return false;
            }
            else if (order.OrderType == "BUY")// && (longPercentage <= risk / 2))
            {
                if ((longPercentage <= risk))
                {
                    return true;
                }
                else if (isSell && isValidSignal(isBuy, isSell, signalStrength))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (order.OrderType == "SELL")// && (shortPercentage <= risk / 2))
            {
                if (shortPercentage <= risk)
                {
                    return true;
                }
                else if (isBuy && isValidSignal(isBuy, isSell, signalStrength))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool OpenPosition(SimplePosition order, bool isBuy, bool isSell, int signalStrength, string mood, string trend)
        {
            if (order.OrderID != -1)
            {
                return false;
            }

            if (isBuy && isValidSignal(isBuy, isSell, signalStrength))
            {
                return true;
            }

            if (isSell && isValidSignal(isBuy, isSell, signalStrength))
            {
                return true;
            }

            return false;
        }

        public bool BookProfit(SimplePosition order, bool isBuy, bool isSell, decimal profitFactor, decimal shortPercentage, decimal longPercentage, decimal reward, int signalStrength)
        {
            if (order.OrderID == -1)
            {
                return false;
            }

            if (order.OrderType == "BUY" && isSell && isValidSignal(isBuy, isSell, signalStrength))
            {
                return true;
            }

            if (order.OrderType == "SELL" && isBuy && isValidSignal(isBuy, isSell, signalStrength))
            {
                return true;
            }

            if (order.OrderType == "BUY" && longPercentage >= reward / 2 && isSell)
            {
                return true;
            }

            if (order.OrderType == "BUY" && longPercentage >= profitFactor * reward)
            {
                return true;
            }

            if (order.OrderType == "SELL" && shortPercentage >= reward / 2 && isBuy)
            {
                return true;
            }

            if (order.OrderType == "SELL" && shortPercentage >= profitFactor * reward)
            {
                return true;
            }

            return false;
        }

        public void CalculatePercentageChange(SimplePosition order, decimal currentClose, decimal leverage, ref decimal longPercentage, ref decimal shortPercentage, ref decimal profitFactor)
        {
            if (order.OrderID != -1)
            {
                shortPercentage = leverage * ((order.EntryPrice - currentClose) / order.EntryPrice) * 100;

                longPercentage = leverage * ((currentClose - order.EntryPrice) / order.EntryPrice) * 100;

                if (shortPercentage < 0 && order.OrderType == "SELL")
                {
                    //profitFactor = (decimal)0.4;
                }
                else if (longPercentage < 0 && order.OrderType == "BUY")
                {
                    //profitFactor = (decimal)0.4;
                }
                else
                {

                }
            }
        }

    }
}
