namespace QuotesCheck
{
    using System;
    using System.IO;
    using System.Net;

    internal class SymbolProvider
    {
        private const string referencePath = @"ReferenceData\BXESymbols-PROD.csv";

        private const string tempReferencePath = @"ReferenceData\BXESymbols-TEMP.csv";

        public void UpdateReference()
        {
            using (var wc = new WebClient())
            {
                wc.DownloadFile(new Uri("https://batstrading.co.uk/bxe/market_data/symbol_listing/csv/"), tempReferencePath);
            }

            File.Delete(referencePath);
            File.Move(tempReferencePath, referencePath);
        }
    }
}