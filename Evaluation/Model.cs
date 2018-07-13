namespace QuotesCheck.Evaluation
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using Accord;
    using Accord.Math;
    using Accord.Neuro;

    internal class Model
    {
        public Model(SymbolInformation symbol, Learner learner, double costOfTrades)
        {
            this.Symbol = symbol;
            this.Learner = learner;
            this.CostOfTrades = costOfTrades;
        }

        public double CostOfTrades { get; }

        public SymbolInformation Symbol { get; }

        public Learner Learner { get; }

        public Network[] Networks { get; private set; }

        public double UpperBound { get; set; } = 10;

        public double LowerBound { get; set; } = -5;

        public int CandleCount { get; set; } = 50;

        public IndexedFeaturesAndTarget[][] GenerateFeatureSets(IntRange range, bool createTargets)
        {
            var High = this.Symbol.High;
            var Low = this.Symbol.Low;
            var Close = this.Symbol.Close;
            var Open = this.Symbol.Open;
            var Volume = this.Symbol.Volume;
            var days = this.Symbol.Day;

            var Ema20 = Indicators.EMA(this.Symbol, SourceType.Close, 20);
            var Ema50 = Indicators.EMA(this.Symbol, SourceType.Close, 50);
            var Ema200 = Indicators.EMA(this.Symbol, SourceType.Close, 200);
            var RSI = Indicators.RSI(Close, 50).Scale(0, 100, -1.0, 1);

            //this.KAMA = Indicators.KAMA(symbol, SourceType.Close, 50);
            var ST = Indicators.ST(this.Symbol, 50, 3);
            var (MACD, Signal) = Indicators.MACD(Close, 50, 200, 20, MovingAverage.EMA);

            var Ema20_50 = Indicators.RelativeDistance(Ema20, Ema50).Scale(-0.20, 0.20, -1.0, 1);
            var Ema50_200 = Indicators.RelativeDistance(Ema50, Ema200).Scale(-0.20, 0.20, -1.0, 1);
            var Ema50_Close = Indicators.RelativeDistance(Ema50, Close).Scale(-0.40, 0.40, -1.0, 1);
            var St_Close = Indicators.RelativeDistance(ST, Close).Scale(-0.30, 0.30, -1.0, 1);
            var Macd_Close = Indicators.Ratio(MACD, Close).Scale(-0.30, 0.30, -1.0, 1);
            var Signal_Close = Indicators.Ratio(Signal, MACD).Scale(-0.30, 0.30, -1.0, 1);
            var vola = Indicators.VOLA(this.Symbol, SourceType.Close, 30, 250).Scale(0, 0.7, -1.0, 1);
            var obos = Indicators.OBOS(this.Symbol, 50).Scale(0, 100, -1.0, 1);
            var (elrBullish, elrBearish) = Indicators.ELR(this.Symbol, 50);
            var bullish = Indicators.Ratio(elrBullish, Close).Scale(-0.10, 0.10, -1.0, 1);
            var bearish = Indicators.Ratio(elrBearish, Close).Scale(-0.10, 0.10, -1.0, 1);
            var (dmi, diPlus, diMinus) = Indicators.DMI(this.Symbol, 50);
            dmi = dmi.Scale(0, 100, -1.0, 1);
            diPlus = diPlus.Scale(0, 100, -1.0, 1);
            diMinus = diMinus.Scale(0, 100, -1.0, 1);

            // create data structure
            var indexedFeatures = new IndexedFeaturesAndTarget[2][];
            for (var i = 0; i < indexedFeatures.Length; i++)
            {
                indexedFeatures[i] = new IndexedFeaturesAndTarget[range.Length];
            }

            for (var idx = range.Min; idx <= range.Max; idx++)
            {
                // features
                var dayOfWeek = Convert.ToInt32(days[idx].DayOfWeek).Scale(0, 6, 1.0, 1);
                var season = Math.Abs(6.5 - days[idx].Month).Scale(0.5, 5.5, 1.0, 1);
                var rsi = RSI[idx];
                var ema20_50 = Ema20_50[idx];
                var ema50_200 = Ema50_200[idx];
                var ema50_Close = Ema50_Close[idx];
                var st_Close = St_Close[idx];
                var macd_Close = Macd_Close[idx];
                var signal_Macd = Signal_Close[idx];
                var intradayGain = Helper.Delta(Close[idx], Open[idx]).Scale(-10.0, 10, -1.0, 1);
                var todaysGain = Helper.Delta(Close[idx], Close[idx + 1]).Scale(-10.0, 10, -1.0, 1);
                var todaysVolumeGain = Helper.Delta(Volume[idx], Volume[idx + 1]).Scale(-20.0, 20, -1.0, 1);

                var target = createTargets ? this.GetTargetValue(this.Symbol.Open[idx - 1], idx - 1) : default(int?);

                indexedFeatures[0][idx] = new IndexedFeaturesAndTarget
                                              {
                                                  Index = idx,
                                                  Target = target,
                                                  Features = new[]
                                                                 {
                                                                     rsi, ema20_50, ema50_200, ema50_Close, signal_Macd, st_Close,
                                                                     bullish[idx]
                                                                 }
                                              };
                indexedFeatures[1][idx] = new IndexedFeaturesAndTarget
                                              {
                                                  Index = idx,
                                                  Target = target,
                                                  Features = new[] { dayOfWeek, season, intradayGain, todaysGain, todaysVolumeGain }
                                              };
            }

            return indexedFeatures;
        }

        public void Learn(IntRange range)
        {
            var featureSets = this.GenerateFeatureSets(range, true);
            Debug.Assert(featureSets.Length > 0);

            var featureSetsCount = featureSets.Length;
            Debug.Assert(featureSetsCount > 0);

            this.Networks = new Network[featureSetsCount];
            for (var idx = 0; idx < featureSetsCount; idx++)
            {
                var features = featureSets[idx].Select(item => item.Features).ToArray();
                var targets = featureSets[idx].Select(item => item.Target.GetValueOrDefault()).ToArray();
                this.Networks[idx] = this.Learner.Learn(features, targets);
            }
        }

        public IndexedTarget[] Apply(IntRange range)
        {
            var featureSets = this.GenerateFeatureSets(range, false);
            Debug.Assert(featureSets.Length > 0);

            var featureSetsCount = featureSets.Length;
            Debug.Assert(featureSetsCount > 0);
            Debug.Assert(featureSetsCount == this.Networks.Length);

            var targets = new IndexedTarget[range.Length];

            for (var idx = range.Min; idx <= range.Max; idx++)
            {
                var sum = 0.0;
                for (var featureIdx = 0; featureIdx < featureSetsCount; featureIdx++)
                {
                    var features = featureSets[featureIdx][idx];
                    var network = this.Networks[featureIdx];
                    sum += network.Compute(features.Features).ArgMax();
                }

                targets[idx] = new IndexedTarget { Index = idx, Target = Convert.ToInt32(sum / featureSetsCount) };
            }

            return targets;
        }

        public EvaluationResult CreateResult(int[] targets, int startIndex, int endIndex = 0)
        {
            var result = new EvaluationResult(
                this.Symbol.CompanyName,
                this.Symbol.ISIN,
                Helper.Delta(this.Symbol.Open[endIndex - 1], this.Symbol.Open[startIndex - 1]));

            Trade trade = null;

            double upperBound = 0;
            double lowerBound = 0;
            for (var i = startIndex; i >= endIndex; i--)
            {
                var label = targets[i];

                if ((trade == null) && (label == 1))
                {
                    trade = new Trade
                                {
                                    BuyIndex = i - 1,
                                    BuyValue = this.Symbol.Open[i - 1],
                                    BuyDate = this.Symbol.Day[i - 1],
                                    CostOfTrades = this.CostOfTrades
                                };
                    result.Trades.Add(trade);

                    upperBound = (1 + this.UpperBound / 100.0) * trade.BuyValue;
                    lowerBound = (1 + this.LowerBound / 100.0) * trade.BuyValue;

                    trade.LowerBoundCurve.Add(lowerBound);
                    trade.UpperBoundCurve.Add(upperBound);
                }
                else if (trade != null)
                {
                    var close = this.Symbol.Close[i];

                    // is this day also expecting more gains, adapt upper and lower for followng days based on todays close
                    if (label == 1)
                    {
                        upperBound = Math.Max(upperBound, (1 + this.UpperBound / 100.0) * close);
                        lowerBound = Math.Max(lowerBound, (1 + this.LowerBound / 100.0) * close);
                    }

                    // set lower/upper bound for next day
                    trade.LowerBoundCurve.Add(lowerBound);
                    trade.UpperBoundCurve.Add(upperBound);

                    // did the close leave the lowerBound -> upperBound range, close the trade on next day open
                    if ((close >= upperBound) || (close <= lowerBound))
                    {
                        trade.SellIndex = i - 1;
                        trade.SellValue = this.Symbol.Open[i - 1];
                        trade.SellDate = this.Symbol.Day[i - 1];

                        this.SetHighestValueForTrade(this.Symbol, trade);

                        trade = null;
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
                this.SetHighestValueForTrade(this.Symbol, trade);
            }

            return result;
        }

        private int GetTargetValue(double startValue, int startIndex)
        {
            for (var idx = startIndex; idx >= startIndex - this.CandleCount; idx--)
            {
                if (Helper.Delta(this.Symbol.Close[idx], startValue) > this.UpperBound)
                {
                    return 1;
                }

                if (Helper.Delta(this.Symbol.Close[idx], startValue) < this.LowerBound)
                {
                    return 0;
                }
            }

            return 0;
        }

        private void SetHighestValueForTrade(SymbolInformation symbol, Trade trade)
        {
            double max = 0;
            for (var i = trade.SellIndex; i < trade.BuyIndex; i++)
            {
                max = Math.Max(max, symbol.Open[i]);
            }

            trade.HighestValue = max;
        }
    }
}