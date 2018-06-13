﻿namespace QuotesCheck
{
    using System.Globalization;
    using System.Threading;

    internal class Program
    {
        private static void Main(string[] args)
        {
            // use always invariant culture
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var symbolProvider = new SymbolProvider();
            var symbol = symbolProvider.LookUpISIN("DE000A0D9PT0");

            var ema20 = Indicators.EMA(symbol, SourceType.Close, 20);
            var dema20 = Indicators.DEMA(symbol, SourceType.Close, 20);
            var sma20 = Indicators.SMA(symbol, SourceType.Close, 20);
            var tema20 = Indicators.TEMA(symbol, SourceType.Close, 20);
            var kama20 = Indicators.KAMA(symbol, SourceType.Close, 20);
            var tma20 = Indicators.TMA(symbol, SourceType.Close, 20);
            var vwma20 = Indicators.VWMA(symbol, SourceType.Close, 20);
            var wma20 = Indicators.WMA(symbol, SourceType.Close, 20);
            var rsl20 = Indicators.RSL(symbol, SourceType.Close, 20);
            var obv20 = Indicators.OBV(symbol, SourceType.Close);
            var (macd, signal) = Indicators.MACD(symbol, SourceType.Close, 12, 26, 9);
            var st = Indicators.ST(symbol, 50, 3);
            var (upper, lower, middle) = Indicators.BB(symbol, SourceType.Close, 20, 2);
            var tp = Indicators.TP(symbol);
            var cci = Indicators.CCI(symbol, 20);
            var stdev = Indicators.STDEV(symbol, SourceType.Close, 20);
            var (bullish, bearish) = Indicators.ELR(symbol, 20);
            var vpt = Indicators.VPT(symbol, SourceType.Close);
            var atr = Indicators.ATR(symbol, 20);
            var hh = Indicators.HH(symbol, 20);
            var ll = Indicators.LL(symbol, 20);
            var vola = Indicators.VOLA(symbol, SourceType.Close, 30, 250);
            var (dmi, diPlus, diMinus) = Indicators.DMI(symbol, 20);
            var adx = Indicators.ADX(symbol, 20);

            var md = Indicators.MD(symbol, SourceType.Close, 20); // --> not correct
        }
    }
}