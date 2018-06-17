namespace QuotesCheck.Evaluation
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    internal abstract class Evaluator
    {
        public abstract string Name { get; }
        public abstract string EntryDescription { get; }
        public abstract string ExitDescription { get; }

        public abstract double[] StartingParamters { get; }

        public abstract (double Lower, double Upper, double Step)[] ParamterRanges { get; }
        public SymbolInformation Symbol { get; }

        public Evaluator(SymbolInformation symbol)
        {
            Debug.Assert(symbol.TimeSeries.Count > 100);
            this.Symbol = symbol;
        }

        protected abstract Trade InitiateTrade(int index);
        protected abstract void ExitTrade(Trade trade, int index);

        protected double[] Parameters { get; private set; }
        internal EvaluationResult Evaluate(double[] parameters, double costOfTrades)
        {
            int startIndex = 1;
            int endIndex = this.Symbol.TimeSeries.Count - 100;

            Debug.Assert(parameters.Length == this.StartingParamters.Length);
            this.Parameters = parameters;
            this.PrepareForParameters();

            var entries = new bool[this.Symbol.TimeSeries.Count];
            var exits = new bool[this.Symbol.TimeSeries.Count];

            //Parallel.For(startIndex, endIndex, index =>
            //{
            //    entries[index] = IsEntry(index);
            //    exits[index] = IsExit(index);
            //});

            for (int index = startIndex; index < endIndex; index++)
            {
                entries[index] = IsEntry(index);
                exits[index] = IsExit(index);
            }

            Trade activeTrade = null;
            var result = new EvaluationResult(this, parameters);

            for (var index = endIndex; index >= startIndex; index--)
            {
                if (activeTrade == null)
                {
                    if(entries[index])
                    {
                        activeTrade = this.InitiateTrade( index);
                        activeTrade.CostOfTrades = costOfTrades;
                        result.Trades.Add(activeTrade);
                    }
                }
                else
                {
                    if (exits[index])
                    {
                        // finish trade
                        this.ExitTrade(activeTrade, index);
                                               
                        activeTrade = null;
                    }
                    else if (Helper.Delta(this.Symbol.TimeSeries[index].Close, activeTrade.BuyValue) < parameters[0])
                    {
                        // finish trade
                        this.ExitTrade(activeTrade, index);

                        activeTrade = null;
                    }
                }
            }

            // Trade still open? -> exit with last close
            if (activeTrade != null)
            {
                // last data point is always considered an exit point
                activeTrade.SellValue = this.Symbol.TimeSeries[0].Close;
                activeTrade.SellDate = this.Symbol.TimeSeries[0].Day;                
            }

            result.IterationsResults.Add(result.CurrentIterationResult);
            return result;
        }

        protected abstract void PrepareForParameters();

        protected abstract bool IsEntry(int index);

        protected abstract bool IsExit(int index);
    }
}