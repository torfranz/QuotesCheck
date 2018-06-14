namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;

    using CsvHelper;

    using Newtonsoft.Json;

    internal class SymbolProvider
    {
        private const string Folder = @"ReferenceData";

        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        private readonly string referencePath = Path.Combine(Folder, "BXESymbols-PROD.csv");

        private readonly string tempReferencePath = Path.Combine(Folder, "BXESymbols-TEMP.csv");

        private IEnumerable<SymbolInformation> emptySymbols;

        public SymbolProvider(bool updateFirst = false)
        {
            if (updateFirst) this.UpdateReference();
        }

        private IEnumerable<SymbolInformation> EmptySymbols => this.emptySymbols ?? (this.emptySymbols = this.ReadEmptySymbols());

        private IDictionary<string, SymbolInformation> Symbols { get; } = new Dictionary<string, SymbolInformation>();

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

        public SymbolInformation LookUpISIN(string isin)
        {
            Trace.TraceInformation($"Lookup data for ISIN {isin}");
            Trace.Indent();
            try
            {
                isin = isin.ToUpperInvariant();

                // check if we know this already
                if (this.Symbols.ContainsKey(isin)) return this.Symbols[isin];

                // check if we can load a file from disk
                var dataPath = Path.Combine(Folder, $"{isin}.json");
                var symbol = File.Exists(dataPath) ? this.LoadSymbolData(dataPath) : this.EmptySymbols.First(item => Equals(item.ISIN.ToUpperInvariant(), isin));

                symbol.UpdateTimeSeries();
                this.SaveSymbolData(dataPath, symbol);

                this.Symbols.Add(isin, symbol);

                return symbol;
            }
            finally
            {
                Trace.Unindent();
            }
        }

        private void SaveSymbolData(string dataPath, SymbolInformation symbol)
        {
            Trace.TraceInformation($"Writing data to {dataPath}");
            File.WriteAllText(dataPath, JsonConvert.SerializeObject(symbol, this.jsonSettings));
        }

        private SymbolInformation LoadSymbolData(string dataPath)
        {
            Trace.TraceInformation($"Reading data from {dataPath}");
            return JsonConvert.DeserializeObject<SymbolInformation>(File.ReadAllText(dataPath), this.jsonSettings);
        }
    }
}