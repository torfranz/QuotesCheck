namespace QuotesCheck.Evaluation
{
    using System;
    using System.IO;
    using System.Linq;

    using Accord.Diagnostics;
    using Accord.MachineLearning.VectorMachines;
    using Accord.MachineLearning.VectorMachines.Learning;
    using Accord.Math;
    using Accord.Neuro;
    using Accord.Neuro.Learning;
    using Accord.Statistics.Kernels;

    internal class SingleLearning
    {
        private readonly double costOfTrades;

        private readonly FeatureExtractor featureExtractor;

        private readonly SymbolInformation symbol;

        private ActivationNetwork network;

        internal SingleLearning(FeatureExtractor featureExtractor, double costOfTrades)
        {
            this.costOfTrades = costOfTrades;
            this.featureExtractor = featureExtractor;
            this.symbol = featureExtractor.Symbol;
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
            this.network?.Save(this.BuildFilePath(folder));
            return this;
        }

        internal SingleLearning Learn(bool relearn = false)
        {
            var inputs = this.featureExtractor.LearnSeries.Select(item => item.features).ToArray();
            Debug.Assert(inputs.Length > 0);
            var inputsCount = inputs[0].Length;

            var outputs = Jagged.OneHot(this.featureExtractor.LearnLabels);
            Debug.Assert(outputs.Length > 0);
            var outputsCount = outputs[0].Length;

            if ((this.network == null) || relearn)
            {
                // Create an activation network with the function and
                this.network = new ActivationNetwork(new RectifiedLinearFunction(), inputsCount, inputsCount + outputsCount, outputsCount);

                // Randomly initialize the network
                new NguyenWidrow(this.network).Randomize();
            }
            else
            {
                Debug.Assert(this.network.InputsCount == inputsCount);
            }

            // Teach the network using parallel Rprop:
            var teacher = new ResilientBackpropagationLearning(this.network);

            // Iterate until stop criteria is met
            var error = teacher.RunEpoch(inputs, outputs);
            double previous;

            do
            {
                previous = error;

                // Compute one learning iteration
                error = teacher.RunEpoch(inputs, outputs);
            }
            while (Math.Abs(previous - error) > 0.0000000001 * previous);
            
            // Checks if the network has learned
            var tp = 0.0;
            var tn = 0.0;
            var fp = 0.0;
            var fn = 0.0;

            for (var i = 0; i < inputs.Length; i++)
            {
                var computed = this.network.Compute(inputs[i]).ArgMax();
                
                var given = this.featureExtractor.LearnLabels[i];

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

            return this;
        }

        internal EvaluationResult Apply()
        {
            return this.CreateResult(this.featureExtractor.LearnSeries);
        }

        internal EvaluationResult Validate()
        {
            return this.CreateResult(this.featureExtractor.ValidationSeries);
        }

        private EvaluationResult CreateResult((int seriesIdx, TimeSeries series, double[] features)[] series)
        {
            var startIndex = series[0].seriesIdx;
            var endIndex = series[series.Length - 1].seriesIdx;
            var result = new EvaluationResult(
                this.symbol.CompanyName,
                this.symbol.ISIN,
                Helper.Delta(this.symbol.Open[endIndex - 1], this.symbol.Open[startIndex - 1]));

            Trade trade = null;

            double upperBound = 0;
            double lowerBound = 0;
            for (var i = 0; i < series.Length; i++)
            {
                var label = this.network.Compute(series[i].features).ArgMax();
                var features = this.featureExtractor.LearnSeries[i];
                if ((trade == null) && (label == 1))
                {
                    trade = new Trade
                                {
                                    BuyIndex = features.seriesIdx - 1,
                                    BuyValue = this.symbol.Open[features.seriesIdx - 1],
                                    BuyDate = this.symbol.Day[features.seriesIdx - 1],
                                    CostOfTrades = this.costOfTrades
                                };
                    result.Trades.Add(trade);

                    upperBound = (1 + this.featureExtractor.UpperBound / 100.0) * trade.BuyValue;
                    lowerBound = (1 + this.featureExtractor.LowerBound / 100.0) * trade.BuyValue;
                }
                else if (trade != null)
                {
                    var close = this.symbol.Close[features.seriesIdx];
                    // is this day also expectiong more gains, adapt upper and lower
                    if (label == 1)
                    {
                        upperBound = Math.Max(upperBound, (1 + this.featureExtractor.UpperBound / 100.0) * close);
                        lowerBound = Math.Max(lowerBound, (1 + this.featureExtractor.LowerBound / 100.0) * close);
                    }
                    else
                    {
                        // did the close leave the lowerBound -> upperBound range, close the trade on next day open
                        if ((close >= upperBound) || (close <= lowerBound))
                        {
                            trade.SellIndex = features.seriesIdx - 1;
                            trade.SellValue = this.symbol.Open[features.seriesIdx - 1];
                            trade.SellDate = this.symbol.Day[features.seriesIdx - 1];

                            this.SetHighestValueForTrade(trade);

                            trade = null;
                        }
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

        private string BuildFilePath(string folder)
        {
            return Path.Combine(folder, $"{this.symbol.ISIN}.bin");
        }
    }
}