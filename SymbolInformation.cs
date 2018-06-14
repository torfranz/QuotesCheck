namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;

    using Avapi.AvapiTIME_SERIES_DAILY;

    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    internal class SymbolInformation
    {
        private readonly DataProvider dataProvider = new DataProvider();

        private double[] close;

        private double[] open;

        private double[] high;

        private double[] low;

        private int[] volume;

        private DateTime[] date;

        [JsonProperty]
        public IList<TimeSeries> TimeSeries { get; private set; } = new List<TimeSeries>();

        public string CompanyName => this.company_name;

        public string ISIN => this.isin;

        public string Symbol => $"{this.bats_name.Substring(0, this.bats_name.Length - 1)}.{this.reuters_exchange_code}";

        public double[] Close => this.close ?? (this.close = this.TimeSeries.Select(item => item.Close).ToArray());

        public double[] Open => this.open ?? (this.open = this.TimeSeries.Select(item => item.Open).ToArray());

        public double[] High => this.high ?? (this.high = this.TimeSeries.Select(item => item.High).ToArray());

        public double[] Low => this.low ?? (this.low = this.TimeSeries.Select(item => item.Low).ToArray());

        public int[] Volume => this.volume ?? (this.volume = this.TimeSeries.Select(item => item.Volume).ToArray());

        public DateTime[] Day => this.date ?? (this.date = this.TimeSeries.Select(item => item.Day).ToArray());

        [JsonProperty]
        private string company_name { get; set; }

        [JsonProperty]
        private string bats_name { get; set; }

        [JsonProperty]
        private string isin { get; set; }

        [JsonProperty]
        private string reuters_exchange_code { get; set; }

        public double[] Data(SourceType sourceType)
        {
            switch (sourceType)
            {
                case SourceType.Close:
                    return this.Close;
                case SourceType.Open:
                    return this.Open;
                case SourceType.High:
                    return this.High;
                case SourceType.Low:
                    return this.Low;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        public void UpdateTimeSeries()
        {
            // reset extracted series
            this.open = null;
            this.close = null;
            this.high = null;
            this.low = null;
            this.volume = null;

            // do we have any data at all so far?
            if (this.TimeSeries.Count == 0)
            {
                this.TimeSeries = this.dataProvider.GetTimeSeries(this);
            }
            else
            {
                // remove the first entry to get it always updated
                this.TimeSeries.RemoveAt(0);

                // check how long ago our latest data point is away
                var daysSpan = (DateTime.Now - this.TimeSeries[0].Day).Days;
                var newSeries = this.dataProvider.GetTimeSeries(
                    this,
                    daysSpan > 90 ? Const_TIME_SERIES_DAILY.TIME_SERIES_DAILY_outputsize.full : Const_TIME_SERIES_DAILY.TIME_SERIES_DAILY_outputsize.compact);

                for (var index = newSeries.Count - 1; index >= 0; index--)
                {
                    daysSpan = (newSeries[index].Day - this.TimeSeries[0].Day).Days;
                    Debug.Assert(daysSpan <= 1);
                    if (daysSpan == 1)
                    {
                        Trace.TraceInformation($"Add new data {newSeries[index]}");
                        this.TimeSeries.Insert(0, newSeries[index]);
                    }
                }
            }

            Debug.Assert(this.TimeSeries.Count > 2);

            // check for missing data, replace with previous day data
            for (var i = 1; i < this.TimeSeries.Count; i++)
            {
                var series = this.TimeSeries[i];
                if ((series.Close == 0) || (series.Open == 0) || (series.High == 0) || (series.Low == 0) || (series.Volume == 0))
                {
                    Trace.TraceWarning($"Data {series} is (partially) missing and will be substituted with previous day data");
                    var previousSeries = this.TimeSeries[i - 1];
                    series.Close = series.Close != 0 ? series.Close : previousSeries.Close;
                    series.Open = series.Open != 0 ? series.Open : previousSeries.Open;
                    series.High = series.High != 0 ? series.High : previousSeries.High;
                    series.Low = series.Low != 0 ? series.Low : previousSeries.Low;
                    series.Volume = series.Volume != 0 ? series.Volume : previousSeries.Volume;
                }
            }
        }
    }
}