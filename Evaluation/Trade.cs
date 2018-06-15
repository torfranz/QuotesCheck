using System;

namespace QuotesCheck.Evaluation
{
    public class Trade
    {
        public double BuyValue { get; set; }
        public double SellValue { get; set; }
        public DateTime BuyDate { get; set; }
        public DateTime SellDate { get; set; }

        public double Gain => this.BuyValue > 0 && this.SellValue > 0 ? (this.SellValue - this.BuyValue) / this.BuyValue * 100 : 0;

        public int Days => (this.SellDate - this.BuyDate).Days;

        public override string ToString()
        {
            return $"{this.Gain:F}";
        }
    }
}
