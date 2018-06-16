namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class EvaluationResult
    {
        public EvaluationResult(Evaluator evaluator, double[] parameters)
        {
            this.CompanyName = evaluator.Symbol.CompanyName;
            this.ISIN = evaluator.Symbol.ISIN;
            this.EvaluatorName = evaluator.Name;
            this.Performance = new PerformanceMeasure(this.Trades);
            this.Parameters = parameters;
        }

        public IList<Trade> Trades { get; } = new List<Trade>();

        public PerformanceMeasure Performance { get; }

        public string ISIN { get; }

        public string CompanyName { get; }

        public double[] Parameters { get; }

        public string EvaluatorName { get; }

        public override string ToString()
        {
            return $"{this.ISIN} - {this.CompanyName} - {this.Performance}";
        }

        public void Save(string folder)
        {
            var now = DateTime.Now;
            Json.Save(Path.Combine(folder, $"Evaluation-{this.ISIN}-{this.Performance.OverallGain:F0}%-{now:yyyy-MM-dd-HH-mm-ss}.json"), this);
        }
    }
}