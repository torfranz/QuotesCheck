﻿namespace QuotesCheck.Evaluation
{
    using Accord.Math.Optimization;

    internal class SingleNelderMeadOptimizer
    {
        private readonly Evaluator evaluator;

        internal SingleNelderMeadOptimizer(Evaluator evaluator)
        {
            this.evaluator = evaluator;
        }

        internal EvaluationResult Run(double costOfTrades)
        {
            // start solver
            var parameterRanges = this.evaluator.ParamterRanges;

            //Parallel.For(Convert.ToInt32(parameterRanges[0].Lower), Convert.ToInt32(parameterRanges[0].Upper), p1 =>
            //{
            //    Parallel.For(Convert.ToInt32(parameterRanges[1].Lower), Convert.ToInt32(parameterRanges[1].Upper), p2 =>
            //    {
            //        Parallel.For(Convert.ToInt32(parameterRanges[2].Lower), Convert.ToInt32(parameterRanges[2].Upper), p3 =>
            //        {
            //            Parallel.For(Convert.ToInt32(parameterRanges[3].Lower), Convert.ToInt32(parameterRanges[3].Upper), p4 =>
            //            {
            //                const int step = 3;
            //                if (p1 % step != 0 || p2 % step != 0 || p3 % step != 0 || p4 % step != 0)
            //                {
            //                    return;
            //                }

            //                var result = this.evaluator.Evaluate(new double[] { p1, p2, p3, p4 });
            //                if (result.Performance.OverallGain > best.Performance.OverallGain)
            //                {
            //                    best = result;
            //                }
            //            });
            //        });
            //    });
            //});

            //return best;
            var solver = new NelderMead(parameterRanges.Length) { Function = x => -this.evaluator.Evaluate(x, costOfTrades).Performance.TotalGain };

            for (var i = 0; i < parameterRanges.Length; i++)
            {
                solver.LowerBounds[i] = parameterRanges[i].Lower;
                solver.UpperBounds[i] = parameterRanges[i].Upper;
                solver.StepSize[i] = parameterRanges[i].Step;
            }

            var annealing = new BacktestingAnnealing(
                                x => 1000 - this.evaluator.Evaluate(x, costOfTrades).Performance.TotalGain,
                                this.evaluator.StartingParamters,
                                this.evaluator.ParamterRanges)
                                { Cycles = 100, StartTemperature = 1000 };
            annealing.Anneal();
            var bestResult = this.evaluator.Evaluate(annealing.Array, costOfTrades);
            return bestResult;

            // result before optimization
            //var bestResult = this.evaluator.Evaluate(this.evaluator.StartingParamters, costOfTrades);
            //return bestResult;

            // Optimize it (first round)
            if (!solver.Maximize(this.evaluator.StartingParamters))
            {
                return null;
            }

            var result = this.evaluator.Evaluate(solver.Solution, costOfTrades);
            result.Iteration = 1;
            result.IterationsResults = bestResult.IterationsResults;
            result.IterationsResults.Add(result.CurrentIterationResult);

            bestResult = result;

            // reiterate with best parameters from previous iteration
            // maxiumum of 10 iterations
            for (var iteration = 2; iteration <= 10; iteration++)
            {
                // gain at least 1%
                if (!solver.Maximize(bestResult.Parameters)
                    || (solver.Value <= (bestResult.Performance.TotalGain > 0.0
                                             ? 1.01 * bestResult.Performance.TotalGain
                                             : 0.99 * bestResult.Performance.TotalGain)))
                {
                    break;
                }

                result = this.evaluator.Evaluate(solver.Solution, costOfTrades);
                result.Iteration = iteration;
                result.IterationsResults = bestResult.IterationsResults;
                result.IterationsResults.Add(result.CurrentIterationResult);

                bestResult = result;
            }

            return bestResult;
        }
    }
}