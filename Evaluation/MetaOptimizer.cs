﻿namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Math.Optimization;

    internal class MetaOptimizer
    {
        private Func<SymbolInformation, Evaluator> evaluatorCreator;

        internal MetaOptimizer(Func<SymbolInformation, Evaluator> evaluatorCreator)
        {
            this.evaluatorCreator = evaluatorCreator;
        }

        internal MetaEvaluationResult Run(SymbolInformation[] symbols)
        {
            var evaluators = symbols.Select(symbol => evaluatorCreator(symbol)).ToArray();
            
            // start solver
            var parameterRanges = evaluators[0].ParamterRanges;
            var solver = new NelderMead(parameterRanges.Length)
                             {
                                 Function = x =>
                                     {
                                         return evaluators.Sum(
                                             evaluator => evaluator.Evaluate(x).Performance.TotalGain);
                                     },
                             };

            for (var i = 0; i < parameterRanges.Length; i++)
            {
                solver.LowerBounds[i] = parameterRanges[i].Lower;
                solver.UpperBounds[i] = parameterRanges[i].Upper;
                solver.StepSize[i] = parameterRanges[i].Step;
            }

            // Optimize it
            if (!solver.Maximize(evaluators[0].StartingParamters))
            {
                return null;
            }

            var bestResult = new MetaEvaluationResult(evaluators.Select(evaluator => evaluator.Evaluate(solver.Solution)).ToArray(), evaluators[0], solver.Solution, 0);
            // reiterate with best parameters from previous iteration
            // maxiumum of 10 iterations
            for (int iteration = 1; iteration <= 10; iteration++)
            {
                // gain at least 1%
                if (!solver.Maximize(bestResult.Parameters) || solver.Value <= 1.01 * bestResult.Performance.TotalGain)
                {
                    break;
                }


                var result = new MetaEvaluationResult(evaluators.Select(evaluator => evaluator.Evaluate(solver.Solution)).ToArray(), evaluators[0], solver.Solution, iteration);
                
                result.IterationsResults = bestResult.IterationsResults;
                result.IterationsResults.Add(result.CurrentIterationResult);

                bestResult = result;
            }

            return bestResult;
        }
    }
}