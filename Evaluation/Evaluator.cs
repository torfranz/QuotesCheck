namespace QuotesCheck.Evaluation
{
    using System.Diagnostics;

    internal abstract class Evaluator
    {
        public abstract string Name { get; }

        public abstract double[] StartingParamters { get; }

        public abstract (double Lower, double Upper, double Step)[] ParamterRanges { get; }

        protected SymbolInformation Symbol { get; private set; }

        public abstract void GenerateFixtures(SymbolInformation symbol);

        internal EvaluationResult Evaluate(SymbolInformation symbol, double[] parameters)
        {
            this.Symbol = symbol;
            Debug.Assert(this.Symbol.TimeSeries.Count > 100);

            var trade = new Trade();

            var lookingForEntry = true;
            var result = new EvaluationResult(symbol, this, parameters);

            for (var i = this.Symbol.TimeSeries.Count - 100; i >= 1; i--)
            {
                if (lookingForEntry)
                {
                    lookingForEntry = !this.IsEntry(i, parameters, trade);
                }
                else
                {
                    lookingForEntry = this.IsExit(i, parameters, trade);
                    if (lookingForEntry)
                    {
                        result.Trades.Add(trade);
                        trade = new Trade();
                    }
                }
            }

            // Trade still open? -> exit with last close
            if (!lookingForEntry)
            {
                // last data point is always considered an exit point
                trade.SellValue = this.Symbol.TimeSeries[0].Close;
                trade.SellDate = this.Symbol.TimeSeries[0].Day;

                result.Trades.Add(trade);
            }

            return result;
        }

        protected abstract bool IsEntry(int index, double[] parameters, Trade trade);

        protected abstract bool IsExit(int index, double[] parameters, Trade trade);
    }
}