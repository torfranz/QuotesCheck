namespace QuotesCheck.Evaluation
{
    using System.Diagnostics;

    internal abstract class Evaluator
    {
        public abstract string Name { get; }
        public abstract string EntryDescription { get; }
        public abstract string ExitDescription { get; }

        public abstract double[] StartingParamters { get; }

        public abstract (double Lower, double Upper, double Step)[] ParamterRanges { get; }

        public abstract void GenerateFixtures(SymbolInformation symbol);

        internal EvaluationResult Evaluate(SymbolInformation symbol, double[] parameters)
        {
            Debug.Assert(symbol.TimeSeries.Count > 100);

            var trade = new Trade();

            var lookingForEntry = true;
            var result = new EvaluationResult(symbol, this, parameters);
            this.PrepareNewParameters(symbol, parameters);

            for (var i = symbol.TimeSeries.Count - 100; i >= 1; i--)
            {
                if (lookingForEntry)
                {
                    lookingForEntry = !this.IsEntry(symbol, i, parameters, trade);
                }
                else
                {
                    lookingForEntry = this.IsExit(symbol, i, parameters, trade);
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
                trade.SellValue = symbol.TimeSeries[0].Close;
                trade.SellDate = symbol.TimeSeries[0].Day;

                result.Trades.Add(trade);
            }

            return result;
        }

        protected abstract void PrepareNewParameters(SymbolInformation symbol, double[] parameters);

        protected abstract bool IsEntry(SymbolInformation symbol, int index, double[] parameters, Trade trade);

        protected abstract bool IsExit(SymbolInformation symbol, int index, double[] parameters, Trade trade);
    }
}