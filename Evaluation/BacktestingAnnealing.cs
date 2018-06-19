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
                this.Array[i] = Math.Min(upper, Math.Max(lower, this.Array[i] + ((rnd.NextBoolean() ? 1 : -1)*this.rnd.NextDouble()) * step));
            }
        }
    }
}