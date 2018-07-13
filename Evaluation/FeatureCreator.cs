namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;

    using Accord;
    using Accord.Math;

    internal class FeatureCreator
    {
        public double[][][] GenerateFeatures(SymbolInformation symbol, IntRange range)
        {
            var High = symbol.High;
            var Low = symbol.Low;
            var Close = symbol.Close;
            var Open = symbol.Open;
            this.Volume = symbol.Volume;
            var days = symbol.Day;

            this.Ema20 = Indicators.EMA(symbol, SourceType.Close, 20);
            this.Ema50 = Indicators.EMA(symbol, SourceType.Close, 50);
            this.Ema200 = Indicators.EMA(symbol, SourceType.Close, 200);
            this.RSI = Indicators.RSI(Close, 50).Scale(0, 100, -1.0, 1);

            //this.KAMA = Indicators.KAMA(symbol, SourceType.Close, 50);
            var ST = Indicators.ST(symbol, 50, 3);
            (this.MACD, this.Signal) = Indicators.MACD(Close, 50, 200, 20, MovingAverage.EMA);

            var Ema20_50 = Indicators.RelativeDistance(this.Ema20, this.Ema50).Scale(-0.20, 0.20, -1.0, 1);
            var Ema50_200 = Indicators.RelativeDistance(this.Ema50, this.Ema200).Scale(-0.20, 0.20, -1.0, 1);
            var Ema50_Close = Indicators.RelativeDistance(this.Ema50, Close).Scale(-0.40, 0.40, -1.0, 1);
            var St_Close = Indicators.RelativeDistance(ST, Close).Scale(-0.30, 0.30, -1.0, 1);
            var Macd_Close = Indicators.Ratio(this.MACD, Close).Scale(-0.30, 0.30, -1.0, 1);
            var Signal_Close = Indicators.Ratio(this.Signal, this.MACD).Scale(-0.30, 0.30, -1.0, 1);
            var vola = Indicators.VOLA(symbol, SourceType.Close, 30, 250).Scale(0, 0.7, -1.0, 1);
            var obos = Indicators.OBOS(symbol, 50).Scale(0, 100, -1.0, 1);
            var (elrBullish, elrBearish) = Indicators.ELR(symbol, 50);
            var bullish = Indicators.Ratio(elrBullish, Close).Scale(-0.10, 0.10, -1.0, 1);
            var bearish = Indicators.Ratio(elrBearish, Close).Scale(-0.10, 0.10, -1.0, 1);
            var (dmi, diPlus, diMinus) = Indicators.DMI(symbol, 50);
            dmi = dmi.Scale(0, 100, -1.0, 1);
            diPlus = diPlus.Scale(0, 100, -1.0, 1);
            diMinus = diMinus.Scale(0, 100, -1.0, 1);

            var indexedFeatures = new double[range.Length][][];
            for (var idx = range.Min; idx <= range.Max; idx++)
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
                var intradayGain = Helper.Delta(Close[idx], Open[idx]).Scale(-10.0, 10, -1.0, 1);
                var todaysGain = Helper.Delta(Close[idx], Close[idx + 1]).Scale(-10.0, 10, -1.0, 1);
                var todaysVolumeGain = Helper.Delta(this.Volume[idx], this.Volume[idx + 1]).Scale(-20.0, 20, -1.0, 1);

                var features = new[]
                                   {
                                       new[] { rsi, ema20_50, ema50_200, ema50_Close, signal_Macd, st_Close, bullish[idx] },
                                       new[] { dayOfWeek, season, intradayGain, todaysGain, todaysVolumeGain }
                                   };

                indexedFeatures[idx] = features;
            }

            return indexedFeatures;
        }

        public int[] Volume { get; set; }
        
        public double[] MACD { get; private set; }

        public double[] Signal { get; private set; }

        public double[] RSI { get; private set; }

       public double[] Ema20 { get; private set; }

        public double[] Ema50 { get; private set; }

        public double[] Ema200 { get; private set; }

        
    }
}