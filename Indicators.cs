namespace QuotesCheck
{
    using System;
    using System.Diagnostics;

    internal static class Indicators
    {
        public static double[] Ema(double[] data, int period)
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

        public static double[] Sma(double[] data, int period)
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

        public static double[] Dema(double[] data, int period)
        {
            // dema = 2*ema(close, n) - ema(ema(close, n), n)

            var ema1 = Ema(data, period);
            var ema2 = Ema(ema1, period);

            var dema = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                dema[index] = 2 * ema1[index] - ema2[index];
            }

            Debug.Assert(dema.Length == data.Length);
            return dema;
        }

        public static double[] Vwma(double[] data, int[] volume, int period)
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

        public static double[] Wma(double[] data, int n)
        {
            // wma = loop((i, res){ res+close[i]*(n-i) }, n) / ((pow(n, 2)-n)/2+n)

            var wma = Create(data.Length);

            var divisor = (Math.Pow(n, 2) - n) / 2 + n;

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var res = 0.0;
                for (var i = 0; i < n; i++)
                {
                    res += data.At(index + i) * (n - i);
                }

                wma[index] = res / divisor;
            }

            Debug.Assert(wma.Length == data.Length);
            return wma;
        }

        public static double[] Tema(double[] data, int period)
        {
            // ema1 = ema(close, n)
            // ema2 = ema(ema1, n)
            // ema3 = ema(ema2, n)
            // tema = 3 * ema1 - 3 * ema2 + ema3

            var ema1 = Ema(data, period);
            var ema2 = Ema(ema1, period);
            var ema3 = Ema(ema2, period);

            var tema = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                tema[index] = 3 * ema1[index] - 3 * ema2[index] + ema3[index];
            }

            Debug.Assert(tema.Length == data.Length);
            return tema;
        }

        public static double[] Tma(double[] data, int period)
        {
            // a = n%2 == 0? n/2 : (n+1)/2
            // b = n % 2 == 0 ? a + 1 : a
            // tma = sma(sma(close, a), b)

            var a = period % 2 == 0 ? period / 2 : (period + 1) / 2;
            var b = period % 2 == 0 ? a + 1 : a;

            var sma = Sma(Sma(data, a), b);

            var tma = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                tma[index] = sma[index];
            }

            Debug.Assert(tma.Length == data.Length);
            return tma;
        }

        public static double[] Rsl(double[] data, int period)
        {
            // rsl = close / sma(close, n)
            
            var sma = Sma(data, period);

            var rsl = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                rsl[index] = data[index] / sma[index];
            }

            Debug.Assert(rsl.Length == data.Length);
            return rsl;
        }

        public static double[] KAMA(double[] data, int length)
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

        private static double At(this double[] data, int index)
        {
            return (index >= 0) && (index < data.Length) ? data[index] : double.NaN;
        }

        private static double At(this int[] data, int index)
        {
            return (index >= 0) && (index < data.Length) ? data[index] : double.NaN;
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