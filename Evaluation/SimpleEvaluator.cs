namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;

    internal class SimpleEvaluator : Evaluator
    {
        private readonly Dictionary<int, double[]> emas = new Dictionary<int, double[]>();

        private readonly Dictionary<int, double[]> temas = new Dictionary<int, double[]>();

        private double[] temaFast;

        //double[] emaFast;
        private double[] emaSlow;

        private double[] temaFastExit;

        private double[] emaSlowExit;

        private double[] shortStop;

        private double[] longStop;

        private double[] signal;

        private double[] macd;

        public SimpleEvaluator(SymbolInformation symbol)
            : base(symbol)
        {
            for (var i = 5; i <= 65; i++)
            {
                this.emas[i] = Indicators.EMA(symbol, SourceType.Close, i);
                this.temas[i] = Indicators.TEMA(symbol, SourceType.Close, i);
            }

            this.emas[200] = Indicators.EMA(symbol, SourceType.Close, 200);
        }

        public override string Name => "Simple Evaluator";

        public override string EntryDescription => "EMA[p1] breaks through EMA[p2] from below";

        public override string ExitDescription => "Close is below ELSZ[p3, p4]";

        public override double[] StartingParamters => new[] { -5, 20.0, 50, 20, 50, 0.01, 0.03 };

        public override (double Lower, double Upper, double Step)[] ParamterRanges =>
            new[]
                {
                    (-10.0, -1.0, 5.0), // stop-loss
                    (5.0, 35.0, 15.0), // fast EMA
                    (35.0, 60.0, 15.0), // slow EMA
                    (5.0, 15.0, 10.0), // exit fast EMA
                    (15.0, 30.0, 10.0), // exit slow EMA
                    (15.0, 30.0, 10.0), // diff
                };

        protected override bool IsEntry(int index)
        {
            // Check 1 - slow ema must rise
            var bSlow = Helper.Slope(this.emaSlow, index, 5);
            if (bSlow < this.Parameters[5])
            {
                return false;
            }

            // Check 2 - 
            var bFast = Helper.Slope(this.temaFast, index, 5);
            if (bFast < bSlow + this.Parameters[6])
            {
                return false;
            }

            var delta = Helper.Delta(this.emaSlow[index], this.temaFast[index]);
            if (delta < 0)
            {
                return false;
            }

            //delta = Helper.Delta(this.Symbol.Close[index], temaFast[index]);
            //if (!delta.IsWithin(this.Parameters[5], this.Parameters[6]))
            //{
            //    return false;
            //}

            if (this.signal[index] - this.macd[index] > 0.0)
            {
                //return false;
            }

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
            (this.macd, this.signal) = Indicators.MACD(this.Symbol, SourceType.Close, 20, 50, 9);
            (this.shortStop, this.longStop) = Indicators.ELSZ(this.Symbol, 20, 2.5);

            this.temaFast = this.temas[Convert.ToInt32(this.Parameters[1])];
            //emaFast = this.emas[Convert.ToInt32(this.Parameters[1])];
            this.emaSlow = this.emas[Convert.ToInt32(this.Parameters[2])];

            this.temaFastExit = this.temas[Convert.ToInt32(this.Parameters[3])];
            this.emaSlowExit = this.emas[Convert.ToInt32(this.Parameters[4])];
        }
    }
}