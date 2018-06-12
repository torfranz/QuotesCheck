namespace QuotesCheck
{
    using System.Collections.Generic;

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
            this.TimeSeries = this.dataProvider.UpdateTimeSeries(this);
        }
    }
}