﻿namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal class SimpleEvaluator : Evaluator
    {
        private Dictionary<string, Dictionary<int, double[]>> emas = new Dictionary<string, Dictionary<int, double[]>>();

        public override string Name => "Simple Evaluator";
        public override string EntryDescription => "Simple Evaluator";
        public override string ExitDescription => "Simple Evaluator";

        public override double[] StartingParamters => new[] { 20, 50, 15, 2.5 };

        public override (double Lower, double Upper, double Step)[] ParamterRanges => new[] { (5.0, 35.0, 10.0), (35.0, 65.0, 10.0), (5.0, 50, 5), (0.5, 5, 1.0) };

        public override void GenerateFixtures(SymbolInformation symbol)
        {
            var emas = new Dictionary<int, double[]>();
            for (var i = 5; i <= 65; i++)
            {
                emas[i] = Indicators.EMA(symbol, SourceType.Close, i);
            }

            this.emas[symbol.ISIN] = emas;
        }

        protected override bool IsEntry(SymbolInformation symbol, int index, double[] parameters, Trade trade)
        {
            Debug.Assert(parameters.Length == 4);
            var emaFast = this.emas[symbol.ISIN][Convert.ToInt32(parameters[0])];
            var emaSlow = this.emas[symbol.ISIN][Convert.ToInt32(parameters[1])];

            if ((emaFast[index + 1] < emaSlow[index + 1]) && (emaFast[index] > emaSlow[index]))
            {
                // trade at next day open
                trade.BuyValue = symbol.Open[index - 1];
                trade.BuyDate = symbol.Day[index - 1];
                return true;
            }

            return false;
        }

        protected override bool IsExit(SymbolInformation symbol, int index, double[] parameters, Trade trade)
        {
            Debug.Assert(parameters.Length == 4);
            //var emaFast = this.emas[symbol.ISIN][Convert.ToInt32(parameters[2])];
            //var emaSlow = this.emas[symbol.ISIN][Convert.ToInt32(parameters[3])];
            
            if (symbol.Close[index] < longStop[index + 1])
                //((emaFast[index + 1] > emaSlow[index + 1]) && (emaFast[index] < emaSlow[index])))
            {
                // trade at next day open
                trade.SellValue = symbol.Open[index - 1];
                trade.SellDate = symbol.Day[index - 1];
                return true;
            }

            return false;
        }

        double[] shortStop;
        double[] longStop;
        protected override void PrepareNewParameters(SymbolInformation symbol, double[] parameters)
        {
            (shortStop, longStop) = Indicators.ELSZ(symbol, Convert.ToInt32(parameters[2]), parameters[3]);
        }
    }
}