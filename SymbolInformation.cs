namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    
    using Avapi.AvapiTIME_SERIES_DAILY;

    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    internal class SymbolInformation
    {
        private readonly DataProvider dataProvider = new DataProvider();

        [JsonProperty]
        private string company_name { get; set; }

        [JsonProperty]
        private string bats_name { get; set; }

        [JsonProperty]
        private string isin { get; set; }

        [JsonProperty]
        private string reuters_exchange_code { get; set; }

        public string CompanyName => this.company_name;

        public string ISIN => this.isin;

        public string Symbol => $"{this.bats_name.Substring(0, this.bats_name.Length - 1)}.{this.reuters_exchange_code}";

        [JsonProperty]
        public IList<TimeSeries> TimeSeries { get; private set; }

        public void UpdateTimeSeries()
        {
            // do we have any data at all so far?
            if (this.TimeSeries == null)
            {
                this.TimeSeries = this.dataProvider.GetTimeSeries(this);
                return;
            }
            
            // check how long ago our latest data point is away
            var daysSpan = (DateTime.Now - this.TimeSeries[0].Day).Days;
            var newSeries = this.dataProvider.GetTimeSeries(this, daysSpan > 90 ? Const_TIME_SERIES_DAILY.TIME_SERIES_DAILY_outputsize.full : Const_TIME_SERIES_DAILY.TIME_SERIES_DAILY_outputsize.compact);

            for (var index = newSeries.Count - 1; index >= 0; index--)
            {
                daysSpan = (newSeries[index].Day - this.TimeSeries[0].Day).Days;
                Debug.Assert(daysSpan <= 1);
                if (daysSpan == 1)
                {
                    this.TimeSeries.Insert(0, newSeries[index]);
                }
            }
        }
    }
}