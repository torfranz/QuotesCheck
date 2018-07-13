namespace QuotesCheck.Evaluation
{
    using System;
    using System.Diagnostics;

    using Accord;

    internal class WalkForwardTester
    {
        public WalkForwardTester(Model model)
        {
            this.Model = model;
        }

        public Model Model { get; }

        public EvaluationResult Evaluate(int inSampleCount, int outOfSampleCount, int startIndex, int endIndex = 0)
        {
            Debug.Assert((startIndex >= 0) && (startIndex < this.Model.Symbol.TimeSeries.Count));
            Debug.Assert((endIndex >= 0) && (endIndex < startIndex));

            Debug.Assert(inSampleCount + outOfSampleCount <= startIndex - endIndex + 1);

            var validationRange = new IntRange(endIndex, startIndex - inSampleCount);
            var validationResult = new TargetResult(validationRange);

            var sliceIdx = 0;
            while (startIndex - sliceIdx * outOfSampleCount > inSampleCount)
            {
                // LEARNING for inSample Range
                // generate ranges for learning 
                var isRange = new IntRange(startIndex - sliceIdx * outOfSampleCount - inSampleCount + 1, startIndex - sliceIdx * outOfSampleCount);
                Debug.Assert(isRange.Length + 1 == inSampleCount);
                Debug.Assert(isRange.Min >= endIndex);
                Debug.Assert(isRange.Max <= startIndex);

                // generate features
                this.Model.Learn(isRange);

                // APPLY TO outOfSample range
                var oosRange = new IntRange(Math.Max(endIndex, isRange.Min - outOfSampleCount), isRange.Min - 1);
                Debug.Assert(oosRange.Length + 1 == outOfSampleCount || (oosRange.Min == endIndex && oosRange.Length + 1 <= outOfSampleCount));
                Debug.Assert(oosRange.Min >= endIndex);
                Debug.Assert(oosRange.Max <= startIndex);

                var result = this.Model.Apply(oosRange);
                for (var idx = result.Range.Min; idx <= result.Range.Max; idx++)
                {
                    Debug.Assert(validationResult.Targets[idx] == null);
                    validationResult.Targets[idx] = result.Targets[idx - result.Range.Min];
                }

                sliceIdx++;
            }

            return this.Model.CreateResult(validationResult);
        }
    }
}