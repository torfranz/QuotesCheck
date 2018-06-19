namespace QuotesCheck.Evaluation
{
    using System;

    using Accord.Diagnostics;

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
            this.Array = startingParameters;
        }

        public override double DetermineEnergy()
        {
            return this.evaluatorFunction.Invoke(this.Array);
        }

        protected override double[] GetArrayCopy()
        {
            return (double[])this.Array.Clone();
        }

        protected override void Randomize()
        {
            for (var i = 0; i < this.Array.Length; i++)
            {
                var (lower, upper, step) = this.paramterRanges[i];
                this.Array[i] = Math.Min(upper, Math.Max(lower, this.Array[i] + (0.5 - this.rnd.NextDouble()) * step));
            }
        }
    }
}