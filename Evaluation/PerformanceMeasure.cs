using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuotesCheck.Evaluation
{
    class PerformanceMeasure
    {
        IList<Trade> trades;
        internal PerformanceMeasure(IList<Trade> trades)
        {
            this.trades = trades;
        }

        public int PositiveTrades => trades.Sum(item => item.SellValue > item.BuyValue ? 1 : 0);

        public int NegativeTrades => trades.Sum(item => item.SellValue < item.BuyValue ? 1 : 0);

        public double OverallGain => trades.Sum(item => item.Gain);

        public override string ToString()
        {
            return $"{OverallGain:F1}% +{PositiveTrades} -{NegativeTrades}";
        }
    }
}
