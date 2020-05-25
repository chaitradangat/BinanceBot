using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceBot.Domain
{
    public enum StrategyDecision
    {
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
        AvoidBadSignalQualitySell,
        Buy,
        Sell,
        Open,
        Exit,
        None
    }
}
