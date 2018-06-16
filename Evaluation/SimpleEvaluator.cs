namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal class SimpleEvaluator : Evaluator
    {
        private Dictionary<int, double[]> emas = new Dictionary<int, double[]>();

        public SimpleEvaluator(SymbolInformation symbol) : base(symbol)
        {
            for (var i = 5; i <= 65; i++)
            {
                emas[i] = Indicators.EMA(symbol, SourceType.Close, i);
            }
        }

        public override string Name => "Simple Evaluator";
        public override string EntryDescription => "EMA[p1] breaks through EMA[p2] from below";
        public override string ExitDescription => "Close is below ELSZ[p3, p4]";

        public override double[] StartingParamters => new[] { 20, 50, 15, 2.5 };

        public override (double Lower, double Upper, double Step)[] ParamterRanges => new[] { (5.0, 35.0, 10.0), (35.0, 65.0, 10.0), (5.0, 50, 5), (0.5, 5, 1.0) };

        protected override bool IsEntry(int index)
        {
            var emaFast = this.emas[Convert.ToInt32(this.Parameters[0])];
            var emaSlow = this.emas[Convert.ToInt32(this.Parameters[1])];

            if ((emaFast[index + 1] < emaSlow[index + 1]) && (emaFast[index] > emaSlow[index]))
            {
                return true;
            }

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
            
            //var emaFast = this.emas[symbol.ISIN][Convert.ToInt32(parameters[2])];
            //var emaSlow = this.emas[symbol.ISIN][Convert.ToInt32(parameters[3])];
            
            if (this.Symbol.Close[index] < longStop[index + 1])
                //((emaFast[index + 1] > emaSlow[index + 1]) && (emaFast[index] < emaSlow[index])))
            {
                // trade at next day open
                
                return true;
            }

            return false;
        }

        double[] shortStop;
        double[] longStop;
        protected override void PrepareForParameters()
        {
            (shortStop, longStop) = Indicators.ELSZ(this.Symbol, Convert.ToInt32(this.Parameters[2]), this.Parameters[3]);
        }
    }
}