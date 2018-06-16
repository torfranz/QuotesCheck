namespace QuotesCheck.Evaluation
{
    using Accord.Math.Optimization;
    using System;
    using System.Threading.Tasks;

    internal class SingleNelderMeadOptimizer
    {
        private readonly Evaluator evaluator;

        internal SingleNelderMeadOptimizer(Evaluator evaluator)
        {
            this.evaluator = evaluator;
        }

        internal EvaluationResult Run()
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

            var solver = new NelderMead(parameterRanges.Length) { Function = x => this.evaluator.Evaluate(x).Performance.OverallGain };

            for (var i = 0; i < parameterRanges.Length; i++)
            {
                solver.LowerBounds[i] = parameterRanges[i].Lower;
                solver.UpperBounds[i] = parameterRanges[i].Upper;
                solver.StepSize[i] = parameterRanges[i].Step;
            }

            // Optimize it (first round)
            if(!solver.Maximize(this.evaluator.StartingParamters))
            {
                return null;
            }

            var bestResult = this.evaluator.Evaluate(solver.Solution);

            // reiterate with best parameters from previous iteration
            // maxiumum of 10 iterations
            for (int iteration = 1; iteration <= 10; iteration++)
            {
                if (!solver.Maximize(solver.Solution) || solver.Value <= bestResult.Performance.OverallGain)
                {
                    break;
                }

                bestResult = this.evaluator.Evaluate(solver.Solution);
                bestResult.Iteration = iteration;
            }
            
            return bestResult;
        }
    }
}