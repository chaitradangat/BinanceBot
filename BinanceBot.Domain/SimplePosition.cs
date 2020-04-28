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
        }

        public long PositionID { get; set; }

        public decimal Quantity { get; set; }

        public string PositionType { get; set; }

        public decimal EntryPrice { get; set; }

        public string Trend { get; set; }

        public string Mood { get; set; }
    }
}
