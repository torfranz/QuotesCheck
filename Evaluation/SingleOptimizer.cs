namespace QuotesCheck.Evaluation
{
    using Accord.Math.Optimization;

    internal class SingleOptimizer
    {
        private readonly Evaluator evaluator;

        internal SingleOptimizer(Evaluator evaluator)
        {
            this.evaluator = evaluator;
        }

        internal EvaluationResult Run(SymbolInformation symbol)
        {
            // generate fixtures
            this.evaluator.GenerateFixtures(symbol);

            // start solver
            var parameterRanges = this.evaluator.ParamterRanges;
            var solver = new NelderMead(parameterRanges.Length) { Function = x => this.evaluator.Evaluate(symbol, x).Performance.OverallGain, };

            for (var i = 0; i < parameterRanges.Length; i++)
            {
                solver.LowerBounds[i] = parameterRanges[i].Lower;
                solver.UpperBounds[i] = parameterRanges[i].Upper;
                solver.StepSize[i] = parameterRanges[i].Step;
            }

            // Optimize it
            if (solver.Maximize(this.evaluator.StartingParamters))
            {
                return this.evaluator.Evaluate(symbol, solver.Solution);
            }

            return null;
        }
    }
}