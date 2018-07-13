namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;

    internal class SimpleEvaluator : Evaluator
    {
        private readonly Dictionary<int, double[]> emas = new Dictionary<int, double[]>();

        private readonly Dictionary<int, double[]> temas = new Dictionary<int, double[]>();

        private readonly Dictionary<int, double[]> demas = new Dictionary<int, double[]>();

        private readonly Dictionary<int, double[]> smas = new Dictionary<int, double[]>();

        private double[] FastEntry;

        private double[] temaFast;

        private double[] SlowEntry;

        private double[] temaFastExit;

        private double[] FastExit;

        private double[] SlowExit;

        private double[] shortStop;

        private double[] longStop;

        private double[] signal;

        private double[] macd;

        private double[] tp;

        private double[] psar;

        public SimpleEvaluator(SymbolInformation symbol)
            : base(symbol)
        {
            this.tp = Indicators.TP(symbol);
            this.psar = Indicators.PSAR(this.Symbol, 0.02, 0.02, 0.2);

            for (var i = 1; i <= 250; i++)
            {
                this.emas[i] = Indicators.EMA(symbol, SourceType.Close, i);
                //this.demas[i] = Indicators.DEMA(symbol, SourceType.Close, i);
                //this.temas[i] = Indicators.TEMA(symbol, SourceType.Close, i);
                this.smas[i] = Indicators.SMA(symbol, SourceType.Close, i);

                //this.emas[i] = Indicators.EMA(this.tp, i);
                //this.demas[i] = Indicators.DEMA(this.tp, i);
                //this.temas[i] = Indicators.TEMA(this.tp, i);
                //this.smas[i] = Indicators.SMA(this.tp, i);
            }
        }

        public override string Name => "Simple Evaluator";

        public override string EntryDescription => "EMA[p1] breaks through EMA[p2] from below";

        public override string ExitDescription => "EMA[p3] breaks through EMA[p4] from above or trailing stop loss p[0] is triggered";

        public override double[] StartingParamters => new[] { 10, 50.0, 200, 20, /*0.01, 0.03*/ };

        public override (double Lower, double Upper, double Step)[] ParamterRanges =>
            new[] { (5, 15, 1.0), (40.0, 60.0, 1.0), (180.0, 220.0, 2.0), (15.0, 25.0, 1), };

        public override IList<(string Name, double[] Values, bool IsLine, bool IsDot)> CurveData =>
            new[]
                {
                    ($"SMA {Convert.ToInt32(this.Parameters[1])}", this.smas[Convert.ToInt32(this.Parameters[1])], true, false),
                    ($"SMA {Convert.ToInt32(this.Parameters[2])}", this.emas[Convert.ToInt32(this.Parameters[2])], true, false),
                    ($"SMA {Convert.ToInt32(this.Parameters[3])}", this.emas[Convert.ToInt32(this.Parameters[3])], true, false),
                    //($"SMA {Convert.ToInt32(this.Parameters[4])}",
                    //    this.smas[Convert.ToInt32(this.Parameters[4])], true, false),
                    //($"PSAR 0.02, 0.02, 0.2",
                    //    this.psar, false, true),
                    //($"SMA 20",
                    //    this.smas[20], true, false),
                    //($"SMA 50",
                    //    this.smas[50], true, false),
                    //($"SMA 200",
                    //    this.smas[200], true, false),
                    //($"TP",this.tp, true, false),
                };

        protected override bool IsEntry(int index)
        {
            if ( /*this.psar[index] < this.Symbol.Close[index] &&*/
                /*(this.FastEntry[index + 2] < this.SlowEntry[index + 2]) && */
                /*this.FastEntry[index + 1] < this.SlowEntry[index + 1] && (this.FastEntry[index] > this.SlowEntry[index])*/
                //this.signal[index + 1] > this.macd[index + 1] && (this.signal[index] < this.macd[index]) &&
                (this.macd[index] < this.Symbol.Close[index] * 0.01) && (this.signal[index] < this.macd[index])
                                                                     && (this.FastExit[index] > this.SlowExit[index]))
            {
                return true;
            }

            return false;

            // Check 1 - slow ema must rise
            var bSlow = Helper.Slope(this.SlowEntry, index, 5);
            if (bSlow < this.Parameters[5])
            {
                return false;
            }

            // Check 2 - 
            var bFast = Helper.Slope(this.FastEntry, index, 5);
            if (bFast < bSlow + this.Parameters[6])
            {
                return false;
            }

            var delta = Helper.Delta(this.SlowEntry[index], this.FastEntry[index]);
            if (delta < 0)
            {
                return false;
            }

            //delta = Helper.Delta(this.Symbol.Close[index], temaFast[index]);
            //if (!delta.IsWithin(this.Parameters[5], this.Parameters[6]))
            //{
            //    return false;
            //}

            //if (this.signal[index] - this.macd[index] > 0.0)
            //{
            //    return false;
            //}

            return true;
        }

        protected override void ExitTrade(Trade trade, int index)
        {
            // trade at next day open
            trade.SellIndex = index - 1;
            trade.SellValue = this.Symbol.Open[index - 1];
            trade.SellDate = this.Symbol.Day[index - 1];
        }

        protected override Trade InitiateTrade(int index)
        {
            // trade at next day open
            return new Trade { BuyIndex = index - 1, BuyValue = this.Symbol.Open[index - 1], BuyDate = this.Symbol.Day[index - 1] };
        }

        protected override bool IsExit(int index)
        {
            if ( /*(this.FastExit[index + 2] > this.SlowExit[index + 2]) && */
                /*(this.FastExit[index + 1] > this.SlowExit[index + 1]) && (this.FastExit[index] < this.SlowExit[index])*/
                (this.signal[index] > this.macd[index]) && (this.FastExit[index] < this.SlowExit[index]))
            {
                return true;
            }

            //if (this.Parameters[3] + Helper.Delta(this.tp[index + 1], this.tp[index]) < 0)
            //{
            //    return true;
            //}

            //if (this.psar[index] > this.Symbol.Close[index])
            //{
            //    return true;
            //}
            return false;
            //double meanDelta10 = 0.0;
            //for (int i = 1; i <= 10; i++)
            //{
            //    meanDelta10 += Helper.Delta(dema[index + i], ema[index + i]) / 10;
            //}
            //var delta = Helper.Delta(dema[index], ema[index]);
            //if (meanDelta10 > 0 && delta < (1 - this.Parameters[2]) * meanDelta10)
            //{
            //    return true;
            //}

            //if (this.Symbol.Close[index] < longStop[index])
            //if (((temaFastExit[index + 1] > emaSlowExit[index + 1]) && (temaFastExit[index] < emaSlowExit[index])))
            //{
            //    return true;
            //}

            if ((this.Symbol.Close[index] < this.longStop[index]) && (this.Symbol.Close[index + 1] < this.longStop[index + 1]))
            {
                return true;
            }

            return false;
        }

        protected override void PrepareForParameters()
        {
            //(this.macd, this.signal) = Indicators.MACD(this.Symbol, SourceType.Close, 20, 50, 9);
            //(this.shortStop, this.longStop) = Indicators.ELSZ(this.Symbol, 20, 2.5);

            (this.macd, this.signal) = Indicators.MACD(
                this.Symbol,
                SourceType.Close,
                Convert.ToInt32(this.Parameters[1]),
                Convert.ToInt32(this.Parameters[2]),
                Convert.ToInt32(this.Parameters[3]),
                MovingAverage.SMA);

            this.FastExit = this.smas[Convert.ToInt32(this.Parameters[3])];
            this.SlowExit = this.smas[Convert.ToInt32(this.Parameters[1])];

            //this.FastExit = this.smas[Convert.ToInt32(this.Parameters[2])];
            //this.SlowExit = this.smas[Convert.ToInt32(this.Parameters[3])];
        }
    }
}