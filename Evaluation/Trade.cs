namespace QuotesCheck.Evaluation
{
    using System;

    using Newtonsoft.Json;

    public class Trade
    {
        public int BuyIndex { get; set; }

        public double BuyValue { get; set; }

        public int SellIndex { get; set; }

        public double SellValue { get; set; }

        public double HighestValue { get; set; }

        [JsonConverter(typeof(DoubleJsonConverter))]
        public double CostOfTrades { get; set; }

        [JsonConverter(typeof(DateJsonConverter))]
        public DateTime BuyDate { get; set; }

        [JsonConverter(typeof(DateJsonConverter))]
        public DateTime SellDate { get; set; }

        [JsonConverter(typeof(DoubleJsonConverter))]
        public double Gain => (this.BuyValue > 0) && (this.SellValue > 0) ? Helper.Delta(this.SellValue, this.BuyValue) - this.CostOfTrades : 0;

        [JsonConverter(typeof(DoubleJsonConverter))]
        public double PossibleGain => (this.HighestValue > 0) && (this.BuyValue > 0) ? Helper.Delta(this.HighestValue, this.BuyValue) - this.CostOfTrades : 0;

        public int Days => (this.SellDate - this.BuyDate).Days;

        public override string ToString()
        {
            return $"{this.Gain:F}";
        }
    }
}