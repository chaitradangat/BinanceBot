using System;
using System.Configuration;

namespace BinanceBot.Settings
{
    public class OpenCloseStrategySettings : ConfigurationSection
    {
        public static OpenCloseStrategySettings settings
        {
            get
            {
                return ConfigurationManager.GetSection("OpenCloseStrategySettings") as OpenCloseStrategySettings;
            }
        }

        [ConfigurationProperty("KandleMultiplier", IsRequired = true)]
        public int KandleMultiplier
        {
            get
            {
                return Convert.ToInt32(this["KandleMultiplier"]);
            }
            set
            {
                this["KandleMultiplier"] = value;
            }
        }
        [ConfigurationProperty("ExitSignalStrength", IsRequired = true)]
        public int ExitSignalStrength
        {
            get
            {
                return Convert.ToInt32(this["ExitSignalStrength"]);
            }
            set
            {
                this["ExitSignalStrength"] = value;
            }
        }
        [ConfigurationProperty("EscapeTrapCandleIdx", IsRequired = true)]
        public int EscapeTrapCandleIdx
        {
            get
            {
                return Convert.ToInt32(this["EscapeTrapCandleIdx"]);
            }
            set
            {
                this["EscapeTrapCandleIdx"] = value;
            }
        }
        [ConfigurationProperty("EscapeTrapSignalStrength", IsRequired = true)]
        public int EscapeTrapSignalStrength
        {
            get
            {
                return Convert.ToInt32(this["EscapeTrapSignalStrength"]);
            }
            set
            {
                this["EscapeTrapSignalStrength"] = value;
            }
        }
        [ConfigurationProperty("MissedPositionStartCandleIndex", IsRequired = true)]
        public int MissedPositionStartCandleIndex
        {
            get
            {
                return Convert.ToInt32(this["MissedPositionStartCandleIndex"]);
            }
            set
            {
                this["MissedPositionStartCandleIndex"] = value;
            }
        }
        [ConfigurationProperty("MissedPositionEndCandleIndex", IsRequired = true)]
        public int MissedPositionEndCandleIndex
        {
            get
            {
                return Convert.ToInt32(this["MissedPositionEndCandleIndex"]);
            }
            set
            {
                this["MissedPositionEndCandleIndex"] = value;
            }
        }
        [ConfigurationProperty("MissedPositionSignalStrength", IsRequired = true)]
        public int MissedPositionSignalStrength
        {
            get
            {
                return Convert.ToInt32(this["MissedPositionSignalStrength"]);
            }
            set
            {
                this["MissedPositionSignalStrength"] = value;
            }
        }
        [ConfigurationProperty("EscapeTraps", IsRequired = true)]
        public bool EscapeTraps
        {
            get
            {
                return bool.Parse(this["EscapeTraps"].ToString());
            }
            set
            {
                this["EscapeTraps"] = value;
            }
        }
        [ConfigurationProperty("GrabMissedPosition", IsRequired = true)]
        public bool GrabMissedPosition
        {
            get
            {
                return bool.Parse(this["GrabMissedPosition"].ToString());
            }
            set
            {
                this["GrabMissedPosition"] = value;
            }
        }
        [ConfigurationProperty("ExitImmediate", IsRequired = true)]
        public bool ExitImmediate
        {
            get
            {
                return bool.Parse(this["ExitImmediate"].ToString());
            }
            set
            {
                this["ExitImmediate"] = value;
            }
        }
        [ConfigurationProperty("HeavyRiskPercentage", IsRequired = true)]
        public decimal HeavyRiskPercentage
        {
            get
            {
                return decimal.Parse(this["HeavyRiskPercentage"].ToString());
            }
            set
            {
                this["HeavyRiskPercentage"] = value;
            }
        }
        [ConfigurationProperty("Smoothing", IsRequired = true)]
        public string Smoothing
        {
            get
            {
                return Convert.ToString(this["Smoothing"]);
            }
            set
            {
                this["Smoothing"] = value;
            }
        }
    }
}