namespace QuotesCheck.Evaluation
{
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Extensions.Statistics;

    using MathNet.Numerics.Statistics;

    using Newtonsoft.Json;

    internal class PerformanceMeasure
    {
        private readonly IList<Trade> trades;

        internal PerformanceMeasure(IList<Trade> trades)
        {
            this.trades = trades;
        }

        public int PositiveTrades => this.trades.Sum(item => item.SellValue > item.BuyValue ? 1 : 0);

        public int NegativeTrades => this.trades.Sum(item => item.SellValue < item.BuyValue ? 1 : 0);

        [JsonConverter(typeof(DoubleJsonConverter))]
        public double TotalGain => this.trades.Sum(item => item.Gain);

        [JsonConverter(typeof(DoubleJsonConverter))]
        public double BestTrade => this.trades.Count > 0 ? this.trades.Max(item => item.Gain) : 0;

        [JsonConverter(typeof(DoubleJsonConverter))]
        public double WorstTrade => this.trades.Count > 0 ? this.trades.Min(item => item.Gain) : 0;

        public int LongestTrade => this.trades.Count > 0 ? this.trades.Max(item => item.Days) : 0;

        public int ShortestTrade => this.trades.Count > 0 ? this.trades.Min(item => item.Days) : 0;

        public int DaysMedian => this.trades.Count > 0 ? this.trades.MedianBy(item => item.Days).Days : 0;

        [JsonConverter(typeof(DoubleJsonConverter))]
        public double PositiveTradeMedian => this.trades.Where(item => item.Gain > 0).Select(item => item.Gain).Median();

        [JsonConverter(typeof(DoubleJsonConverter))]
        public double NegativeTradeMedian => this.trades.Where(item => item.Gain < 0).Select(item => item.Gain).Median();

        public override string ToString()
        {
            return $"{this.TotalGain:F1}% +{this.PositiveTrades} -{this.NegativeTrades}";
        }
    }
}