namespace QuotesCheck
{
    internal class SymbolInformation
    {
        private string company_name { get; set; }

        private string bats_name { get; set; }

        private string isin { get; set; }

        private string reuters_exchange_code { get; set; }

        public string CompanyName => this.company_name;

        public string ISIN => this.isin;

        public string Symbol => $"{this.bats_name.Substring(0, this.bats_name.Length - 1)}.{this.reuters_exchange_code}";
    }
}