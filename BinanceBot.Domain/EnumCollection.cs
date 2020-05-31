using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceBot.Domain
{
    public enum StrategyDecision
    {
        #region -old code retained for histroical sakes-
        /*
        OpenPositionWithBuy,
        OpenPositionWithSell,
        ExitPositionWithBuy,
        ExitPositionWithSell,
        BookProfitWithBuy,
        BookProfitWithSell,
        EscapeTrapWithBuy,
        EscapeTrapWithSell,
        MissedPositionBuy,
        MissedPositionSell,
        ExitPositionHeavyLossWithBuy,
        ExitPositionHeavyLossWithSell,
        AvoidOpenWithBuy,
        AvoidOpenWithSell,
        AvoidLowSignalGapBuy,
        AvoidLowSignalGapSell,
        AvoidOpenWithBuyOnRedKandle,
        AvoidOpenWithSellOnGreenKandle,
        AvoidEscapeWithSell,
        AvoidEscapeWithBuy,
        AvoidBuyNoEntryPoint,
        AvoidSellNoEntryPoint,
        AvoidInvalidBollingerOpenBuy,
        AvoidInvalidBollingerOpenSell,
        AvoidEscapeBuyOppositeTrend,
        AvoidEscapeSellOppositeTrend,
        AvoidBadSignalQualityBuy,
        AvoidBadSignalQualitySell,*/
        #endregion

        //avoid reasons
        RedKandle,
        GreenKandle,
        InvalidBollinger,
        AgainstTrend,
        LowSignalQuality,
        LowSignalGap,
        InconsistentKandles,

        //avoid decision type
        SkipOpen,
        SkipMissedOpen,
        SkipExit,
        SkipEscape,


        //order actions
        Buy,
        Sell,
        
        //decision type
        Open,
        OpenMissed,
        TakeProfit,
        Exit,
        ExitHeavy,
        Escape,
        None
    }

    public enum PositionType
    { 
        Buy,
        Sell,
        Long,
        Short
    }
}