namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal class MetaOptimizer
    {
        private readonly Func<SymbolInformation, Evaluator> evaluatorCreator;

        private readonly double costOfTrades;

        internal MetaOptimizer(Func<SymbolInformation, Evaluator> evaluatorCreator, double costOfTrades)
        {
            this.evaluatorCreator = evaluatorCreator;
            this.costOfTrades = costOfTrades;
        }

        internal MetaEvaluationResult Run(SymbolInformation[] symbols)
        {
            var evaluators = symbols.Select(symbol => this.evaluatorCreator(symbol)).ToArray();

            // start solver
            var annealing =
                new BacktestingAnnealing(
                    x => this.Evaluator(evaluators, x),
                    evaluators[0].StartingParamters,
                    evaluators[0].ParamterRanges) { Cycles = 10000, StartTemperature = 1000 };
            annealing.Anneal();

            var bestResult = new MetaEvaluationResult(
                evaluators.Select(evaluator => evaluator.Evaluate(annealing.Array, this.costOfTrades)).ToArray(),
                evaluators[0],
                annealing.Array,
                0);

            // reiterate with best parameters from previous iteration
            // maxiumum of 10 iterations
            for (var iteration = 1; iteration <= 10; iteration++)
            {
                annealing =
                    new BacktestingAnnealing(
                        x => this.Evaluator(evaluators, x),
                        evaluators[0].StartingParamters,
                        evaluators[0].ParamterRanges) { Cycles = 10000, StartTemperature = 1000 };
                annealing.Anneal();

                // gain at least 1%
                if (annealing.Energy <= (bestResult.Performance.TotalGain > 0.0
                                             ? 1.01 * bestResult.Performance.TotalGain
                                             : 0.99 * bestResult.Performance.TotalGain))
                {
                    break;
                }

                var result = new MetaEvaluationResult(
                    evaluators.Select(evaluator => evaluator.Evaluate(annealing.Array, this.costOfTrades)).ToArray(),
                    evaluators[0],
                    annealing.Array,
                    iteration);

                result.IterationsResults = bestResult.IterationsResults;
                result.IterationsResults.Add(result.CurrentIterationResult);

                bestResult = result;
            }

            return bestResult;
        }

        private double Evaluator(Evaluator[] evaluators, double[] parameters)
        {
            var results = new Dictionary<string, double>();
            Parallel.ForEach(evaluators, evaluator => results[evaluator.Symbol.ISIN] = evaluator.Evaluate(parameters, this.costOfTrades).Performance.TotalGain);
            return results.Values.Sum();
        }
    }
}