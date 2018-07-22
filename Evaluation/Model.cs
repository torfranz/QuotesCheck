namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Accord;
    using Accord.Math;
    using Accord.Math.Distances;
    using Accord.Neuro;

    internal class Model
    {
        private readonly DateTime[] Days;

        private readonly double[] RSI;

        private readonly double[] ST;

        private readonly double[] RelativeMACD;

        private readonly double[] Ema20_50;

        private readonly double[] Ema50_200;

        private readonly double[] bullish;

        private double[] obos;

        private double[] bearish;

        private double[] dmi;

        private double[] diPlus;

        private double[] diMinus;

        public Model(SymbolInformation symbol, Learner learner, double costOfTrades)
        {
            this.Symbol = symbol;
            this.Learner = learner;
            this.CostOfTrades = costOfTrades;

            // create additional data required for features
            this.High = this.Symbol.High;
            this.Low = this.Symbol.Low;
            this.Close = this.Symbol.Close;
            this.Open = this.Symbol.Open;
            this.Volume = this.Symbol.Volume;
            this.Days = this.Symbol.Day;

            this.Ema20 = Indicators.EMA(this.Symbol, SourceType.Close, 20);
            this.Ema50 = Indicators.EMA(this.Symbol, SourceType.Close, 50);
            this.Ema200 = Indicators.EMA(this.Symbol, SourceType.Close, 200);
            this.RSI = Indicators.RSI(this.Close, 50).Scale(0, 100, -1.0, 1);

            //this.KAMA = Indicators.KAMA(symbol, SourceType.Close, 50);
            this.ST = Indicators.ST(this.Symbol, 50, 3);
            (this.RelativeMACD, this.RelativeSignal) = Indicators.RelativeMACD(this.Close, 20, 200, 9, MovingAverage.EMA);
            this.RelativeMACD = this.Scale(this.RelativeMACD, -0.15, 0.15);
            this.RelativeSignal = this.Scale(this.RelativeSignal, -0.15, 0.15);
            this.Ema20_50 = this.Scale(Indicators.RelativeDistance(this.Ema20, this.Ema50), -0.20, 0.20);
            this.Ema50_200 = this.Scale(Indicators.RelativeDistance(this.Ema50, this.Ema200), -0.20, 0.20);
            this.Ema50_Close = this.Scale(Indicators.RelativeDistance(this.Ema50, this.Close), -0.40, 0.40);
            this.St_Close = this.Scale(Indicators.RelativeDistance(this.ST, this.Close), -0.30, 0.30);
            this.Vola = this.Scale(Indicators.VOLA(this.Symbol, SourceType.Close, 30, 250), 0, 0.7);
            this.obos = this.Scale(Indicators.OBOS(this.Symbol, 50), 0, 100);
            var (elrBullish, elrBearish) = Indicators.ELR(this.Symbol, 50);
            this.bullish = this.Scale(Indicators.Ratio(elrBullish, this.Close), -0.10, 0.10);
            this.bearish = this.Scale(Indicators.Ratio(elrBearish, this.Close), -0.10, 0.101);
            var (dmi, diPlus, diMinus) = Indicators.DMI(this.Symbol, 50);
            this.dmi = this.Scale(dmi, 0, 100);
            this.diPlus = this.Scale(diPlus, 0, 100);
            this.diMinus = this.Scale(diMinus, 0, 100);
            this.DayOfWeek = Scale(Indicators.DayOfWeek(symbol), 0, 6);
            this.Month = Scale(Indicators.Month(symbol), 1, 12);
            this.RelMacdSignal = this.Scale(Indicators.Distance(RelativeMACD, RelativeSignal), -0.1, 0.1);
            //var corr = new PearsonCorrelation();
            //var sim = corr.Similarity(this.Ema20, this.Ema20);
        }

        public double[] DayOfWeek { get; }

        public double[] Month { get; }
        public double[] RelMacdSignal { get; }
        public double[] Vola { get; }

        public double[] St_Close { get; }

        public double[] Ema50_Close { get; }

        public double[] RelativeSignal { get; }

        public double[] Ema200 { get; }

        public double[] Ema50 { get; }

        public double[] Ema20 { get; }

        public int[] Volume { get; }

        public double[] Open { get; }

        public double[] Close { get; }

        public double[] Low { get; }

        public double[] High { get; }

        public double CostOfTrades { get; }

        public SymbolInformation Symbol { get; }

        public Learner Learner { get; }

        public Network[] Networks { get; private set; }

        public double UpperBound { get; set; } = 0.15;

        public double LowerBound { get; set; } = -0.05;

        public int CandleCount { get; set; } = 50;

        public int FeatureSetsCount => 1;

        public IList<(string Name, double[] Values, bool IsLine, bool IsDot)> CurveData =>
            new[] { ($"Ema200", this.Ema200, true, false), ($"Ema50", this.Ema50, true, false), ($"Ema20", this.Ema20, true, false), };

        public void Learn(IntRange range)
        {
            var featureSets = this.GenerateFeatureSets(range, true);
            var featureSetsCount = featureSets.Length;
            Debug.Assert(featureSetsCount > 0);

            this.Networks = new Network[featureSetsCount];
            for (var idx = 0; idx < featureSetsCount; idx++)
            {
                Console.WriteLine($"Learning feature set {idx+1} for {Symbol.ISIN} from {Symbol.Day[range.Max]:yyyy-MM-dd} - {Symbol.Day[range.Min]:yyyy-MM-dd} [{range.Length + 1} data points]");
                var features = featureSets[idx].Select(item => item.Features).ToArray();
                var targets = featureSets[idx].Select(item => item.Target.GetValueOrDefault()).ToArray();
                this.Networks[idx] = this.Learner.Learn(features, targets);
            }
        }

        public TargetResult Apply(IntRange range)
        {
            var featureSets = this.GenerateFeatureSets(range, false);

            var featureSetsCount = featureSets.Length;
            Debug.Assert(featureSetsCount > 0);
            Debug.Assert(featureSetsCount == this.Networks.Length);

            var result = new TargetResult(range);

            for (var idx = range.Min; idx <= range.Max; idx++)
            {
                var sum = 0.0;
                for (var featureIdx = 0; featureIdx < featureSetsCount; featureIdx++)
                {
                    var indexedFeatures = featureSets[featureIdx][idx - range.Min];
                    Debug.Assert(indexedFeatures.Index == idx);

                    var network = this.Networks[featureIdx];
                    sum += network.Compute(indexedFeatures.Features).ArgMax();
                }

                result.Targets[idx - range.Min] = new IndexedTarget { Index = idx, Target = Convert.ToInt32(sum / featureSetsCount) };
            }

            return result;
        }

        public EvaluationResult CreateResult(TargetResult targetResult)
        {
            var startIndex = targetResult.Range.Max;
            var endIndex = targetResult.Range.Min;
            var result = new EvaluationResult(
                this.Symbol.CompanyName,
                this.Symbol.ISIN,
                Helper.Delta(this.Symbol.Close[endIndex], this.Symbol.Open[startIndex - 1]));

            Trade trade = null;

            double targetUpperBound = 0;
            double initialTargetUpperBound = 0;
            double lowerBound = 0;
            double equityGain = 0;
            result.EquityCurve.Add((this.Symbol.Day[startIndex], equityGain));

            for (var i = startIndex; i >= endIndex; i--)
            {
                var indexedTarget = targetResult.Targets[i - targetResult.Range.Min];
                Debug.Assert(indexedTarget.Index == i);

                if ((trade == null) && (indexedTarget.Target == 1))
                {
                    trade = new Trade
                                {
                                    BuyIndex = i - 1,
                                    BuyValue = this.Symbol.Open[i - 1],
                                    BuyDate = this.Symbol.Day[i - 1],
                                    CostOfTrades = this.CostOfTrades
                                };
                    result.Trades.Add(trade);
                    result.EquityCurve.Add((trade.BuyDate, equityGain));

                    initialTargetUpperBound = targetUpperBound = (1 + UpperBound) * trade.BuyValue;
                    lowerBound = (1 + this.LowerBound) * trade.BuyValue;

                    trade.LowerBoundCurve.Add(lowerBound);
                    trade.UpperBoundCurve.Add(targetUpperBound);
                }
                else if (trade != null)
                {
                    var close = this.Symbol.Close[i];

                    // equity change for this day compared to buy
                    //result.EquityCurve.Add((this.Symbol.Day[i], 100 * Helper.Delta(close, trade.BuyValue) + equityGain));

                    // did the close leave the lowerBound -> upperBound range, close the trade on next day open
                    if ((close >= targetUpperBound) || (close <= lowerBound))
                    {
                        trade.SellIndex = i - 1;
                        trade.SellValue = this.Symbol.Open[i - 1];
                        trade.SellDate = this.Symbol.Day[i - 1];

                        this.SetHighestValueForTrade(trade);

                        // equity change for next day sell
                        equityGain += trade.Gain;
                        result.EquityCurve.Add((trade.SellDate, equityGain));

                        trade = null;
                    }
                    else
                    {
                        // is this day also expecting more gains, adapt upper and lower for following days
                        var newTargetUpperBound = (1 + UpperBound) * close;
                        if (newTargetUpperBound > targetUpperBound)
                        {
                            lowerBound += newTargetUpperBound - targetUpperBound;
                            targetUpperBound = newTargetUpperBound;
                        }

                        // in case close reached the minimum upper bound, take this as new lower bound
                        if (close >= 0.9999 * initialTargetUpperBound)
                        {
                            lowerBound = Math.Max(initialTargetUpperBound, lowerBound);
                        }

                        // set lower/upper bound for next day
                        trade.LowerBoundCurve.Add(lowerBound);
                        trade.UpperBoundCurve.Add(targetUpperBound);
                    }
                }
            }

            // Trade still open? -> exit with last close
            if (trade != null)
            {
                // last data point is always considered an exit point
                trade.SellIndex = endIndex;
                trade.SellValue = this.Symbol.Close[endIndex];
                trade.SellDate = this.Symbol.Day[endIndex];
                this.SetHighestValueForTrade(trade);

                // equity change for next day sell
                equityGain += trade.Gain;
                result.EquityCurve.Add((trade.SellDate, equityGain));
            }

            return result;
        }

        public EvaluationResult EvaluateModelValidity()
        {
            var range = new IntRange(this.CandleCount + 1, this.Symbol.TimeSeries.Count - this.CandleCount);
            var targets = this.GenerateFeatureSets(range, true)[0];
            var result = new TargetResult(range);
            targets.Select(item => new IndexedTarget { Index = item.Index, Target = item.Target.GetValueOrDefault() }).ToArray().CopyTo(result.Targets);

            Debug.Assert(result.Targets[0].Index == range.Min);
            Debug.Assert(result.Targets[result.Range.Length].Index == range.Max);
            return this.CreateResult(result);
        }

        private double[] Scale(double[] data, double lower, double upper)
        {
            var scaleRange = this.Learner.ScaleRange;
            return data.Scale(lower, upper, scaleRange.Min, scaleRange.Max);
        }

        private double Scale(double data, double lower, double upper)
        {
            var scaleRange = this.Learner.ScaleRange;
            return data.Scale(lower, upper, scaleRange.Min, scaleRange.Max);
        }

        private IndexedFeaturesAndTarget[][] GenerateFeatureSets(IntRange range, bool createTargets)
        {
            // create data structure
            var indexedFeatures = new IndexedFeaturesAndTarget[this.FeatureSetsCount][];
            for (var i = 0; i < indexedFeatures.Length; i++)
            {
                indexedFeatures[i] = new IndexedFeaturesAndTarget[range.Length + 1];
            }

            for (var idx = range.Min; idx <= range.Max; idx++)
            {
                // features
                //var target = createTargets ? this.GetTargetValue(this.Symbol.Open[idx - 1], idx - 1) : default(double?);
                var target = createTargets ? this.GetClass(this.Symbol.Open[idx - 1], idx - 1) : default(int?);

                indexedFeatures[0][idx - range.Min] = new IndexedFeaturesAndTarget
                                                          {
                                                              Index = idx,
                                                              Target = target,
                                                              Features = new[]
                                                                             {
                                                                  Ema20_50[idx],
                                                                  RSI[idx],
                                                                  RelMacdSignal[idx],
                                                                  bullish[idx],
                                                                  St_Close[idx]
                                                                  //, this.DayOfWeek[idx], this.Month[idx]
                                                                             }
                                                          };
                //indexedFeatures[1][idx - range.Min] =
                //    new IndexedFeaturesAndTarget
                //        {
                //            Index = idx,
                //            Target = target,
                //            Features = new[] { dayOfWeek, season, intradayGain, todaysGain, todaysVolumeGain }
                //        };
            }

            return indexedFeatures;
        }

        private double GetTargetValue(double startValue, int startIndex)
        {
            var target = this.LowerBound;
            for (var idx = startIndex; idx >= startIndex - this.CandleCount; idx--)
            {
                var gain = Helper.Delta(this.Symbol.Close[idx], startValue);
                if (gain > target)
                {
                    target = gain;
                }

                if (gain < this.LowerBound)
                {
                    break;
                }
            }

            return target;
        }

        private int GetClass(double startValue, int startIndex)
        {
            var target = this.LowerBound;
            for (var idx = startIndex; idx >= Math.Max(0, startIndex - this.CandleCount); idx--)
            {
                var gain = Helper.Delta(this.Symbol.Close[idx], startValue);
                if (gain > UpperBound)
                {
                    return 1;
                }

                if (gain < this.LowerBound)
                {
                    break;
                }
            }

            return 0;
        }

        private void SetHighestValueForTrade(Trade trade)
        {
            double max = 0;
            for (var i = trade.SellIndex; i < trade.BuyIndex; i++)
            {
                max = Math.Max(max, this.Symbol.Open[i]);
            }

            trade.HighestValue = max;
        }
    }
}