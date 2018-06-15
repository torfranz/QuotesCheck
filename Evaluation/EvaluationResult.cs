namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class EvaluationResult
    {
        public EvaluationResult(SymbolInformation symbol, Evaluator evaluator, double[] parameters)
        {
            this.CompanyName = symbol.CompanyName;
            this.ISIN = symbol.ISIN;
            this.EvaluatorName = evaluator.Name;
            this.Performance = new PerformanceMeasure(this.Trades);
            this.Paramters = parameters;
        }

        public IList<Trade> Trades { get; } = new List<Trade>();

        public PerformanceMeasure Performance { get; }

        public string ISIN { get; }

        public string CompanyName { get; }

        public double[] Paramters { get; }

        public string EvaluatorName { get; }

        public override string ToString()
        {
            return $"{this.ISIN} - {this.CompanyName} - {this.Performance}";
        }

        public void Save(string folder)
        {
            var now = DateTime.Now;
            Json.Save(Path.Combine(folder, $"Evaluation-{this.ISIN}-{this.Performance.OverallGain:F0}-{now:yyyy-MM-dd-hh-mm-ss}.json"), this);
        }
    }
}