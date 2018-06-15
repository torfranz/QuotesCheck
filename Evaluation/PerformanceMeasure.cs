namespace QuotesCheck.Evaluation
{
    using System.Collections.Generic;
    using System.Linq;

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

        public override string ToString()
        {
            return $"{this.OverallGain:F1}% +{this.PositiveTrades} -{this.NegativeTrades}";
        }
    }
}