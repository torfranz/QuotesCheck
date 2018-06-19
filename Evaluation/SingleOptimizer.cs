namespace QuotesCheck.Evaluation
{
    using Accord.Math.Optimization;
    using System;
    using System.Threading.Tasks;

    internal class SingleOptimizer
    {
        private readonly Evaluator evaluator;
        private double costOfTrades;

        internal SingleOptimizer(Evaluator evaluator, double costOfTrades)
        {
            this.evaluator = evaluator;
            this.costOfTrades = costOfTrades;
        }

        private double Evaluator(double[] parameters)
        {
            var result = this.evaluator.Evaluate(parameters, costOfTrades);
            return result.Performance.TotalGain;
        }

        internal EvaluationResult Run()
        {
            //var parameterRanges = evaluator.ParamterRanges;
            //var optimalResult = new EvaluationResult(evaluator, evaluator.StartingParamters);
            //Parallel.For(Convert.ToInt32(parameterRanges[1].Lower), Convert.ToInt32(parameterRanges[1].Upper), p1 =>
            //{
            //    Parallel.For(Convert.ToInt32(parameterRanges[2].Lower), Convert.ToInt32(parameterRanges[2].Upper), p2 =>
            //    {
            //        Parallel.For(Convert.ToInt32(parameterRanges[3].Lower), Convert.ToInt32(parameterRanges[3].Upper), p3 =>
            //        {
            //            Parallel.For(Convert.ToInt32(parameterRanges[4].Lower), Convert.ToInt32(parameterRanges[4].Upper), p4 =>
            //            {
            //                const int step = 1;
            //                if (p1 % step != 0 || p2 % step != 0 || p3 % step != 0 || p4 % step != 0)
            //                {
            //                    return;
            //                }

            //                var evaluatorResult = this.evaluator.Evaluate(new double[] { evaluator.StartingParamters[0], p1, p2, p3, p4 }, costOfTrades);
            //                if (evaluatorResult.Performance.TotalGain > optimalResult.Performance.TotalGain)
            //                {
            //                    optimalResult = evaluatorResult;
            //                }
            //            });
            //        });
            //    });
            //});

            //return optimalResult;

            // result before optimization
            var bestResult = this.evaluator.Evaluate(this.evaluator.StartingParamters, costOfTrades);

            // Optimize it (first round)
            var annealing = new BacktestingAnnealing(
                                x => Evaluator(x),
                                this.evaluator.StartingParamters,
                                this.evaluator.ParamterRanges)
                                { Cycles = 10000, StartTemperature = 1000 };
            annealing.Anneal();
            var result = this.evaluator.Evaluate(annealing.Array, costOfTrades);
            result.Iteration = 1;
            result.IterationsResults = bestResult.IterationsResults;
            result.IterationsResults.Add(result.CurrentIterationResult);

            bestResult = result;

            // reiterate with best parameters from previous iteration
            // maxiumum of 10 iterations
            for (var iteration = 2; iteration <= 10; iteration++)
            {
                annealing = new BacktestingAnnealing(
                                x => Evaluator(x),
                                bestResult.Parameters,
                                this.evaluator.ParamterRanges)
                { Cycles = 10000, StartTemperature = 1000 };
                annealing.Anneal();

                // gain at least 1%
                var best = Evaluator(bestResult.Parameters);
                if (annealing.Energy <= (best > 0.0
                                             ? 1.01 * best
                                             : 0.99 * best))
                {
                    break;
                }

                result = this.evaluator.Evaluate(annealing.Array, costOfTrades);
                result.Iteration = iteration;
                result.IterationsResults = bestResult.IterationsResults;
                result.IterationsResults.Add(result.CurrentIterationResult);

                bestResult = result;
            }

            return bestResult;
            
        }
    }
}