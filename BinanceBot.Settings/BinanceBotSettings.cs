using System;
using System.Collections.Generic;
using System.Text;

using System.Configuration;

namespace BinanceBot.Settings
{
    public class BinanceBotSettings : ConfigurationSection
    {
        public static BinanceBotSettings settings
        {
            get
            {
                return ConfigurationManager.GetSection("BinanceBotSettings") as BinanceBotSettings;
            }
        }

        [ConfigurationProperty("PINGTIMER", DefaultValue = 1500, IsRequired = false)]
        public int PingTimer
        {
            get
            {
                return Convert.ToInt32(this["PINGTIMER"]);
            }
            set
            {
                this["PINGTIMER"] = value;
            }
        }

        [ConfigurationProperty("SYMBOL", IsRequired = true)]
        public string Symbol
        {
            get
            {
                return Convert.ToString(this["SYMBOL"]);
            }
            set
            {
                this["SYMBOL"] = value;
            }

        }

        [ConfigurationProperty("QUANTITY", IsRequired = true)]
        public decimal Quantity
        {
            get
            {
                return decimal.Parse(this["QUANTITY"].ToString());
            }
            set
            {
                this["QUANTITY"] = value;
            }
        }

        [ConfigurationProperty("APIKEY",  IsRequired = true)]
        public string ApiKey
        {
            get
            {
                return Convert.ToString(this["APIKEY"]);
            }
            set
            {
                this["APIKEY"] = value;
            }

        }

        [ConfigurationProperty("APISECRET", IsRequired = true)]
        public string ApiSecret
        {
            get
            {
                return Convert.ToString(this["APISECRET"]);
            }
            set
            {
                this["APISECRET"] = value;
            }

        }

        [ConfigurationProperty("RISKPERCENTAGE", IsRequired = true)]
        public decimal RiskPercentage
        {
            get
            {
                return decimal.Parse(this["RISKPERCENTAGE"].ToString());
            }
            set
            {
                this["RISKPERCENTAGE"] = value;
            }
        }

        [ConfigurationProperty("REWARDPERCENTAGE",  IsRequired = true)]
        public decimal RewardPercentage
        {
            get
            {
                return decimal.Parse(this["REWARDPERCENTAGE"].ToString());
            }
            set
            {
                this["REWARDPERCENTAGE"] = value;
            }
        }

        [ConfigurationProperty("DECREASEONNEGATIVE", IsRequired = true)]
        public decimal DecreaseOnNegative
        {
            get
            {
                return decimal.Parse(this["DECREASEONNEGATIVE"].ToString());
            }
            set
            {
                this["DECREASEONNEGATIVE"] = value;
            }
        }

        [ConfigurationProperty("LEVERAGE", IsRequired = true)]
        public decimal Leverage
        {
            get
            {
                return decimal.Parse(this["LEVERAGE"].ToString());
            }
            set
            {
                this["LEVERAGE"] = value;
            }
        }

        [ConfigurationProperty("SIGNALSTRENGTH", IsRequired = true)]
        public int SignalStrength
        {
            get
            {
                return Convert.ToInt32(this["SIGNALSTRENGTH"]);
            }
            set
            {
                this["SIGNALSTRENGTH"] = value;
            }
        }

        [ConfigurationProperty("TIMEFRAME", IsRequired = true)]
        public string TimeFrame
        {
            get
            {
                return Convert.ToString(this["TIMEFRAME"]);
            }
            set
            {
                this["TIMEFRAME"] = value;
            }

        }

        [ConfigurationProperty("CANDLECOUNT", IsRequired = true)]
        public int CandleCount
        {
            get
            {
                return Convert.ToInt32(this["CANDLECOUNT"]);
            }
            set
            {
                this["CANDLECOUNT"] = value;
            }
        }

        [ConfigurationProperty("ISLIVE", IsRequired = true)]
        public bool IsLive
        {
            get
            {
                return bool.Parse(this["ISLIVE"].ToString());
            }
            set
            {
                this["ISLIVE"] = value;
            }
        }

        [ConfigurationProperty("REOPENONESCAPE", IsRequired = true)]
        public bool ReOpenOnEscape
        {
            get
            {
                return bool.Parse(this["REOPENONESCAPE"].ToString());
            }
            set
            {
                this["REOPENONESCAPE"] = value;
            }
        }


    }
}
