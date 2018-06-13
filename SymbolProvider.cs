namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
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
            isin = isin.ToUpperInvariant();

            // check if we know this already
            if (this.Symbols.ContainsKey(isin)) return this.Symbols[isin];

            // check if we can load a file from disk
            var dataPath = Path.Combine(Folder, $"{isin}.json");
            if (File.Exists(dataPath))
            {
                var symbol = this.LoadSymbolData(dataPath);

                // are time series still current, no => update
                if (symbol.TimeSeries.Count == 0 || symbol.TimeSeries[0].Day.DayOfYear < DateTime.Now.DayOfYear
                                              || symbol.TimeSeries[0].Day.Year < DateTime.Now.Year)
                {
                    symbol.UpdateTimeSeries();
                    this.SaveSymbolData(dataPath, symbol);
                }

                this.Symbols.Add(isin, symbol);
                return symbol;
            }
            else
            {
                // nothing exists so far, create new 
                var symbol = this.EmptySymbols.First(item => Equals(item.ISIN.ToUpperInvariant(), isin));
                symbol.UpdateTimeSeries();
                this.SaveSymbolData(dataPath, symbol);

                this.Symbols.Add(isin, symbol);

                return symbol;
            }
        }

        private void SaveSymbolData(string dataPath, SymbolInformation symbol)
        {
            File.WriteAllText(dataPath, JsonConvert.SerializeObject(symbol, this.jsonSettings));
        }

        private SymbolInformation LoadSymbolData(string dataPath)
        {
            return JsonConvert.DeserializeObject<SymbolInformation>(File.ReadAllText(dataPath), this.jsonSettings);
        }
    }
}