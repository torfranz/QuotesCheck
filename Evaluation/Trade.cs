using System;

namespace QuotesCheck.Evaluation
{
    public class Trade
    {
        public int BuyIndex { get; set; }
        public double BuyValue { get; set; }
        public int SellIndex { get; set; }
        public double SellValue { get; set; }
        public double CostOfTrades { get; set; }
        public DateTime BuyDate { get; set; }
        public DateTime SellDate { get; set; }

        public double Gain => this.BuyValue > 0 && this.SellValue > 0 ? Helper.Delta(this.SellValue , this.BuyValue) - CostOfTrades : 0;

        public int Days => (this.SellDate - this.BuyDate).Days;

        public override string ToString()
        {
            return $"{this.Gain:F}";
        }
    }
}
