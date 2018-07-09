namespace QuotesCheck.Evaluation
{
    using System.Collections.Generic;
    using System.Linq;

    internal class FeatureExtractor
    {
        private double[] KAMA;

        private double[] ST;

        public SymbolInformation Symbol { get; }
        public FeatureExtractor(SymbolInformation symbol)
        {
            this.Symbol = symbol;
            this.High = symbol.High;
            this.Low = symbol.Low;
            this.Close = symbol.Close;
            this.Open = symbol.Open;

            this.Ema20 = Indicators.KAMA(symbol, SourceType.Close, 20);
            this.Ema50 = Indicators.KAMA(symbol, SourceType.Close, 50);
            this.Ema200 = Indicators.KAMA(symbol, SourceType.Close, 200);
            this.RSI = Indicators.RSI(this.Close, 50);
            //this.KAMA = Indicators.KAMA(symbol, SourceType.Close, 50);
            this.ST = Indicators.ST(symbol, 50, 3);
            (this.MACD, this.Signal) = Indicators.MACD(this.Close, 50, 200, 20, MovingAverage.EMA);

            var learnSeries = new List<(int, TimeSeries, double[])>();
            var learnLabels = new List<int>();
            var validationSeries = new List<(int, TimeSeries, double[])>();

            for (var idx = symbol.TimeSeries.Count - 20; idx > 0; idx--)
            {
                // features
                var rsi = 1 - (this.RSI[idx] / 50);
                var ema20_50 = Helper.Delta(this.Ema20[idx], this.Ema50[idx]) / 100.0;
                var ema50_200 = Helper.Delta(this.Ema50[idx], this.Ema200[idx]) / 100.0;
                var ema50_Close = Helper.Delta(this.Ema50[idx], this.Close[idx]) / 100.0;
                var st_Close = Helper.Delta(this.ST[idx], this.Close[idx]) / 100.0;

                var features = new[] { rsi, st_Close, ema20_50, ema50_200, ema50_Close, this.MACD[idx], this.Signal[idx] };

                // decide if the data point goes to learning or validation
                // currently split at 66%
                if (idx > 0.33 * (symbol.TimeSeries.Count - 20))
                {
                    // output
                    learnSeries.Add((idx, symbol.TimeSeries[idx], features));
                    learnLabels.Add(this.FindLabel(this.Open[idx - 1], idx - 1));
                }
                else
                {
                    validationSeries.Add((idx, symbol.TimeSeries[idx], features));
                }
            }

            this.LearnSeries = learnSeries.ToArray();
            this.LearnLabels = learnLabels.ToArray();
            this.ValidationSeries = validationSeries.ToArray();
        }

        public double UpperBound { get; set; } = 10;

        public double LowerBound { get; set; } = -5;

        public int CandleCount { get; set; } = 50;

        public double[] MACD { get; }

        public double[] Signal { get; }

        public double[] RSI { get; }

        public double[] High { get; }

        public double[] Low { get; }

        public double[] Close { get; }

        public double[] Open { get; }

        public double[] Ema20 { get; }

        public double[] Ema50 { get; }

        public double[] Ema200 { get; }

        public (int seriesIdx, TimeSeries series, double[] features)[] LearnSeries { get; }

        public int[] LearnLabels { get; }

        public (int seriesIdx, TimeSeries series, double[] features)[] ValidationSeries { get; }

        private int FindLabel(double startValue, int startIndex)
        {
            for (var idx = startIndex; idx >= startIndex - this.CandleCount; idx--)
            {
                if (Helper.Delta(this.Close[idx], startValue) > this.UpperBound)
                {
                    return 1;
                }

                if (Helper.Delta(this.Close[idx], startValue) < this.LowerBound)
                {
                    return 0;
                }
            }

            return 0;
        }
    }
}