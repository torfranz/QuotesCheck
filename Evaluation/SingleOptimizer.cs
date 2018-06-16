namespace QuotesCheck.Evaluation
{
    using Accord.Math.Optimization;
    using System;
    using System.Threading.Tasks;

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
            var best = new EvaluationResult(symbol, evaluator, null);
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
            //                if(p1 % step != 0 || p2 % step != 0 || p3 % step != 0 || p4 % step != 0)
            //                {
            //                    return;
            //                }

            //                var result = this.evaluator.Evaluate(symbol, new double[] { p1, p2, p3, p4 });
            //                if(result.Performance.OverallGain > best.Performance.OverallGain)
            //                {
            //                    best = result;
            //                }
            //            });
            //        });
            //    });
            //});

            //return best;
            
            var solver = new NelderMead(parameterRanges.Length) { Function = x => this.evaluator.Evaluate(symbol, x).Performance.OverallGain };

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