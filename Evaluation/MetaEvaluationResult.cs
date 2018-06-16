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
            this.EvaluatorEntryDescription = evaluator.EntryDescription;
            this.EvaluatorExitDescription = evaluator.ExitDescription;
            this.Performance = new PerformanceMeasure(this.Trades);
            this.Parameters = parameters;
        }

        IList<Trade> Trades { get; }

        public PerformanceMeasure Performance { get; }

        public EvaluationResult[] Results { get; }
        
        public double[] Parameters { get; }

        public string EvaluatorName { get; }

        public string EvaluatorEntryDescription { get; }

        public string EvaluatorExitDescription { get; }

        public string Iteration { get; set; }

        public override string ToString()
        {
            return $"MetaResults - {this.Performance}";
        }

        public void Save(string folder, long duration)
        {
            var now = DateTime.Now;
            Json.Save(Path.Combine(folder, $"Evaluation [{this.Iteration}]-MetaResult-{this.Performance.OverallGain:F0}%-{duration}ms-{now:yyyy-MM-dd-HH-mm-ss}.json"), this);
        }
    }
}