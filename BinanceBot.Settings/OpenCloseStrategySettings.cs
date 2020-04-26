using System;
using System.Collections.Generic;
using System.Text;

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

        [ConfigurationProperty("StopLossSignalStrength", DefaultValue = 20, IsRequired = false)]
        public int StopLossSignalStrength
        {
            get
            {
                return Convert.ToInt32(this["StopLossSignalStrength"]);
            }
            set
            {
                this["StopLossSignalStrength"] = value;
            }
        }


        //commented code for reference
        /*[ConfigurationProperty("frontPagePostCount", DefaultValue = 20, IsRequired = false)]
        [IntegerValidator(MinValue = 1, MaxValue = 100)]
        public int FrontPagePostCount
        {
            get { return (int)this["frontPagePostCount"]; }
            set { this["frontPagePostCount"] = value; }
        }


        [ConfigurationProperty("title", IsRequired = true)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\", MinLength = 1, MaxLength = 256)]
        public string Title
        {
            get { return (string)this["title"]; }
            set { this["title"] = value; }
        }*/


    }
}
