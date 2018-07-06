namespace QuotesCheck.Evaluation
{
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Math;

    internal class FeatureExtractor
    {
        public FeatureExtractor(IList<TimeSeries> series)
        {
            this.High = series.Select(item => item.High).ToArray();
            this.Low = series.Select(item => item.Low).ToArray();
            this.Close = series.Select(item => item.Close).ToArray();
            this.Open = series.Select(item => item.Open).ToArray();

            this.Ema20 = Indicators.EMA(this.Close, 20);
            this.Ema50 = Indicators.EMA(this.Close, 50);
            this.Ema200 = Indicators.EMA(this.Close, 200);

            var features = new List<(TimeSeries series, double[] features, int label)>();
            
            for (var idx = series.Count - 1; idx >= 50; idx--)
            {
                // features
                var diffEma20_50 = Helper.Delta(this.Ema20[idx], this.Ema50[idx]);
                var diffEma50_200 = Helper.Delta(this.Ema50[idx], this.Ema200[idx]);
                var diffEma20_Close = Helper.Delta(this.Ema20[idx], this.Close[idx]);
                
                // output
                var label = this.FindLabel(this.Open[idx - 1], idx - 1, 50, -5, 10);

                features.Add((series[idx], new[] { diffEma20_50, diffEma50_200, diffEma20_Close }, label));
            }

            this.Features = features.ToArray();
        }

        private int FindLabel(double startValue, int startIndex, int maxCount, double lowerBound, double upperBound)
        {
            for (var idx = startIndex; idx >= startIndex - maxCount; idx--)
            {
                if (Helper.Delta(this.High[idx], startValue) > upperBound)
                {
                    return 1;
                }

                if (Helper.Delta(this.Low[idx], startValue) < lowerBound)
                {
                    return 0;
                }
            }

            return 0;
        }

        public double[] High { get; }

        public double[] Low { get; }

        public double[] Close { get; }

        public double[] Open { get; }

        public double[] Ema20 { get; }

        public double[] Ema50 { get; }

        public double[] Ema200 { get; }

        public (TimeSeries series, double[] features, int label)[] Features { get; }
    }
}