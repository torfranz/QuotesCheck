namespace QuotesCheck.Evaluation
{
    using System;

    using Accord.Diagnostics;
    using Accord.Math;
    using Accord.Neuro;
    using Accord.Neuro.Learning;

    internal class Learner
    {
        public Network Learn(double[][] features, int[] labels, int? hiddenLayerCount = null)
        {
            Debug.Assert(features.Length > 0);
            var inputsCount = features[0].Length;

            var outputs = Jagged.OneHot(labels);
            Debug.Assert(outputs.Length > 0);
            var outputsCount = outputs[0].Length;

            // Create an activation network with the function and
            var network = new ActivationNetwork(
                new BipolarSigmoidFunction(0.5),
                inputsCount,
                hiddenLayerCount.GetValueOrDefault(2 * inputsCount - outputsCount),
                outputsCount);

            // Randomly initialize the network
            new NguyenWidrow(network).Randomize();

            // Teach the network using parallel Rprop:
            //var teacher = new ResilientBackpropagationLearning(network);
            var teacher = new EvolutionaryLearning(network, 100);

            // Iterate until stop criteria is met
            var error = teacher.RunEpoch(features, outputs);
            double previous;

            do
            {
                previous = error;

                // Compute one learning iteration
                error = teacher.RunEpoch(features, outputs);
            }
            while (Math.Abs(previous - error) > 0.00000001 * previous);

            return network;
        }

        /*
        private EvaluationResult CreateResult((int seriesIdx, double[][] features)[] series, int iteration)
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

                    upperBound = (1 + this.featureCreator.UpperBound / 100.0) * trade.BuyValue;
                    lowerBound = (1 + this.featureCreator.LowerBound / 100.0) * trade.BuyValue;

                    trade.LowerBoundCurve.Add(lowerBound);
                    trade.UpperBoundCurve.Add(upperBound);
                }
                else if (trade != null)
                {
                    var close = this.symbol.Close[seriesItem.seriesIdx];

                    // is this day also expecting more gains, adapt upper and lower for followng days based on todays close
                    if (label == 1)
                    {
                        upperBound = Math.Max(upperBound, (1 + this.featureCreator.UpperBound / 100.0) * close);
                        lowerBound = Math.Max(lowerBound, (1 + this.featureCreator.LowerBound / 100.0) * close);
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
        */
    }
}