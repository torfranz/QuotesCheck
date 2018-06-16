namespace QuotesCheck.Evaluation
{
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal class ScriptingEvaluator : Evaluator
    {
        private Dictionary<string, Dictionary<int, double[]>> emas = new Dictionary<string, Dictionary<int, double[]>>();

        public override string Name => "Scripting Evaluator";
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

        public class ScriptHost
        {
            public ScriptingEvaluator Evaluator { get; set; }
            public SymbolInformation Symbol { get; set; }
            public int Index { get; set; }
        }

        protected override bool IsExit(SymbolInformation symbol, int index, double[] parameters, Trade trade)
        {
            Debug.Assert(parameters.Length == 4);
            //var emaFast = this.emas[symbol.ISIN][Convert.ToInt32(parameters[2])];
            //var emaSlow = this.emas[symbol.ISIN][Convert.ToInt32(parameters[3])];

            var script = @"bool IsExit(ScriptingEvaluator evaluator, SymbolInformation symbol, int index) {
return symbol.Close[index] < evaluator.longStop[index + 1];
}
IsExit(Evaluator, Symbol, Index)";

            //note: we block here, because we are in Main method, normally we could await as scripting APIs are async
            var result = CSharpScript.EvaluateAsync<bool>(script, null, new ScriptHost { Symbol = symbol, Evaluator = this, Index = index }).Result;

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