namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal class SimpleEvaluator : Evaluator
    {
        private Dictionary<int, double[]> emas = new Dictionary<int, double[]>();
        private Dictionary<int, double[]> demas = new Dictionary<int, double[]>();

        public SimpleEvaluator(SymbolInformation symbol) : base(symbol)
        {
            for (var i = 5; i <= 65; i++)
            {
                emas[i] = Indicators.EMA(symbol, SourceType.Close, i);
                demas[i] = Indicators.DEMA(symbol, SourceType.Close, i);
            }

            emas[200] = Indicators.EMA(symbol, SourceType.Close, 200);
            
        }

        public override string Name => "Simple Evaluator";
        public override string EntryDescription => "EMA[p1] breaks through EMA[p2] from below";
        public override string ExitDescription => "Close is below ELSZ[p3, p4]";

        public override double[] StartingParamters => new[] { -5, 10.0, 50, 10, 20 };

        public override (double Lower, double Upper, double Step)[] ParamterRanges => new[] {
            (-10.0, -1.0, 5.0), // stop-loss
            (5.0, 15.0, 10.0), // fast EMA
            (35.0, 60.0, 15.0), // slow EMA
            (5.0, 15.0, 10.0), // exit fast EMA
            (15.0, 30.0, 10.0), // exit slow EMA
        };

        protected override bool IsEntry(int index)
        {
            if (((emaFast[index + 1] < emaSlow[index + 1]) && (emaFast[index] > emaSlow[index])))
            {
                return true;
            }
            // 
            //double meanDelta10 = 0.0;
            //for (int i = 1; i <= 10; i++)
            //{
            //    meanDelta10 += Helper.Delta(emaFast[index + i] , dema[index + i]) / 10;
            //}
            //var delta = Helper.Delta(dema[index], emaFast[index]);
            //if(meanDelta10 < 0 || delta > (1 - this.Parameters[2]) * meanDelta10)
            //{
            //    return false;
            //}

            //delta = Helper.Delta(emaFast[index], emas[200][index]);
            //if (!delta.IsWithin(this.Parameters[3], this.Parameters[4]))
            //{
            //    return false;
            //}

            //delta = Helper.Delta(this.Symbol.Close[index],dema[index]);
            //if (!delta.IsWithin(this.Parameters[5], this.Parameters[6]))
            //{
            //    return false;
            //}

            //if (signal[index] - macd[index] >= 0.01)
            //{
            //    return false;
            //}

            return false;
        }

        protected override void ExitTrade(Trade trade, int index)
        {
            // trade at next day open
            trade.SellValue = this.Symbol.Open[index - 1];
            trade.SellDate = this.Symbol.Day[index - 1];
        }

        protected override Trade InitiateTrade(int index)
        {
            // trade at next day open
            return new Trade
            {
                BuyValue = this.Symbol.Open[index - 1],
                BuyDate = this.Symbol.Day[index - 1]
            };
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
            if (((emaFastExit[index + 1] > emaSlowExit[index + 1]) && (emaFastExit[index] < emaSlowExit[index])))
            {
                return true;
            }

            return false;
        }

        double[] dema;
        double[] emaFast;
        double[] emaSlow;
        double[] emaFastExit;
        double[] emaSlowExit;
        double[] shortStop;
        double[] longStop;
        private double[] signal;
        private double[] macd;

        protected override void PrepareForParameters()
        {
            //(macd, signal) = Indicators.MACD(this.Symbol, SourceType.Close, Convert.ToInt32(this.Parameters[0]), 200, 9);
            //(shortStop, longStop) = Indicators.ELSZ(this.Symbol, Convert.ToInt32(this.Parameters[5]), this.Parameters[6]);

            //dema = this.demas[Convert.ToInt32(this.Parameters[0])];
            emaFast = this.emas[Convert.ToInt32(this.Parameters[1])];
            emaSlow = this.emas[Convert.ToInt32(this.Parameters[2])];
            emaFastExit = this.emas[Convert.ToInt32(this.Parameters[3])];
            emaSlowExit = this.emas[Convert.ToInt32(this.Parameters[4])];
        }
    }
}