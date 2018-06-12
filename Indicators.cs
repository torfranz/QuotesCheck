namespace QuotesCheck
{
    using System.Diagnostics;

    internal static class Indicators
    {
        public static double[] Ema(double[] data, int period)
        {
            var wf = 2.0 / (period + 1);

            var ema = new double[data.Length];

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var valueAtIndex = data[index];
                Debug.Assert(valueAtIndex > 0);

                var value = index < data.Length - 1 ? ema[index + 1] : valueAtIndex;
                var factor = index < data.Length - 1 ? valueAtIndex - ema[index + 1] : 0;
                ema[index] = value + wf * factor;
            }

            Debug.Assert(ema.Length == data.Length);
            return ema;
        }

        public static double[] Dema(double[] data, int period)
        {
            var ema = Ema(data, period);
            var emaOfEma = Ema(ema, period);

            var dema = new double[data.Length];

            for (var index = data.Length - 1; index >= 0; index--)
            {
                dema[index] = 2 * ema[index] - emaOfEma[index];
            }

            return dema;
        }
    }
}