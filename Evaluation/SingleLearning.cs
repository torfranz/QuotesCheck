namespace QuotesCheck.Evaluation
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Accord.Diagnostics;
    using Accord.Math;
    using Accord.Neuro;
    using Accord.Neuro.Learning;

    internal class SingleLearning
    {
        private readonly double costOfTrades;

        private readonly SymbolInformation symbol;

        private ActivationNetwork network;

        internal SingleLearning(SymbolInformation symbol, double costOfTrades)
        {
            this.costOfTrades = costOfTrades;
            this.symbol = symbol;
        }

        internal SingleLearning Load(string folder)
        {
            var filePath = this.BuildFilePath(folder);
            if (File.Exists(filePath))
            {
                this.network = Network.Load(filePath) as ActivationNetwork;
            }

            return this;
        }

        internal SingleLearning Save(string folder)
        {
            Debug.Assert(this.network != null);

            this.network.Save(this.BuildFilePath(folder));
            return this;
        }

        internal SingleLearning Learn(FeatureExtractor featureExtractor, bool relearn = false)
        {
            var inputs = featureExtractor.Features.Select(item => item.features).ToArray();
            Debug.Assert(inputs.Length > 0);
            var inputsCount = inputs[0].Length;
            
            var outputs = Jagged.OneHot(featureExtractor.Features.Select(item => item.label).ToArray());
            Debug.Assert(outputs.Length > 0);
            var outputsCount = outputs[0].Length;

            if ((this.network == null) || relearn)
            {
                // Create an activation network with the function and
                this.network = new ActivationNetwork(
                    new BipolarSigmoidFunction(),
                    inputsCount,
                    (inputsCount + outputsCount) / 2,
                    outputsCount);
                
                // Randomly initialize the network
                new NguyenWidrow(this.network).Randomize();
            }
            else
            {
                Debug.Assert(this.network.InputsCount == inputsCount);
            }

            // Teach the network using parallel Rprop:
            var teacher = new ParallelResilientBackpropagationLearning(this.network);

            var errors = new List<double>();
            var epoch = 0;
            var error = double.MaxValue;
            while (true)
            {
                var newError = teacher.RunEpoch(inputs, outputs);
                if (error - newError < 0.01)
                {
                    break;
                }

                error = newError;
                errors.Add(error);
                epoch++;
            }
            
            // Checks if the network has learned
            for (var i = 0; i < inputs.Length; i++)
            {
                var answer = this.network.Compute(inputs[i]);
            }

            return this;
        }

        internal EvaluationResult Apply()
        {
            return null;
        }

        private string BuildFilePath(string folder)
        {
            return Path.Combine(folder, $"{this.symbol.ISIN}.bin");
        }
    }
}