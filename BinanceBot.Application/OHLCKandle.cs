using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceBot.Application
{
    public class OHLCKandle
    {
        public decimal Open { get; set; }

        public decimal Close { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public DateTime OpenTime { get; set; }

        public DateTime CloseTime { get; set; }
    }
}
