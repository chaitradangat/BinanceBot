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

        //variables for basic strategy setup
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

        //variables to set signal strength for decisions
        [ConfigurationProperty("OpenPositionSignalStrength", IsRequired = true)]
        public int OpenPositionSignalStrength
        {
            get
            {
                return Convert.ToInt32(this["OpenPositionSignalStrength"]);
            }
            set
            {
                this["OpenPositionSignalStrength"] = value;
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
        [ConfigurationProperty("ExitPositionHeavyLossSignalStrength", IsRequired = true)]
        public int ExitPositionHeavyLossSignalStrength
        {
            get
            {
                return Convert.ToInt32(this["ExitPositionHeavyLossSignalStrength"]);
            }
            set
            {
                this["ExitPositionHeavyLossSignalStrength"] = value;
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
        [ConfigurationProperty("TakeProfitSignalStrength", IsRequired = true)]
        public int TakeProfitSignalStrength
        {
            get
            {
                return Convert.ToInt32(this["TakeProfitSignalStrength"]);
            }
            set
            {
                this["TakeProfitSignalStrength"] = value;
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

        //variables for escape traps decision policy
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

        //variables for missed position decision policy
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
        
        //variables for exit heavy decision policy
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





        //variables for validating the decision taken
        [ConfigurationProperty("BollingerFactor", IsRequired = true)]
        public decimal BollingerFactor
        {
            get
            {
                return decimal.Parse(this["BollingerFactor"].ToString());
            }
            set
            {
                this["BollingerFactor"] = value;
            }
        }
        [ConfigurationProperty("SignalGap", IsRequired = true)]
        public int SignalGap
        {
            get
            {
                return Convert.ToInt32(this["SignalGap"]);
            }
            set
            {
                this["SignalGap"] = value;
            }
        }
        [ConfigurationProperty("ValidationRuleSet", IsRequired = true)]
        public string ValidationRuleSet
        {
            get
            {
                //sanitize the input ruleset for garbage charachters
                return Convert.ToString(this["ValidationRuleSet"]).Replace(" ", "").Replace("\t", "").Replace("\r\n", "");
            }
            set
            {
                this["ValidationRuleSet"] = value;
            }
        }
        [ConfigurationProperty("DecisionSet", IsRequired = true)]
        public string DecisionSet
        {
            get
            {
                //sanitize the input ruleset for garbage charachters
                return Convert.ToString(this["DecisionSet"]).Replace(" ", "").Replace("\t", "").Replace("\r\n", "");
            }
            set
            {
                this["DecisionSet"] = value;
            }
        }
        [ConfigurationProperty("BollingerCrossLookBack", IsRequired = true)]
        public int BollingerCrossLookBack
        {
            get
            {
                return Convert.ToInt32(this["BollingerCrossLookBack"]);
            }
            set
            {
                this["BollingerCrossLookBack"] = value;
            }
        }
        [ConfigurationProperty("ConsistentKandlesLookBack", IsRequired = true)]
        public int ConsistentKandlesLookBack
        {
            get
            {
                return Convert.ToInt32(this["ConsistentKandlesLookBack"]);
            }
            set
            {
                this["ConsistentKandlesLookBack"] = value;
            }
        }
        [ConfigurationProperty("RequiredSignalQuality", IsRequired = true)]
        public int RequiredSignalQuality
        {
            get
            {
                return Convert.ToInt32(this["RequiredSignalQuality"]);
            }
            set
            {
                this["RequiredSignalQuality"] = value;
            }
        }
    }
}