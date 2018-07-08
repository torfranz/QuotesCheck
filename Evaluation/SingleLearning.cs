namespace QuotesCheck.Evaluation
{
    using System;
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
            Directory.CreateDirectory(folder);
            this.network.Save(this.BuildFilePath(folder));
            return this;
        }

        internal SingleLearning Learn(FeatureExtractor featureExtractor, bool relearn = false)
        {
            var inputs = featureExtractor.Features.Select(item => item.features).ToArray();
            Debug.Assert(inputs.Length > 0);
            var inputsCount = inputs[0].Length;
            
            var outputs =  featureExtractor.Features.Select(item => new double[] { item.label }).ToArray();
            Debug.Assert(outputs.Length > 0);
            var outputsCount = outputs[0].Length;

            if ((this.network == null) || relearn)
            {
                // Create an activation network with the function and
                this.network = new ActivationNetwork(
                    new BipolarSigmoidFunction(),
                    inputsCount,
                    (inputsCount + outputsCount),
                    outputsCount);
                
                // Randomly initialize the network
                new NguyenWidrow(this.network).Randomize();
            }
            else
            {
                Debug.Assert(this.network.InputsCount == inputsCount);
            }

            // Teach the network using parallel Rprop:
            var teacher = new ResilientBackpropagationLearning(network);

            // Iterate until stop criteria is met
            double error = teacher.RunEpoch(inputs, outputs);
            double previous;

            do
            {
                previous = error;

                // Compute one learning iteration
                error = teacher.RunEpoch(inputs, outputs);

            } while (Math.Abs(previous - error) > 0.0000000001 * previous);

            // Checks if the network has learned
            var answers = new List<(int, double)>();
            for (var i = 0; i < inputs.Length; i++)
            {
                answers.Add((featureExtractor.Features[i].label, this.network.Compute(inputs[i])[0]));
            }

            return this;
        }

        internal EvaluationResult Apply(FeatureExtractor featureExtractor)
        {
            return null;
        }

        private string BuildFilePath(string folder)
        {
            return Path.Combine(folder, $"{this.symbol.ISIN}.bin");
        }
    }
}