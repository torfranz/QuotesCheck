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

        private static double nn(double value, double defaultValue)
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
    }
}