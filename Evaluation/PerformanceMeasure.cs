namespace QuotesCheck.Evaluation
{
    using System.Collections.Generic;
    using System.Linq;
    
    using MathNet.Numerics.Statistics;

    internal class PerformanceMeasure
    {
        private readonly IList<Trade> trades;

        internal PerformanceMeasure(IList<Trade> trades)
        {
            this.trades = trades;
        }

        public int PositiveTrades => this.trades.Sum(item => item.SellValue > item.BuyValue ? 1 : 0);

        public int NegativeTrades => this.trades.Sum(item => item.SellValue < item.BuyValue ? 1 : 0);

        public double OverallGain => this.trades.Sum(item => item.Gain);

        public double BestTrade => this.trades.Max(item => item.Gain);

        public double WorstTrade => this.trades.Min(item => item.Gain);

        public double PositiveTradeMedian => this.trades.Where(item => item.Gain > 0).Select(item => item.Gain).Median();

        public double NegativeTradeMedian => this.trades.Where(item => item.Gain < 0).Select(item => item.Gain).Median();

        public override string ToString()
        {
            return $"{this.OverallGain:F1}% +{this.PositiveTrades} -{this.NegativeTrades}";
        }
    }
}