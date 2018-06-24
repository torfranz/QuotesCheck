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

        public SimpleEvaluator(SymbolInformation symbol)
            : base(symbol)
        {
            for (var i = 1; i <= 200; i++)
            {
                this.emas[i] = Indicators.EMA(symbol, SourceType.Close, i);
                this.demas[i] = Indicators.DEMA(symbol, SourceType.Close, i);
                this.temas[i] = Indicators.TEMA(symbol, SourceType.Close, i);
                this.smas[i] = Indicators.SMA(symbol, SourceType.Close, i);
            }
        }

        public override string Name => "Simple Evaluator";

        public override string EntryDescription => "EMA[p1] breaks through EMA[p2] from below";

        public override string ExitDescription => "EMA[p3] breaks through EMA[p4] from above or trailing stop loss p[0] is triggered";

        public override double[] StartingParamters => new[] { -6, 10.0, 40, 20, 80, /*0.01, 0.03*/ };

        public override (double Lower, double Upper, double Step)[] ParamterRanges =>
            new[]
                {
                    (-20.0, -5.0, 1.0), // stop-loss
                    (1.0, 20.0, 1.0), // fast EMA
                    (21.0, 60.0, 1.0), // slow EMA
                    (1.0, 50.0, 1.0), // exit fast EMA
                    (51.0, 200.0, 1.0), // exit slow EMA
                    //(0.001, 0.04, 0.2), // diff
                    //(0.001, 0.04, 0.2), // diff
                };

        protected override bool IsEntry(int index)
        {
            if ((this.FastEntry[index + 1] < this.SlowEntry[index + 1]) && (this.FastEntry[index] > this.SlowEntry[index]))
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
            if ((this.FastExit[index + 1] > this.SlowExit[index + 1]) && (this.FastExit[index] < this.SlowExit[index]))
            {
                return true;
            }

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

            this.FastEntry = this.smas[Convert.ToInt32(this.Parameters[1])];
            this.SlowEntry = this.smas[Convert.ToInt32(this.Parameters[2])];

            this.FastExit = this.smas[Convert.ToInt32(this.Parameters[3])];
            this.SlowExit = this.smas[Convert.ToInt32(this.Parameters[4])];
        }
    }
}