namespace QuotesCheck.Evaluation
{
    internal class SingleOptimizer
    {
        private readonly Evaluator evaluator;

        private readonly double costOfTrades;

        internal SingleOptimizer(Evaluator evaluator, double costOfTrades)
        {
            this.evaluator = evaluator;
            this.costOfTrades = costOfTrades;
        }

        internal EvaluationResult Run()
        {
            //var parameterRanges = this.evaluator.ParamterRanges;
            //var optimalResult = new EvaluationResult(this.evaluator, this.evaluator.StartingParamters);
            //for (var p1 = Convert.ToInt32(parameterRanges[1].Lower); p1 <= Convert.ToInt32(parameterRanges[1].Upper); p1++)
            //    for (var p2 = Convert.ToInt32(parameterRanges[2].Lower); p2 <= Convert.ToInt32(parameterRanges[2].Upper); p2++)
            //        for (var p3 = Convert.ToInt32(parameterRanges[3].Lower); p3 <= Convert.ToInt32(parameterRanges[3].Upper); p3++)
            //            for (var p4 = Convert.ToInt32(parameterRanges[4].Lower); p4 <= Convert.ToInt32(parameterRanges[4].Upper); p4++)
            //            {
            //                var evaluatorResult = this.evaluator.Evaluate(new[] { this.evaluator.StartingParamters[0], p1, p2, p3, p4 }, this.costOfTrades);
            //                if (evaluatorResult.Performance.TotalGain > optimalResult.Performance.TotalGain)
            //                {
            //                    optimalResult = evaluatorResult;
            //                }
            //            }

            //return optimalResult;

            // result before optimization
            var bestResult = this.evaluator.Evaluate(this.evaluator.StartingParamters, this.costOfTrades);

            // Optimize it (first round)
            var annealing =
                new BacktestingAnnealing(this.Evaluator, this.evaluator.StartingParamters, this.evaluator.ParamterRanges)
                    {
                        Cycles = 10000,
                        StartTemperature = 1000
                    };
            annealing.Anneal();
            var result = this.evaluator.Evaluate(annealing.Array, this.costOfTrades);
            result.Iteration = 1;
            result.IterationsResults = bestResult.IterationsResults;
            result.IterationsResults.Add(result.CurrentIterationResult);

            bestResult = result;

            // reiterate with best parameters from previous iteration
            // maxiumum of 10 iterations
            for (var iteration = 2; iteration <= 10; iteration++)
            {
                annealing =
                    new BacktestingAnnealing(this.Evaluator, bestResult.Parameters, this.evaluator.ParamterRanges) { Cycles = 10000, StartTemperature = 1000 };
                annealing.Anneal();

                // gain at least 1%
                var best = this.Evaluator(bestResult.Parameters);
                if (annealing.Energy <= (best > 0.0 ? 1.01 * best : 0.99 * best))
                {
                    break;
                }

                result = this.evaluator.Evaluate(annealing.Array, this.costOfTrades);
                result.Iteration = iteration;
                result.IterationsResults = bestResult.IterationsResults;
                result.IterationsResults.Add(result.CurrentIterationResult);

                bestResult = result;
            }

            return bestResult;
        }

        private double Evaluator(double[] parameters)
        {
            var result = this.evaluator.Evaluate(parameters, this.costOfTrades);
            var totalGain = result.Performance.TotalGain;

            return totalGain;

            /*
            return totalGain > 0
                       ? result.Performance.TotalGain * result.Performance.PositiveTrades
                         / (result.Performance.PositiveTrades + result.Performance.NegativeTrades)
                       : result.Performance.TotalGain * result.Performance.NegativeTrades
                         / (result.Performance.PositiveTrades + result.Performance.NegativeTrades);
                         */
        }
    }
}