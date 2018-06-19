namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class MetaEvaluationResult
    {
        public MetaEvaluationResult(EvaluationResult[] results, Evaluator evaluator, double[] parameters, int iteration)
        {
            this.Results = results;
            this.Trades = this.Results.Select(item => item.Trades.ToList()).Aggregate((acc, list) => acc.Concat(list).ToList());
            this.EvaluatorName = evaluator.Name;
            this.EvaluatorEntryDescription = evaluator.EntryDescription;
            this.EvaluatorExitDescription = evaluator.ExitDescription;
            this.Performance = new PerformanceMeasure(this.Trades);
            this.Parameters = parameters;
            this.Iteration = iteration;
            this.IterationsResults = new List<IterationResult> { this.CurrentIterationResult };
        }

        public PerformanceMeasure Performance { get; }

        public EvaluationResult[] Results { get; }

        public double[] Parameters { get; }

        public string EvaluatorName { get; }

        public string EvaluatorEntryDescription { get; }

        public string EvaluatorExitDescription { get; }

        public int Iteration { get; }

        public IList<IterationResult> IterationsResults { get; set; }

        public IterationResult CurrentIterationResult =>
            new IterationResult { Iteration = this.Iteration, Parameters = (double[])this.Parameters.Clone(), Value = this.Performance.TotalGain };

        private IList<Trade> Trades { get; }

        public override string ToString()
        {
            return $"MetaResults - {this.Performance}";
        }

        public void Save(string folder, long duration)
        {
            var now = DateTime.Now;
            Json.Save(
                Path.Combine(folder, $"Evaluation-MetaResult--[{this.Iteration}]-[{this.Performance.TotalGain:F0}% +{this.Performance.PositiveTrades} -{this.Performance.NegativeTrades}]-{duration}ms-{now:yyyy-MM-dd-HH-mm-ss}.json"),
                this);
        }
    }
}