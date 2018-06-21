namespace QuotesCheck.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    internal abstract class Evaluator
    {
        public Evaluator(SymbolInformation symbol)
        {
            Debug.Assert(symbol.TimeSeries.Count > 100);
            this.Symbol = symbol;
        }

        public SymbolInformation Symbol { get; }

        public abstract string Name { get; }

        public abstract string EntryDescription { get; }

        public abstract string ExitDescription { get; }

        public abstract double[] StartingParamters { get; }

        public abstract (double Lower, double Upper, double Step)[] ParamterRanges { get; }

        protected double[] Parameters { get; private set; }

        internal EvaluationResult Evaluate(double[] parameters, double costOfTrades)
        {
            var startIndex = 1;
            var endIndex = this.Symbol.TimeSeries.Count - 100;

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

            for (var index = startIndex; index < endIndex; index++)
            {
                entries[index] = this.IsEntry(index);
                exits[index] = this.IsExit(index);
            }

            var result = new EvaluationResult(this, parameters);

            // read ideal trades if available
            var idealTradesPath = Path.Combine("ReferenceData", $"{this.Symbol.ISIN}-IdealTrades.json");
            if (File.Exists(idealTradesPath))
            {
                foreach (var idealTrade in Json.Load<IdealTrades>(idealTradesPath).Trades)
                {
                    var trade = new Trade { CostOfTrades = costOfTrades, };
                    for (var i = 0; i < this.Symbol.TimeSeries.Count; i++)
                    {
                        var series = this.Symbol.TimeSeries[i];
                        if (idealTrade.BuyDate == series.Day)
                        {
                            trade.BuyIndex = i;
                            trade.BuyValue = series.Open;
                            trade.BuyDate = series.Day;
                        }

                        if (idealTrade.SellDate == series.Day)
                        {
                            trade.SellIndex = i;
                            trade.SellValue = series.Open;
                            trade.SellDate = series.Day;
                        }
                    }

                    this.SetHighestValueForTrade(trade);
                    result.IdealTrades.Add(trade);
                }
            }

            // debug
            var entryPoints = new List<TimeSeries>();
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i])
                {
                    entryPoints.Add(this.Symbol.TimeSeries[i]);
                }
            }

            // find trades
            Trade activeTrade = null;
            double highestClose = 0;
            for (var index = endIndex; index >= startIndex; index--)
            {
                if (activeTrade == null)
                {
                    if (entries[index])
                    {
                        activeTrade = this.InitiateTrade(index);
                        activeTrade.CostOfTrades = costOfTrades;
                        result.Trades.Add(activeTrade);

                        // 
                        highestClose = this.Symbol.Close[activeTrade.BuyIndex];
                    }
                }
                else
                {
                    // finish if exit criteria met or stop loss value is triggered
                    if (exits[index] || 
                        (Helper.Delta(this.Symbol.TimeSeries[index].Close, highestClose) < parameters[0]))
                    {
                        // finish trade
                        this.ExitTrade(activeTrade, index);
                        this.SetHighestValueForTrade(activeTrade);

                        activeTrade = null;
                        highestClose = 0;
                    }
                    else
                    {
                        // track highest close since buy to apply trailing stop-loss
                        highestClose = Math.Max(highestClose, this.Symbol.Close[index]);
                    }
                }
            }

            // Trade still open? -> exit with last close
            if (activeTrade != null)
            {
                // last data point is always considered an exit point
                activeTrade.SellIndex = 0;
                activeTrade.SellValue = this.Symbol.TimeSeries[0].Close;
                activeTrade.SellDate = this.Symbol.TimeSeries[0].Day;
                this.SetHighestValueForTrade(activeTrade);
            }

            return result;
        }

        protected abstract Trade InitiateTrade(int index);

        protected abstract void ExitTrade(Trade trade, int index);

        protected abstract void PrepareForParameters();

        protected abstract bool IsEntry(int index);

        protected abstract bool IsExit(int index);

        private void SetHighestValueForTrade(Trade trade)
        {
            double max = 0;
            for (var i = trade.SellIndex; i < trade.BuyIndex; i++)
            {
                max = Math.Max(max, this.Symbol.Open[i]);
            }

            trade.HighestValue = max;
        }
    }
}