namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;

    using Accord;
    using Accord.Math;

    internal class FeatureExtractor
    {
        private readonly double[] ST;

        private double[] KAMA;

        public FeatureExtractor(SymbolInformation symbol, int timeSlices = 3)
        {
            this.LearnSeries = new (int seriesIdx, TimeSeries series, double[][] features)[timeSlices][];
            this.LearnLabels = new int[timeSlices][];
            var validationSeries = new List<(int, TimeSeries, double[][])>();
            var testSeries = new List<(int, TimeSeries, double[][])>();
            var learnSeries = new List<(int, TimeSeries, double[][])>[timeSlices];
            var learnLabels = new List<int>[timeSlices];

            this.Symbol = symbol;
            this.High = symbol.High;
            this.Low = symbol.Low;
            this.Close = symbol.Close;
            this.Open = symbol.Open;
            this.Volume = symbol.Volume;
            var days = symbol.Day;

            this.Ema20 = Indicators.EMA(symbol, SourceType.Close, 20);
            this.Ema50 = Indicators.EMA(symbol, SourceType.Close, 50);
            this.Ema200 = Indicators.EMA(symbol, SourceType.Close, 200);
            this.RSI = Indicators.RSI(this.Close, 50).Scale(0, 100, -1.0, 1);

            //this.KAMA = Indicators.KAMA(symbol, SourceType.Close, 50);
            this.ST = Indicators.ST(symbol, 50, 3);
            (this.MACD, this.Signal) = Indicators.MACD(this.Close, 50, 200, 20, MovingAverage.EMA);

            var Ema20_50 = Indicators.RelativeDistance(this.Ema20, this.Ema50).Scale(-0.20, 0.20, -1.0, 1);
            var Ema50_200 = Indicators.RelativeDistance(this.Ema50, this.Ema200).Scale(-0.20, 0.20, -1.0, 1);
            var Ema50_Close = Indicators.RelativeDistance(this.Ema50, this.Close).Scale(-0.40, 0.40, -1.0, 1);
            var St_Close = Indicators.RelativeDistance(this.ST, this.Close).Scale(-0.30, 0.30, -1.0, 1);
            var Macd_Close = Indicators.Ratio(this.MACD, this.Close).Scale(-0.30, 0.30, -1.0, 1);
            var Signal_Close = Indicators.Ratio(this.Signal, this.MACD).Scale(-0.30, 0.30, -1.0, 1);
            var vola = Indicators.VOLA(symbol, SourceType.Close, 30, 250).Scale(0, 0.7, -1.0, 1);
            var obos = Indicators.OBOS(symbol, 50).Scale(0, 100, -1.0, 1);
            var (elrBullish, elrBearish) = Indicators.ELR(symbol, 50);
            var bullish = Indicators.Ratio(elrBullish, this.Close).Scale(-0.10, 0.10, -1.0, 1);
            var bearish = Indicators.Ratio(elrBearish, this.Close).Scale(-0.10, 0.10, -1.0, 1);
            var (dmi, diPlus, diMinus) = Indicators.DMI(symbol, 50);
            dmi = dmi.Scale(0, 100, -1.0, 1);
            diPlus = diPlus.Scale(0, 100, -1.0, 1);
            diMinus = diMinus.Scale(0, 100, -1.0, 1);

            var startIndex = symbol.TimeSeries.Count - 20;

            // ranges
            var fullLearningRange = new IntRange(Convert.ToInt32(0.4 * startIndex), startIndex);
            var validationRange = new IntRange(Convert.ToInt32(0.2 * startIndex), Convert.ToInt32(0.4 * startIndex));
            var testRange = new IntRange(0, Convert.ToInt32(0.2 * startIndex));

            var learnRanges = new IntRange[timeSlices];
            for (var idx = 0; idx < timeSlices; idx++)
            {
                var overlappingSliceLength = fullLearningRange.Length / (timeSlices - 0);
                var sliceLength = fullLearningRange.Length / timeSlices;
                var sliceStart = fullLearningRange.Min + idx * (sliceLength - (overlappingSliceLength - sliceLength) / (timeSlices - 0));
                var sliceEnd = sliceStart + overlappingSliceLength;
                learnRanges[idx] = new IntRange(sliceStart, sliceEnd);
                learnSeries[idx] = new List<(int, TimeSeries, double[][])>();
                learnLabels[idx] = new List<int>();
            }

            for (var idx = startIndex; idx > 0; idx--)
            {
                // features
                var dayOfWeek = Convert.ToInt32(days[idx].DayOfWeek).Scale(0, 6, 1.0, 1);
                var season = Math.Abs(6.5 - days[idx].Month).Scale(0.5, 5.5, 1.0, 1);
                var rsi = this.RSI[idx];
                var ema20_50 = Ema20_50[idx];
                var ema50_200 = Ema50_200[idx];
                var ema50_Close = Ema50_Close[idx];
                var st_Close = St_Close[idx];
                var macd_Close = Macd_Close[idx];
                var signal_Macd = Signal_Close[idx];
                var intradayGain = Helper.Delta(this.Close[idx], this.Open[idx]).Scale(-10.0, 10, -1.0, 1);
                var todaysGain = Helper.Delta(this.Close[idx], this.Close[idx + 1]).Scale(-10.0, 10, -1.0, 1);
                var todaysVolumeGain = Helper.Delta(this.Volume[idx], this.Volume[idx + 1]).Scale(-20.0, 20, -1.0, 1);

                var features = new[]
                                   {
                                       new[] { rsi, ema20_50, ema50_200, ema50_Close, signal_Macd, st_Close, bullish[idx] },
                                       new[] { dayOfWeek, season, intradayGain, todaysGain, todaysVolumeGain }
                                   };

                // decide if the data point goes to learning, validation or test
                for (var sliceIdx = 0; sliceIdx < timeSlices; sliceIdx++)
                {
                    if (learnRanges[sliceIdx].IsInside(idx))
                    {
                        learnSeries[sliceIdx].Add((idx, symbol.TimeSeries[idx], features));
                        learnLabels[sliceIdx].Add(this.FindLabel(this.Open[idx - 1], idx - 1));
                    }
                }

                if (validationRange.IsInside(idx))
                {
                    validationSeries.Add((idx, symbol.TimeSeries[idx], features));
                }

                if (testRange.IsInside(idx))
                {
                    testSeries.Add((idx, symbol.TimeSeries[idx], features));
                }
            }

            for (var idx = 0; idx < timeSlices; idx++)
            {
                this.LearnLabels[idx] = learnLabels[idx].ToArray();
                this.LearnSeries[idx] = learnSeries[idx].ToArray();
            }

            this.ValidationSeries = validationSeries.ToArray();
            this.TestSeries = testSeries.ToArray();
        }

        public int[] Volume { get; set; }

        public SymbolInformation Symbol { get; }

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

        public (int seriesIdx, TimeSeries series, double[][] features)[][] LearnSeries { get; }

        public int[][] LearnLabels { get; }

        public (int seriesIdx, TimeSeries series, double[][] features)[] ValidationSeries { get; }

        public (int seriesIdx, TimeSeries series, double[][] features)[] TestSeries { get; }

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