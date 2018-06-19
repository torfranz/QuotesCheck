namespace QuotesCheck.Evaluation
{
    using System;

    using Accord.Diagnostics;
    using MathNet.Numerics.Random;

    internal class BacktestingAnnealing : SimulatedAnnealing<double>
    {
        private readonly Func<double[], double> evaluatorFunction;

        private readonly (double Lower, double Upper, double Step)[] paramterRanges;

        internal BacktestingAnnealing(
            Func<double[], double> evaluatorFunction,
            double[] startingParameters,
            (double Lower, double Upper, double Step)[] paramterRanges)
        {
            Debug.Assert(startingParameters.Length == paramterRanges.Length);
            this.evaluatorFunction = evaluatorFunction;
            this.paramterRanges = paramterRanges;
            this.Array = (double[])startingParameters.Clone();
        }

        public override double DetermineEnergy()
        {
            return this.evaluatorFunction.Invoke(this.Array);
        }

        protected override void Randomize()
        {
            for (var i = 0; i < this.Array.Length; i++)
            {
                var (lower, upper, step) = this.paramterRanges[i];
                if(step == 0.0)
                {
                    continue;
                }

                var maxsteps = Convert.ToInt32((upper - lower) / step / 3);
                this.Array[i] = Math.Min(upper, Math.Max(lower, this.Array[i] + ((rnd.NextBoolean() ? 1 : -1) * rnd.Next(0, maxsteps + 1)) * step));
                //this.Array[i] = lower + step * rnd.Next(0, maxsteps + 1);
            }
        }
    }
}