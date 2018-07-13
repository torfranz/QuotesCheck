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

            var totalDataCount = startIndex - endIndex;
            Debug.Assert(inSampleCount + outOfSampleCount <= totalDataCount);

            var validationLabels = new int[startIndex];
            var sliceIdx = 0;
            while (startIndex - sliceIdx * inSampleCount > endIndex)
            {
                // LEARNING for inSample Range
                // generate ranges for learning 
                var isRange = new IntRange(startIndex - (sliceIdx + 1) * inSampleCount, startIndex - sliceIdx * inSampleCount);
                Debug.Assert(isRange.Min >= endIndex);
                Debug.Assert(isRange.Max <= startIndex);

                // generate features
                this.Model.Learn(isRange);

                // APPLY TO outOfSample range
                var oosRange = new IntRange(Math.Max(endIndex, isRange.Min - outOfSampleCount - 1), isRange.Min - 1);
                Debug.Assert(oosRange.Min >= endIndex);
                Debug.Assert(oosRange.Max <= startIndex);

                var targets = this.Model.Apply(oosRange);
                for (var idx = 0; idx < targets.Length; idx++)
                {
                    Debug.Assert(oosRange.Min + idx == targets[idx].Index);
                    validationLabels[oosRange.Min + idx] = targets[idx].Target;
                }

                sliceIdx++;
            }

            return this.Model.CreateResult(validationLabels, startIndex, endIndex);
        }
    }
}