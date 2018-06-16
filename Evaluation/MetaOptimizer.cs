namespace QuotesCheck.Evaluation
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
                                             evaluator => evaluator.Evaluate(x).Performance.OverallGain);
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

            var results = evaluators.Select(evaluator => evaluator.Evaluate(solver.Solution)).ToArray();
            return new MetaEvaluationResult(results, evaluators[0], solver.Solution);
        }
    }
}