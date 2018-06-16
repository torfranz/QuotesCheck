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
    }
}
