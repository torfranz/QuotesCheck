namespace QuotesCheck
{
    using System;

    internal class TimeSeries
    {
        public double Open { get; set; }

        public double Close { get; set; }

        public double High { get; set; }

        public double Low { get; set; }

        public int Volume { get; set; }

        public DateTime Day { get; set; }

        public override string ToString()
        {
            return $"{this.Day:d} - O: {this.Open:F} C: {this.Close:F} H: {this.High:F} L: {this.Low:F} V: {this.Volume}";
        }
    }
}