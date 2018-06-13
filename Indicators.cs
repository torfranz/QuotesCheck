namespace QuotesCheck
{
    using System;
    using System.Diagnostics;

    internal static class Indicators
    {
        public static double[] EMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return EMA(symbol.Data(sourceType), period);
        }

        public static double[] SMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return SMA(symbol.Data(sourceType), period);
        }

        public static double[] DEMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return DEMA(symbol.Data(sourceType), period);
        }

        public static double[] VWMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return VWMA(symbol.Data(sourceType), symbol.Volume, period);
        }

        public static double[] VPT(SymbolInformation symbol, SourceType sourceType)
        {
            return VPT(symbol.Data(sourceType), symbol.Volume);
        }

        public static double[] OBV(SymbolInformation symbol, SourceType sourceType)
        {
            return OBV(symbol.Data(sourceType), symbol.Volume);
        }

        public static double[] MD(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return MD(symbol.Data(sourceType), period);
        }

        public static double[] WMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return WMA(symbol.Data(sourceType), period);
        }

        public static (double[] MACD, double[] Signal) MACD(SymbolInformation symbol, SourceType sourceType, int fastPeriod, int slowPeriod, int signalPeriod)
        {
            return MACD(symbol.Data(sourceType), fastPeriod, slowPeriod, signalPeriod);
        }

        public static double[] TEMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return TEMA(symbol.Data(sourceType), period);
        }

        public static double[] ST(SymbolInformation symbol, int period, double factor)
        {
            // p = integer("Period", 10)
            // f = integer("Factor", 3)

            // # calculation
            // atr = sum(max(high - low, high - close[1], close[1] - low), p) / p
            // up = (high + low) / 2 - f * atr
            // down = (high + low) / 2 + f * atr

            // trendUp = close[1] > trendUp[1] ? max(up, trendUp[1]) : up
            // trendDown = close[1] < trendDown[1] ? min(down, trendDown[1]) : down
            // trend = close > trendDown[1] ? 1 : (close < trendUp[1] ? -1 : nn(trend[1], 1))

            // st = skip(trend == 1 ? trendUp : trendDown, p)

            var high = symbol.High;
            var low = symbol.Low;
            var close = symbol.Close;

            var max = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                max[index] = Math.Max(Math.Max(high[index] - low[index], high[index] - close.At(index + 1)), close.At(index + 1) - low[index]);
            }

            var atr = Create(close.Length);
            var trendUp = Create(close.Length);
            var trendDown = Create(close.Length);
            var trend = new int[close.Length];
            var st = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                atr[index] = Sum(max, index, period) / period;
                var up = (high[index] + low[index]) / 2 - factor * atr[index];
                var down = (high[index] + low[index]) / 2 + factor * atr[index];

                trendUp[index] = close.At(index + 1) > trendUp.At(index + 1) ? Math.Max(up, trendUp.At(index + 1)) : up;
                trendDown[index] = close.At(index + 1) < trendDown.At(index + 1) ? Math.Min(down, trendDown.At(index + 1)) : down;
                trend[index] = close[index] > trendDown.At(index + 1) ? 1 : (close[index] < trendUp.At(index + 1) ? -1 : trend.At(index + 1, 1));

                st[index] = trend[index] == 1 ? trendUp[index] : trendDown[index];
            }

            return st;
        }

        public static double[] TP(SymbolInformation symbol)
        {
            // tp = (close + high + low) / 3

            var high = symbol.High;
            var low = symbol.Low;
            var close = symbol.Close;

            var tp = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                tp[index] = (close[index] + low[index] + high[index]) / 3;
            }

            return tp;
        }

        public static (double[] Bullish, double[] Bearish) ELR(SymbolInformation symbol, int length)
        {
            // x = ema(close, length)
            // bullish = high-x
            // bearish = low-x

            var high = symbol.High;
            var low = symbol.Low;
            var close = symbol.Close;

            var x = EMA(close, length);
            var bullish = Create(close.Length);
            var bearish = Create(close.Length);

            for (var index = close.Length - 1; index >= 0; index--)
            {
                bullish[index] = high[index] - x[index];
                bearish[index] = low[index] - x[index];
            }

            return (bullish, bearish);
        }

        public static double[] CCI(SymbolInformation symbol, int period)
        {
            // n = integer("Period", 20)

            // # calculation
            // tp = (high + low + close) / 3
            // avg = sum(tp, n) / n
            // sAvg = loop((i, res) { res + abs(tp[i] - avg) }, n) / n
            // cci = (sAvg == 0 ? 0 : ((tp - avg) / (0.015 * sAvg)))

            var close = symbol.Close;

            var tp = TP(symbol);
            var cci = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                var avg = Sum(tp, index, period) / period;
                var res = 0.0;
                for (var i = 0; i < period; i++)
                {
                    res += Math.Abs(tp.At(index + i) - avg);
                }

                var sAvg = res / period;
                cci[index] = sAvg == 0 ? 0 : (tp[index] - avg) / (0.015 * sAvg);
            }

            return cci;
        }

        public static double[] ATR(SymbolInformation symbol, int period)
        {
            // atr = sum(max(high - low, high - close[1], close[1] - low), n) / n
            var close = symbol.Close;
            var high = symbol.High;
            var low = symbol.Low;

            var max = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                max[index] = Math.Max(Math.Max(high[index] - low[index], high[index] - close.At(index + 1)), close.At(index + 1) - low[index]);
            }

            var atr = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                atr[index] = Sum(max, index, period) / period;
            }

            return atr;
        }

        public static double[] TMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return TMA(symbol.Data(sourceType), period);
        }

        public static double[] STDEV(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return STDEV(symbol.Data(sourceType), period);
        }

        public static (double[] Upper, double[] Lower, double[] Middle) BB(SymbolInformation symbol, SourceType sourceType, int period, double factor)
        {
            return BB(symbol.Data(sourceType), period, factor);
        }

        public static double[] RSL(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return RSL(symbol.Data(sourceType), period);
        }

        public static double[] KAMA(SymbolInformation symbol, SourceType sourceType, int length)
        {
            return KAMA(symbol.Data(sourceType), length);
        }

        private static double[] EMA(double[] data, int period)
        {
            // wf = 2 / (n + 1)
            // ema = nn(ema[1], close) + wf * nn(close - ema[1], 0)

            var wf = 2.0 / (period + 1);

            var ema = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                ema[index] = nn(ema.At(index + 1), data[index]) + wf * nn(data[index] - ema.At(index + 1));
            }

            Debug.Assert(ema.Length == data.Length);
            return ema;
        }

        private static double[] SMA(double[] data, int period)
        {
            // sma = nn(sma[1], 0) + (close / n) - nn(close[n] / n, 0)
            var sma = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                sma[index] = nn(sma.At(index + 1)) + data[index] / period - nn(data.At(index + period) / period);
            }

            Debug.Assert(sma.Length == data.Length);
            return sma;
        }

        private static double[] DEMA(double[] data, int period)
        {
            // dema = 2*ema(close, n) - ema(ema(close, n), n)

            var ema1 = EMA(data, period);
            var ema2 = EMA(ema1, period);

            var dema = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                dema[index] = 2 * ema1[index] - ema2[index];
            }

            Debug.Assert(dema.Length == data.Length);
            return dema;
        }

        private static double Median(double[] data, int startIndex, int length)
        {
            var workingData = new double[length];

            for (var i = 0; i < length; i++)
            {
                workingData[i] = data.At(startIndex + i);
            }
            
            Array.Sort(workingData);
            return workingData.Length % 2 == 0
                       ? (workingData[workingData.Length / 2 - 1] + workingData[workingData.Length / 2]) / 2
                       : workingData[workingData.Length / 2];
        }

        private static double[] MD(double[] data, int period)
        {
            // md = md(close, n)

            var median = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                median[index] = Math.Abs(data[index] - Median(data, index, period));
            }

            Debug.Assert(median.Length == data.Length);
            return median;
        }

        private static double[] VWMA(double[] data, int[] volume, int period)
        {
            // vwma = loop((i, res){ res+close[i]*volume[i] }, n) / sum(volume, n)

            var vwma = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var divisor = Sum(volume, index, period);

                var res = 0.0;
                for (var i = 0; i < period; i++)
                {
                    res += data.At(index + i) * volume.At(index + i);
                }

                vwma[index] = res / divisor;
            }

            Debug.Assert(vwma.Length == data.Length);
            return vwma;
        }

        private static double[] VPT(double[] data, int[] volume)
        {
            // vpt = nn(vpt[1],0) + volume*((close-close[1])/close[1])

            var vpt = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                vpt[index] = nn(vpt.At(index + 1)) + volume[index] * ((data[index] - data.At(index + 1)) / data.At(index + 1));
            }

            Debug.Assert(vpt.Length == data.Length);
            return vpt;
        }

        private static double[] OBV(double[] data, int[] volume)
        {
            // obv = nn(close < close[1] ? obv[1]-volume : (close > close[1] ? obv[1]+volume : obv[1]), 0)

            var obv = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                obv[index] = nn(
                    data[index] < data.At(index + 1)
                        ? obv.At(index + 1) - volume[index]
                        : (data[index] > data.At(index + 1) ? obv.At(index + 1) + volume[index] : obv.At(index + 1)));
            }

            Debug.Assert(obv.Length == data.Length);
            return obv;
        }

        private static double[] WMA(double[] data, int period)
        {
            // wma = loop((i, res){ res+close[i]*(n-i) }, n) / ((pow(n, 2)-n)/2+n)

            var wma = Create(data.Length);

            var divisor = (Math.Pow(period, 2) - period) / 2 + period;

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var res = 0.0;
                for (var i = 0; i < period; i++)
                {
                    res += data.At(index + i) * (period - i);
                }

                wma[index] = res / divisor;
            }

            Debug.Assert(wma.Length == data.Length);
            return wma;
        }

        private static (double[] MACD, double[] Signal) MACD(double[] data, int fastPeriod, int slowPeriod, int signalPeriod)
        {
            // macd = ema(close, p1) - ema(close, p2)
            // signal = ema(macd, pS)

            var macd = Create(data.Length);

            var emaFast = EMA(data, fastPeriod);
            var emaSlow = EMA(data, slowPeriod);

            // macd
            for (var index = data.Length - 1; index >= 0; index--)
            {
                macd[index] = emaFast[index] - emaSlow[index];
                macd[index] = emaFast[index] - emaSlow[index];
            }

            Debug.Assert(macd.Length == data.Length);
            return (macd, EMA(macd, signalPeriod));
        }

        private static double[] TEMA(double[] data, int period)
        {
            // ema1 = ema(close, n)
            // ema2 = ema(ema1, n)
            // ema3 = ema(ema2, n)
            // tema = 3 * ema1 - 3 * ema2 + ema3

            var ema1 = EMA(data, period);
            var ema2 = EMA(ema1, period);
            var ema3 = EMA(ema2, period);

            var tema = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                tema[index] = 3 * ema1[index] - 3 * ema2[index] + ema3[index];
            }

            Debug.Assert(tema.Length == data.Length);
            return tema;
        }

        private static double[] TMA(double[] data, int period)
        {
            // a = n%2 == 0? n/2 : (n+1)/2
            // b = n % 2 == 0 ? a + 1 : a
            // tma = sma(sma(close, a), b)

            var a = period % 2 == 0 ? period / 2 : (period + 1) / 2;
            var b = period % 2 == 0 ? a + 1 : a;

            var sma = SMA(SMA(data, a), b);

            var tma = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                tma[index] = sma[index];
            }

            Debug.Assert(tma.Length == data.Length);
            return tma;
        }

        private static double[] STDEV(double[] data, int period)
        {
            // stdev(close, n)
            var stdev = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                stdev[index] = Stdev(data, index, period);
            }

            Debug.Assert(stdev.Length == data.Length);
            return stdev;
        }

        private static (double[] Upper, double[] Lower, double[] Middle) BB(double[] data, int period, double factor)
        {
            // # input
            // n = integer("Period", 20)
            // f = float("Factor", 2)

            // # calculation
            // middle = sma(close, n)
            // upper = middle + f * stdev(close, n)
            // lower = middle - f * stdev(close, n)

            var middle = SMA(data, period);
            var upper = Create(data.Length);
            var lower = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                upper[index] = middle[index] + factor * Stdev(data, index, period);
                lower[index] = middle[index] - factor * Stdev(data, index, period);
            }

            Debug.Assert(middle.Length == data.Length);
            return (upper, lower, middle);
        }

        private static double[] RSL(double[] data, int period)
        {
            // rsl = close / sma(close, n)

            var sma = SMA(data, period);

            var rsl = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                rsl[index] = data[index] / sma[index];
            }

            Debug.Assert(rsl.Length == data.Length);
            return rsl;
        }

        private static double[] KAMA(double[] data, int length)
        {
            // # input
            // length = integer("length", 21)
            // nFastend = 0.666
            // nSlowend = 0.0645

            // # calculation
            // xPrice = close

            // xvNoise = abs(xPrice - xPrice[1])
            // nSignal = abs(xPrice - xPrice[length])
            // nNoise = sum(xvNoise, length)

            // nefRatio = nNoise != 0 ? nSignal / nNoise : 0
            // nSmooth = pow(nefRatio * (nFastend - nSlowend) + nSlowend, 2)
            // nAMA = nn(nAMA[1]) + nSmooth * (xPrice - nn(nAMA[1]))

            var nFastend = 0.666;
            var nSlowend = 0.0645;
            var xPrice = data;

            var kama = Create(data.Length);
            var xvNoise = Create(data.Length);
            var nSignal = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                xvNoise[index] = Math.Abs(xPrice[index] - xPrice.At(index + 1));
                nSignal[index] = Math.Abs(xPrice[index] - xPrice.At(index + length));
                var nNoise = Sum(xvNoise, index, length);

                var nefRatio = nNoise != 0 ? nSignal[index] / nNoise : 0;
                var nSmooth = Math.Pow(nefRatio * (nFastend - nSlowend) + nSlowend, 2);

                kama[index] = nn(kama.At(index + 1)) + nSmooth * (xPrice[index] - nn(kama.At(index + 1)));
            }

            Debug.Assert(kama.Length == data.Length);
            return kama;
        }

        private static double Stdev(double[] data, int startIndex, int length)
        {
            var sum = 0.0;
            for (var index = startIndex; index < startIndex + length; index++)
            {
                sum += data.At(index);
            }

            var average = sum / length;
            var sumOfDerivation = 0.0;
            for (var index = startIndex; index < startIndex + length; index++)
            {
                sumOfDerivation += (data.At(index) - average) * (data.At(index) - average);
            }

            return Math.Sqrt(sumOfDerivation / (length - 1));
        }

        private static double At(this double[] data, int index)
        {
            return (index >= 0) && (index < data.Length) ? data[index] : double.NaN;
        }

        private static int At(this int[] data, int index, int defaultValue = 0)
        {
            return (index >= 0) && (index < data.Length) ? data[index] : defaultValue;
        }

        private static double nn(double value, double defaultValue = 0)
        {
            return double.IsNaN(value) ? defaultValue : value;
        }

        private static double[] Create(int length, double value = double.NaN)
        {
            var values = new double[length];

            for (var index = length - 1; index >= 0; index--)
            {
                values[index] = value;
            }

            return values;
        }

        private static double Sum(double[] data, int startIndex, int length)
        {
            var result = 0.0;
            for (var index = startIndex; index < startIndex + length; index++)
            {
                result += data.At(index);
            }

            return result;
        }

        private static double Sum(int[] data, int startIndex, int length)
        {
            var result = 0.0;
            for (var index = startIndex; index < startIndex + length; index++)
            {
                result += data.At(index);
            }

            return result;
        }
    }
}