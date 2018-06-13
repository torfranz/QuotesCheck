﻿namespace QuotesCheck
{
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    internal class Program
    {
        private static void Main(string[] args)
        {
            // use always invariant culture
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var symbolProvider = new SymbolProvider();
            var symbol = symbolProvider.LookUpISIN("DE000A0D9PT0");
            var ema20 = Indicators.Ema(symbol.TimeSeries.Select(item => item.Close).ToArray(), 20);
            var dema20 = Indicators.Dema(symbol.TimeSeries.Select(item => item.Close).ToArray(), 20);
            var sma20 = Indicators.Sma(symbol.TimeSeries.Select(item => item.Close).ToArray(), 20);
            var tema20 = Indicators.Tema(symbol.TimeSeries.Select(item => item.Close).ToArray(), 20);
            var kama20 = Indicators.KAMA(symbol.TimeSeries.Select(item => item.Close).ToArray(), 20);
            var tma20 = Indicators.Tma(symbol.TimeSeries.Select(item => item.Close).ToArray(), 20);
            var vwma20 = Indicators.Vwma(symbol.TimeSeries.Select(item => item.Close).ToArray(), symbol.TimeSeries.Select(item => item.Volume).ToArray(), 20);
            var wma20 = Indicators.Wma(symbol.TimeSeries.Select(item => item.Close).ToArray(), 20);
            var rsl20 = Indicators.Rsl(symbol.TimeSeries.Select(item => item.Close).ToArray(), 20);
            var obv20 = Indicators.Obv(symbol.TimeSeries.Select(item => item.Close).ToArray(), symbol.TimeSeries.Select(item => item.Volume).ToArray());
            var (macd, signal) = Indicators.Macd(symbol.TimeSeries.Select(item => item.Close).ToArray(), 12, 26, 9);
        }
    }
}