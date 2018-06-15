namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal class SimpleEvaluator : Evaluator
    {
        private Dictionary<int, double[]> emas;

        public override string Name => "Simple Evaluator";

        public override double[] StartingParamters => new[] { 20.0, 50.0, 20.0, 50.0 };

        public override (double Lower, double Upper, double Step)[] ParamterRanges => new[] { (5.0, 35.0, 5.0), (35.0, 65.0, 5.0), (5.0, 35.0, 5.0), (35.0, 65.0, 5.0) };

        public override void GenerateFixtures(SymbolInformation symbol)
        {
            this.emas = new Dictionary<int, double[]>();
            for (var i = 5; i <= 65; i++)
            {
                this.emas[i] = Indicators.EMA(symbol, SourceType.Close, i);
            }
        }

        protected override bool IsEntry(int index, double[] parameters, Trade trade)
        {
            Debug.Assert(parameters.Length == 4);
            var emaFast = this.emas[Convert.ToInt32(parameters[0])];
            var emaSlow = this.emas[Convert.ToInt32(parameters[1])];

            if ((emaFast[index + 1] < emaSlow[index + 1]) && (emaFast[index] > emaSlow[index]))
            {
                // trade at next day open
                trade.BuyValue = this.Symbol.Open[index - 1];
                trade.BuyDate = this.Symbol.Day[index - 1];
                return true;
            }

            return false;
        }

        protected override bool IsExit(int index, double[] parameters, Trade trade)
        {
            Debug.Assert(parameters.Length == 4);
            var emaFast = this.emas[Convert.ToInt32(parameters[2])];
            var emaSlow = this.emas[Convert.ToInt32(parameters[3])];

            if ((emaFast[index + 1] > emaSlow[index + 1]) && (emaFast[index] < emaSlow[index]))
            {
                // trade at next day open
                trade.SellValue = this.Symbol.Open[index - 1];
                trade.SellDate = this.Symbol.Day[index - 1];
                return true;
            }

            return false;
        }
    }
}