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
            var dataProvider = new DataProvider();

            var symbol = symbolProvider.LookUpISIN("DE000A0D9PT0");
            var dailyAdjusted = dataProvider.GetDailyData(symbol);
            var ema20 = Indicators.Ema(dailyAdjusted.Select(item => item.Close).ToArray(), 20);
        }
    }
}