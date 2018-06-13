namespace QuotesCheck
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
            var ema20 = Indicators.Ema(symbol.Close, 20);
            var dema20 = Indicators.Dema(symbol.Close, 20);
            var sma20 = Indicators.Sma(symbol.Close, 20);
            var tema20 = Indicators.Tema(symbol.Close, 20);
            var kama20 = Indicators.KAMA(symbol.Close, 20);
            var tma20 = Indicators.Tma(symbol.Close, 20);
            var vwma20 = Indicators.Vwma(symbol.Close, symbol.Volume, 20);
            var wma20 = Indicators.Wma(symbol.Close, 20);
            var rsl20 = Indicators.Rsl(symbol.Close, 20);
            var obv20 = Indicators.Obv(symbol.Close, symbol.Volume);
            var (macd, signal) = Indicators.Macd(symbol.Close, 12, 26, 9);
            var st = Indicators.ST(symbol, 50, 3);
            var (upper, lower, middle) = Indicators.BB(symbol.Close.ToArray(), 20, 2);
            var tp = Indicators.TP(symbol);
            var cci = Indicators.CCI(symbol, 20);
            var stdev = Indicators.STDEV(symbol.Close, 20);
        }
    }
}