namespace QuotesCheck
{
    using System;
    using System.Diagnostics;

    internal static class Indicators
    {
        private static double At(this double[] data, int index)
        {
            return index >=0 && index < data.Length ? data[index]: double.NaN;
        }

        private static double nn(double value, double defaultValue = 0)
        {
            return double.IsNaN(value) ? defaultValue : value;
        }


        public static double[] Ema(double[] data, int period)
        {
            var wf = 2.0 / (period + 1);

            var ema = new double[data.Length];

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var value = nn(ema.At(index + 1), data[index]);
                var factor = nn(data[index] - ema.At(index + 1), 0);
                ema[index] = value + wf * factor;
            }

            Debug.Assert(ema.Length == data.Length);
            return ema;
        }

        public static double[] Sma(double[] data, int period)
        {
            // sma = nn(sma[1], 0) + (close / n) - nn(close[n] / n, 0)
            var sma = new double[data.Length];

            for (var index = data.Length - 1; index >= 0; index--)
            {
                sma[index] = nn(sma.At(index + 1), 0) + data[index] / period - nn(data.At(index + period) / period, 0);
            }

            Debug.Assert(sma.Length == data.Length);
            return sma;
        }

        public static double[] Dema(double[] data, int period)
        {
            // dema = 2*ema(close, n) - ema(ema(close, n), n)

            var ema = Ema(data, period);
            var emaOfEma = Ema(ema, period);

            var dema = new double[data.Length];

            for (var index = data.Length - 1; index >= 0; index--)
            {
                dema[index] = 2*ema[index] - emaOfEma[index];
            }

            Debug.Assert(dema.Length == data.Length);
            return dema;
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

            var tema = new double[data.Length];

            for (var index = data.Length - 1; index >= 0; index--)
            {
                tema[index] = 3 * ema1[index] - 3 * ema2[index] + ema3[index];
            }

            Debug.Assert(tema.Length == data.Length);
            return tema;
        }

        private static double sum(double[] data, int startIndex, int length)
        {
            var result = 0.0;
            for (int index = startIndex; index < startIndex + length; index++)
            {
                result += nn(data.At(index));
            }
            return result;
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

            var kama = new double[data.Length];
            var xvNoise = new double[data.Length];
            var nSignal = new double[data.Length];

            for (var index = data.Length - 1; index >= 0; index--)
            {
                xvNoise[index] = Math.Abs(xPrice[index] - nn(xPrice.At(index + 1)));
                nSignal[index] = Math.Abs(xPrice[index] - nn(xPrice.At(index + length)));
                var nNoise = sum(xvNoise, index, length);


                var nefRatio = nNoise != 0 ? nSignal[index] / nNoise : 0;
                var nSmooth = Math.Pow(nefRatio * (nFastend - nSlowend) + nSlowend, 2);
                
                kama[index] = nn(kama.At(index + 1)) + nSmooth * (xPrice[index] - nn(kama.At(index + 1)));                
            }

            Debug.Assert(kama.Length == data.Length);
            return kama;
        }
    }
}