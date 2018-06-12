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
    }
}