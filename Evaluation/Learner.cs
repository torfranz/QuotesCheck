namespace QuotesCheck.Evaluation
{
    using System;
    using System.Linq;

    using Accord;
    using Accord.Diagnostics;
    using Accord.Math;
    using Accord.Neuro;
    using Accord.Neuro.Learning;

    internal class Learner
    {
        public DoubleRange ScaleRange => new DoubleRange(-1, 1);

        public Network Learn(double[][] features, double[] targets, int? hiddenLayerCount = null)
        {
            Debug.Assert(features.Length > 0);
            var inputsCount = features[0].Length;

            var outputs = targets.Select(item => new[]{item}).ToArray();
            var outputsCount = 1;

            // Create an activation network with the function and
            var network = new ActivationNetwork(
                new BipolarSigmoidFunction(0.5),
                inputsCount,
                hiddenLayerCount.GetValueOrDefault(2 * inputsCount - outputsCount),
                outputsCount);

            // Randomly initialize the network
            new NguyenWidrow(network).Randomize();

            // Teach the network using parallel Rprop:
            var teacher = new ResilientBackpropagationLearning(network);
            //var teacher = new EvolutionaryLearning(network, 100);

            // Iterate until stop criteria is met
            var error = teacher.RunEpoch(features, outputs);
            double previous;

            do
            {
                previous = error;

                // Compute one learning iteration
                error = teacher.RunEpoch(features, outputs);
            }
            while (Math.Abs(previous - error) > 0.000000001 * previous);

            return network;
        }
    }
}