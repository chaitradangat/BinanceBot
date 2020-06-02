using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceBot.Domain
{
    public enum StrategyDecision
    {
        //order actions
        Buy,
        Sell,

        //avoid decision type
        SkipOpen,
        SkipMissedOpen,
        SkipTakeProfit,
        SkipExit,
        SkipExitHeavy,
        SkipEscape,

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
        Short,
        None
    }

    public enum SkipReason
    {
        //#avoid or skip reasons
        SkipBuy,
        SkipSell,
        RedKandle,
        GreenKandle,
        InvalidBollinger,
        AgainstTrend,
        LowSignalQuality,
        LowSignalGap,
        InconsistentKandles
    }
}