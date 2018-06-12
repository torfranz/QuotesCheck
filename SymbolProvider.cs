namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;

    using CsvHelper;

    internal class SymbolProvider
    {
        private const string ReferencePath = @"ReferenceData\BXESymbols-PROD.csv";

        private const string TempReferencePath = @"ReferenceData\BXESymbols-TEMP.csv";

        public SymbolProvider(bool updateFirst = false)
        {
            if (updateFirst) this.UpdateReference();

            this.Read();
        }

        public IEnumerable<SymbolInformation> Symbols { get; private set; } = Enumerable.Empty<SymbolInformation>();

        public void UpdateReference()
        {
            using (var wc = new WebClient())
            {
                wc.DownloadFile(new Uri("https://batstrading.co.uk/bxe/market_data/symbol_listing/csv/"), TempReferencePath);
            }

            File.Delete(ReferencePath);
            File.Move(TempReferencePath, ReferencePath);
        }

        public void Read()
        {
            using (TextReader fileReader = File.OpenText(ReferencePath))
            {
                var firstLine = fileReader.ReadLine();

                var csv = new CsvReader(fileReader);
                csv.Configuration.IncludePrivateMembers = true;
                this.Symbols = csv.GetRecords<SymbolInformation>().ToArray();
            }
        }

        public SymbolInformation LookUpISIN(string isin)
        {
            return this.Symbols.First(item => Equals(item.ISIN.ToUpperInvariant(), isin.ToUpperInvariant()));
        }
    }
}