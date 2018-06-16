namespace QuotesCheck.Evaluation
{
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Math.Optimization;

    internal class MetaOptimizer
    {
        private readonly Evaluator evaluator;

        internal MetaOptimizer(Evaluator evaluator)
        {
            this.evaluator = evaluator;
        }

        internal MetaEvaluationResult Run(SymbolInformation[] symbols)
        {
            /*
            // generate fixtures
            foreach (var symbol in symbols)
            {
                this.evaluator.GenerateFixtures(symbol);
            }

            // start solver
            var parameterRanges = this.evaluator.ParamterRanges;
            var solver = new NelderMead(parameterRanges.Length)
                             {
                                 Function = x =>
                                     {
                                         return symbols.Sum(
                                             symbol => this.evaluator.Evaluate(symbol, x).Performance.OverallGain);
                                     },
                             };

            for (var i = 0; i < parameterRanges.Length; i++)
            {
                solver.LowerBounds[i] = parameterRanges[i].Lower;
                solver.UpperBounds[i] = parameterRanges[i].Upper;
                solver.StepSize[i] = parameterRanges[i].Step;
            }

            // Optimize it
            if (solver.Maximize(this.evaluator.StartingParamters))
            {
                var list = new List<EvaluationResult>();
                foreach (var symbol in symbols)
                {
                    list.Add(this.evaluator.Evaluate(symbol, solver.Solution));
                }

                return new MetaEvaluationResult(list.ToArray(), this.evaluator, solver.Solution);
            }
            */
            return null;
        }
    }
}