namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class MetaEvaluationResult
    {
        public MetaEvaluationResult(EvaluationResult[] results, Evaluator evaluator, double[] parameters)
        {
            this.Results = results;
            this.Trades = this.Results.Select(item => item.Trades.ToList()).Aggregate((acc, list) => acc.Concat(list).ToList());
            this.EvaluatorName = evaluator.Name;
            this.Performance = new PerformanceMeasure(this.Trades);
            this.Parameters = parameters;
        }

        IList<Trade> Trades { get; }

        public PerformanceMeasure Performance { get; }

        public EvaluationResult[] Results { get; }
        
        public double[] Parameters { get; }

        public string EvaluatorName { get; }

        public override string ToString()
        {
            return $"MetaResults - {this.Performance}";
        }

        public void Save(string folder)
        {
            var now = DateTime.Now;
            Json.Save(Path.Combine(folder, $"Evaluation-MetaResult-{this.Performance.OverallGain:F0}%-{now:yyyy-MM-dd-hh-mm-ss}.json"), this);
        }
    }
}