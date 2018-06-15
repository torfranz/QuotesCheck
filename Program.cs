namespace QuotesCheck
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using QuotesCheck.Evaluation;

    internal class Program
    {
        private static void Main(string[] args)
        {
            Trace.TraceInformation($"Started at {DateTime.Now}");
            // use always invariant culture
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var symbolProvider = new SymbolProvider();

            var symbols = Indizes.DAX.ToDictionary(isin => isin, isin => symbolProvider.LookUpISIN(isin, false));

            //var ema20 = Indicators.EMA(symbol, SourceType.Close, 20);
            //var dema20 = Indicators.DEMA(symbol, SourceType.Close, 20);
            //var sma20 = Indicators.SMA(symbol, SourceType.Close, 20);
            //var tema20 = Indicators.TEMA(symbol, SourceType.Close, 20);
            //var kama20 = Indicators.KAMA(symbol, SourceType.Close, 20);
            //var tma20 = Indicators.TMA(symbol, SourceType.Close, 20);
            //var vwma20 = Indicators.VWMA(symbol, SourceType.Close, 20);
            //var wma20 = Indicators.WMA(symbol, SourceType.Close, 20);
            //var rsl20 = Indicators.RSL(symbol, SourceType.Close, 20);
            //var obv20 = Indicators.OBV(symbol, SourceType.Close);
            //var (macd, signal) = Indicators.MACD(symbol, SourceType.Close, 12, 26, 9);
            //var st = Indicators.ST(symbol, 50, 3);
            //var (upper, lower, middle) = Indicators.BB(symbol, SourceType.Close, 20, 2);
            //var tp = Indicators.TP(symbol);
            //var cci = Indicators.CCI(symbol, 20);
            //var stdev = Indicators.STDEV(symbol, SourceType.Close, 20);
            //var (bullish, bearish) = Indicators.ELR(symbol, 20);
            //var vpt = Indicators.VPT(symbol, SourceType.Close);
            //var atr = Indicators.ATR(symbol, 20);
            //var hh = Indicators.HH(symbol, 20);
            //var ll = Indicators.LL(symbol, 20);
            //var vola = Indicators.VOLA(symbol, SourceType.Close, 30, 250);
            //var (dmi, diPlus, diMinus) = Indicators.DMI(symbol, 20);
            //var adx = Indicators.ADX(symbol, 20);
            //var roc = Indicators.ROC(symbol, SourceType.Close, 20);
            //var obos = Indicators.OBOS(symbol, 20);
            //var rsi = Indicators.RSI(symbol, SourceType.Close, 20);
            //var (shortStop, longStop) = Indicators.ELSZ(symbol, 20, 2.5);
            //var dix = Indicators.DIX(symbol, SourceType.Close, 20);
            //var (stochasticLine,triggerLine) = Indicators.DSSBR(symbol, 21, 3, 8);
            //var (fastStochasticLine, fastSmoothedLine) = Indicators.FSTOC(symbol, 5, 3);
            //var (slowStochasticLine, slowSmoothedLine) = Indicators.SSTOC(symbol, 5, 5, 3);
            //var pcr = Indicators.PCR(symbol, 20);
            //var (smi, smiSignal) = Indicators.SMI(symbol, 5, 3);
            //var mom = Indicators.MOM(symbol, SourceType.Close, 20);
            //var(pK, pD, pJ, pKema) = Indicators.KDJ(symbol, 5, 3, 3);
            //var aroonUp = Indicators.AROUp(symbol, SourceType.High, 20);
            //var aroondown = Indicators.ARODown(symbol, SourceType.Low, 20);
            //var aos = Indicators.AOS(symbol, 20);
            // var md = Indicators.MD(symbol, SourceType.Close, 20); // --> not correct

            Trace.TraceInformation("Start optimization");
            Trace.Indent();

            // Single
            var singleOptimizer = new SingleOptimizer(new SimpleEvaluator());

            foreach (var symbol in symbols.Values)
            {
                var singleResult = singleOptimizer.Run(symbol);
                if (singleResult != null)
                {
                    Trace.TraceInformation($"Optimization finished for {singleResult}");
                    singleResult.Save("SingleBestData");
                }
            }

            // Multi
            var optimizer = new MetaOptimizer(new SimpleEvaluator());

            var metaResult = optimizer.Run(symbols.Values.ToArray());
            if (metaResult != null)
            {
                Trace.TraceInformation($"Optimization finished for {metaResult}");
                metaResult.Save("MetaBestData");
            }

            Trace.Unindent();
            Trace.TraceInformation($"Exited at {DateTime.Now}");
            Trace.TraceInformation(string.Empty);
        }
    }
}