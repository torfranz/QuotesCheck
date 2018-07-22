namespace QuotesCheck.Evaluation
{
    using System;
    using System.CodeDom.Compiler;
    using System.Linq;

    using Accord;
    using Accord.Diagnostics;
    using Accord.Math;
    using Accord.Neuro;
    using Accord.Neuro.ActivationFunctions;
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
                new BipolarSigmoidFunction(),
                inputsCount,
                hiddenLayerCount.GetValueOrDefault(2 * inputsCount),
                outputsCount);

            // Randomly initialize the network
            new NguyenWidrow(network).Randomize();

            // Teach the network using parallel Rprop:
            var teacher = new ResilientBackpropagationLearning(network);
            // var teacher = new LevenbergMarquardtLearning(network);
            //var teacher = new EvolutionaryLearning(network, 100);

            // Iterate until stop criteria is met
            var error = teacher.RunEpoch(features, outputs);
            double previous;
            var iteration = 0;

            do
            {
                previous = error;
                iteration ++;
                // Compute one learning iteration
                error = teacher.RunEpoch(features, outputs);
            }
            while (Math.Abs(previous - error) > 0.000000001 * previous);

            // learn result
            var results = new (double, double, double)[features.Length];
            for (var index = 0; index < features.Length; index++)
            {
                var feature = features[index];
                var target = targets[index];
                var result = network.Compute(feature)[0];
                results[index] = (target, result, 100 * (result - target));
            }

            return network;
        }

        public Network Learn(double[][] features, int[] classes, int? hiddenLayerCount = null)
        {
            var class0 = 100.0 * classes.Count(item => item == 0) / classes.Length;
            var class1 = 100.0 * classes.Count(item => item == 1) / classes.Length;
            Console.WriteLine($"Class 0: {class0:F2} Class 1: {class1:F2}");

            Debug.Assert(features.Length > 0);
            var inputsCount = features[0].Length;

            var outputs = Jagged.OneHot(classes);
            var outputsCount = outputs[0].Length;

            // Create an activation network with the function and
            var network = new ActivationNetwork(
                new BipolarSigmoidFunction(),
                inputsCount,
                //hiddenLayerCount.GetValueOrDefault(2 * inputsCount),
                12, 6,
                outputsCount);

            // Randomly initialize the network
            new NguyenWidrow(network).Randomize();

            // Teach the network using parallel Rprop:
            //var teacher = new ResilientBackpropagationLearning(network);
            var teacher = new LevenbergMarquardtLearning(network);

            // Iterate until stop criteria is met
            var error = teacher.RunEpoch(features, outputs);
            double previous;
            var iteration = 0;

            do
            {
                previous = error;
                iteration++;
                // Compute one learning iteration
                error = teacher.RunEpoch(features, outputs);

                var results = new(int, int)[features.Length];
                for (var index = 0; index < features.Length; index++)
                {
                    var feature = features[index];
                    var @class = classes[index];
                    var result = network.Compute(feature).ArgMax();
                    results[index] = (@class, result);
                }

                var tp = 100.0 * results.Count(item => item.Item1 == 1 && item.Item1 == item.Item2) / results.Length;
                var tn = 100.0 * results.Count(item => item.Item1 == 1 && item.Item1 != item.Item2) / results.Length;
                var fp = 100.0 * results.Count(item => item.Item1 == 0 && item.Item1 == item.Item2) / results.Length;
                var fn = 100.0 * results.Count(item => item.Item1 == 0 && item.Item1 != item.Item2) / results.Length;

                Console.WriteLine($"Iterations: {iteration} Error: {error:F2} TP: {tp:F2} TN: {tn:F2} FP: {fp:F2} FN: {fn:F2}");
            }
            while (Math.Abs(previous - error) > 0.00001 * previous);

            // learn result
            //var results = new(int, int)[features.Length];
            //for (var index = 0; index < features.Length; index++)
            //{
            //    var feature = features[index];
            //    var @class = classes[index];
            //    var result = network.Compute(feature).ArgMax();
            //    results[index] = (@class, result);
            //}

            //var tp = 100.0 * results.Count(item => item.Item1 == 1 && item.Item1 == item.Item2) / results.Length;
            //var tn = 100.0 * results.Count(item => item.Item1 == 1 && item.Item1 != item.Item2) / results.Length;
            //var fp = 100.0 * results.Count(item => item.Item1 == 0 && item.Item1 == item.Item2) / results.Length;
            //var fn = 100.0 * results.Count(item => item.Item1 == 0 && item.Item1 != item.Item2) / results.Length;

            //Console.WriteLine($"Iterations: {iteration} Error: {error:F2} TP: {tp:F2} TN: {tn:F2} FP: {fp:F2} FN: {fn:F2}");
            return network;
        }
    }
}