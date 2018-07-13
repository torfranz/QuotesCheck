namespace QuotesCheck.Evaluation
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using Accord;
    using Accord.Math;
    using Accord.Neuro;

    internal class WalkForwardTester
    {
        public WalkForwardTester(SymbolInformation symbol, FeatureCreator featureCreator, LabelCreator labelCreator, Learning learning, ResultCreator resultCreator)
        {
            this.Symbol = symbol;
            this.FeatureCreator = featureCreator;
            this.LabelCreator = labelCreator;
            this.Learning = learning;
            this.ResultCreator = resultCreator;
        }

        public ResultCreator ResultCreator { get; }

        public Learning Learning { get; }

        public LabelCreator LabelCreator { get; }

        public FeatureCreator FeatureCreator { get; }

        public SymbolInformation Symbol { get; }

        public EvaluationResult Evaluate(int inSampleCount, int outOfSampleCount, int startIndex, int endIndex = 0)
        {
            Debug.Assert((startIndex >= 0) && (startIndex < this.Symbol.TimeSeries.Count));
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
                var networks = this.Learn(isRange);

                // APPLY TO outOfSample range
                var oosRange = new IntRange(Math.Max(endIndex, isRange.Min - outOfSampleCount - 1), isRange.Min - 1);
                Debug.Assert(oosRange.Min >= endIndex);
                Debug.Assert(oosRange.Max <= startIndex);

                var labels = this.Apply(oosRange, networks);
                for (int idx = 0; idx < labels.Length; idx++)
                {
                    validationLabels[oosRange.Min + idx] = labels[idx];
                }

                sliceIdx++;
            }

            return this.ResultCreator.CreateResult(Symbol, validationLabels, startIndex, endIndex);
        }

        private Network[] Learn(IntRange range)
        {
            var features = this.FeatureCreator.GenerateFeatures(this.Symbol, range);
            Debug.Assert(features.Length > 0);

            var learnLabels = this.LabelCreator.GenerateLabels(this.Symbol, range);
            Debug.Assert(learnLabels.Length == features.Length);

            var featureSetsCount = features[0].Length;
            Debug.Assert(featureSetsCount > 0);

            var networks = new Network[featureSetsCount];
            for (var idx = 0; idx < featureSetsCount; idx++)
            {
                Debug.Assert(featureSetsCount == features[idx].Length);
                var extractedFeatureSet = features.Select(item => item[idx]).ToArray();
                networks[idx] = this.Learning.Learn(extractedFeatureSet, learnLabels);
            }

            return networks;
        }

        private int[] Apply(IntRange range, Network[] networks)
        {
            var features = this.FeatureCreator.GenerateFeatures(this.Symbol, range);
            Debug.Assert(features.Length > 0);

            var featureSetsCount = features[0].Length;
            Debug.Assert(featureSetsCount > 0);
            Debug.Assert(featureSetsCount == networks.Length);

            var labels = new int[range.Length];

            for (var idx = range.Min; idx <= range.Max; idx++)
            {
                var sum = 0.0;
                for (var featureIdx = 0; featureIdx < featureSetsCount; featureIdx++)
                {
                    var network = networks[featureIdx];
                    sum += network.Compute(features[idx][featureIdx]).ArgMax();
                }

                labels[idx] = Convert.ToInt32(sum / featureSetsCount);
            }

            return labels;
        }
    }
}