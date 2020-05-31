using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceBot.Domain
{
    public class SimplePosition
    {
        public SimplePosition()
        {
            this.Quantity = (decimal)0.002;

            this.PositionType = PositionType.None;

            this.EntryPrice = -1;
        }

        public SimplePosition(decimal Quantity)
        {
            this.Quantity = Quantity;

            this.PositionType = PositionType.None;

            this.EntryPrice = -1;
        }

        public decimal Quantity { get; set; }

        public PositionType PositionType { get; set; }

        public decimal EntryPrice { get; set; }

    }
}
