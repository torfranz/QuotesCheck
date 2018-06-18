using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuotesCheck.Evaluation
{
    static class Helper
    {
        public static bool IsWithin(this double value, double middle, double extension)
        {
            return value >= middle - extension && value <= middle + extension;
        }

        public static double Delta(double d1, double d2)
        {
            return 100.0 * (d1 - d2) / d1;
        }

        public static double Slope(double[] values, int startIndex, int length)
        {
            double[] x = new double[length];
            double[] y = new double[length];
            for (int i = 0; i < length; i++)
            {
                x[i] = startIndex + i;
                y[i] = values[startIndex + i];
            }
            var (a, b) = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(x, y);
            return -b;
        }
    }
}
