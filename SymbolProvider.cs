namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;

    using CsvHelper;

    internal class SymbolProvider
    {
        private const string Folder = @"ReferenceData";

        private readonly string referencePath = Path.Combine(Folder, "BXESymbols-PROD.csv");

        private readonly string tempReferencePath = Path.Combine(Folder, "BXESymbols-TEMP.csv");

        private IEnumerable<SymbolInformation> emptySymbols;

        public SymbolProvider(bool updateFirst = false)
        {
            if (updateFirst)
            {
                this.UpdateReference();
            }
        }

        private IDictionary<string, SymbolInformation> Symbols { get; } = new Dictionary<string, SymbolInformation>();

        private IEnumerable<SymbolInformation> EmptySymbols => this.emptySymbols ?? (this.emptySymbols = this.ReadEmptySymbols());

        public SymbolInformation LookUpISIN(string isin, bool updateExisting = true)
        {
            Trace.TraceInformation($"Lookup data for ISIN {isin}");
            Trace.Indent();
            try
            {
                isin = isin.ToUpperInvariant();

                // check if we know this already
                if (this.Symbols.ContainsKey(isin))
                {
                    return this.Symbols[isin];
                }

                // check if we can load a file from disk
                var dataPath = Path.Combine(Folder, $"{isin}.json");
                var symbol = File.Exists(dataPath) ? LoadSymbolData(isin) : this.EmptySymbols.First(item => Equals(item.ISIN.ToUpperInvariant(), isin));

                symbol.UpdateTimeSeries(updateExisting);
                SaveSymbolData(symbol);

                this.Symbols.Add(isin, symbol);

                return symbol;
            }
            finally
            {
                Trace.Unindent();
            }
        }

        private static void SaveSymbolData(SymbolInformation symbol)
        {
            var dataPath = Path.Combine(Folder, $"{symbol.ISIN}.json");
            Json.Save(dataPath, symbol);
        }

        private static SymbolInformation LoadSymbolData(string ISIN)
        {
            var dataPath = Path.Combine(Folder, $"{ISIN}.json");
            return Json.Load<SymbolInformation>(dataPath);
        }

        private void UpdateReference()
        {
            using (var wc = new WebClient())
            {
                wc.DownloadFile(new Uri("https://batstrading.co.uk/bxe/market_data/symbol_listing/csv/"), this.tempReferencePath);
            }

            File.Delete(this.referencePath);
            File.Move(this.tempReferencePath, this.referencePath);
        }

        private IEnumerable<SymbolInformation> ReadEmptySymbols()
        {
            using (TextReader fileReader = File.OpenText(this.referencePath))
            {
                var firstLine = fileReader.ReadLine();

                var csv = new CsvReader(fileReader);
                csv.Configuration.IncludePrivateMembers = true;
                return csv.GetRecords<SymbolInformation>().ToArray();
            }
        }
    }
}