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

        private readonly FeatureExtractor featureExtractor;

        private readonly SymbolInformation symbol;

        private readonly ActivationNetwork[][] networks; // [learnSeries][features]

        internal SingleLearning(FeatureExtractor featureExtractor, double costOfTrades)
        {
            this.costOfTrades = costOfTrades;
            this.featureExtractor = featureExtractor;
            this.symbol = featureExtractor.Symbol;
            this.networks = new ActivationNetwork[this.featureExtractor.LearnSeries.Length][];

            for (var i = 0; i < this.networks.Length; i++)
            {
                this.networks[i] = new ActivationNetwork[featureExtractor.LearnSeries[0][0].features.Length];
            }
        }

        public IList<(string Name, double[] Values, bool IsLine, bool IsDot)> CurveData =>
            new[]
                {
                    ("EMA 20", this.featureExtractor.Ema20, true, false), ("EMA 50", this.featureExtractor.Ema50, true, false),
                    ("EMA 200", this.featureExtractor.Ema200, true, false),
                };

        internal SingleLearning Load(string folder, string postFix)
        {
            for (var laernRangeIdx = 0; laernRangeIdx < this.networks.Length; laernRangeIdx++)
            {
                for (var featureIdx = 0; featureIdx < this.networks[0].Length; featureIdx++)
                {
                    var filePath = this.BuildFilePath(folder, $"_L{laernRangeIdx}_F{featureIdx}{postFix}");
                    if (File.Exists(filePath))
                    {
                        this.networks[laernRangeIdx][featureIdx] = Network.Load(filePath) as ActivationNetwork;
                    }
                }
            }

            return this;
        }

        internal SingleLearning Save(string folder, string postFix)
        {
            for (var learnRangeIdx = 0; learnRangeIdx < this.networks.Length; learnRangeIdx++)
            {
                for (var featureIdx = 0; featureIdx < this.networks[0].Length; featureIdx++)
                {
                    Debug.Assert(this.networks[learnRangeIdx] != null);
                    Directory.CreateDirectory(folder);
                    this.networks[learnRangeIdx][featureIdx].Save(this.BuildFilePath(folder, $"_L{learnRangeIdx}_F{featureIdx}{postFix}"));
                }
            }

            return this;
        }

        internal SingleLearning Learn(int? hiddenLayerCount = null, bool relearn = false)
        {
            for (var learnRangeIdx = 0; learnRangeIdx < this.networks.Length; learnRangeIdx++)
            {
                for (var featureIdx = 0; featureIdx < this.networks[0].Length; featureIdx++)
                {
                    var network = this.networks[learnRangeIdx][featureIdx];
                    this.Learn(
                        ref network,
                        this.featureExtractor.LearnSeries[learnRangeIdx].Select(item => item.features[featureIdx]).ToArray(),
                        this.featureExtractor.LearnLabels[learnRangeIdx],
                        hiddenLayerCount,
                        relearn);

                    this.networks[learnRangeIdx][featureIdx] = network;
                }
            }

            return this;
        }

        internal EvaluationResult[] Apply()
        {
            var results = new EvaluationResult[this.featureExtractor.LearnSeries.Length];
            for (var learnRangeIdx = 0; learnRangeIdx < this.networks.Length; learnRangeIdx++)
            {
                results[learnRangeIdx] = this.CreateResult(this.featureExtractor.LearnSeries[learnRangeIdx], learnRangeIdx);
            }

            return results;
        }

        internal EvaluationResult Validate()
        {
            return this.CreateResult(this.featureExtractor.ValidationSeries, 0);
        }

        private void Learn(ref ActivationNetwork network, double[][] inputs, int[] labels, int? hiddenLayerCount = null, bool relearn = false)
        {
            Debug.Assert(inputs.Length > 0);
            var inputsCount = inputs[0].Length;

            var outputs = Jagged.OneHot(labels);
            Debug.Assert(outputs.Length > 0);
            var outputsCount = outputs[0].Length;

            if ((network == null) || relearn)
            {
                // Create an activation network with the function and
                network = new ActivationNetwork(
                    new BipolarSigmoidFunction(0.5),
                    inputsCount,
                    hiddenLayerCount.GetValueOrDefault(2 * inputsCount - outputsCount),
                    outputsCount);

                // Randomly initialize the network
                new NguyenWidrow(network).Randomize();
            }
            else
            {
                Debug.Assert(network.InputsCount == inputsCount);
            }

            // Teach the network using parallel Rprop:
            var teacher = new ResilientBackpropagationLearning(network);

            // Iterate until stop criteria is met
            var error = teacher.RunEpoch(inputs, outputs);
            double previous;

            do
            {
                previous = error;

                // Compute one learning iteration
                error = teacher.RunEpoch(inputs, outputs);
            }
            while (Math.Abs(previous - error) > 0.00000001 * previous);

            // Checks if the network has learned
            var tp = 0.0;
            var tn = 0.0;
            var fp = 0.0;
            var fn = 0.0;

            for (var i = 0; i < inputs.Length; i++)
            {
                var computed = network.Compute(inputs[i]).ArgMax();

                var given = labels[i];

                if (given == 0)
                {
                    if (computed == 0)
                    {
                        tn++;
                    }
                    else
                    {
                        fn++;
                    }
                }
                else
                {
                    if (computed == 1)
                    {
                        tp++;
                    }
                    else
                    {
                        fp++;
                    }
                }
            }

            // sensitivity, recall, hit rate, or true positive rate(TPR)
            var tpr = tp / (tp + fn);

            //specificity or true negative rate(TNR)
            var tnr = tn / (tn + fp);

            //precision or positive predictive value(PPV)
            var ppv = tp / (tp + fp);

            //negative predictive value(NPV)
            var npv = tn / (tn + fn);

            //miss rate or false negative rate(FNR)
            var fnr = 1 - tpr;

            // fall -out or false positive rate(FPR)
            var fpr = 1 - tnr;

            //false discovery rate(FDR)
            var fdr = 1 - ppv;

            // false omission rate(FOR)
            var @for = 1 - npv;

            // accuracy(ACC)
            var acc = (tp + tn) / (tp + tn + fp + fn);
        }

        private EvaluationResult CreateResult((int seriesIdx, TimeSeries series, double[][] features)[] series, int iteration)
        {
            var startIndex = series[0].seriesIdx;
            var endIndex = series[series.Length - 1].seriesIdx;
            var result = new EvaluationResult(
                this.symbol.CompanyName,
                this.symbol.ISIN,
                Helper.Delta(this.symbol.Open[endIndex - 1], this.symbol.Open[startIndex - 1]));
            result.Iteration = iteration;

            Trade trade = null;

            double upperBound = 0;
            double lowerBound = 0;
            for (var i = 0; i < series.Length; i++)
            {
                var sum = 0.0;
                var networkCount = 0;
                for (var learnRangeIndex = 0; learnRangeIndex < this.networks.Length; learnRangeIndex++)
                {
                    for (var featureIndex = 0; featureIndex < this.networks[0].Length; featureIndex++)
                    {
                        sum += this.networks[learnRangeIndex][featureIndex].Compute(series[i].features[featureIndex]).ArgMax();
                        networkCount++;
                    }
                }

                var label = Convert.ToInt32(sum / networkCount);

                var seriesItem = series[i];
                if ((trade == null) && (label == 1))
                {
                    trade = new Trade
                                {
                                    BuyIndex = seriesItem.seriesIdx - 1,
                                    BuyValue = this.symbol.Open[seriesItem.seriesIdx - 1],
                                    BuyDate = this.symbol.Day[seriesItem.seriesIdx - 1],
                                    CostOfTrades = this.costOfTrades
                                };
                    result.Trades.Add(trade);

                    upperBound = (1 + this.featureExtractor.UpperBound / 100.0) * trade.BuyValue;
                    lowerBound = (1 + this.featureExtractor.LowerBound / 100.0) * trade.BuyValue;

                    trade.LowerBoundCurve.Add(lowerBound);
                    trade.UpperBoundCurve.Add(upperBound);
                }
                else if (trade != null)
                {
                    var close = this.symbol.Close[seriesItem.seriesIdx];

                    // is this day also expecting more gains, adapt upper and lower for followng days based on todays close
                    if (label == 1)
                    {
                        upperBound = Math.Max(upperBound, (1 + this.featureExtractor.UpperBound / 100.0) * close);
                        lowerBound = Math.Max(lowerBound, (1 + this.featureExtractor.LowerBound / 100.0) * close);
                    }

                    // set lower/upper bound for next day
                    trade.LowerBoundCurve.Add(lowerBound);
                    trade.UpperBoundCurve.Add(upperBound);

                    // did the close leave the lowerBound -> upperBound range, close the trade on next day open
                    if ((close >= upperBound) || (close <= lowerBound))
                    {
                        trade.SellIndex = seriesItem.seriesIdx - 1;
                        trade.SellValue = this.symbol.Open[seriesItem.seriesIdx - 1];
                        trade.SellDate = this.symbol.Day[seriesItem.seriesIdx - 1];

                        this.SetHighestValueForTrade(trade);

                        trade = null;
                    }
                }
            }

            // Trade still open? -> exit with last close
            if (trade != null)
            {
                // last data point is always considered an exit point
                trade.SellIndex = endIndex;
                trade.SellValue = this.symbol.TimeSeries[endIndex].Close;
                trade.SellDate = this.symbol.TimeSeries[endIndex].Day;
                this.SetHighestValueForTrade(trade);
            }

            return result;
        }

        private void SetHighestValueForTrade(Trade trade)
        {
            double max = 0;
            for (var i = trade.SellIndex; i < trade.BuyIndex; i++)
            {
                max = Math.Max(max, this.symbol.Open[i]);
            }

            trade.HighestValue = max;
        }

        private string BuildFilePath(string folder, string postFix)
        {
            return Path.Combine(folder, $"{this.symbol.ISIN}{postFix}.bin");
        }
    }
}