namespace QuotesCheck.Evaluation
{
    using System;

    using MathNet.Numerics.LinearRegression;

    internal static class Helper
    {
        public static bool IsWithin(this double value, double middle, double extension)
        {
            return (value >= middle - extension) && (value <= middle + extension);
        }

        public static double Delta(double d1, double d2)
        {
            return 100.0 * (d1 - d2) / d2;
        }

        public static double Slope(double[] values, int startIndex, int length)
        {
            var x = new double[length];
            var y = new double[length];
            for (var i = 0; i < length; i++)
            {
                x[i] = startIndex + i;
                y[i] = values[startIndex + i];
            }

            var (a, b) = SimpleRegression.Fit(x, y);
            return -b;
        }
    }
}